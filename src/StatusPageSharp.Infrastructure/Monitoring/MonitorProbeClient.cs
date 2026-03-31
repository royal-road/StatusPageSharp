using System.Diagnostics;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using StatusPageSharp.Application.Models.Monitoring;
using StatusPageSharp.Domain.Enums;

namespace StatusPageSharp.Infrastructure.Monitoring;

public sealed class MonitorProbeClient(IHttpClientFactory httpClientFactory)
{
    public async Task<CheckProbeResult> ExecuteAsync(
        CheckProbeRequest request,
        CancellationToken cancellationToken
    )
    {
        return request.MonitorType switch
        {
            MonitorType.Tcp => await ExecuteTcpAsync(request, cancellationToken),
            MonitorType.Http or MonitorType.Https => await ExecuteHttpAsync(
                request,
                cancellationToken
            ),
            _ => new CheckProbeResult(
                false,
                0,
                null,
                CheckFailureKind.Exception,
                "Unsupported monitor type.",
                null
            ),
        };
    }

    private static async Task<CheckProbeResult> ExecuteTcpAsync(
        CheckProbeRequest request,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(request.Host) || request.Port is null)
        {
            return new CheckProbeResult(
                false,
                0,
                null,
                CheckFailureKind.Exception,
                "TCP monitors require host and port.",
                null
            );
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken
            );
            timeoutSource.CancelAfter(TimeSpan.FromSeconds(request.TimeoutSeconds));

            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(request.Host, request.Port.Value, timeoutSource.Token);
            stopwatch.Stop();
            return new CheckProbeResult(
                true,
                (int)stopwatch.ElapsedMilliseconds,
                null,
                CheckFailureKind.None,
                null,
                null
            );
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            return new CheckProbeResult(
                false,
                (int)stopwatch.ElapsedMilliseconds,
                null,
                CheckFailureKind.Timeout,
                "The TCP probe timed out.",
                null
            );
        }
        catch (SocketException exception)
        {
            stopwatch.Stop();
            return new CheckProbeResult(
                false,
                (int)stopwatch.ElapsedMilliseconds,
                null,
                CheckFailureKind.TcpConnectionFailure,
                exception.Message,
                null
            );
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            return new CheckProbeResult(
                false,
                (int)stopwatch.ElapsedMilliseconds,
                null,
                CheckFailureKind.Exception,
                exception.Message,
                null
            );
        }
    }

    private async Task<CheckProbeResult> ExecuteHttpAsync(
        CheckProbeRequest request,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(request.Url))
        {
            return new CheckProbeResult(
                false,
                0,
                null,
                CheckFailureKind.Exception,
                "HTTP monitors require a URL.",
                null
            );
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken
            );
            timeoutSource.CancelAfter(TimeSpan.FromSeconds(request.TimeoutSeconds));

            using var httpClient = CreateHttpClient(request);
            using var httpRequest = new HttpRequestMessage(
                new HttpMethod(request.HttpMethod),
                request.Url
            );

            if (!string.IsNullOrWhiteSpace(request.RequestBody))
            {
                httpRequest.Content = new StringContent(
                    request.RequestBody,
                    Encoding.UTF8,
                    "application/json"
                );
            }

            foreach (var header in ParseHeaders(request.RequestHeadersJson))
            {
                if (!httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value))
                {
                    httpRequest.Content ??= new StringContent(string.Empty);
                    httpRequest.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            using var response = await httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                timeoutSource.Token
            );
            var responseBody = await response.Content.ReadAsStringAsync(timeoutSource.Token);
            var responseSnippet = responseBody.Length > 500 ? responseBody[..500] : responseBody;

            if (
                !ExpectedStatusCodeEvaluator.Matches(
                    request.ExpectedStatusCodes,
                    (int)response.StatusCode
                )
            )
            {
                stopwatch.Stop();
                return new CheckProbeResult(
                    false,
                    (int)stopwatch.ElapsedMilliseconds,
                    (int)response.StatusCode,
                    CheckFailureKind.HttpStatusMismatch,
                    $"Received status code {(int)response.StatusCode}.",
                    responseSnippet
                );
            }

            if (
                !string.IsNullOrWhiteSpace(request.ExpectedResponseSubstring)
                && !responseBody.Contains(
                    request.ExpectedResponseSubstring,
                    StringComparison.Ordinal
                )
            )
            {
                stopwatch.Stop();
                return new CheckProbeResult(
                    false,
                    (int)stopwatch.ElapsedMilliseconds,
                    (int)response.StatusCode,
                    CheckFailureKind.ResponseBodyMismatch,
                    "The expected response text was not found.",
                    responseSnippet
                );
            }

            stopwatch.Stop();
            return new CheckProbeResult(
                true,
                (int)stopwatch.ElapsedMilliseconds,
                (int)response.StatusCode,
                CheckFailureKind.None,
                null,
                responseSnippet
            );
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            return new CheckProbeResult(
                false,
                (int)stopwatch.ElapsedMilliseconds,
                null,
                CheckFailureKind.Timeout,
                "The HTTP probe timed out.",
                null
            );
        }
        catch (HttpRequestException exception) when (IsTlsError(exception))
        {
            stopwatch.Stop();
            return new CheckProbeResult(
                false,
                (int)stopwatch.ElapsedMilliseconds,
                null,
                CheckFailureKind.TlsValidationFailure,
                exception.Message,
                null
            );
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            return new CheckProbeResult(
                false,
                (int)stopwatch.ElapsedMilliseconds,
                null,
                CheckFailureKind.Exception,
                exception.Message,
                null
            );
        }
    }

    private HttpClient CreateHttpClient(CheckProbeRequest request)
    {
        if (request.MonitorType != MonitorType.Https || request.VerifyTlsCertificate)
        {
            return httpClientFactory.CreateClient(nameof(MonitorProbeClient));
        }

        var handler = new SocketsHttpHandler
        {
            SslOptions = new SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = static (_, _, _, _) => true,
            },
        };

        return new HttpClient(handler, disposeHandler: true);
    }

    private static Dictionary<string, string> ParseHeaders(string? requestHeadersJson)
    {
        if (string.IsNullOrWhiteSpace(requestHeadersJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(requestHeadersJson) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static bool IsTlsError(HttpRequestException exception) =>
        exception.Message.Contains("certificate", StringComparison.OrdinalIgnoreCase)
        || exception.Message.Contains("ssl", StringComparison.OrdinalIgnoreCase)
        || exception.InnerException is AuthenticationException;
}

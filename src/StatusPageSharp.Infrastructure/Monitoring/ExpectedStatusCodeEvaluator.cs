namespace StatusPageSharp.Infrastructure.Monitoring;

public static class ExpectedStatusCodeEvaluator
{
    public static bool Matches(string expectedStatusCodes, int statusCode)
    {
        var tokens = expectedStatusCodes.Split(
            ',',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
        );

        if (tokens.Length == 0)
        {
            return statusCode is >= 200 and <= 299;
        }

        foreach (var token in tokens)
        {
            var rangeParts = token.Split(
                '-',
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
            );
            if (
                rangeParts.Length == 1
                && int.TryParse(rangeParts[0], out var exactStatusCode)
                && exactStatusCode == statusCode
            )
            {
                return true;
            }

            if (
                rangeParts.Length == 2
                && int.TryParse(rangeParts[0], out var startStatusCode)
                && int.TryParse(rangeParts[1], out var endStatusCode)
                && statusCode >= startStatusCode
                && statusCode <= endStatusCode
            )
            {
                return true;
            }
        }

        return false;
    }
}

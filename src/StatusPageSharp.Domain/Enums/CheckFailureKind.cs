namespace StatusPageSharp.Domain.Enums;

public enum CheckFailureKind
{
    None = 0,
    Timeout = 1,
    TcpConnectionFailure = 2,
    HttpStatusMismatch = 3,
    ResponseBodyMismatch = 4,
    TlsValidationFailure = 5,
    Exception = 6,
}

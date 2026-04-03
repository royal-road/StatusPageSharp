namespace StatusPageSharp.Infrastructure.Services;

public static class LocalDateTimeConverter
{
    public static DateTime ConvertLocalInputToUtc(DateTime localValue, int timeZoneOffsetMinutes)
    {
        var unspecifiedLocal = DateTime.SpecifyKind(localValue, DateTimeKind.Unspecified);
        var utcValue = unspecifiedLocal.AddMinutes(timeZoneOffsetMinutes);
        return DateTime.SpecifyKind(utcValue, DateTimeKind.Utc);
    }
}

namespace StatusPageSharp.Application.Models.Public;

public sealed record DailyStatusModel(DateOnly Day, bool IsOperational);

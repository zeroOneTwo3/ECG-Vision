namespace EcgVision.Core.Dtos;

public record EcgPlotUrlResponse(string? Url, DateTimeOffset? ExpiresAt, string? Error)
{
    public static EcgPlotUrlResponse Success(string url, DateTimeOffset expiresAt)
        => new(url, expiresAt, null);

    public static EcgPlotUrlResponse Failure(string message)
        => new(null, null, message);
}

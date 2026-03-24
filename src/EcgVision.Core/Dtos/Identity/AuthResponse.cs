namespace EcgVision.Core.Dtos.Identity;

public record AuthResponse(string Token, DateTime ExpiresAt);
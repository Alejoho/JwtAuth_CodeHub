using System;
using System.ComponentModel.DataAnnotations;

namespace WebApiRefreshTokenDemo.Options;

public class JwtSettingsOptions
{
    public const string SectionPath = "JwtSettings";

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Required]
    public string SecurityKey { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int AccessTokenExpirationMinutes { get; set; }

    [Range(1, int.MaxValue)]
    public int RefreshTokenExpirationDays { get; set; }
}

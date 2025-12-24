namespace WebApiRefreshTokenDemo.DTOs;

public record RegisterDto(string Username, string Email, string Password);
public record LoginDto(string Email, string Password);
public record TokenRequestDto(string AccessToken, string RefreshToken);
public record TokenResponseDto(string AccessToken, string RefreshToken);
public record AssignRoleDto(string Email, string RoleName);
public record CreateRoleDto(string RoleName);


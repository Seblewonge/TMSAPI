namespace TmsApi.Application.Auth.DTOs;

public record LoginRequest(
    string Username,
    string Password
);
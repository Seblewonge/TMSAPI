namespace TmsApi.Application.Auth.DTOs;

public record UserProfileDto(
    string DisplayName,
    string Role
);
namespace Bruktborsen.Api.DTOs.Auth;

public sealed record CurrentUserDto(
    string Id,
    string Email,
    string FullName,
    IList<string> Roles
);

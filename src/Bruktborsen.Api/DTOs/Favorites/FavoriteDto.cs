namespace Bruktborsen.Api.DTOs.Favorites;

public sealed record FavoriteDto(
    int ListingId,
    string Title,
    int Price,
    string? ImageUrl,
    string CategoryName
);

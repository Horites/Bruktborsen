namespace Bruktborsen.Api.Services;

public enum ListingResult
{
    Success,
    NotFound,
    InvalidData,
    CategoryNotFound,
    Forbidden,
    NotActive,
    Sold,
    Removed
}

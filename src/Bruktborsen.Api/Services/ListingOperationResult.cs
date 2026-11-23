
namespace Bruktborsen.Api.Services;

public sealed record ListingOperationResult<T>(
    ListingResult Result,
    T? Data = default,
    string? Message = null
);

namespace Bruktborsen.Api.Services;

public sealed record OfferOperationResult<T>(
    OfferResult Result,
    T? Data = default,
    string? Message = null
);

namespace Bruktborsen.Api.Services;

public enum CategoryResult
{
    Success,
    NotFound,
    InvalidName,
    DuplicateName,
    HasListings
}

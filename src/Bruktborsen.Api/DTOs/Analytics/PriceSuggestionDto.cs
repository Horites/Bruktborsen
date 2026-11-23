using Bruktborsen.Api.Models.Enums;

namespace Bruktborsen.Api.DTOs.Analytics;

public sealed record PriceSuggestionDto(
    int CategoryId,
    Condition Condition,
    int SoldCount,
    decimal? SuggestedPrice,
    string Message
);

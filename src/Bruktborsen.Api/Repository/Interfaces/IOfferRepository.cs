using Bruktborsen.Api.Models;

namespace Bruktborsen.Api.Repository.Interfaces;

public interface IOfferRepository
{
    Task<Offer?> GetByIdAsync(int id);

    Task<List<Offer>> GetByListingAsync(int listingId);

    Task<List<Offer>> GetByBuyerAsync(string buyerId);

    Task<bool> HasPendingOfferAsync(
        int listingId,
        string buyerId);

    Task<Offer> AddAsync(Offer offer);

    Task UpdateAsync();

    Task AcceptOfferAsync(Offer offer);
}

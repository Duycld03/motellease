using MotelLease.Application.Catalogue.Contracts;

namespace MotelLease.Application.SavedListings.Contracts;

public sealed record SaveListingRequest(Guid BoardingHouseId);

public sealed record SavedListingResponse(
    Guid Id,
    Guid BoardingHouseId,
    PublicBoardingHouseCardResponse BoardingHouse,
    DateTimeOffset SavedAt);

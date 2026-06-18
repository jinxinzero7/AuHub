using Auctions.Domain.Entities;

namespace Auctions.Application.Services;

public static class LotVisibilityPolicy
{
    public static bool CanViewDetails(Lot lot, Guid? requesterUserId, bool requesterIsAdmin)
    {
        if (requesterIsAdmin)
            return true;

        if (lot.IsDeleted)
            return false;

        if (requesterUserId.HasValue &&
            (requesterUserId.Value == lot.SellerId || requesterUserId.Value == lot.WinnerId))
            return true;

        return IsPubliclyVisible(lot);
    }

    public static bool IsPubliclyVisible(Lot lot)
    {
        return !lot.IsDeleted && lot.Status is
            LotStatus.Active or LotStatus.Completed or LotStatus.CompletedNoWinner;
    }
}

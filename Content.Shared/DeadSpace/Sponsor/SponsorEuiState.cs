using Content.Shared.Eui;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.Sponsor;

[Serializable, NetSerializable]
public sealed class SponsorEuiState : EuiStateBase
{
    public required List<SponsorCatalogItem> Catalog;
    public SponsorPlayerInfo? PlayerInfo;
}

[Serializable, NetSerializable]
public sealed class SponsorPlayerUpdateEuiMsg : EuiMessageBase
{
    public required SponsorPlayerInfo PlayerInfo;
}

[Serializable, NetSerializable]
public sealed class SponsorPlayerPrintRentEuiMsg : EuiMessageBase
{
    public required int RentId;
}
[Serializable, NetSerializable]
public sealed class SponsorPlayerBuyEuiMsg : EuiMessageBase
{
    public required int ItemId;
}
[Serializable, NetSerializable]
public sealed class SponsorPlayerBuyItemRentEuiMsg : EuiMessageBase
{
    public required int ItemId;
    public required int Days;
}

[Serializable, NetSerializable]
public sealed class SponsorCatalogItem
{
    public int ItemId { get; set; }
    public required string CategoryName { get; set; }
    public required string Name { get; set; }
    public string? GamePrototype { get; set; }
    public int? Price { get; set; }
    public Dictionary<int,int> PriceRent { get; set; } = new();
}

[Serializable, NetSerializable]
public sealed class SponsorPlayerInfo
{
    public int Id { get; set; }
    public int? DiscordId { get; set; }
    public Guid UserId { get; set; }

    public int Hours { get; set; }
    public int Crystal { get; set; }
    public int Coin { get; set; }

    public SponsorPlayerItem[] RentItems { get; set; } = [];
}

[Serializable, NetSerializable]
public sealed class SponsorPlayerItem
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public DateTime? ExpirationDate { get; set; }
}

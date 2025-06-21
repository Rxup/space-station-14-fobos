using Content.Shared.Eui;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.Sponsor;

[Serializable, NetSerializable]
public sealed class SponsorEuiState : EuiStateBase
{
    public required List<SponsorItem> Catalog;
    public required List<SponsorCalendar> Calendars;
    public SponsorPlayerInfo? PlayerInfo;
}

[Serializable, NetSerializable]
public sealed class SponsorPlayerUpdateEuiMsg : EuiMessageBase
{
    public required SponsorPlayerInfo PlayerInfo;
}

[Serializable, NetSerializable]
public sealed class SponsorCalendarUpdateEuiMsg : EuiMessageBase
{
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
    public required PriceType PriceType;
    public required int Days;
}
[Serializable, NetSerializable]
public sealed class SponsorTryGetCalendarItemEuiMsg : EuiMessageBase
{
    public required int CalendarId;
}

[Serializable, NetSerializable]
public sealed class OperationResultEuiMsg : EuiMessageBase
{
    public required string Result = string.Empty;
}

[Serializable, NetSerializable]
public enum PriceType
{
    Coin = 1,
    Crystal = 2,
}

#region Calendar

[Serializable, NetSerializable]
public sealed class SponsorCalendar
{
    public int Id { get; set; }
    public required string Name { get; set; }

    public List<SponsorCalendarItem> CalendarItems { get; set; } = [];
}

[Serializable, NetSerializable]
public sealed class SponsorCalendarItem
{
    public int Id { get; set; }

    public int Crystal { get; set; }

    public SponsorItem? Item { get; set; }

    public DateTime Date { get; set; }
}

[Serializable, NetSerializable]
public sealed class SponsorCalendarClaimItem
{
    public int Id { get; set; }
    public int CalendarItemId { get; set; }
    public int ItemId { get; set; }
    public DateTime ClaimedDate { get; set; }
}

#endregion

[Serializable, NetSerializable]
public sealed class SponsorItem
{
    public int Id { get; set; }
    public required string CategoryName { get; set; }
    public required string Name { get; set; }
    public string? GamePrototype { get; set; }
    public Dictionary<PriceType, List<PriceRow>>? Prices { get; set; }

    [Serializable, NetSerializable]
    public sealed class PriceRow
    {
        public int Days { get; set; }
        public int Price { get; set; }
    }

    public SponsorItemType ItemType { get; set; }
}

[Serializable, NetSerializable]
public enum SponsorItemType
{
    Entity = 0,
    TTS = 1,
    Customization = 2,
    Trait = 3,
}

[Serializable, NetSerializable]
public sealed class SponsorPlayerInfo
{
    public int Id { get; set; }
    public int? DiscordId { get; set; }
    public Guid UserId { get; set; }

    public int? LastPlayerLevelId { get; set; }
    public int Hours { get; set; }
    public int Crystal { get; set; }
    public int Coin { get; set; }

    public SponsorPlayerItem[] RentItems { get; set; } = [];
    public SponsorPlayerMoon[] Moons { get; set; } = [];
    public SponsorPlayerSubscription? Subscription { get; set; }
    public SponsorCalendarClaimItem[] ClaimedCalendarItems { get; set; } = [];
}

[Serializable, NetSerializable]
public sealed class SponsorPlayerItem
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public DateTime? ExpirationDate { get; set; }
}

[Serializable, NetSerializable]
public sealed class SponsorPlayerSubscription
{
    public DateTime? ExpirationDate { get; set; }
    public required SponsorSubscription Subscription { get; set; }

    [Serializable, NetSerializable]
    public sealed class SponsorSubscription
    {
        public required string Name { get; set; }
        public string? Description { get; set; }
        public string OocColor { get; set; } = "#FF0000";
        public bool HavePriorityJoin { get; set; }
        public bool AllowJob { get; set; }

        public string Markings { get; set; } = "[]";

        public List<SponsorItem.PriceRow>? Prices { get; set; }
    }
}

#region Moon

[Serializable, NetSerializable]
public sealed class SponsorPlayerMoon
{
    public int Id { get; set; }

    public required SponsorMoon Moon { get; set; }
}

[Serializable, NetSerializable]
public sealed class SponsorMoon
{
    public int Id { get; set; }

    public required string Name { get; set; }
    public string? GameImagePath { get; set; }
    public string? GameSoundPath { get; set; }

    public int? Price { get; set; }
    public PriceType? PriceType { get; set; }

    public List<SponsorMoonItem> MoonItems { get; set; } = [];
}

[Serializable, NetSerializable]
public sealed class SponsorMoonItem
{
    public int Id { get; set; }
    public required string Name { get; set; }

    public SponsorItem? Item { get; set; }

    public int CrystalMinAmount { get; set; }
    public int CrystalMaxAmount { get; set; }

    public double OnlyCrystalChance { get; set; }
    public double OnlyItemChance { get; set; }
    public double BothChance { get; set; }
}

#endregion

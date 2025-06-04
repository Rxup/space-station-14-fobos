using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.EUI;
using Content.Shared.DeadSpace.Sponsor;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Shared.Network;

namespace Content.Server.DeadSpace.Sponsor;

[UsedImplicitly]
public sealed class SponsorEui : BaseEui
{
    private readonly SponsorUiSystem _system;
    private readonly NetUserId _userId;

    public SponsorEui(SponsorUiSystem system, NetUserId userId)
    {
        _system = system;
        _userId = userId;
        State = new SponsorEuiState
        {
            Catalog = system.Catalog,
            Calendars = system.Calendars,
            PlayerInfo = null
        };
    }

    public readonly ReaderWriterLockSlim LoadLock = new();

    public async Task Load()
    {
        if (LoadLock.IsWriteLockHeld)
        {
            return;
        }

        try
        {
            LoadLock.EnterWriteLock();
            State = new SponsorEuiState
            {
                Catalog = _system.Catalog,
                Calendars = _system.Calendars,
                PlayerInfo = await _system.GetPlayerInfo(_userId),
            };

            if (State.PlayerInfo != null)
            {
                SendMessage(new SponsorPlayerUpdateEuiMsg
                {
                    PlayerInfo = State.PlayerInfo
                });
            }
        }
        finally
        {
            LoadLock.ExitWriteLock();
        }
    }

    public override async void HandleMessage(EuiMessageBase msg)
    {
        if (LoadLock.IsWriteLockHeld)
        {
            return;
        }

        try
        {
            LoadLock.EnterWriteLock();
            if (msg is SponsorPlayerPrintRentEuiMsg rentMsg)
            {
                _system.Log.Info($"Пользователь отправил запрос на выдачу {rentMsg.RentId}");
                var playerRent = State.PlayerInfo?.RentItems.FirstOrDefault(x => x.Id == rentMsg.RentId);
                if (playerRent == null || playerRent.ExpirationDate < DateTime.Now)
                {
                    return;
                }

                _system.PrintItem(_userId, playerRent);

            }

            if (msg is SponsorPlayerBuyEuiMsg buy)
            {
                _system.Log.Info($"Пользователь отправил запрос на разовую покупки {buy.ItemId}");
                await _system.BuyItem(_userId, buy.ItemId, buy.PriceType, buy.Days);
                return;
            }

            if (msg is SponsorTryGetCalendarItemEuiMsg calendar)
            {
                _system.Log.Info($"Пользователь пытается получить предмет календаря({calendar.CalendarId})");
                await _system.ClaimCalendarItem(_userId, calendar.CalendarId);
                return;
            }
        }
        finally
        {
            LoadLock.ExitWriteLock();
        }
    }

    public SponsorEuiState State { get; set; }

    public override EuiStateBase GetNewState()
    {
        return State;
    }
}

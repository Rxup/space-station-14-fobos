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
            if (msg is SponsorPlayerPrintRentEuiMsg rent)
            {
                _system.Log.Info($"Пользователь отправил запрос на выдачу {rent.RentId}");
                // Печать аренды
                return;
            }

            if (msg is SponsorPlayerBuyEuiMsg buy)
            {
                _system.Log.Info($"Пользователь отправил запрос на разовую покупки {buy.ItemId}");
                await _system.BuyItem(_userId, buy.ItemId, buy.PriceType, buy.Days);
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

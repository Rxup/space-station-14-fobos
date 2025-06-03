using Content.Client.DeadSpace.Sponsor.UI;
using Content.Client.Eui;
using Content.Shared.DeadSpace.Sponsor;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client.DeadSpace.Sponsor;

[UsedImplicitly]
public sealed class SponsorEui : BaseEui
{
    public interface ISponsorEui
    {
        void SetEui(SponsorEui currentEui);

        void UpdateCategory()
        {

        }

        void UpdatePlayerInfo()
        {

        }
    }

    private SponsorMenu _window;

    public SponsorEui()
    {
        _window = new SponsorMenu();
        _window.OnClose += () =>
        {
            SendMessage(new CloseEuiMessage());
        };
    }

    public override void Opened()
    {
        _window.SetEui(this);
        _window.OpenCentered();
    }

    public override void Closed()
    {
        _window.Close();
    }

    public IReadOnlyList<SponsorCatalogItem> Catalog { get; private set; } = [];
    public SponsorPlayerInfo PlayerInfo { get; private set; } = new();

    public override void HandleState(EuiStateBase state)
    {
        if(state is not SponsorEuiState sponsorEuiState)
            return;
        Catalog = sponsorEuiState.Catalog;
        _window.UpdateCategory();

        if (sponsorEuiState.PlayerInfo != null)
        {
            PlayerInfo = sponsorEuiState.PlayerInfo;
            _window.UpdatePlayerInfo();
        }
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        if (msg is SponsorPlayerUpdateEuiMsg playerUpdateEuiMsg)
        {
            PlayerInfo = playerUpdateEuiMsg.PlayerInfo;
            _window.UpdatePlayerInfo();
            return;
        }

        if (msg is OperationResultEuiMsg message)
        {
            if (!string.IsNullOrEmpty(message.Result))
            {
                var messageBox = new SponsorMessageBox(message.Result);
                messageBox.OpenCentered();
            }
            return;
        }
    }

    public void BuyButton(int itemId, PriceType priceType, int days)
    {
        SendMessage(new SponsorPlayerBuyEuiMsg()
        {
            ItemId = itemId,
            PriceType = priceType,
            Days = days,
        });
    }

    public void GetButton(int rentId)
    {
        SendMessage(new SponsorPlayerPrintRentEuiMsg
        {
            RentId = rentId,
        });
    }
}

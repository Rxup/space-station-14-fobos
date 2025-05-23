using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.DeadSpace.CCCCVars;
using Content.Shared.DeadSpace.Sponsor;
using Content.Shared.GameTicking;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Network;

namespace Content.Server.DeadSpace.Sponsor;

public sealed class SponsorUiSystem : SharedSponsorUiSystem
{
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private bool _enabled = false;
    private string _apiToken = "";
    private string _apiUrl = "";

    public List<SponsorCatalogItem> Catalog { get; private set; } = [];

    public override void Initialize()
    {
        base.Initialize();
        _console.RegisterCommand("open_sponsor_ui", OpenUiCommand);

        Subs.CVar(_cfg,
            CCCCVars.SponsorUiApi,
            (val) =>
            {
                _apiUrl = val;
                ApiChangeHandler();
            },
            true);
        Subs.CVar(_cfg,
            CCCCVars.SponsorUiApiToken,
            (val) =>
            {
                _apiToken = val;
                ApiChangeHandler();
            },
            true);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnCatalogForceUpdate);
    }

    private async void OnCatalogForceUpdate(RoundRestartCleanupEvent ev)
    {
        try
        {
            await ReLoadCatalog();
        }
        catch (Exception e)
        {
            Log.Warning("SponsorUiSystem cleanup failed: " + e.ToString());
        }
    }

    private HttpClient _httpClient = new();

    private async Task ReLoadCatalog()
    {
        ClearAllUi();
        Catalog = [];
        Catalog = await _httpClient.GetFromJsonAsync<List<SponsorCatalogItem>>("api/sponsorUi/catalog") ?? [];
    }

    private void ClearAllUi()
    {
        foreach (var v in _sponsorEui.ToList())
        {
            try
            {
                _euiManager.CloseEui(v.Value);
            }
            catch (Exception e)
            {
                // ignore
            }
        }

        _sponsorEui.Clear();
    }

    private async void ApiChangeHandler()
    {
        if (string.IsNullOrEmpty(_apiUrl) || string.IsNullOrEmpty(_apiToken))
        {
            _enabled = false;
            ClearAllUi();
            return;
        }

        _httpClient = new HttpClient()
        {
            BaseAddress = new Uri(_apiUrl),
            DefaultRequestHeaders =
            {
                { "Authorization", ["Bearer " + _apiToken] }
            }
        };

        await ReLoadCatalog();

        _enabled = true;
    }

    [AnyCommand]
    private async void OpenUiCommand(IConsoleShell shell, string argstr, string[] args)
    {
        if (!_enabled)
        {
            shell.WriteError($"system sponsor ui is offline.");
            return;
        }

        if (shell.Player == null)
        {
            shell.WriteError($"You need to be logged in to open sponsor ui.");
            return;
        }

        var player = shell.Player.UserId;

        if (_sponsorEui.ContainsKey(player))
        {
            if (_sponsorEui[player].LoadLock.IsWriteLockHeld)
                return;

            try
            {
                _euiManager.CloseEui(_sponsorEui[player]);
            }
            catch (Exception e)
            {
                // ignore
            }

            _sponsorEui.Remove(player);
        }

        var ui = new SponsorEui(this, shell.Player.UserId);
        _sponsorEui.Add(player, ui);
        _euiManager.OpenEui(ui, shell.Player);
        ui.StateDirty();
        await ui.Load();
    }

    private Dictionary<NetUserId, SponsorEui> _sponsorEui = new();

    public async Task<SponsorPlayerInfo?> GetPlayerInfo(NetUserId userId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<SponsorPlayerInfo>($"api/sponsorUi/{userId}");
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            return null;
        }
    }

    public async Task<bool> BuyItem(NetUserId userId, int buyItemId, PriceType priceType, int days)
    {
        try
        {
            var response =
                await _httpClient.PostAsJsonAsync($"api/sponsorUi/{userId}", new { buyItemId, priceType, days });
            return response.IsSuccessStatusCode;
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            return false;
        }
    }
}

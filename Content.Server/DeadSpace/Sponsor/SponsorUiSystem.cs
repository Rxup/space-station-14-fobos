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
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mobs.Systems;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.DeadSpace.Sponsor;

public sealed class SponsorUiSystem : SharedSponsorUiSystem
{
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

    private bool _enabled = false;
    private string _apiToken = "";
    private string _apiUrl = "";

    public List<SponsorItem> Catalog { get; private set; } = [];
    public List<SponsorCalendar> Calendars { get; private set; } = [];
    public HashSet<int> SpawnedRents = [];

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
            SpawnedRents.Clear();
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
        Catalog = await _httpClient.GetFromJsonAsync<List<SponsorItem>>("api/sponsorUi/catalog") ?? [];
        Calendars = [];
        Calendars = await _httpClient.GetFromJsonAsync<List<SponsorCalendar>>("api/sponsorUi/calendar") ?? [];
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

    public async Task<bool> ClaimCalendarItem(NetUserId userId, int calendarId)
    {
        try
        {
            var response = await _httpClient
                .PostAsync($"api/sponsorUi/calendar/{userId}?calendarId={calendarId}", null);
            await NotifyAfterRequest(userId, response);
            if (_sponsorEui.TryGetValue(userId, out var sponsorEui))
            {
                sponsorEui.SendMessage(new SponsorCalendarUpdateEuiMsg());
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            return false;
        }
    }


    public async Task<bool> BuyItem(NetUserId userId, int buyItemId, PriceType priceType, int days)
    {
        try
        {
            var response = await _httpClient
                .PostAsJsonAsync($"api/sponsorUi/{userId}", new { buyItemId, priceType, days });
            await NotifyAfterRequest(userId, response);

            return response.IsSuccessStatusCode;
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            return false;
        }
    }

    /// <summary>
    /// Делает popup на клиент с ошибкой или обновляет инофрмацию о пользователе при успехе
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="response">Ответ должен быть строкой в случае ошибки или SponsorPlayerInfo в случае успеха</param>
    private async Task NotifyAfterRequest(NetUserId userId, HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            SendMessage(userId, await response.Content.ReadAsStringAsync());
        }
        else
        {
            if (_sponsorEui.TryGetValue(userId, out var sponsorEui))
            {
                sponsorEui.State.PlayerInfo = await response.Content.ReadFromJsonAsync<SponsorPlayerInfo>();
                if (sponsorEui.State.PlayerInfo != null)
                {
                    sponsorEui.SendMessage(new SponsorPlayerUpdateEuiMsg
                    {
                        PlayerInfo = sponsorEui.State.PlayerInfo!,
                    });
                }
            }
        }
    }

    public void SendMessage(NetUserId userId, string msg)
    {
        if (!_sponsorEui.TryGetValue(userId, out var sponsorEui))
            return;

        sponsorEui.SendMessage(new OperationResultEuiMsg
        {
            Result = msg,
        });
    }

    public void PrintItem(NetUserId userId, SponsorPlayerItem playerRent)
    {
        if (SpawnedRents.Contains(playerRent.Id))
        {
            SendMessage(userId, "Уже использовано в этом раунде");
            return;
        }

        var session = _playerManager.GetSessionById(userId);
        if (session.AttachedEntity is { Valid: true } playerEnt && _mobStateSystem.IsAlive(playerEnt))
        {
            var catalogItem = Catalog.First(x => x.Id == playerRent.ItemId);
            var item = SpawnAtPosition(catalogItem.GamePrototype, Transform(playerEnt).Coordinates);
            SpawnedRents.Add(playerRent.Id);
            _handsSystem.TryPickupAnyHand(playerEnt, item);
            return;
        }

        SendMessage(userId, "Невозможно выдать предмет в текущем состоянии");
    }
}

using UnityEngine;
using UnityEngine.UIElements;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Net.Sockets;
using System.Threading.Tasks;
using Unity.Collections;
using System.Collections;
using System.Linq;
// NEW: UGS
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;


public class NetBootstrap : MonoBehaviour
{
	[Header("UI Roots")]
	[SerializeField] public GameObject MainMenuView;
	[SerializeField] public GameObject MatchmakingManager;
	[SerializeField] private GameObject MatchMakerView;
	[SerializeField] private GameObject MatchView;

	[Header("QuickPlay (UGS)")]          // NEW
	[SerializeField] private int maxPlayers = 8;
	[SerializeField] private string quickLobbyName = "DuckPond";
	[SerializeField] private bool makeQuickLobbiesPublic = true;

	public Button QuickPlayButton { get; private set; }
	public Button RunServerButton;
	public Button HostGameButton;
	public Button RunClientButton;
	public Button MenuQuitButton;
	public Button RunSinglePlayerButton;
	public Button MuteButton;
	Label menu_TitleLabel;
	VisualElement menu_Root;
	VisualElement match_Root;
	VisualElement view_Root;
	private FixedString64Bytes _pendingName = "";
	private TextField _nameField;
	private Button _enterBtn;

	// NEW: Lobby/Relay state
	private string _currentLobbyId;
	private string _currentRelayCode;
	private float _lobbyHeartbeatTimer;

	void OnEnable()
	{
		var menuUiDocument = MainMenuView.GetComponent<UIDocument>();
		match_Root = MatchMakerView.GetComponent<UIDocument>().rootVisualElement;
		menu_Root  = menuUiDocument.GetComponent<UIDocument>().rootVisualElement;
		view_Root  = MatchView.GetComponent<UIDocument>().rootVisualElement;

		ShowMainMenuUI();
		HideMatchmakingUI();
		HideMatchView();


		//QuickPlayButton = menu_Root.Q<Button>("findMatchButton");        
		//if (QuickPlayButton != null) QuickPlayButton.RegisterCallback<ClickEvent>(OnClickQuickPlay); // NEW

		RunSinglePlayerButton = menu_Root.Q<Button>("singlePlayerButton");
		RunSinglePlayerButton.RegisterCallback<ClickEvent>(OnClickStartSinglePlayer);
		
		MuteButton=menu_Root.Q<Button>("muteButton");
		MuteButton.RegisterCallback<ClickEvent>(OnToggleMusicClick);
		
		MenuQuitButton = menu_Root.Q<Button>("quitButton");
		MenuQuitButton.RegisterCallback<ClickEvent>(OnClickQuit);

		RunServerButton = menu_Root.Q<Button>("serverButton");
		RunServerButton.RegisterCallback<ClickEvent>(OnClickRunServer);

		HostGameButton = menu_Root.Q<Button>("hostButton");
		HostGameButton.RegisterCallback<ClickEvent>(OnClickHostGame);

		RunClientButton = menu_Root.Q<Button>("clientButton");
		RunClientButton.RegisterCallback<ClickEvent>(OnClickClientGame);

		_nameField = menu_Root.Q<TextField>("playerName");

		// Initialize UGS in the background for QuickPlay (no effect on Host/Client)  // NEW
		_ = InitializeUgsAsync(); // safe if already initialized
	}

	// ─────────────────────────────────────────────────────────────────────────────
	// QUICKPLAY: try to join any open lobby; if none, host via Relay+Lobby.        // NEW
	async void OnClickFindMatch(ClickEvent evt)
	{
		HideMainMenuUI();
		ShowMatchmakingUI();

		// Ensure UGS ready
		await InitializeUgsAsync();

		// If we're already host/client, bail (or you can add leave logic)
		if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
		{
			Debug.LogWarning("Already in a session; QuickPlay skipped.");
			return;
		}

		// 1) Try Quick Join (requires a public lobby with a 'relay' code & free slot)
		var lobby = await TryQuickJoinLobbyWithRelay();
		if (lobby != null)
		{
			_currentLobbyId = lobby.Id;
			string relayCode = lobby.Data["relay"].Value;
			bool ok = await JoinByRelayCode(relayCode);
			if (ok) { Debug.Log($"QuickPlay: joined {lobby.Name}"); return; }
			Debug.LogWarning("QuickPlay: failed to join relay from lobby; falling back to host.");
		}

		// 2) None found: become host via Relay + create Lobby (doesn't touch your Host button flow)
		bool hosted = await HostWithRelayAndLobby(quickLobbyName, maxPlayers, makeQuickLobbiesPublic);
		if (!hosted)
		{
			Debug.LogError("QuickPlay: failed to host using Relay+Lobby.");
			// show menu again if you want
			ShowMainMenuUI();
			HideMatchmakingUI();
		}
	}

	// ─────────────────────────────────────────────────────────────────────────────
	// Your existing local server/host/client flows (unchanged)

	void OnClickRunServer(ClickEvent evt)
	{
		HideMainMenuUI();
		NetworkManager.Singleton.StartServer();
	}
	
	public void OnToggleMusicClick(ClickEvent evt)
	{
		
		var bg = BackgroundMusicPlayer.Instance;
		if (bg != null) bg.ToggleMusic();
	}
	
	void OnClickHostGame(ClickEvent evt)
	{
		HideMainMenuUI();

		_pendingName = (_nameField.value ?? "").Trim();

		var nm = NetworkManager.Singleton;
		nm.OnClientConnectedCallback += OnLocalClientConnected;
		nm.StartHost();      
		SetName();

		ShowMatchmakingUI();
	}

	void OnClickClientGame(ClickEvent evt)
	{
		HideMainMenuUI();

		_pendingName = (_nameField.value ?? "").Trim();

		var nm = NetworkManager.Singleton;
		nm.OnClientConnectedCallback += OnLocalClientConnected;
		nm.StartClient();     // kept as your local client (no Relay)

		ShowMatchmakingUI();
	}

	// ─────────────────────────────────────────────────────────────────────────────
	// Name sync helpers (unchanged)

	public void SetName()
	{
		var po = NetworkManager.Singleton?.LocalClient?.PlayerObject;
		if (po == null) return;

		var player = po.GetComponent<PlayerMovement>();
		if (player == null) return;

		var s = _nameField.value?.Trim() ?? "";
		if (s.Length == 0) return;

		FixedString64Bytes f = s;
		player.RequestSetNameServerRpc(f);
	}

	private void OnLocalClientConnected(ulong clientId)
	{
		var nm = NetworkManager.Singleton;
		if (clientId != nm.LocalClientId) return;

		nm.OnClientConnectedCallback -= OnLocalClientConnected;
		StartCoroutine(SendNameWhenReady());
	}

	private IEnumerator SendNameWhenReady()
	{
		var nm = NetworkManager.Singleton;
		while (nm.LocalClient == null || nm.LocalClient.PlayerObject == null)
			yield return null;

		var po = nm.LocalClient.PlayerObject;
		var player = po ? po.GetComponent<PlayerMovement>() : null;
		if (player == null) yield break;

		var s = (_pendingName.ToString() ?? "").Trim();
		if (s.Length == 0) yield break;
		if (s.Length > 20) s = s.Substring(0, 20);

		FixedString64Bytes f = s;
		player.RequestSetNameServerRpc(f);
	}

	// ─────────────────────────────────────────────────────────────────────────────
	// UI helpers (unchanged)

	void ShowMatchmakingUI() { match_Root.style.display = DisplayStyle.Flex; }
	void HideMatchmakingUI()
	{
		match_Root = MatchMakerView.GetComponent<UIDocument>().rootVisualElement;
		match_Root.style.display = DisplayStyle.None;
	}
	private void ShowMatchView()  { view_Root.style.display = DisplayStyle.Flex; }
	private void HideMatchView()  { view_Root.style.display = DisplayStyle.None; }
	void ShowMainMenuUI()         { menu_Root.style.display = DisplayStyle.Flex; }
	void HideMainMenuUI()         { menu_Root.style.display = DisplayStyle.None; }

	void OnClickQuit(ClickEvent evt)
	{
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#else
		UnityEngine.Application.Quit();
#endif
	}

	void OnClickStartSinglePlayer(ClickEvent evt)
	{
		
		HideMainMenuUI();

		_pendingName = (_nameField.value ?? "").Trim();

		var nm = NetworkManager.Singleton;
		nm.OnClientConnectedCallback += OnLocalClientConnected;
		nm.StartHost();      
		SetName();

		ShowMatchmakingUI();
	
		
	}

	// ─────────────────────────────────────────────────────────────────────────────
	// NEW: UGS init + QuickPlay helpers

	private async Task InitializeUgsAsync()
	{
		if (UnityServices.State == ServicesInitializationState.Initialized) return;

		await UnityServices.InitializeAsync();
		if (!AuthenticationService.Instance.IsSignedIn)
			await AuthenticationService.Instance.SignInAnonymouslyAsync();

		Debug.Log($"UGS ready. PlayerId={AuthenticationService.Instance.PlayerId}");
	}

	// Try QuickJoin first; if it throws, fallback to query-and-pick.
	private async System.Threading.Tasks.Task<Lobby> TryQuickJoinLobbyWithRelay()
	{
		// 1) Try QuickJoin with no options (SDK will pick something); validate after.
		try
		{
			var lobby = await LobbyService.Instance.QuickJoinLobbyAsync();
			if (lobby != null &&
				lobby.Data != null &&
				lobby.Data.TryGetValue("relay", out var d) &&
				!string.IsNullOrWhiteSpace(d.Value) &&
				lobby.AvailableSlots > 0)
			{
				return lobby;
			}
		}
			catch
			{
				// ignore; fall through to manual query
			}

		// 2) Manual query → filter results in code (version-agnostic)
		try
		{
			var resp = await LobbyService.Instance.QueryLobbiesAsync(); // no options → all visible lobbies
			if (resp?.Results != null)
			{
				foreach (var l in resp.Results)
				{
					if (l?.Data != null &&
						l.AvailableSlots > 0 &&
						l.Data.TryGetValue("relay", out var d) &&
						!string.IsNullOrWhiteSpace(d.Value))
					{
						return l; // first good one
					}
				}
			}
		}
			catch
			{
				// no suitable lobby found
			}

		return null; // signal caller to host
	}

	private async Task<bool> JoinByRelayCode(string joinCode)
	{
		try
		{
			var joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);

			var utp = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
			utp.SetRelayServerData(
				joinAlloc.RelayServer.IpV4,
				(ushort)joinAlloc.RelayServer.Port,
				joinAlloc.AllocationIdBytes,
				joinAlloc.Key,
				joinAlloc.ConnectionData,
				joinAlloc.HostConnectionData,  // required for clients
				true                            // DTLS (secure)
			);

			NetworkManager.Singleton.StartClient();
			Debug.Log("Client joined via Relay.");
			return true;
		}
			catch (System.Exception e)
			{
				Debug.LogError($"JoinByRelayCode failed: {e.Message}");
				return false;
			}
	}

	private async Task<bool> HostWithRelayAndLobby(string lobbyName, int capacity, bool isPublic)
	{
		try
		{
			var a = await RelayService.Instance.CreateAllocationAsync(capacity - 1);
			_currentRelayCode = await RelayService.Instance.GetJoinCodeAsync(a.AllocationId);

			var lobby = await LobbyService.Instance.CreateLobbyAsync(
				lobbyName, capacity,
				new CreateLobbyOptions {
				IsPrivate = !isPublic,
				Data = new System.Collections.Generic.Dictionary<string, DataObject> {
					{ "relay", new DataObject(DataObject.VisibilityOptions.Public, _currentRelayCode) }
				}
				});
			_currentLobbyId = lobby.Id;

			var utp = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;

			// ensure byte[] (if your fields are NativeArray<byte>, call .ToArray())
			byte[] allocId = a.AllocationIdBytes as byte[] ?? a.AllocationIdBytes.ToArray();
			byte[] key     = a.Key                as byte[] ?? a.Key.ToArray();
			byte[] conn    = a.ConnectionData     as byte[] ?? a.ConnectionData.ToArray();

			
			utp.SetRelayServerData(
    a.RelayServer.IpV4 /* or .Ipv4 depending on your SDK */,
    (ushort)a.RelayServer.Port,
    allocId,
    key,
    conn,
    null,   // <-- hostConnectionDataBytes (hosts don't have this)
    true    // secure (DTLS)
			);

			// (keep your existing name-flow)
			_pendingName = (_nameField != null ? (_nameField.value ?? "").Trim() : "");
			var nm = NetworkManager.Singleton;
			nm.OnClientConnectedCallback += OnLocalClientConnected;

			nm.StartHost();
			SetName();

			Debug.Log($"Hosted via Relay. LobbyId={_currentLobbyId} JoinCode={_currentRelayCode}");
			return true;
		}
			catch (System.Exception e)
			{
				Debug.LogError($"HostWithRelayAndLobby failed: {e.Message}");
				return false;
			}
	}

	// Keep lobby alive while we’re the host
	void Update()
	{
		if (!string.IsNullOrEmpty(_currentLobbyId) && (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer))
		{
			_lobbyHeartbeatTimer += Time.deltaTime;
			if (_lobbyHeartbeatTimer >= 25f)
			{
				_lobbyHeartbeatTimer = 0f;
				_ = LobbyService.Instance.SendHeartbeatPingAsync(_currentLobbyId);
			}
		}
	}
	
	public async void OnClickHostOnline(ClickEvent evt)
	{
		HideMainMenuUI();
		ShowMatchmakingUI();

		await InitializeUgsAsync();
		await HostWithRelayAndLobby(quickLobbyName, maxPlayers, isPublic: true);
	}
	

	public async void OnClickJoinByCode(ClickEvent evt, string codeFromInput)
	{
		HideMainMenuUI();
		ShowMatchmakingUI();

		await InitializeUgsAsync();
		await JoinByRelayCode(codeFromInput);
	}
	
	public async void OnClickQuickPlay(ClickEvent evt)
	{
		HideMainMenuUI();
		ShowMatchmakingUI();

		await InitializeUgsAsync(); // anon auth + services init

		// try to find a public lobby with a relay code + open slot
		var lobby = await TryQuickJoinLobbyWithRelay();
		if (lobby != null &&
			lobby.Data != null &&
			lobby.Data.TryGetValue("relay", out var d) &&
			!string.IsNullOrWhiteSpace(d.Value))
		{
			await JoinByRelayCode(d.Value); // client path
			return;
		}

		// none found → host online via Relay + Lobby
		await HostWithRelayAndLobby(quickLobbyName, maxPlayers, isPublic: true);
	}
}




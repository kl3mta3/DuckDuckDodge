using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.UIElements;
public class MatchmakingManager : NetworkBehaviour
{
	[Header("UI")]
	[SerializeField] private GameObject MatchMakerView;  
	[SerializeField] private GameObject MatchView; 
	
	[Header("Match Rules")]
	[SerializeField] private int maxPlayers = 8;
	[SerializeField] private float initialCountdown = 30f;
	
	public Button MatchQuitButton;
	Label menu_TitleLabel;
	VisualElement match_Root;
	VisualElement view_Root;
	
	[Header("Game")]
	[SerializeField] private GameObject GameServerObject; 
	private GameServer server;
	public NetworkVariable<bool> MatchStarted =
		new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

	public NetworkVariable<float> TimeLeft =
		new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	public static MatchmakingManager Instance { get; private set; }
	private void Awake()
	{
		if (MatchMakerView) MatchMakerView.SetActive(true);
		server = GameServerObject.GetComponent<GameServer>();
		
		var matchUiDocument = MatchMakerView.GetComponent<UIDocument>();
		match_Root = matchUiDocument.rootVisualElement;
		
		var viewUiDocument= MatchView.GetComponent<UIDocument>();
		view_Root = viewUiDocument.rootVisualElement;
		
		MatchQuitButton = match_Root.Q<Button>("quitButton");
		MatchQuitButton.RegisterCallback<ClickEvent>(OnClickQuit);
		
		Instance = this;
	}

	public override void OnNetworkSpawn()
	{
		if (IsServer)
		{
			MatchStarted.Value = false;
			TimeLeft.Value = initialCountdown;

			// Update when players join/leave during lobby
			NetworkManager.OnClientConnectedCallback += _ => CheckStartCondition();
			NetworkManager.OnClientDisconnectCallback += _ => CheckStartCondition();
		}

		MatchStarted.OnValueChanged += OnMatchStartedChanged;
		TimeLeft.OnValueChanged += OnTimeLeftChanged;

		// Initialize UI for late joiners
		OnMatchStartedChanged(false, MatchStarted.Value);
		OnTimeLeftChanged(0, TimeLeft.Value);
	}

	private void Update()
	{
		if (!IsServer || MatchStarted.Value) return;

		TimeLeft.Value = Mathf.Max(0, TimeLeft.Value - Time.deltaTime);

		if (TimeLeft.Value <= 0f)        // time up
			StartMatchServer();
		else
			CheckStartCondition();       // or enough players
	}

	private void CheckStartCondition()
	{
		if (MatchStarted.Value) return;

		int connected = NetworkManager != null ? NetworkManager.ConnectedClientsList.Count : 0;
		if (connected >= maxPlayers)
			StartMatchServer();
	}

	private void StartMatchServer()
	{
		if (!IsServer || MatchStarted.Value) return;
		MatchStarted.Value = true;
		HideMatchMakerView();
		ShowMatchView();
		ShowMatchViewClientRpc();
		server.StartGameServerRpc();
	}

	private void OnMatchStartedChanged(bool oldVal, bool newVal)
	{

		if (MatchMakerView) MatchMakerView.SetActive(!newVal);

	}

	private void OnTimeLeftChanged(float oldVal, float newVal)
	{
		
	}
	
	[ClientRpc]
	private void ShowMatchViewClientRpc()
	{
		HideMatchMakerView();
		ShowMatchView();
	}
	
	private void ShowMatchView()
	{
		view_Root.style.display = DisplayStyle.Flex;	
	}
	
	private void HideMatchView()
	{
		view_Root.style.display = DisplayStyle.None;	
	}
	
	
	private void ShowMatchMakerView()
	{
		match_Root.style.display = DisplayStyle.Flex;	
	}
	
	private void HideMatchMakerView()
	{
		match_Root.style.display = DisplayStyle.None;	
	}
	
	// “Start Now” button that only the server honors
	public void OnClickStartNow()
	{
		RequestStartNowServerRpc();
	}

	[ServerRpc(RequireOwnership = false)]
	private void RequestStartNowServerRpc(ServerRpcParams _ = default)
	{
		StartMatchServer();
	}
	
	
	void OnClickQuit(ClickEvent evt)
	{
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#else
		UnityEngine.Application.Quit();
#endif
	}
	
}

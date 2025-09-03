using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class GameServer : NetworkBehaviour
{
	public static GameServer Instance { get; private set; }

	[Header("Config")]
	[SerializeField] private int preStartCountdown = 5;
	[SerializeField] private int gameLengthSeconds = 180;

	[Header("State (net-synced)")]
	public NetworkVariable<int> TeamAScore =
		new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

	public NetworkVariable<int> TeamBScore =
		new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

	public NetworkVariable<bool> GameRunning =
		new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

	
	public NetworkVariable<int> SecondsRemaining =
		new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

	private Coroutine _loop;

	private void Awake()
	{
		if (Instance != null && Instance != this) { Destroy(gameObject); return; }
		Instance = this;
		
	}

	public override void OnNetworkSpawn()
	{
		if (!IsServer) return;

		// optional: autostart when host starts or when enough players connect
		StartGame(); 
	}

	// Call this from a button on the host, or let any client request it:
	[ServerRpc(RequireOwnership = true)]
	public void StartGameServerRpc()
	{
		StartGame();
	}

	/// <summary>Server-only: kicks off the pre-countdown and match timer.</summary>
	public void StartGame()
	{
		if (!IsServer) return;
		if (_loop != null) StopCoroutine(_loop);
		_loop = StartCoroutine(GameLoop());
	}

	private IEnumerator GameLoop()
	{
		// Reset scores/state
		TeamAScore.Value = 0;
		TeamBScore.Value = 0;
		GameRunning.Value = false;

		// Pre-start countdown visible to clients
		SecondsRemaining.Value = preStartCountdown;
		while (SecondsRemaining.Value > 0)
		{
			yield return new WaitForSecondsRealtime(1f);
			SecondsRemaining.Value -= 1;
		}

		// Start!
		GameRunning.Value = true;
		SecondsRemaining.Value = gameLengthSeconds;

		while (SecondsRemaining.Value > 0)
		{
			yield return new WaitForSecondsRealtime(1f);
			SecondsRemaining.Value -= 1;
		}

		GameRunning.Value = false;
		EndGame();
	}

	// Called only on the server
	public void AddScore(TeamId team, int points)
	{
		if (!IsServer) return;

		switch (team)
		{
		case TeamId.TeamA: TeamAScore.Value += points; break;
		case TeamId.TeamB: TeamBScore.Value += points; break;
		}
	}

	private void EndGame()
	{
		// Decide winner 
		int a = TeamAScore.Value;
		int b = TeamBScore.Value;
		TeamId? winner = a == b ? (TeamId?)null : (a > b ? TeamId.TeamA : TeamId.TeamB);

		// Needed: reset field, return followers to pool, respawn pickups, etc.
		// You can restart with StartGame() again if you want continuous rounds.
	}

	private void OnDestroy()
	{
		if (_loop != null) StopCoroutine(_loop);
	}
}
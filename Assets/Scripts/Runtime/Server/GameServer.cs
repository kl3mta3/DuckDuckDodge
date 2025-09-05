using Unity.Netcode;
using UnityEngine;
using System.Collections;
using Unity.Collections;
using System.Collections.Generic;
using System;
public class GameServer : NetworkBehaviour
{
	public static GameServer Instance { get; private set; }
	private readonly Dictionary<ulong, int> _totals = new();
	[SerializeField] public NetworkList<PlayerScoreEntry> PlayerScores { get; private set; }
	private readonly Dictionary<ulong, int> _rowIndexByClient = new();
	
	
	[Header("Config")]
	[SerializeField] private int preStartCountdown = 5;
	[SerializeField] private int gameLengthSeconds = 180;
	[SerializeField] private NetworkObject MatchResultsPrefab;
	private NetworkObject _endGamePrefab;
	
	[Header("State (net-synced)")]
	public NetworkVariable<int> TeamAScore =
		new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

	public NetworkVariable<int> TeamBScore =
		new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

	public NetworkVariable<bool> GameRunning =
		new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

	public NetworkVariable<bool> GameEnded =
		new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	
	public NetworkVariable<int> SecondsRemaining =
		new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
		
	public NetworkVariable<MatchResult> Result =
		new(MatchResult.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

	private Coroutine _loop;

	private void Awake()
	{
		if (Instance != null && Instance != this) { Destroy(gameObject); return; }
		Instance = this;
		
		if (PlayerScores == null)
			PlayerScores = new NetworkList<PlayerScoreEntry>();
		
	}

	public override void OnNetworkSpawn()
	{
		if (!IsServer) return;
		PlayerScores ??= new NetworkList<PlayerScoreEntry>();
		var nm = NetworkManager.Singleton;
		nm.OnClientConnectedCallback += OnClientConnected;
		nm.OnClientDisconnectCallback += OnClientDisconnected;
		
		foreach (var id in nm.ConnectedClientsIds)
			EnsurePlayerEntry(id, GetDefaultName(id), TeamAllocator.GetClientTeam(id));
			
	}

	// Call this from a button on the host, or let any client request it:
	[ServerRpc(RequireOwnership = true)]
	public void StartGameServerRpc()
	{
		//if (!IsServer) return;
		StartGame();
	}

	/// Server-only: kicks off the pre-countdown and match timer.</summary>
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
	public void AddScore(TeamId team, int points, ulong clientId, FixedString64Bytes playerName)
	{
		if (!IsServer) return;

		switch (team)
		{
		case TeamId.TeamA: TeamAScore.Value += points; break;
		case TeamId.TeamB: TeamBScore.Value += points; break;
		}
		
		AddPlayerScore(clientId, points, playerName, team);
	}

	private void EndGame()
	{
		// Decide winner 
		if (!IsServer) return;
		int a = TeamAScore.Value;
		int b = TeamBScore.Value;
		TeamId? winner = a == b ? (TeamId?)null : (a > b ? TeamId.TeamA : TeamId.TeamB);
		GameRunning.Value=false;
		GameEnded.Value=true;
		Result.Value = a == b ? MatchResult.Tie : (a > b ? MatchResult.Red : MatchResult.Blue);
		
		_endGamePrefab = Instantiate(MatchResultsPrefab);
		_endGamePrefab.Spawn(true);
	}
	
	private void OnDestroy()
	{
		if (_loop != null) StopCoroutine(_loop);
		if (!IsServer || NetworkManager.Singleton == null) return;
		NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
		NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
	}
	
	private void OnClientConnected(ulong clientId)
	{
		if (!IsServer ) return;
		EnsurePlayerEntry(clientId, GetDefaultName(clientId), TeamAllocator.GetClientTeam(clientId));
	}

	private void OnClientDisconnected(ulong clientId)
	{
		_totals.Remove(clientId);
		
		int idx = IndexOfClient(clientId);
		if (idx >= 0) PlayerScores.RemoveAt(idx);

	}	
	
	private void EnsurePlayerEntry(ulong clientId, FixedString64Bytes name, TeamId team)
	{
		if (!_totals.ContainsKey(clientId))
			_totals[clientId] = 0;

		int idx = IndexOfClient(clientId);
		if (idx >= 0)
		{
			var e = PlayerScores[idx];
			e.DisplayName = name;
			e.Team        = team;
			e.Total       = _totals[clientId];
			PlayerScores[idx] = e; 
			return;
		}

		var row = new PlayerScoreEntry {
			ClientId    = clientId,
			DisplayName = name,
			Team        = team,
			Total       = _totals[clientId],
			IsNPC       = false
		};
		PlayerScores.Add(row);
		_rowIndexByClient[clientId] = PlayerScores.Count - 1;
	}
	
	
	//private void EnsurePlayerEntry(ulong clientId, FixedString64Bytes name, TeamId team)
	//{
	//	if (!_totals.ContainsKey(clientId))
	//		_totals[clientId] = 0;

	//	// update or add in NetworkList
	//	int idx = IndexOfClient(clientId);
	//	if (idx >= 0)
	//	{
	//		var e = PlayerScores[idx];
	//		e.DisplayName = name;
	//		e.Total       = _totals[clientId];
	//		PlayerScores[idx] = e;
	//	}
	//	else
	//	{
	//		PlayerScores.Add(new PlayerScoreEntry {
	//			ClientId    = clientId,
	//			Total       = _totals[clientId],
	//			DisplayName = name,
	//			IsNPC       = false,
	//			Team        = team
	//		});
	//	}
	//}
	private void BuildPlayerEntry(ulong clientId, FixedString64Bytes name, TeamId team, bool isNPC)
	{
		if (!_totals.ContainsKey(clientId))
			_totals[clientId] = 0;

			PlayerScores.Add(new PlayerScoreEntry {
				ClientId    = clientId,
				Total       = _totals[clientId],
				DisplayName = name,
				Team = team,
				IsNPC= isNPC
			});
		
	}
	
	//private int IndexOfClient(ulong clientId)
	//{
	//	for (int i = 0; i < PlayerScores.Count; i++)
	//		if (PlayerScores[i].ClientId == clientId) return i;
	//	return -1;
	//}
	
	private int IndexOfClient(ulong clientId)
	{
		if (_rowIndexByClient.TryGetValue(clientId, out var cached) &&
			cached >= 0 && cached < PlayerScores.Count &&
			PlayerScores[cached].ClientId == clientId)
			return cached;

		for (int i = 0; i < PlayerScores.Count; i++)
			if (PlayerScores[i].ClientId == clientId)
				return _rowIndexByClient[clientId] = i;

		return -1;
	}
	
	
	private static FixedString64Bytes GetDefaultName(ulong id)
	{
		
		FixedString64Bytes name = new FixedString64Bytes();
		name = "Player " + id.ToString();
		return name;
	}

	
	public void AddPlayerScore(ulong clientId, int points, FixedString64Bytes playerName, TeamId team)
	{
		if (!IsServer) return;

		// 1) Make sure we have a row for this client
		int idx = IndexOfClient(clientId);
		if (idx < 0)
		{

			EnsurePlayerEntry(clientId, playerName, team);
			idx = IndexOfClient(clientId); // re-fetch after adding
			if (idx < 0) return;           // guard just in case
		}

		// 2) Update total (remember: NetworkList<T> requires write-back of the struct)
		var entry = PlayerScores[idx];
		entry.Total += points;
		PlayerScores[idx] = entry;
	}
	
	public void SetPlayerName(ulong clientId, FixedString64Bytes name)
	{
		
		int idx = IndexOfClient(clientId);
		if (idx >= 0)
		{
			var e = PlayerScores[idx];
			e.DisplayName = name;
			PlayerScores[idx] = e;     
		}
		//else
		//{

		//	EnsurePlayerEntry(clientId, name, TeamAllocator.GetClientTeam(clientId));
		//}
	}
	
	
}


public struct PlayerScoreEntry : INetworkSerializable, System.IEquatable<PlayerScoreEntry>
{
	public ulong ClientId;
	public int   Total;
	public FixedString64Bytes DisplayName; 
	public bool IsNPC; 
	public TeamId Team;

	public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
	{
		s.SerializeValue(ref ClientId);
		s.SerializeValue(ref Total);
		s.SerializeValue(ref DisplayName);
		s.SerializeValue(ref IsNPC);
		s.SerializeValue(ref Team);
	}

	public bool Equals(PlayerScoreEntry other) =>
	ClientId == other.ClientId &&
	Total    == other.Total &&
	DisplayName.Equals(other.DisplayName) &&
	IsNPC    == other.IsNPC &&
	Team     == other.Team;
	
	public override int GetHashCode() =>
	HashCode.Combine(ClientId, Total, DisplayName.GetHashCode(), IsNPC, (int)Team);
}
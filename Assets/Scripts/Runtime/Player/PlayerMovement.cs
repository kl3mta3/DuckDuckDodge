using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController), typeof(NetworkObject))]
public class PlayerMovement : NetworkBehaviour
{
	[Header("Movement")]
	[SerializeField] private float baseSpeed = 3f;
	[SerializeField] private float ducklingSpeedBoost = .01f;
	[SerializeField] private InputActionReference moveAction;
	[SerializeField] private InputActionReference muteAction;
	[SerializeField] private string teamALayerName = "TeamA";
	[SerializeField] private string teamBLayerName = "TeamB";
	
	[Header("Visuals")]
	[SerializeField] private List<Renderer> teamRenderers = new(); 
	[SerializeField] private Material[] teamMaterials;             
	
	[Header("Team")]
	public NetworkVariable<TeamId> Team = new NetworkVariable<TeamId>(TeamId.TeamA, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	
	public NetworkVariable<FixedString64Bytes> DisplayName =
		new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
		
	private GameObject playerGoal;
	private string teamAGoalTag = "TeamAGoal";
	private string teamBGoalTag = "TeamBGoal";
	
	
	[SerializeField] public NetworkVariable<int> DucklingCount = new NetworkVariable<int>(0);
	[SerializeField] private FollowerPool followerPool;
	
	private bool _canMove;
	
	private CharacterController _cc;
	private Vector2 _lastSent;
	private float _sendTimer;
	private const float SendInterval = 1f / 30f;
	private readonly List<DucklingFollower> _chain = new();
	private Vector2 _serverMove;

	void Awake()
	{
		_cc = GetComponent<CharacterController>();
		var a = muteAction.action;
		a.performed += OnToggleMusicClick;  
		a.Enable();
	}

	public void OnToggleMusicClick(InputAction.CallbackContext ctx)
	{
		if (!ctx.performed) return;
		var bg = BackgroundMusicPlayer.Instance;
		if (bg != null) bg.ToggleMusic();
	}
	
	
	
	


	public override void OnNetworkSpawn()
	{
		if (IsOwner && moveAction) moveAction.action.Enable();

		Team.OnValueChanged += OnTeamChanged;
		
		if (IsServer)
		{
			var clientId = this.OwnerClientId;
			Team.Value = TeamAllocator.GetNextTeam(clientId);
		
			if ( DisplayName.Value.IsEmpty)
			{
				DisplayName.Value = $"Player {OwnerClientId}";
				GameServer.Instance.SetPlayerName(OwnerClientId, DisplayName.Value);
			}
		}
		
			
		ApplyTeamMaterial(Team.Value);
		ApplyTeamLayer(Team.Value);
		ResolvePlayerGoal(Team.Value);
		followerPool= GameObject.FindGameObjectWithTag("FollowerPool").GetComponent<FollowerPool>();
		
		var gs = GameServer.Instance;
		if (gs != null)
		{
			ApplyRunning(gs.GameRunning.Value);
			gs.GameRunning.OnValueChanged += (_, v) => ApplyRunning(v);
		}
		
		if (IsServer) SpawnAtGoalArea();
	}

	void OnDestroy()
	{
		Team.OnValueChanged -= OnTeamChanged;
	}

	private void OnTeamChanged(TeamId prev, TeamId cur)
	{
		ApplyTeamMaterial(cur);
		ApplyTeamLayer(cur);
		ResolvePlayerGoal(cur);   
		if (IsServer) SpawnAtGoalArea();
	}


	private void ApplyRunning(bool running)
	{
		_canMove = running;

		// optionally also disable the input action so clients don't spam RPCs
		if (IsOwner && moveAction)
		{
			if (running) moveAction.action.Enable();
			else moveAction.action.Disable();
		}
	}
	
	[ServerRpc(RequireOwnership = true)]
	public void RequestSetNameServerRpc(FixedString64Bytes requested)
	{
		var s = requested.ToString().Trim();
		if (string.IsNullOrEmpty(s)) return;
		if (s.Length > 20) s = s.Substring(0, 20);

		FixedString64Bytes clean = s;
		DisplayName.Value = clean;
		GameServer.Instance.SetPlayerName(OwnerClientId, clean);
	}
	
	private void SpawnAtGoalArea()
	{
		if (!playerGoal) return;

		var goalArea = playerGoal.GetComponent<GoalArea>();
		if (!goalArea || !goalArea.SpawnArea)
		{
			Debug.LogWarning("Goal has no SpawnArea assigned.");
			return;
		}

		Vector3 spawnPos;
		if (!TryPickPointInArea(goalArea.SpawnArea, out spawnPos))
			spawnPos = goalArea.SpawnArea.bounds.center;

		// Keep player on current ground Y (or use plane Y if you prefer)
		spawnPos.y = transform.position.y;

		ServerTeleport(spawnPos, transform.rotation);
	}

	private void ServerTeleport(Vector3 pos, Quaternion rot)
	{
		if (!IsServer) return;
	
		bool wasEnabled = _cc.enabled;
		_cc.enabled = false;
		transform.SetPositionAndRotation(pos, rot);
		_cc.enabled = wasEnabled;
	}

	
	private bool TryPickPointInArea(Collider area, out Vector3 point)
	{
		var b = area.bounds;
		for (int i = 0; i < 12; i++)
		{
			var p = new Vector3(
				Random.Range(b.min.x, b.max.x),
				b.center.y,
				Random.Range(b.min.z, b.max.z)
			);

			if (IsClearForController(p))
			{
				point = p;
				return true;
			}
		}
		point = b.center;
		return false;
	}

	private bool IsClearForController(Vector3 pos)
	{
		
		float r = _cc.radius * 0.95f;
		float h = Mathf.Max(_cc.height, r * 2f);
		Vector3 centerWS = pos + _cc.center;
		Vector3 bottom = centerWS + Vector3.up * r;
		Vector3 top    = centerWS + Vector3.up * (h - r);
		
		return !Physics.CheckCapsule(bottom, top, r, ~0, QueryTriggerInteraction.Ignore);
	}

	private void ApplyTeamLayer(TeamId t)
	{
		
		int layer = LayerMask.NameToLayer(t == TeamId.TeamA ? teamALayerName : teamBLayerName);
		if (layer < 0)
		{
			Debug.LogWarning($"[PlayerMovement] Layer not found: {(t==TeamId.TeamA?teamALayerName:teamBLayerName)}");
			return;
		}
		SetLayerRecursively(transform, layer);
	}

	private static void SetLayerRecursively(Transform root, int layer)
	{
		root.gameObject.layer = layer;
		for (int i = 0; i < root.childCount; i++)
			SetLayerRecursively(root.GetChild(i), layer);
	}

	private void ApplyTeamMaterial(TeamId t)
	{
		if (teamMaterials == null) return;
		int idx = (int)t;
		if (idx < 0 || idx >= teamMaterials.Length) return;
		var targetMat = teamMaterials[idx];
		if (targetMat == null) return;

		// Swap ALL sub-material slots on every listed renderer
		foreach (var r in teamRenderers)
		{
			if (r == null) continue;
			var mats = r.sharedMaterials;
			if (mats == null || mats.Length == 0) continue;

			for (int i = 0; i < mats.Length; i++)
				mats[i] = targetMat;

			r.sharedMaterials = mats; 
		}
	}

	private void ResolvePlayerGoal(TeamId t)
	{
		string tagToFind = (t == TeamId.TeamA) ? teamAGoalTag : teamBGoalTag;

		var go = GameObject.FindWithTag(tagToFind);
		if (go == null)
		{
			Debug.LogWarning($"[PlayerMovement] No GameObject found with tag '{tagToFind}'.");
			return;
		}
		playerGoal = go;

	}

	void OnDisable()
	{
		if (IsOwner && moveAction) moveAction.action.Disable();
	}

	void Update()
	{
		if (!_canMove) return;
		
		if (IsOwner && moveAction)
		{
			Vector2 input = moveAction.action.ReadValue<Vector2>();
			_sendTimer += Time.deltaTime;
			if (_sendTimer >= SendInterval || (input - _lastSent).sqrMagnitude > 0.0001f)
			{
				_lastSent = input; _sendTimer = 0f;
				SubmitInputServerRpc(input);
			}
		}


		if (IsServer) Simulate(_serverMove, Time.deltaTime);
	}

	private void Simulate(Vector2 input, float dt)
	{
		Vector3 dir = new(input.x, 0f, input.y);
		if (dir.sqrMagnitude > 1f) dir.Normalize();

		_cc.SimpleMove(dir * baseSpeed*(1f + (float)DucklingCount.Value* 0.01f));

		if (dir.sqrMagnitude > 0.0001f)
		{
			float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
			transform.rotation = Quaternion.Euler(0f, yaw, 0f);
		}
	}

	[ServerRpc] private void SubmitInputServerRpc(Vector2 input)
	=> _serverMove = Vector2.ClampMagnitude(input, 1f);

	// ===== chain helpers  =====
	public Transform TailTransform => _chain.Count == 0 ? transform : _chain[^1].transform;

	public void AppendFollower(DucklingFollower f)
	{
		if (!IsServer) return;
		f.AssignOwner(this);
		f.SetFollowTarget(TailTransform);
		_chain.Add(f);
		DucklingCount.Value = _chain.Count;
	}
	
	public void ClearChain(bool returnFollowersToPool = true)
	{
		if (!IsServer) return;

		if (returnFollowersToPool)
		{
			
			foreach (var f in _chain)
			{
				
				followerPool.Return(f.gameObject);
			}
		}
		else
		{
			
			foreach (var f in _chain)
				f.GetComponent<NetworkObject>()?.Despawn();
		}

		_chain.Clear();
		DucklingCount.Value = 0;
	}
	
	public bool TryIndexOf(DucklingFollower follower, out int index)
	{
		index = _chain.IndexOf(follower);
		return index >= 0;
	}
	
	public IReadOnlyList<DucklingFollower> SliceFrom(int startIndex)
	=> _chain.GetRange(startIndex, _chain.Count - startIndex);
	
	public void RemoveFrom(int startIndex)
	{
		_chain.RemoveRange(startIndex, _chain.Count - startIndex);
		DucklingCount.Value = _chain.Count;
		
		var owner = this;
		if (owner == null || !owner.IsSpawned) return;
		
		var rpc = new ClientRpcParams
		{
			Send = new ClientRpcSendParams { TargetClientIds = new[] { owner.OwnerClientId } }
		};
		AudioManager.Instance.PlayStolenFromSfxClientRpc();
	}
	
	public void AddSegment(List<DucklingFollower> seg)
	{
		foreach (var f in seg)
		{
			f.AssignOwner(this);
			f.SetFollowTarget(TailTransform);
			_chain.Add(f);
		}
		DucklingCount.Value = _chain.Count;
		
		var owner = this;
		if (owner == null || !owner.IsSpawned) return;
		
		var rpc = new ClientRpcParams
		{
			Send = new ClientRpcSendParams { TargetClientIds = new[] { owner.OwnerClientId } }
		};
		AudioManager.Instance.PlayStealingClientRpc();
	}
}
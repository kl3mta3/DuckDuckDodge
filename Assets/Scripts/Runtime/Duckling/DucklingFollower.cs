using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject), typeof(Collider))]
public class DucklingFollower : NetworkBehaviour
{
	[Header("Follow Tuning")]
	[SerializeField] private float followDistance = 0.8f;
	[SerializeField] private float moveSpeed = 6f;
	[SerializeField] private float rotateSpeed = 12f;
	
	[SerializeField] private float collapseRate = 3f;        // units/sec toward 0
	[SerializeField] private float expandRate   = 6f;        // units/sec back to followDistance
	[SerializeField] private float stopSpeedEps = 0.05f;  

	[Header("Visuals")]
	[SerializeField] private List<Renderer> teamRenderers = new();
	[SerializeField] private Material[] teamMaterials;  
	

	public NetworkVariable<ulong> OwnerClientId = new NetworkVariable<ulong>(
		0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

	public NetworkVariable<TeamId> Team = new NetworkVariable<TeamId>(
		TeamId.TeamA, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

	private Transform _target;   
	private Vector3 _desiredPos; 
	private Vector3   _lastTargetPos;
	private float     _currentDist;     
	
	private void Reset()
	{
		gameObject.SetActive(true);
		var c = GetComponent<Collider>();
		c.isTrigger = true;
		ApplyTeamMaterial(Team.Value);
	}

	public override void OnNetworkSpawn()
	{
		Team.OnValueChanged += OnTeamChanged;
		ApplyTeamMaterial(Team.Value);
	}

	public override void OnNetworkDespawn()
	{
		Team.OnValueChanged -= OnTeamChanged;
		gameObject.SetActive(false);
	}

	public void AssignOwner(PlayerMovement newOwner)
	{
		if (!IsServer || newOwner == null) return;
		OwnerClientId.Value = newOwner.OwnerClientId;
		Team.Value        = newOwner.Team.Value;
	}

	private void OnTeamChanged(TeamId prev, TeamId cur) => ApplyTeamMaterial(cur);
	
	
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

			r.sharedMaterials = mats; // apply back
		}
	}

	public void SetFollowTarget(Transform t) => _target = t;


 private void Update()
	{
		if (_target == null) return;

		// --- measure target speed ---
		Vector3 tp = _target.position;
		float targetSpeed = (tp - _lastTargetPos).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
		_lastTargetPos = tp;

		// --- adjust spacing based on target motion ---
		if (targetSpeed <= stopSpeedEps)
			_currentDist = Mathf.MoveTowards(_currentDist, 0f, collapseRate * Time.deltaTime);
		else
			_currentDist = Mathf.MoveTowards(_currentDist, followDistance, expandRate * Time.deltaTime);

		// --- compute desired position (behind target by dynamic distance) ---
		Vector3 behind = tp - _target.forward * _currentDist;

		if (_desiredPos == Vector3.zero) _desiredPos = transform.position;
		_desiredPos = Vector3.Lerp(_desiredPos, behind, 12f * Time.deltaTime);

		// --- move & face travel ---
		Vector3 to = _desiredPos - transform.position;
		transform.position = Vector3.MoveTowards(transform.position, _desiredPos, moveSpeed * Time.deltaTime);

		if (to.sqrMagnitude > 0.0001f)
		{
			Quaternion face = Quaternion.LookRotation(new Vector3(to.x, 0f, to.z));
			transform.rotation = Quaternion.Slerp(transform.rotation, face, rotateSpeed * Time.deltaTime);
		}
	}
	

	
	// ==== STEAL MECHANIC ====
	private void OnTriggerEnter(Collider other)
	{
		if (!IsServer) return;

		// Did we bump into a player head or any collider under it?
		var captor = other.GetComponentInParent<PlayerMovement>();
		if (captor == null || !captor.IsSpawned) return;

	
		if (captor.Team.Value == Team.Value) return;

		if (captor.OwnerClientId == OwnerClientId.Value) return;

		var nm = NetworkManager.Singleton;
		if (nm == null) return;

		var playerObj = nm.SpawnManager.GetPlayerNetworkObject(OwnerClientId.Value);
		if (playerObj == null) return;

		var victim = playerObj.GetComponent<PlayerMovement>();
		if (victim == null) return;

		ChainServer.StealSegment(victim, this, captor);
		
		Team.OnValueChanged += OnTeamChanged;
		ApplyTeamMaterial(Team.Value);
	}
}
using UnityEngine;
using Unity.Netcode;
using System.Collections;

[RequireComponent(typeof(Collider), typeof(NetworkObject))]
public class DucklingCollectible : NetworkBehaviour
{
	[SerializeField] private FollowerPool pool;   
	[SerializeField] private GameObject body;   
	[SerializeField] private float respawnDelay = 30f;

	private Collider col;

	private readonly NetworkVariable<bool> isAvailable =
		new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone,
		NetworkVariableWritePermission.Server);

	private void Awake()
	{
		col = GetComponent<Collider>();
		col.isTrigger = true;
		
	}

	public override void OnNetworkSpawn()
	{

		SetVisible(isAvailable.Value);
		isAvailable.OnValueChanged += (_, v) => SetVisible(v);

		if (IsServer) isAvailable.Value = true;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!IsServer || !isAvailable.Value) return;

		var owner = other.GetComponentInParent<PlayerMovement>();
		if (owner == null || !owner.IsSpawned) return;

		// Get follower from the pool
		var followerGo = pool.Get(gameObject.transform.position);          
		if (followerGo == null) return;

		var no = followerGo.GetComponent<NetworkObject>();
		if (!no.IsSpawned)
		{
			no.transform.SetPositionAndRotation(transform.position, Quaternion.identity);
			no.Spawn(true);
		}

		var follower = followerGo.GetComponent<DucklingFollower>();
		owner.AppendFollower(follower);

		isAvailable.Value = false;           
		
		var rpc = new ClientRpcParams
		{
			Send = new ClientRpcSendParams { TargetClientIds = new[] { owner.OwnerClientId } }
		};
		
		AudioManager.Instance.PlayPickupSfxClientRpc(rpc);
		
		StartCoroutine(RespawnAfterDelay());
		
	}

	private IEnumerator RespawnAfterDelay()
	{
		yield return new WaitForSeconds(respawnDelay);
		if (this != null && IsServer)
			isAvailable.Value = true;            
	}





	private void SetVisible(bool v)
	{
		if (body) body.SetActive(v);
		if (col)  col.enabled = v;
	}
}
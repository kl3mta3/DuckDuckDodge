using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class FollowerPool : NetworkBehaviour
{
	[SerializeField] private GameObject followerPrefab;
	[SerializeField] private int poolSize = 100;

	private int availableCount = 0;
	private readonly List<GameObject> pool = new();
	private int _nameIndex = 0;

	// DO NOT generate in Awake — runs on clients too.
	private void Awake()
	{
		pool.Capacity = poolSize;
	}

	public override void OnNetworkSpawn()
	{
		if (!IsServer) return;

		if (pool.Count == 0)
			GenerateFollowers(poolSize);          // server-only instantiate (not spawned yet)

		// Spawn each follower once so clients have them, then hide via RPC
		foreach (var f in pool)
		{
			var no = f.GetComponent<NetworkObject>();
			f.SetActive(true);                    // must be active to Spawn
			if (!no.IsSpawned) no.Spawn(true);

			// start hidden everywhere
			SetFollowerActive(f, false);
			HideFollowerClientRpc(no.NetworkObjectId);
		}

		availableCount = pool.Count;
	}

	public GameObject Get(Vector3 location)
	{
		if (!IsServer) return null;

		if (availableCount == 0)
		{
			GenerateFollowers(25);
			// Immediately spawn & hide the new ones
			for (int i = pool.Count - 25; i < pool.Count; i++)
			{
				var f = pool[i];
				var no = f.GetComponent<NetworkObject>();
				f.SetActive(true);
				if (!no.IsSpawned) no.Spawn(true);
				SetFollowerActive(f, false);
				HideFollowerClientRpc(no.NetworkObjectId);
			}
			availableCount += 25;
		}

		for (int i = 0; i < pool.Count; i++)
		{
			if (!pool[i].activeSelf)           
			{
				availableCount--;

				var go = pool[i];
				go.transform.position = location;

				// show on server + clients
				SetFollowerActive(go, true);
				var no = go.GetComponent<NetworkObject>();
				ShowFollowerClientRpc(no.NetworkObjectId, location);
				return go;
			}
		}
		return null;
	}

	public void Return(GameObject go)
	{
		if (!IsServer || go == null) return;

		SetFollowerActive(go, false);
		availableCount++;

		var no = go.GetComponent<NetworkObject>();
		HideFollowerClientRpc(no.NetworkObjectId);

		go.transform.SetParent(transform, false);
		go.transform.localPosition = Vector3.zero;
	}

	[ClientRpc]
	private void ShowFollowerClientRpc(ulong netId, Vector3 pos)
	{
		if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(netId, out var no))
		{
			no.transform.position = pos;
			SetFollowerActive(no.gameObject, true);
		}
	}

	[ClientRpc]
	private void HideFollowerClientRpc(ulong netId)
	{
		if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(netId, out var no))
			SetFollowerActive(no.gameObject, false);
	}

	private void SetFollowerActive(GameObject go, bool v)
	{
		
		go.SetActive(v);
	}

	private void GenerateFollowers(int amount)    // server-only
	{
		for (int i = 0; i < amount; i++)
		{
			var follower = Instantiate(followerPrefab, transform);
			follower.name = $"Follower_{_nameIndex++:D4}";
			follower.transform.localPosition = Vector3.zero;
			follower.SetActive(false);            // will be spawned & hidden in OnNetworkSpawn
			pool.Add(follower);
		}
	}
}
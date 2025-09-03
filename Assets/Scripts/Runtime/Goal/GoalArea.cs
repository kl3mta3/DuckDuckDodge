using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Collider))]
public class GoalArea : NetworkBehaviour
{
	[SerializeField] private TeamId team;
	[SerializeField] private Collider spawnArea; 
	
	public Collider SpawnArea => spawnArea;
	
	
	private void Reset() => GetComponent<Collider>().isTrigger = true;

	private void OnTriggerEnter(Collider other)
	{
		if (!IsServer) return;

		var player = other.GetComponentInParent<PlayerMovement>();
		if (player == null) return;

		if (player.Team.Value != team) return; 

		int points = player.DucklingCount.Value;
		if (points <= 0) return;

		GameServer.Instance.AddScore(team, points); 
		player.ClearChain(returnFollowersToPool: true); 
	}
}
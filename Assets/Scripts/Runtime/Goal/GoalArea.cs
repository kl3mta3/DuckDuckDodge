using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Collider))]
public class GoalArea : NetworkBehaviour
{
	[SerializeField] private TeamId team;
	[SerializeField] private Collider spawnArea; 
	[SerializeField] private AudioClip scoreSfx;
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
		
		//remove
		string text = $"AddScore(GoalArea) clientId={player.DisplayName.Value}  points={points} team={team.ToString()}";
			Debug.Log(text );
		//remove
		
		GameServer.Instance.AddScore(team, points, player.OwnerClientId, player.DisplayName.Value); 
		player.ClearChain(returnFollowersToPool: true); 
		
	
		if (!player.IsSpawned) return;
		
		var rpc = new ClientRpcParams
		{
			Send = new ClientRpcSendParams { TargetClientIds = new[] { player.OwnerClientId } }
		};
		
		AudioManager.Instance.PlayScoreSfxClientRpc(rpc);
		
	}
	

	
	
	
}
using UnityEngine;
using Unity.Netcode;

public class AudioManager : NetworkBehaviour
{

	[SerializeField] public AudioClip  DucklingPickUpSFX;
	[SerializeField] public AudioClip  ScoringSFX;
	[SerializeField] public AudioClip  StealingSFX;
	[SerializeField] public AudioClip  StolenFromSFX;
	[SerializeField] public AudioClip  GameEndedSFX;
	[SerializeField] public AudioClip  GameStartedSFX;
	[SerializeField] public AudioClip  GameStartingTimerSFX;
	[SerializeField] public AudioClip  GameEndingTimerSFX;
	[SerializeField] public AudioClip  GameEndingTimerUrgentSFX;
	[SerializeField] public AudioClip  GameWonSFX;
	[SerializeField] public AudioClip  GameLostSFX;

	[SerializeField] public AudioClip  GameMenuMusic;
	[SerializeField] public AudioClip  GameBackgroundMusic;
	[SerializeField] public AudioClip  GameResultsMusic;
	public float Volume= .25f;
	public static AudioManager Instance { get; private set; }
	
	void Awake()
	{
		
		Instance= this;
		
	}
	
	[ClientRpc]
	public void PlayGameEndingTimerSFXClientRpc(ClientRpcParams rpcParams = default)
	{

		var pos = Camera.main ? Camera.main.transform.position : Vector3.zero;
		AudioSource.PlayClipAtPoint(GameEndingTimerSFX, pos, Volume);
	}
	
	[ClientRpc]
	public void PlayGameEndingTimerUrgentSFXClientRpc(ClientRpcParams rpcParams = default)
	{

		var pos = Camera.main ? Camera.main.transform.position : Vector3.zero;
		AudioSource.PlayClipAtPoint(GameEndingTimerUrgentSFX, pos, Volume);
	}
	
	[ClientRpc]
	public void PlayGameBackgroundMusicClientRpc(ClientRpcParams rpcParams = default)
	{

		var pos = Camera.main ? Camera.main.transform.position : Vector3.zero;
		AudioSource.PlayClipAtPoint(GameBackgroundMusic, pos, Volume);
	}

	[ClientRpc]
	public void PlayGameResultsMusicClientRpc(ClientRpcParams rpcParams = default)
	{

		var pos = Camera.main ? Camera.main.transform.position : Vector3.zero;
		AudioSource.PlayClipAtPoint(GameResultsMusic, pos, Volume);
	}

	[ClientRpc]
	public void PlayGameMenuMusicClientRpc(ClientRpcParams rpcParams = default)
	{

		var pos = Camera.main ? Camera.main.transform.position : Vector3.zero;
		AudioSource.PlayClipAtPoint(GameMenuMusic, pos, Volume);
	}

	[ClientRpc]
	public void PlayStolenFromSfxClientRpc(ClientRpcParams rpcParams = default)
	{

		var pos = Camera.main ? Camera.main.transform.position : Vector3.zero;
		AudioSource.PlayClipAtPoint(StolenFromSFX, pos, Volume);
	}

	[ClientRpc]
	public void PlayGameEndedClientRpc(ClientRpcParams rpcParams = default)
	{

		var pos = Camera.main ? Camera.main.transform.position : Vector3.zero;
		AudioSource.PlayClipAtPoint(GameEndedSFX, pos, Volume);
	}
	
	[ClientRpc]
	public void PlayGameStartingTimerClientRpc(ClientRpcParams rpcParams = default)
	{

		var pos = Camera.main ? Camera.main.transform.position : Vector3.zero;
		AudioSource.PlayClipAtPoint(GameStartingTimerSFX, pos, Volume+.4f);
	}
	
	[ClientRpc]
	public void PlayGameStartedClientRpc(ClientRpcParams rpcParams = default)
	{

		var pos = Camera.main ? Camera.main.transform.position : Vector3.zero;
		AudioSource.PlayClipAtPoint(GameStartedSFX, pos, Volume);
	}
	
	[ClientRpc]
	public void PlayGameWonClientRpc(ClientRpcParams rpcParams = default)
	{

		var pos = Camera.main ? Camera.main.transform.position : Vector3.zero;
		AudioSource.PlayClipAtPoint(GameWonSFX, pos, Volume);
	}
	
	[ClientRpc]
	public void PlayGameLostClientRpc(ClientRpcParams rpcParams = default)
	{

		var pos = Camera.main ? Camera.main.transform.position : Vector3.zero;
		AudioSource.PlayClipAtPoint(GameLostSFX, pos, Volume);
	}
	
	[ClientRpc]
	public void PlayPickupSfxClientRpc(ClientRpcParams rpcParams = default)
	{

		var pos = Camera.main ? Camera.main.transform.position : Vector3.zero;
		AudioSource.PlayClipAtPoint(DucklingPickUpSFX, pos, Volume);
	}
	
	[ClientRpc]
	public void PlayScoreSfxClientRpc(ClientRpcParams rpcParams = default)
	{

		var pos = Camera.main ? Camera.main.transform.position : Vector3.zero;
		AudioSource.PlayClipAtPoint(ScoringSFX, pos, Volume);
	}
	
	
	[ClientRpc]
	public void PlayStealingClientRpc(ClientRpcParams rpcParams = default)
	{

		var pos = Camera.main ? Camera.main.transform.position : Vector3.zero;
		AudioSource.PlayClipAtPoint(StealingSFX, pos, Volume);
	}
}

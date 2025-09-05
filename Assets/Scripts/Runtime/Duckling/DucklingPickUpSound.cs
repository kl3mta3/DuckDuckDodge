using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(AudioSource))]
public class DucklingPickupSound : NetworkBehaviour
{
	 private AudioSource audioSource;

	private void Awake()
	{
		audioSource = GetComponent<AudioSource>();
	}

	public override void OnNetworkDespawn()
	{
		if (IsOwner || IsLocalPlayer)
		{
			if (audioSource != null)
				audioSource.Play();
		}
	}
	
	
	public void PlayPickUpSound()
	{
		
		if (IsOwner || IsLocalPlayer)
		{
			if (audioSource != null)
				audioSource.Play();
		}
		
	}
}

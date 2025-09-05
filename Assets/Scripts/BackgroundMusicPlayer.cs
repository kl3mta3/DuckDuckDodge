using UnityEngine;
using	Unity.Netcode;

public class BackgroundMusicPlayer : NetworkBehaviour
{
	[SerializeField] GameObject BgMusic;
	private AudioSource audio;
	private GameServer GS;
	private bool musicMuted=false;
	public static BackgroundMusicPlayer Instance {get; private set;}
	
    void Start()
	{
		Instance=this;
		GS= GameServer.Instance;
		
		GS.GameEnded.OnValueChanged += StopPlayer;
		audio= BgMusic.GetComponent<AudioSource>();
    }

	public void ToggleMusic()
	{
		
		if(!musicMuted)
		{
			musicMuted= true;
			//audio.Pause();
			BgMusic.SetActive(false);
			
		}
		else
		{
			musicMuted= false;
			//audio.Play();
			BgMusic.SetActive(true);
		}
		
		
		
	}
	private void OnDestroy()
	{
	
		if (GS != null)
			GS.GameEnded.OnValueChanged -= StopPlayer;
	}
	
	private void StopPlayer(bool oldValue, bool newValue)
	{

		if(newValue)
			BgMusic.SetActive(false);
		
	}
}

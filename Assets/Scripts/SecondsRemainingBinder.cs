using UnityEngine;
using UnityEngine.UIElements;
using Unity.Netcode;

public class SecondsRemainingBinder : NetworkBehaviour
{
	[SerializeField] private UIDocument uiDoc;

	private Label _label;

	VisualElement match_Root;
	private void Start()
	{
		_label = uiDoc.rootVisualElement.Q<Label>("timerLabel");

	
		GameServer.Instance.SecondsRemaining.OnValueChanged += OnSecondsChanged;

		
		OnSecondsChanged(0, GameServer.Instance.SecondsRemaining.Value);
	}

	private void OnSecondsChanged(int oldValue, int newValue)
	{
		
		if (_label != null)
		{
			//if new value is less than 10 run flash loop
			int minutes = newValue / 60;
			int seconds = newValue % 60;
			_label.text = $"{minutes:00}:{seconds:00}";
		}
		
		var gs = GameServer.Instance;
		if (gs == null) return;

		
		if (!gs.GameRunning.Value)
		{
			if (newValue > 0)                       
			{
				
				AudioManager.Instance.PlayGameStartingTimerClientRpc();
			}
			else if (oldValue > 0 && newValue <= 0)  
			{
				AudioManager.Instance.PlayGameStartedClientRpc();
			}
			return;
		}

		if (gs.GameRunning.Value)
		{
			if (oldValue > 20 && newValue <= 20)        
			{
				AudioManager.Instance.PlayGameEndingTimerSFXClientRpc();
			}
			else if (oldValue > 10 && newValue <= 10)   
			{
				AudioManager.Instance.PlayGameEndingTimerUrgentSFXClientRpc();
			}
			else if (oldValue > 0 && newValue <= 0)     
			{
				AudioManager.Instance.PlayGameEndedClientRpc();
			}
		}
		
		
	}
}


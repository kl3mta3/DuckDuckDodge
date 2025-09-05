using UnityEngine;
using UnityEngine.UIElements;
using Unity.Netcode;

public class MatchMakerTimerBinder : MonoBehaviour
{
	[SerializeField] private UIDocument uiDoc;
	
	private Label _label;

	
	private Button quitButton;
	VisualElement match_Root;
	
	
	private void Start()
	{
		match_Root = uiDoc.GetComponent<UIDocument>().rootVisualElement;
		_label = match_Root.Q<Label>("timerLabel"); 

		quitButton = match_Root.Q<Button>("quitButton");
		quitButton.RegisterCallback<ClickEvent>(OnClickQuit);
		
		MatchmakingManager.Instance.TimeLeft.OnValueChanged += OnSecondsChanged;
		OnSecondsChanged(0, GameServer.Instance.SecondsRemaining.Value);
		
	}	
	
	
	private void OnSecondsChanged(float oldValue, float newValue)
	{
		
			
			
		if (_label != null)
		{
			int minutes = Mathf.FloorToInt(newValue / 60);
			int seconds = Mathf.FloorToInt(newValue % 60);
			_label.text = $"{minutes:00}:{seconds:00}";
		}
		

		
	}
	
	void OnClickQuit(ClickEvent evt)
	{
		#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
		#else
			UnityEngine.Application.Quit();
		#endif
	}
	
	
}

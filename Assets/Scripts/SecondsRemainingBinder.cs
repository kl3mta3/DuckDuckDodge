using UnityEngine;
using UnityEngine.UIElements;
using Unity.Netcode;

public class SecondsRemainingBinder : MonoBehaviour
{
	[SerializeField] private UIDocument uiDoc;

	private Label _label;

	private void Start()
	{
		_label = uiDoc.rootVisualElement.Q<Label>("timerLabel"); // name in UXML

		// subscribe to NGO variable changes
		GameServer.Instance.SecondsRemaining.OnValueChanged += OnSecondsChanged;

		// set initial value
		OnSecondsChanged(0, GameServer.Instance.SecondsRemaining.Value);
	}

	private void OnSecondsChanged(int oldValue, int newValue)
	{
		if (_label != null)
		{
			int minutes = newValue / 60;
			int seconds = newValue % 60;
			_label.text = $"{minutes:00}:{seconds:00}";
		}
	}
}


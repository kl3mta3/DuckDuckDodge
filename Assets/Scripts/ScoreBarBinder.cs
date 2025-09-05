using UnityEngine;
using UnityEngine.UIElements;
using Unity.Netcode;

public class ScoreBarSimple : MonoBehaviour
{
	[SerializeField] private UIDocument uiDoc;

	private VisualElement _teamBBackdrop;
	
	private float _baseline = 25f;    // baseline %
	private float _scale = 0.25f;     // 0.25% width per 1% score diff
	
	private Label _teamAScoreLabel;
	private Label _teamBScoreLabel;
	
	private void Start()
	{
		var root = uiDoc.rootVisualElement;
		_teamBBackdrop = root.Q<VisualElement>("TeamBBackdrop");
		_teamAScoreLabel = root.Q<Label>("teamAScoreLabel"); 
		_teamBScoreLabel = root.Q<Label>("teamBScoreLabel"); 

		var gs = GameServer.Instance;
		
		gs.TeamAScore.OnValueChanged += (_, __) => UpdateBar(gs.TeamAScore.Value, gs.TeamBScore.Value);
		gs.TeamBScore.OnValueChanged += (_, __) => UpdateBar(gs.TeamAScore.Value, gs.TeamBScore.Value);
		UpdateBar(gs.TeamAScore.Value, gs.TeamBScore.Value);
	}

	private void UpdateBar(int a, int b)
	{
		float total = Mathf.Max(1, a + b);
		float diffPct = ((float)b - a) / total * 100f;

		float newWidth = _baseline + diffPct * _scale;
		newWidth = Mathf.Clamp(newWidth, 0f, 100f);

		_teamBBackdrop.style.width = Length.Percent(newWidth);
		
		UpdateScores(a, b);
	}
	
	private void UpdateScores(int a, int b)
	{
		if (_teamAScoreLabel != null)
		{
			_teamAScoreLabel.text = a.ToString();
		}
		
		if (_teamBScoreLabel != null)
		{
			_teamBScoreLabel.text = b.ToString();
		}	
	}
	
	
	
}
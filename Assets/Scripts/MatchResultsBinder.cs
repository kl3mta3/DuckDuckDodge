using UnityEngine;
using UnityEngine.UIElements;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

public class MatchResultsBinder : NetworkBehaviour
{
	[SerializeField] private UIDocument uiDoc;

	private Label _winnerLabel, _redTotal, _blueTotal;
	private VisualElement _redMark, _blueMark;
	private Label _player1Name, _player1Score, _player2Name, _player2Score, _player3Name, 
		_player3Score, _player4Name, _player4Score, _player5Name, _player5Score, _player6Name,
		_player6Score, _player7Name, _player7Score, _player8Name, _player8Score;
		
	private Button PlayAgain;

	public override void OnNetworkSpawn()
	{
		var root = (uiDoc ? uiDoc : GetComponent<UIDocument>()).rootVisualElement;

		_winnerLabel = root.Q<Label>("winnerLabel");
		_redTotal    = root.Q<Label>("teamATotalLabel");   
		_blueTotal   = root.Q<Label>("teamBTotalLabel"); 
		
		PlayAgain= root.Q<Button>("playAgainButton");
		PlayAgain.RegisterCallback<ClickEvent>(OnClickPlayAgain);
		
		_player1Name = root.Q<Label>("player1Name");
		_player1Score = root.Q<Label>("player1Score");
		_player2Name = root.Q<Label>("player2Name"); 
		_player2Score = root.Q<Label>("player2Score"); 
		_player3Name = root.Q<Label>("player3Name"); 
		_player3Score = root.Q<Label>("player3Score"); 
		_player4Name = root.Q<Label>("player4Name"); 
		_player4Score = root.Q<Label>("player4Score"); 
		_player5Name = root.Q<Label>("player5Name"); 
		_player5Score = root.Q<Label>("player5Score"); 
		_player6Name = root.Q<Label>("player6Name"); 
		_player6Score = root.Q<Label>("player6Score"); 
		_player7Name = root.Q<Label>("player7Name"); 
		_player7Score = root.Q<Label>("player7Score"); 
		_player8Name = root.Q<Label>("player8Name"); 
		_player8Score = root.Q<Label>("player8Score"); 
			
		_redMark = root.Q<VisualElement>("winningMarkA");
		_blueMark = root.Q<VisualElement>("winningMarkB");

		var gs = GameServer.Instance;

		gs.Result.OnValueChanged     += (_, __) => Repaint();
		gs.TeamAScore.OnValueChanged += (_, __) => Repaint();
		gs.TeamBScore.OnValueChanged += (_, __) => Repaint();
		gs.PlayerScores.OnListChanged += OnListChanged;
		Rebuild(gs.PlayerScores);
		
		for (int i = 0; i < gs.PlayerScores.Count; i++)
		{
				
		
			Debug.Log($"{gs.PlayerScores[i].DisplayName} has score of {gs.PlayerScores[i].Total.ToString()}");
		}
		Repaint();
	}

	public override void OnNetworkDespawn()
	{
		if (GameServer.Instance == null) return;
		GameServer.Instance.Result.OnValueChanged     -= (_, __) => Repaint();
		GameServer.Instance.TeamAScore.OnValueChanged -= (_, __) => Repaint();
		GameServer.Instance.TeamBScore.OnValueChanged -= (_, __) => Repaint();
		GameServer.Instance.PlayerScores.OnListChanged -= OnListChanged;
	}

	private void OnListChanged(NetworkListEvent<PlayerScoreEntry> ev)
	=> Rebuild(GameServer.Instance.PlayerScores);

	private void Rebuild(NetworkList<PlayerScoreEntry> list)
	{
		// 1) copy & sort desc by total
		var ordered = new List<PlayerScoreEntry>(list.Count);
		foreach (var entry in list)
		{
			ordered.Add(entry);
		}
		ordered.Sort((a, b) => b.Total.CompareTo(a.Total));

		// 2) split by team and take top 4 each
		var topA = new List<PlayerScoreEntry>(4);
		var topB = new List<PlayerScoreEntry>(4);

		foreach (var e in ordered)
		{
			if (e.Team == TeamId.TeamA && topA.Count < 4) topA.Add(e);
			else if (e.Team == TeamId.TeamB && topB.Count < 4) topB.Add(e);

			if (topA.Count == 4 && topB.Count == 4) break;
		}

		// 3) write to UI (A on left: 1,3,5,7 — B on right: 2,4,6,8)
		SetRow(_player1Name, _player1Score, topA, 0);
		SetRow(_player3Name, _player3Score, topA, 1);
		SetRow(_player5Name, _player5Score, topA, 2);
		SetRow(_player7Name, _player7Score, topA, 3);

		SetRow(_player2Name, _player2Score, topB, 0);
		SetRow(_player4Name, _player4Score, topB, 1);
		SetRow(_player6Name, _player6Score, topB, 2);
		SetRow(_player8Name, _player8Score, topB, 3);
	}

	void SetRow(Label name, Label score, List<PlayerScoreEntry> src, int i)
	{
		if (i < src.Count)
		{
			name.text  = src[i].DisplayName.ToString(); 
			score.text = src[i].Total.ToString();
		}
		else
		{
			name.text  = "————";
			score.text = "----";
		}
	}
	
	private void Repaint()
	{
		var gs = GameServer.Instance;
		if (gs == null) return;

		// winner text
		if (_winnerLabel!=null)
		{
			_winnerLabel.text = gs.Result.Value switch
			{
				MatchResult.Red  => "Red Wins",
				MatchResult.Blue => "Blue Wins",
				MatchResult.Tie  => "Tie",
				_                => ""
			};
		}
	
		if (_redTotal!=null)
		{
			{_redTotal.text  = gs.TeamAScore.Value.ToString();}

		}
		if (_blueTotal!=null)
		{_blueTotal.text = gs.TeamBScore.Value.ToString();}

		// show only the winning checkmark
		Hide(_redMark); Hide(_blueMark);

		if (gs.TeamAScore.Value > gs.TeamBScore.Value) 
		{
			Show(_redMark);
			
		}
		else if (gs.TeamBScore.Value > gs.TeamAScore.Value) Show(_blueMark);
		
	}

	
	private static void Show(VisualElement ve)
	{
		if (ve == null) return;
		ve.style.display = DisplayStyle.Flex;
		ve.style.opacity = 1f; 

	}
	
	private void OnClickPlayAgain(ClickEvent evt)
	{
		string sceneName = SceneManager.GetActiveScene().name;   
		SceneManager.LoadScene(sceneName);

	}
	
	private static void Hide(VisualElement ve)
	{
		if (ve == null) return;
		ve.style.display = DisplayStyle.None;
		ve.style.opacity = 0f;
		
	}
}
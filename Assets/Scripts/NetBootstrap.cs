using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;


public class NetBootstrap : MonoBehaviour
{
	
	[SerializeField] public GameObject MainMenuView;
	
	public Button RunClientButton {get; private set;}
	public Button RunServerButton;
	public Button HostGameButton;
	public Button QuitButton;
	public Button RunSinglePlayerButton;
	Label m_TitleLabel;
	VisualElement m_Root;
	
	void OnEnable()
	{
		MainMenuView.SetActive(true);
		var uiDocument = MainMenuView.GetComponent<UIDocument>();
		m_Root = uiDocument.rootVisualElement;
		
		RunClientButton = m_Root.Q<Button>("findMatchButton");
		RunClientButton.RegisterCallback<ClickEvent>(OnClickFindMatch);
		
		
		RunSinglePlayerButton= m_Root.Q<Button>("singlePlayerButton");
		RunSinglePlayerButton.RegisterCallback<ClickEvent>(OnClickStartSinglePlayer);
		
		QuitButton = m_Root.Q<Button>("quitButton");
		QuitButton.RegisterCallback<ClickEvent>(OnClickQuit);
		
		RunServerButton = m_Root.Q<Button>("serverButton");
		RunServerButton.RegisterCallback<ClickEvent>(OnClickRunServer);
		
		HostGameButton = m_Root.Q<Button>("hostButton");
		HostGameButton.RegisterCallback<ClickEvent>(OnClickHostGame);
		
	}
	
	void OnClickFindMatch(ClickEvent evt)
	{
		MainMenuView.SetActive(false);
		NetworkManager.Singleton.StartClient();
	}
	
	void OnClickQuit(ClickEvent evt)
	{
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#else
		UnityEngine.Application.Quit();
#endif
	}
	
	void OnClickRunServer(ClickEvent evt)
	{
		MainMenuView.SetActive(false);
		NetworkManager.Singleton.StartServer();
	}
	
	void OnClickHostGame(ClickEvent evt)
	{
		MainMenuView.SetActive(false);
		NetworkManager.Singleton.StartHost();
	}
	
	void OnClickStartSinglePlayer(ClickEvent evt)
	{

	}

}
//using UnityEngine;
//using UnityEngine.UIElements;
//using Unity.Netcode;
//using Unity.Collections;

//public class PlayerNameBinder : NetworkBehaviour
//{
//	[SerializeField] private UIDocument uiDoc;
//	public PlayerNameBinder Instance {get; private set;}
//	private TextField _nameField;
//	private Button _enterBtn;

//	void Start()
//	{
//		var root = uiDoc.rootVisualElement;
//		_nameField = root.Q<TextField>("playerNameField");
//		_enterBtn  = root.Q<Button>("enterNameButton");
//		Instance=this;
//		_enterBtn.clicked += OnClickSetName;
//	}

//	public void OnClickSetName()
//	{
//		var po = NetworkManager.Singleton?.LocalClient?.PlayerObject;
//		if (po == null) return;

//		var player = po.GetComponent<PlayerMovement>();
//		if (player == null) return;

//		var s = _nameField.value?.Trim() ?? "";
//		if (s.Length == 0) return;

//		// send to server
//		FixedString64Bytes f = s;
//		player.RequestSetNameServerRpc(f);
//	}
//}

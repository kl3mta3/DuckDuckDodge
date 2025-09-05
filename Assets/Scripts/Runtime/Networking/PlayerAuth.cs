using Unity.Services.Core;
using Unity.Services.Authentication;
using UnityEngine;

public class PlayerAuth : MonoBehaviour
{
	async void Start()
	{
		await UnityServices.InitializeAsync();

		if (!AuthenticationService.Instance.IsSignedIn)
		{
			await AuthenticationService.Instance.SignInAnonymouslyAsync();
			Debug.Log($"Signed in as anonymous player {AuthenticationService.Instance.PlayerId}");
		}
	} 
}

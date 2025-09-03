using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class NetcodeCleanup : MonoBehaviour
{
	void OnDisable() => TryShutdown();
	void OnApplicationQuit() => TryShutdown();

	static void TryShutdown()
	{
		var nm = NetworkManager.Singleton;
		if (nm == null) return;

		// if we were running anything, shut it down cleanly
		if (nm.IsServer || nm.IsClient)
			nm.Shutdown();

		// turn off the transport component so its driver tears down
		var utp = nm.NetworkConfig?.NetworkTransport as UnityTransport;
		if (utp != null && utp.enabled)
			utp.enabled = false; // triggers OnDisable -> closes sockets
	}
}
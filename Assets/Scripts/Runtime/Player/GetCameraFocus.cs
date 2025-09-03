using UnityEngine;
using Unity.Netcode;
using Unity.Cinemachine;


public class GetCameraFocus : NetworkBehaviour
{
	void Start()
	{
		if(!IsOwner)	
			return;
			
		var cam = FindAnyObjectByType<CinemachineCamera>();
		cam.Target.TrackingTarget = transform;
		
		
		
	}
}

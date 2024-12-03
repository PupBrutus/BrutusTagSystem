using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class TagGroupScript : UdonSharpBehaviour
{
    void Start()
    {
        
    }

    public override void PostLateUpdate()
    {
        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        VRCPlayerApi ownerPlayer = Networking.GetOwner(gameObject);

        //set the position of this gameobject as 115% of the owner's height above the owner's head
        if (ownerPlayer != null && localPlayer != null)
        {
            Vector3 targetHeadPosition;
            Vector3 targetOriginPosition;

            if (ownerPlayer == localPlayer)
            {
                targetHeadPosition = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
                targetOriginPosition = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin).position;
            }
            else
            {
                targetHeadPosition = ownerPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
                targetOriginPosition = ownerPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin).position;
            }

            Vector3 targetHeight = targetHeadPosition - targetOriginPosition;
            Vector3 newPosition = targetHeadPosition + new Vector3(0, targetHeight.y * 0.15f, 0);

            transform.position = newPosition;

        }
        else
        {
            Debug.LogError("Owner or local player is null");
        }

        //set the rotation of the object to always look at the local player's head if the owner of the object is not the local player
        if (ownerPlayer != null && localPlayer != null && ownerPlayer != localPlayer)
        {
            Vector3 lookAtPosition = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
            transform.LookAt(lookAtPosition);
        }
        else
        {
            Debug.LogError("Owner or local player is null");
        }

    }
}

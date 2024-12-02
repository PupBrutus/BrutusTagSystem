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

        //set the position of this gameobject as 110% of the owner's height above the owner's head
        if (ownerPlayer != null && localPlayer != null)
        {
            Vector3 ownerHeadPosition = ownerPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
            Vector3 ownerOriginPosition = ownerPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin).position;
            Vector3 ownerHeight = ownerHeadPosition - ownerOriginPosition;
            Vector3 newPosition = ownerHeadPosition + new Vector3(0, ownerHeight.y * 0.1f, 0);
            transform.position = newPosition;
        }
        //set the rotation of the object to always look at the local player's head if the owner of the object is not the local player
        if (ownerPlayer != null && localPlayer != null && ownerPlayer != localPlayer)
        {
            Vector3 lookAtPosition = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
            transform.LookAt(lookAtPosition);
        }
        //if the local player is the owner, match the owner's head rotation
        else if (ownerPlayer == localPlayer)
        {
            Vector3 ownerHeadPosition = ownerPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
            //transform.LookAt(ownerHeadPosition);
        }
        //else error
        else
        {
            Debug.LogError("Owner or local player is null");
        }

    }
}

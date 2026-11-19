using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

// Optional zone filter for Brutus Tag System. Put this on a PlayerObject template (an object with a
// VRCPlayerObject component, no persistence needed) and drag that object into btsTagDisplay's zoneTemplate.
// Each player's copy reports which zone of the btsZoneMap they're in; tags of players in another zone are hidden.
// Only the owner writes zoneId, and only when it changes, so the network cost is a byte per zone change.
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class btsPlayerZone : UdonSharpBehaviour
{
    // btsTagDisplay reads this by name (its zoneVariableName setting, "zoneId" by default). 0 = unknown/everywhere.
    [UdonSynced] public byte zoneId;

    public btsZoneMap zoneMap;
    [Tooltip("Seconds between the owner's zone checks")]
    public float checkInterval = 0.5f;

    void Start()
    {
        if (!Networking.IsOwner(gameObject) || !Utilities.IsValid(zoneMap)) return;
        // Random start so players' checks don't all land on the same frame
        SendCustomEventDelayedSeconds(nameof(_CheckZone), Random.Range(0f, checkInterval));
    }

    public void _CheckZone()
    {
        if (!Networking.IsOwner(gameObject)) return;

        byte zone = zoneMap._ZoneAt(Networking.LocalPlayer.GetPosition());
        if (zone != zoneId)
        {
            zoneId = zone;
            RequestSerialization();
        }

        SendCustomEventDelayedSeconds(nameof(_CheckZone), checkInterval);
    }
}

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

// Optional zone filter for Brutus Tag System: your world's broad areas (rooms, floors, outside...) as marker points.
// A position belongs to the zone of its nearest marker. Local only, nothing here is synced.
// Zone ids are numbers you choose, 1-255; 0 means unknown and matches every zone (so tags still show by distance).
// A zone can have several markers: add one per room-sized area and give them the same id.
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class btsZoneMap : UdonSharpBehaviour
{
    public Transform[] markers;
    [Tooltip("Zone id for each marker, same order as Markers")]
    public int[] markerZone;
    [Tooltip("Farther than this from every marker counts as unknown (0)")]
    public float maxMarkerDistance = 80f;

    public byte _ZoneAt(Vector3 position)
    {
        float bestSq = maxMarkerDistance * maxMarkerDistance;
        int best = 0;
        int count = Mathf.Min(markers.Length, markerZone.Length);
        for (int i = 0; i < count; i++)
        {
            if (!Utilities.IsValid(markers[i])) continue;
            float sq = (markers[i].position - position).sqrMagnitude;
            if (sq < bestSq)
            {
                bestSq = sq;
                best = markerZone[i];
            }
        }
        return (byte)Mathf.Clamp(best, 0, 255);
    }
}

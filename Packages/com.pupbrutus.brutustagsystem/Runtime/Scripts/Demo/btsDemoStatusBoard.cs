using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

// Demo scene only: a sign that shows what Brutus Tag System is doing for you, so you can check the zone filter and the
// distance tiers while testing. Read-only: it never changes tags, zones or sync. Not needed in your world.
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class btsDemoStatusBoard : UdonSharpBehaviour
{
    public TextMeshPro text;
    [Tooltip("The btsTagDisplay template (BTS/btsTagDisplay)")]
    public btsTagDisplay tagDisplayTemplate;
    [Tooltip("Optional: the btsPlayerZone template, to show each player's zone")]
    public btsPlayerZone zoneTemplate;
    [Tooltip("Optional: the tag mirror object, to show whether your own tag is visible")]
    public GameObject tagMirror;
    public float refreshInterval = 0.5f;

    private VRCPlayerApi[] _players = new VRCPlayerApi[100];

    void Start()
    {
        SendCustomEventDelayedSeconds(nameof(_Refresh), 1f);
    }

    public void _Refresh()
    {
        SendCustomEventDelayedSeconds(nameof(_Refresh), refreshInterval);
        VRCPlayerApi local = Networking.LocalPlayer;
        if (!Utilities.IsValid(local) || !Utilities.IsValid(text)) return;

        int localZone = _ZoneOf(local);
        string s = "<b>Brutus Tag System test</b>\n";
        s += "You are in zone " + _ZoneName(localZone);
        if (Utilities.IsValid(tagMirror)) s += "\nYour own tag: " + (tagMirror.activeInHierarchy ? "shown (tag mirror on)" : "hidden (tag mirror off)");
        s += "\n\n<b>Other players' tags</b>\n";

        int count = VRCPlayerApi.GetPlayerCount();
        if (_players.Length < count) _players = new VRCPlayerApi[count];
        VRCPlayerApi.GetPlayers(_players);
        int others = 0;
        for (int i = 0; i < count; i++)
        {
            VRCPlayerApi player = _players[i];
            if (!Utilities.IsValid(player) || player.isLocal) continue;
            others++;
            float distance = Vector3.Distance(player.GetPosition(), local.GetPosition());
            int zone = _ZoneOf(player);
            s += player.displayName + ": " + distance.ToString("F0") + " m, zone " + _ZoneName(zone) + ", " + _TagState(player, distance, zone, localZone) + "\n";
        }
        if (others == 0) s += "No one else here yet. Use Build & Test with 2 or more clients.";
        text.text = s;
    }

    private string _TagState(VRCPlayerApi player, float distance, int zone, int localZone)
    {
        if (!Utilities.IsValid(tagDisplayTemplate)) return "?";
        Component found = Networking.FindComponentInPlayerObjects(player, tagDisplayTemplate);
        if (!Utilities.IsValid(found)) return "no tag object yet";
        UdonBehaviour display = (UdonBehaviour)found;

        if (!(bool)display.GetProgramVariable("_hasTags")) return "no tags picked";
        if (!(bool)display.GetProgramVariable("_visible"))
        {
            if (zone > 0 && localZone > 0 && zone != localZone) return "<color=#ff8080>hidden: other zone</color>";
            if (distance > (float)display.GetProgramVariable("maxTagDistance")) return "<color=#ff8080>hidden: out of range</color>";
            return "<color=#ff8080>hidden</color>";
        }
        int tier = (int)display.GetProgramVariable("_tier");
        if (tier <= 1) return "<color=#80ff80>moves every frame</color>";
        if (tier == 2) return "<color=#ffff80>moves every 2nd frame</color>";
        return "<color=#ffc080>moves every 3rd frame</color>";
    }

    // Zone id from the player's btsPlayerZone, or -1 when there is no zone filter or it hasn't spawned yet
    private int _ZoneOf(VRCPlayerApi player)
    {
        if (!Utilities.IsValid(zoneTemplate)) return -1;
        Component found = Networking.FindComponentInPlayerObjects(player, zoneTemplate);
        if (!Utilities.IsValid(found)) return -1;
        return (byte)((UdonBehaviour)found).GetProgramVariable("zoneId");
    }

    private string _ZoneName(int zone)
    {
        if (zone < 0) return "-";
        if (zone == 0) return "0 (none)";
        return zone.ToString();
    }
}

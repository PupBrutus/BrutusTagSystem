using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class btsUiManager : UdonSharpBehaviour
{
    public UdonBehaviour localPlayerTagDisplay;

    public int maxTags = 5;

    void Start()
    {
        //Debug.Log("btsUiManager Start: Initializing...");
        findLocalPlayerTagDisplay();
    }

    // Called by the Tag Mirror toggle: your own tag only shows while the tag mirror is on, so re-check it now
    public void _OnTagMirrorChanged()
    {
        UdonBehaviour display = _GetLocalTagDisplay();
        if (display != null) display.SendCustomEvent("_RecheckVisibility");
    }

    // Player objects can spawn after Start, so callers fetch through here
    public UdonBehaviour _GetLocalTagDisplay()
    {
        if (localPlayerTagDisplay == null) findLocalPlayerTagDisplay();
        return localPlayerTagDisplay;
    }

    private void findLocalPlayerTagDisplay()
    {
        //Debug.Log("findLocalPlayerTagDisplay: Searching for local player tag display...");
        var objects = Networking.GetPlayerObjects(Networking.LocalPlayer);

        if (objects == null || objects.Length == 0)
        {
            Debug.LogError("findLocalPlayerTagDisplay: No objects found for local player.");
            return;
        }

        foreach (var obj in objects)
        {
            //Debug.Log($"findLocalPlayerTagDisplay: Checking object {obj.name}...");
            if (obj.name.StartsWith("btsTagDisplay"))
            {  
                localPlayerTagDisplay = (UdonBehaviour)obj.GetComponent<UdonBehaviour>();
                if (localPlayerTagDisplay != null)
                {
                    //Debug.Log("findLocalPlayerTagDisplay: Successfully found and assigned localPlayerTagDisplay.");
                }
                else
                {
                    Debug.LogError("findLocalPlayerTagDisplay: Found btsTagDisplay but failed to get UdonBehaviour component.");
                }
                break;
            }
        }

        if (localPlayerTagDisplay == null)
        {
            Debug.LogError("findLocalPlayerTagDisplay: Failed to find TagDisplay object for localPlayer.");
        }
    }

}

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

public class btsSlider : UdonSharpBehaviour
{
    public Slider btsHeightSlider;
    public btsUiManager btsUiManager;
    public UdonBehaviour btsTagDisplay;

    void Start()
    {
        //Debug.Log("btsSlider script started");
        if (btsHeightSlider != null)
        {
           //Debug.Log("btsHeightSlider is assigned");
        }
        else
        {
            Debug.LogError("btsHeightSlider is not assigned");
        }
    }

    public override void OnPlayerRestored(VRCPlayerApi player)
    {
        if (player.isLocal)
        {
            btsTagDisplay = btsUiManager.localPlayerTagDisplay;
            float initialValue = (float)btsTagDisplay.GetProgramVariable("_playerHeightOffset");
            btsHeightSlider.SetValueWithoutNotify(initialValue);
        }
        
    }

    public void sliderUpdate()
    {
        //Debug.Log("sliderUpdate method called");
        
        btsTagDisplay = btsUiManager.localPlayerTagDisplay;
        //Debug.Log("btsTagDisplay object found: " + (btsTagDisplay != null));

        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        float sliderValue = btsHeightSlider.value;
        //Debug.Log("Slider value: " + sliderValue);
        
        btsTagDisplay.SetProgramVariable("_playerHeightOffset", sliderValue);
        //Debug.Log("_playerHeightOffset set to: " + sliderValue);
    }
}

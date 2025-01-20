using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Data;
using System.Collections.Generic;
using VRC.Udon.Common.Interfaces;
public class btsTagButton : UdonSharpBehaviour
{

    public btsUiManager btsUiManager;

    public GameObject enabledIndicator;
    public GameObject MaxTagsMessage;
    private bool buttonActive = true;
    private UdonBehaviour btsTagDisplay;
    private string _tempPlayerTagString;
    private DataList _userTagList;
    private DataToken _tempToken;

    private int maxTags;

    void start()
    {

    }

    private bool TagCheck()
    {
        return _userTagList.Contains(gameObject.name);
    }

    private void MaxTagsCheck()
    {
        maxTags = btsUiManager.maxTags;

        Debug.Log("Tag count: " + _userTagList.Count + " Max tags: " + maxTags);

        if (_userTagList.Count >= maxTags && !TagCheck())
        {
            buttonActive = false;
            MaxTagsMessage.SetActive(true);
            Debug.Log("Max tags reached");
        }
        else if (_userTagList.Count >= maxTags && TagCheck())
        {
            buttonActive = true;
            MaxTagsMessage.SetActive(true);
            Debug.Log("Max tags reached, but tag is already added");
        }
        else
        {
            buttonActive = true;
            MaxTagsMessage.SetActive(false);
            Debug.Log("Max tags not reached");
        }
    }

    public override void OnPlayerRestored(VRCPlayerApi player)
    {
        if (player.isLocal)
        {
            btsTagDisplay = btsUiManager.localPlayerTagDisplay;
            deserializePlayerData();
            if (TagCheck())
            {
                enabledIndicator.SetActive(true);
            }
            else
            {
                enabledIndicator.SetActive(false);
            }
        }
    }

    public void tagUpdate()
    {
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        deserializePlayerData();
        if (TagCheck() && buttonActive)
        {
            _userTagList.Remove(gameObject.name);
            enabledIndicator.SetActive(false);
            Debug.Log("Removed " + gameObject.name + " from _userTagList");

        }
        else
        {
            if (buttonActive)
            {
                _userTagList.Add(gameObject.name);
                enabledIndicator.SetActive(true);
                Debug.Log("Added " + gameObject.name + " to _userTagList");
            }
            else
            {
                Debug.Log("Max tags reached, cannot add " + gameObject.name);
            }
        }
        seralizePlayerData();
        deserializePlayerData();
    }

    private void seralizePlayerData()
    {
        if (VRCJson.TrySerializeToJson(_userTagList, JsonExportType.Minify, out DataToken result))
        {
            _tempPlayerTagString = result.String;
            btsTagDisplay.SetProgramVariable("_playerTagString", _tempPlayerTagString); // Correct variable name
            btsTagDisplay.SendCustomEvent("deserializePlayerData"); // Ensure the display updates

            Debug.Log("Set playerTagString " + _tempPlayerTagString);
        }
        else
        {
            Debug.LogError("Failed to serialize to JSON: " + result.ToString());
        }
    }


    private void deserializePlayerData() 
    {  
        btsTagDisplay = btsUiManager.localPlayerTagDisplay;

        Debug.Log("Deserializing player data from" + btsTagDisplay.name);

        _tempPlayerTagString = (string)btsTagDisplay.GetProgramVariable("_playerTagString");

        if (VRCJson.TryDeserializeFromJson(_tempPlayerTagString, out _tempToken))
        {
            if (_tempToken.IsNull)
            {
                Debug.LogError("Failed to deserialize _playerTagString _tempToken null, creating new _userTagList and syncing");
                _userTagList = new DataList();
                MaxTagsCheck();
                return;
            }
            else if (_tempToken.TokenType == TokenType.DataList)
            {
                _userTagList = _tempToken.DataList;
                Debug.Log("Deserialized _playerTagString");
                MaxTagsCheck();
                return;
            }
            else
            {
                Debug.LogError("Failed to deserialize _playerTagString - not DataList, creating new _userTagList and syncing");
                _userTagList = new DataList();
                MaxTagsCheck();
                return;

            }
        }
        else
        {
            Debug.LogError("Failed to deserialize _playerTagString into JSON, creating new _userTagList and syncing");
            _userTagList = new DataList();
            MaxTagsCheck();
            return;
        }



    }
}

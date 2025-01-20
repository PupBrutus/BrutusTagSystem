using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Data;
using System.Collections.Generic;
using VRC.Udon.Common.Interfaces;
using VRC.SDK3.Persistence;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class btsTagDisplay : UdonSharpBehaviour
{
    [UdonSynced, FieldChangeCallback(nameof(playerTagString))] public string _playerTagString; 
    [UdonSynced, FieldChangeCallback(nameof(playerHeightOffset))] public float _playerHeightOffset = 0.15f;

    public string playerTagString
    {
        get => _playerTagString; 
        set
        {
            if (_playerTagString != value)
            {
                Debug.Log("playerTagString changed to: " + value);
                _playerTagString = value;
                deserializePlayerData();
            }
        }   
    }

    public float playerHeightOffset
    {
        get => _playerHeightOffset;
        set
        {
            if (_playerHeightOffset != value)
            {
                Debug.Log("playerHeightOffset changed to: " + value);
                _playerHeightOffset = value;
                RequestSerialization();
            }
        }
    }

    

    public Vector3 newPosition;

    public GameObject[] _tagTemplates;

    public GameObject LayoutPanel;

    private DataToken _tempToken;
    private DataList _userTagList;

    private GameObject[] _activeTags;

    public bool active = false;

    private VRCPlayerApi localPlayer;

    private VRCPlayerApi ownerPlayer;

    private float localPlayerViewOffset;


    void Start()
    {
        gameObject.SetActive(true);
        playerTagString = "[]";
        ownerPlayer = Networking.GetOwner(gameObject);
        localPlayer = Networking.LocalPlayer;

        Vector3 LocalHeadPosition = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
        Vector3 LocalOriginPosition = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin).position;
        Vector3 LocalHeight = LocalHeadPosition - LocalOriginPosition;
        
        localPlayerViewOffset = LocalHeight.y/10;
        Debug.Log("localPlayerViewOffset: " + localPlayerViewOffset);

    }

    public void deserializePlayerData() 
    { 
        ownerPlayer = Networking.GetOwner(gameObject);
        if (VRCJson.TryDeserializeFromJson(_playerTagString, out _tempToken))
        {
            if (_tempToken.IsNull)
            {
                Debug.LogError("Failed to deserialize playerTagString _tempToken=null, creating new _userTagList");
                active = false;
                ResetTagList();
                return;
            }
            else if (_tempToken.TokenType == TokenType.DataList)
            {
                _userTagList = _tempToken.DataList;
                Debug.Log("Deserialized playerTagString: " + _playerTagString);
                active = true;
                TagUpdate();
                return;
            }
            else
            {
                Debug.LogError("Failed to deserialize playerTagString - not DataList: " + _playerTagString + ", creating new _userTagList");
                active = false;
                ResetTagList();
                return;
            }
        }
        else
        {
            Debug.LogError("Failed to deserialize playerTagString into JSON: " + _playerTagString + ", creating new _userTagList");
            active = false;
            ResetTagList();
            return;
        }
    }

    private void ResetTagList()
    {
        _userTagList = new DataList();
        Debug.Log("Created new _userTagList");

        if (VRCJson.TrySerializeToJson(_userTagList, JsonExportType.Minify, out DataToken result))
        {
            _playerTagString = result.String;
            Debug.Log("Serialized blank  playerTagString");
        }
        else
        {
            Debug.LogError("Failed to serialize to JSON: " + result.ToString());
        }
        TagUpdate();
    }

    private void TagUpdate()
    {
        Debug.Log("Running TagUpdate");

        // Destroy current active tags
        if (_activeTags != null)
        {
            foreach (GameObject tag in _activeTags)
            {
                if (tag != null)
                {
                    Destroy(tag);
                }
            }

            _activeTags = null;
        }

        // Reset the _activeTags array
        _activeTags = new GameObject[_userTagList.Count];

        for (int i = 0; i < _userTagList.Count; i++)
        {
            DataToken tagName = _userTagList[i];
            int intTagName;
            if (int.TryParse(tagName.String, out intTagName) && intTagName >= 0 && intTagName < _tagTemplates.Length)
            {
                GameObject tagObject = _tagTemplates[intTagName];
                GameObject newTagObject = Instantiate(tagObject, LayoutPanel.transform);
                newTagObject.name = tagName.String;
                newTagObject.SetActive(true);
                _activeTags[i] = newTagObject;
                Debug.Log("Instantiated and activated tag: " + tagName.String);
            }
            else
            {
                Debug.LogError("Invalid tag index: " + tagName.String);
                continue;
            }
        }

        RequestSerialization();
    }

    public override void PostLateUpdate()
    {    
        if (active)
        {
            

            Vector3 targetHeadPosition;
            Vector3 targetOriginPosition;

            // Use the same reference point for both local and remote players
            targetHeadPosition = ownerPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
            //Debug.Log(ownerPlayer.displayName + " targetHeadPosition: " + targetHeadPosition);
            targetOriginPosition = ownerPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin).position;
            //Debug.Log(ownerPlayer.displayName + " targetOriginPosition: " + targetOriginPosition);

            Vector3 targetHeight = targetHeadPosition - targetOriginPosition;

            float offset = targetHeight.y * _playerHeightOffset;

            if (ownerPlayer != localPlayer)
            {
                //Debug.Log("ownerPlayer != localPlayer");
                offset = offset+localPlayerViewOffset;
            }

            newPosition = targetHeadPosition + new Vector3(0, offset, 0);
            //Debug.Log("newPosition: " + newPosition);
            transform.position = newPosition;

            if (ownerPlayer != localPlayer)
            {
                Vector3 lookAtPosition = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;  
                transform.LookAt(lookAtPosition);
            }

        }
    }
}

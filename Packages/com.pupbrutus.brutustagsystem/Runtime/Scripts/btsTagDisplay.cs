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
                //Debug.Log("playerTagString changed to: " + value);
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
                //Debug.Log("playerHeightOffset changed to: " + value);
                _playerHeightOffset = value;
                // Never push before persistence has restored, or the default overwrites the saved value
                if (restored && Networking.IsOwner(gameObject)) RequestSerialization();
            }
        }
    }

    

    public Vector3 newPosition;

    // Baked mesh tags, by tag id (BtsTagMeshBaker); one shared atlas and material
    public GameObject[] _tagTemplates;
    // Height of each tag in panel units, by tag id, and the gap under the bottom tag (baked with the templates)
    public float[] _tagHeights;
    public float stackBottomPadding = 250f;

    public GameObject LayoutPanel;

    private DataToken _tempToken;
    private DataList _userTagList;

    private GameObject[] _activeTags;

    public bool active = false;

    private VRCPlayerApi localPlayer;

    private VRCPlayerApi ownerPlayer;

    private float localPlayerViewOffset;

    // True once VRChat has restored the owner's persisted data into this object
    [System.NonSerialized] public bool restored = false;

    // Display only: which tags are drawn and how often they move. Never synced, never writes tag data.
    [Header("Visibility")]
    [Tooltip("Optional. A per-player object template (PlayerObject) whose script holds the player's zone as a synced byte. " +
             "Tags of players in another zone are hidden; zone 0 means everywhere. Leave empty to use distance only.")]
    public UdonSharpBehaviour zoneTemplate;
    [Tooltip("Name of the byte variable on the zone script that holds the zone id")]
    public string zoneVariableName = "zoneId";
    public float maxTagDistance = 60f;
    [Tooltip("Within this distance a tag moves every frame")]
    public float nearDistance = 15f;
    [Tooltip("Within this distance every 2nd frame, beyond it every 3rd")]
    public float midDistance = 30f;
    public float visibilityCheckInterval = 0.5f;
    [Tooltip("The tag mirror's mirror object. Your own tag only shows (and only moves) while it is on; you can't see it above your head otherwise.")]
    public GameObject tagMirror;

    private bool _hasTags;
    private bool _visible = true;
    private int _tier = 1;
    private int _frameOffset;
    private float _nextVisibilityCheck;
    private UdonBehaviour _ownerZone;
    private UdonBehaviour _localZone;


    void Start()
    {
        gameObject.SetActive(true);
        ownerPlayer = Networking.GetOwner(gameObject);
        localPlayer = Networking.LocalPlayer;

        Vector3 LocalHeadPosition = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
        Vector3 LocalOriginPosition = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin).position;
        Vector3 LocalHeight = LocalHeadPosition - LocalOriginPosition;
        
        localPlayerViewOffset = LocalHeight.y/10;
        //Debug.Log("localPlayerViewOffset: " + localPlayerViewOffset);

        _visible = !Utilities.IsValid(LayoutPanel) || LayoutPanel.activeSelf;
        // Spread tags across frames and check times so they don't all do their work together
        _frameOffset = Random.Range(0, 6);
        _nextVisibilityCheck = Time.time + Random.Range(0f, visibilityCheckInterval);
    }

    public override void OnPlayerRestored(VRCPlayerApi player)
    {
        if (!player.isLocal || !Networking.IsOwner(gameObject)) return;
        restored = true;
        deserializePlayerData();
    }

    public void deserializePlayerData() 
    { 
        ownerPlayer = Networking.GetOwner(gameObject);
        // Nothing synced or restored yet; show no tags
        string tagJson = string.IsNullOrEmpty(_playerTagString) ? "[]" : _playerTagString;
        if (VRCJson.TryDeserializeFromJson(tagJson, out _tempToken))
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
                //Debug.Log("Deserialized playerTagString: " + _playerTagString);
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
        // Display-only reset: leave _playerTagString alone so a bad read never overwrites saved tags
        _userTagList = new DataList();
        TagUpdate();
    }

    private void TagUpdate()
    {
        //Debug.Log("Running TagUpdate");

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
        int[] tagIds = new int[_userTagList.Count];
        int builtCount = 0;

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
                tagIds[i] = intTagName;
                builtCount++;
                //Debug.Log("Instantiated and activated tag: " + tagName.String);
            }
            else
            {
                Debug.LogError("Invalid tag index: " + tagName.String);
                continue;
            }
        }

        // Stack the tags in the order they were picked: first picked on top, last just above the bottom gap.
        // Same placement the layout group gave the UI tags, worked out once here instead of by a Canvas.
        float stackY = stackBottomPadding;
        for (int i = _activeTags.Length - 1; i >= 0; i--)
        {
            if (_activeTags[i] == null) continue;
            float height = _tagHeights[tagIds[i]];
            _activeTags[i].transform.localPosition = new Vector3(0f, stackY + height * 0.5f, 0f);
            stackY += height;
        }

        _hasTags = builtCount > 0;
        // Re-decide visibility on the next frame instead of waiting for the next check
        _nextVisibilityCheck = 0f;

        if (restored && Networking.IsOwner(gameObject)) RequestSerialization();
    }

    public override void PostLateUpdate()
    {
        // Nothing to draw: no valid tag data, or an empty tag list
        if (!active || !_hasTags)
        {
            if (_visible) _SetVisible(false);
            return;
        }

        if (Time.time >= _nextVisibilityCheck)
        {
            _nextVisibilityCheck = Time.time + visibilityCheckInterval;
            bool show = _DecideVisibility();
            if (show != _visible)
            {
                // Place the tag before it appears so it never flashes at an old spot
                if (show) _PositionTag();
                _SetVisible(show);
                return;
            }
        }

        if (!_visible) return;
        if (_tier > 1 && (Time.frameCount + _frameOffset) % _tier != 0) return;
        _PositionTag();
    }

    // Called when the tag mirror is switched, so your own tag shows or hides right away instead of at its next check
    public void _RecheckVisibility()
    {
        _nextVisibilityCheck = 0f;
    }

    // Same zone, within range, and the update tier for this tag. Your own tag shows only while the tag mirror is on.
    private bool _DecideVisibility()
    {
        if (!Utilities.IsValid(ownerPlayer)) return false;
        if (ownerPlayer == localPlayer)
        {
            _tier = 1;
            return !Utilities.IsValid(tagMirror) || tagMirror.activeInHierarchy;
        }
        if (!_IsSameZone()) return false;

        float distanceSq = (ownerPlayer.GetPosition() - localPlayer.GetPosition()).sqrMagnitude;
        if (distanceSq > maxTagDistance * maxTagDistance) return false;

        if (distanceSq <= nearDistance * nearDistance) _tier = 1;
        else if (distanceSq <= midDistance * midDistance) _tier = 2;
        else _tier = 3;
        return true;
    }

    // Fails open: if either zone is unknown or not found yet, treat it as the same zone and let distance decide
    private bool _IsSameZone()
    {
        if (!Utilities.IsValid(zoneTemplate)) return true;
        // Per-player objects can appear after Start and vanish without warning, so look up again until found
        if (!Utilities.IsValid(_ownerZone)) _ownerZone = _FindZone(ownerPlayer);
        if (!Utilities.IsValid(_localZone)) _localZone = _FindZone(localPlayer);
        if (!Utilities.IsValid(_ownerZone) || !Utilities.IsValid(_localZone)) return true;

        byte ownerZoneId = (byte)_ownerZone.GetProgramVariable(zoneVariableName);
        byte localZoneId = (byte)_localZone.GetProgramVariable(zoneVariableName);
        return ownerZoneId == 0 || localZoneId == 0 || ownerZoneId == localZoneId;
    }

    private UdonBehaviour _FindZone(VRCPlayerApi player)
    {
        Component found = Networking.FindComponentInPlayerObjects(player, zoneTemplate);
        if (!Utilities.IsValid(found)) return null;
        return (UdonBehaviour)found;
    }

    // Hides only the tag panel (the spawned tags' parent): this object and script stay active so tag updates keep arriving
    private void _SetVisible(bool visible)
    {
        _visible = visible;
        if (Utilities.IsValid(LayoutPanel)) LayoutPanel.SetActive(visible);
    }

    // The original per-frame positioning, unchanged (inner block kept so the lines stay as they were)
    private void _PositionTag()
    {
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
            else if (Utilities.IsValid(tagMirror))
            {
                // Your own tag only shows while the tag mirror is on: face it into the room, parallel to the mirror,
                // so the mirror sees it face-on (and, the tag being double-sided, it reads the right way round there)
                transform.rotation = Quaternion.LookRotation(-tagMirror.transform.forward, Vector3.up);
            }

        }
    }
}

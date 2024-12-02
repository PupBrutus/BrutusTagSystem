using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Data;
using System.Collections.Generic;
using VRC.Udon.Common.Interfaces;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class TagButton : UdonSharpBehaviour
{
    public GameObject enabledIndicator;
    public GameObject PlayerObjectAssigner;
    [UdonSynced] public string activeUsersJson;
    private DataList activeUsers;
    private int tagIndex;



    void Start()
    {
        
        if (activeUsers == null || activeUsers.Count == 0)
        {
            activeUsers = new DataList();
        }
        if (!int.TryParse(gameObject.name, out tagIndex))
        {
            Debug.LogWarning("GameObject name is not a valid integer");
        }

        //Debug.Log("TagButton Start called on GameObject: " + gameObject.name);
    }

    public void tagUpdate()
    {
        Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
        string userName = Networking.LocalPlayer.displayName;

        if (activeUsers.Contains(userName))
        {
            Debug.Log("Removing user: " + userName + " from activeUsers on GameObject: " + gameObject.name);
            activeUsers.Remove(userName);
            enabledIndicator.SetActive(false);
        }
        else
        {
            Debug.Log("Adding user: " + userName + " from activeUsers on GameObject: " + gameObject.name);
            activeUsers.Add(userName);
            enabledIndicator.SetActive(true);
        }
        sendEvent();
    }

    private void serializeJSON()
    {
        //Debug.Log("serializeJSON called on GameObject: " + gameObject.name);
        if (VRCJson.TrySerializeToJson(activeUsers, JsonExportType.Minify, out DataToken result))
        {
            activeUsersJson = result.String;
            Debug.Log("Serialized activeUsersJson: " + activeUsersJson);
        }
        else
        {
            Debug.LogError(result.ToString());
        }
    }

    private void sendEvent()
    {
        //Debug.Log("sendEvent called on GameObject: " + gameObject.name);
        serializeJSON();
        RequestSerialization();
        CallUpdate();
        Debug.Log("sendEvent completed on GameObject: " + gameObject.name);
    }

    private void CallUpdate()
    {
        //Debug.Log("CallUpdate called on GameObject: " + gameObject.name);
        if (tagIndex >= 0)
        {
            Transform[] PoolObjects = PlayerObjectAssigner.GetComponentsInChildren<Transform>();
            foreach (Transform obj in PoolObjects)
            {
                if (obj.gameObject.activeSelf)
                {
                    //Debug.Log("Checking object: " + obj.gameObject.name);
                    TagDisplay tagDisplay = obj.GetComponent<TagDisplay>();
                    if (tagDisplay != null)
                    {
                        Debug.Log("Updating TagDisplay with tagIndex: " + tagIndex + " - on object: " + obj.gameObject.name);
                        tagDisplay.TagUpdate(tagIndex);
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("Tag index is not valid");
        }
    }

    public override void OnDeserialization()
    {
        //Debug.Log("OnDeserialization called on GameObject: " + gameObject.name);
        if (VRCJson.TryDeserializeFromJson(activeUsersJson, out DataToken result))
        {
            activeUsers = result.DataList;
            Debug.Log("Deserialized activeUsers: " + activeUsers.ToString());
            CallUpdate();
        }
        else
        {
            Debug.LogError(result.ToString());
        }

        if (activeUsers == null)
        {
            activeUsers = new DataList();
        }


    }

    public override void OnPlayerLeft(VRCPlayerApi leavingPlayer)
    {
        //Debug.Log("OnPlayerLeft called for player: " + leavingPlayer.displayName + " on GameObject: " + gameObject.name);
        if (Networking.IsMaster)
        {
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            string userName = leavingPlayer.displayName;
            if (activeUsers.Contains(userName))
            {
                Debug.Log("Removing leaving player: " + userName + " on GameObject: " + gameObject.name);
                activeUsers.Remove(userName);
                sendEvent();
            }
        }
    }



}

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Data;
using System.Collections.Generic;
using VRC.Udon.Common.Interfaces;

//Purpose: This script is attached to the TagButton prefab and is used to manage the tag button interactivity, manage which players have the tag selected and data serialization for the Brutus Tag System 2.0

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
        //Instantiate activeUsers if it is null        
        if (activeUsers == null || activeUsers.Count == 0)
        {
            activeUsers = new DataList();
        }

        //Confirm tag button gameobject has valid integer name
        if (!int.TryParse(gameObject.name, out tagIndex))
        {
            Debug.LogWarning("GameObject name is not a valid integer");
        }

        //Debug.Log("TagButton Start called on GameObject: " + gameObject.name);
    }


    //Set ownership to allow interactivity and manage internal DataList variable
    public void tagUpdate()
    {
        
        Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
        
        //set username to local player's display name for DataList
        string userName = Networking.LocalPlayer.displayName;

        if (activeUsers.Contains(userName))
        {
            //remove if present
            Debug.Log("Removing user: " + userName + " from activeUsers on GameObject: " + gameObject.name);
            activeUsers.Remove(userName);
            enabledIndicator.SetActive(false);
        }
        else
        {
            //add if not present
            Debug.Log("Adding user: " + userName + " from activeUsers on GameObject: " + gameObject.name);
            activeUsers.Add(userName);
            enabledIndicator.SetActive(true);
        }
        sendEvent();
    }

    //Convert activeUsers to JSON string for serialization
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

    //Send event to update activeUsersJson and downstream Tag Displays
    private void sendEvent()
    {
        //Debug.Log("sendEvent called on GameObject: " + gameObject.name);
        serializeJSON();
        RequestSerialization();
        CallUpdate();
        Debug.Log("sendEvent completed on GameObject: " + gameObject.name);
    }


    //Tag update calls on all TagDisplay objects in the PlayerObjectAssigner object
    private void CallUpdate()
    {
        

        //Debug.Log("CallUpdate called on GameObject: " + gameObject.name);

        //confirm tagIndex is valid integer > 0 - if not, log error
        if (tagIndex >= 0)
        {
            Transform[] PoolObjects = PlayerObjectAssigner.GetComponentsInChildren<Transform>();
            foreach (Transform obj in PoolObjects)
            {
                // Check if object is active and only act on active objects
                if (obj.gameObject.activeSelf)
                {
                    //Gather tagDisplay UdonBehaviour from objects, if present - if not, log error

                    //Debug.Log("Checking object: " + obj.gameObject.name);
                    TagDisplay tagDisplay = obj.GetComponent<TagDisplay>();
                    if (tagDisplay != null)
                    {
                        Debug.Log("Updating TagDisplay with tagIndex: " + tagIndex + " - on object: " + obj.gameObject.name);
                        tagDisplay.TagUpdate(tagIndex);
                    }
                    else
                    {
                        Debug.LogError("TagDisplay UdonBehavior not found on object: " + obj.gameObject.name);
                    }
                }
            }
        }
        else
        {
            Debug.LogError("Tag index is not valid, found tagIndex: " + tagIndex);
        }
    }


    //Recieve and deserialize activeUsersJson from remote players into activeUsers DataList
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

    //OnPlayerLeft return ownership to world master and remove player from activeUsers if present then update.
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

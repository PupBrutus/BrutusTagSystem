using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Data;
using System.Collections.Generic;
using Newtonsoft.Json;

//Purpose: This script is attached to the TagDisplay prefab and is used to manage the update the tag display based on the activeUsersJson data from the TagButtons

public class TagDisplay : UdonSharpBehaviour
{
    public VRCPlayerApi Owner;
    //public UdonBehaviour UIManager;
    public GameObject[] TagObjects;
    public GameObject[] TagButtons;

    void Start()
    {
        //Debug.Log($"TagDisplay script started on {gameObject.name}");
    }
    
    //OnEnable logic disables all tags for new player to ensure cleanup from previous player if TagDisplay has been recycled (the only time a game object should transition from disabled to enabled). Then perform TagUpdate for each tag in the tag array to ensure the latest tag state for the player is displayed.

    void OnEnable()
    {
        Debug.Log($"OnEnable called on {gameObject.name}");
        DisableAllTags();
        for (int i = 0; i < TagButtons.Length; i++)
        {
            TagUpdate(i + 1);
        }
    }

    //_OnOwnerSet logic is required for CyanTriggerObjectPool objects. Disable all tags occurs here to again ensure clean-up from previous users (should be redundant). Then set the owner of the TagDisplay and all child objects to the new owner. 
    public void _OnOwnerSet()
    {
        //Debug.Log($"_OnOwnerSet called on {gameObject.name}");
        DisableAllTags();
        if (Owner != null)
        {
            //Debug.Log($"Owner set to {Owner.displayName} on {gameObject.name}");
            Networking.SetOwner(Owner, gameObject);
            foreach (Transform child in transform)
            {
                Networking.SetOwner(Owner, child.gameObject);
            }
        }
        else
        {
            Debug.LogWarning($"Owner is null on {gameObject.name}");
        }

    }

    //OnCleanup logic is required for CyanTriggerObjectPool objects. Disable all tags occurs here to ensure clean-up from previous users however due to race conditions with this object becoming disabled prior to cleanup, cleanup code is also added to the onEnable function and the _OnOwnerSet function.
    public void _OnCleanup()
    {
        Debug.Log($"_OnCleanup called on {gameObject.name}");
        DisableAllTags();
    }

    //TagUpdate logic is called by the TagButton script when the activeUsersJson data is updated. This function will deserialize the activeUsersJson for the tagIndex at the provided int valuea and check if the owner is in the activeUsers list. If the owner is in the activeUsers list, the tag is enabled, otherwise the tag is disabled.

    public void TagUpdate(int tagIndex)
    {
        Debug.Log($"TagUpdate called with tagIndex: {tagIndex} on {gameObject.name}");

        //Check if tagIndex is valid and within the bounds of the TagButtons array
        if (tagIndex < 1 || tagIndex > TagButtons.Length)
        {
            Debug.LogError($"Invalid tag index on {gameObject.name}");
            return;
        }

        //Get the UdonBehaviour component from the TagButton object at the provided tagIndex
        UdonBehaviour tagButton = TagButtons[tagIndex - 1].GetComponent<UdonBehaviour>();
        if (tagButton == null)
        {
            Debug.LogError($"TagButton does not have an UdonBehaviour component on {gameObject.name}");
            return;
        }

        //Get the activeUsersJson string from the TagButton object at the provided tagIndex
        string activeUsersJson = (string)tagButton.GetProgramVariable("activeUsersJson");
        if (string.IsNullOrEmpty(activeUsersJson))
        {
            Debug.LogError($"activeUsersJson is null or empty on {gameObject.name}");
            return;
        }

        //Deserialize the activeUsersJson string into a DataToken object
        if (VRCJson.TryDeserializeFromJson(activeUsersJson, out DataToken result))
        {
            //Debug.Log($"Successfully deserialized activeUsersJson on {gameObject.name}");

            //Get the activeUsers DataList from the DataToken object
            DataList activeUsers = result.DataList;
            if (Owner != null && activeUsers.Contains(Owner.displayName))
            {
                //Enable the tag if the owner is in the activeUsers list
                Debug.Log($"Owner is in activeUsers list, enabling tag on {gameObject.name}");
                TagObjects[tagIndex - 1].SetActive(true);
            }
            else
            {
                //Disable the tag if the owner is not in the activeUsers list or the owner is null
                Debug.Log($"Owner is not in activeUsers list, disabling tag on {gameObject.name}");
                TagObjects[tagIndex - 1].SetActive(false);
            }
        }
        else
        {
            Debug.LogError($"Failed to deserialize activeUsersJson on {gameObject.name}");
        }
    }

    //Simple logic to disable all tags in the TagObjects array for cleanup purposes
    public void DisableAllTags()
    {
        Debug.Log($"DisableAllTags called on {gameObject.name}");
        foreach (GameObject tagObject in TagObjects)
        {
            tagObject.SetActive(false);
        }
    }

}

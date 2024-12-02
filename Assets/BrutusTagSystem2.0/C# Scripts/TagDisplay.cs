using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Data;
using System.Collections.Generic;
using Newtonsoft.Json;

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
    
    void OnEnable()
    {
        Debug.Log($"OnEnable called on {gameObject.name}");
        DisableAllTags();
        for (int i = 0; i < TagButtons.Length; i++)
        {
            TagUpdate(i + 1);
        }
    }

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

    public void _OnCleanup()
    {
        Debug.Log($"_OnCleanup called on {gameObject.name}");
        DisableAllTags();
    }

    public void TagUpdate(int tagIndex)
    {
        Debug.Log($"TagUpdate called with tagIndex: {tagIndex} on {gameObject.name}");
        if (tagIndex < 1 || tagIndex > TagButtons.Length)
        {
            Debug.LogWarning($"Invalid tag index on {gameObject.name}");
            return;
        }

        UdonBehaviour tagButton = TagButtons[tagIndex - 1].GetComponent<UdonBehaviour>();
        if (tagButton == null)
        {
            Debug.LogWarning($"TagButton does not have an UdonBehaviour component on {gameObject.name}");
            return;
        }

        string activeUsersJson = (string)tagButton.GetProgramVariable("activeUsersJson");
        if (string.IsNullOrEmpty(activeUsersJson))
        {
            Debug.LogWarning($"activeUsersJson is null or empty on {gameObject.name}");
            return;
        }

        if (VRCJson.TryDeserializeFromJson(activeUsersJson, out DataToken result))
        {
            //Debug.Log($"Successfully deserialized activeUsersJson on {gameObject.name}");
            DataList activeUsers = result.DataList;
            if (Owner != null && activeUsers.Contains(Owner.displayName))
            {
                Debug.Log($"Owner is in activeUsers list, enabling tag on {gameObject.name}");
                TagObjects[tagIndex - 1].SetActive(true);
            }
            else
            {
                Debug.Log($"Owner is not in activeUsers list, disabling tag on {gameObject.name}");
                TagObjects[tagIndex - 1].SetActive(false);
            }
        }
        else
        {
            Debug.LogWarning($"Failed to deserialize activeUsersJson on {gameObject.name}");
        }
    }

    public void DisableAllTags()
    {
        Debug.Log($"DisableAllTags called on {gameObject.name}");
        foreach (GameObject tagObject in TagObjects)
        {
            tagObject.SetActive(false);
        }
    }

}

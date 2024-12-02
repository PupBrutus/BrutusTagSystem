# BrutusTagSystem

**[Demo World](https://vrchat.com/home/launch?worldId=wrld_341620ab-e985-4176-b842-599cdfb20ed1)**

BrutusTagSystem is intended as a VRC world asset to allow players to select any number of provided icons to be displayed above their heads in a VRC world. Helpful for displaying pronouns, ice-breakers or sharing other social information/interests that players would want to express to other players in game. It's not required to credit the use of this asset in your world but it is appreciated! 

Big thank you to [PlayerBush001](https://www.youtube.com/@PlayerBush001) for their awesome tutorial on using CyanPlayerObjectPool and troubleshooting assistance building the asset.

## Installation

### Requirements

Before using the Brutus Tag System, ensure that you have the [CyanPlayerObjectPool](https://github.com/CyanLaser/CyanPlayerObjectPool) installed in your VRC World.

### Installation Steps
1. Follow the installation steps for CyanLaser's CyanPlayerObjectPool and it's necessary prerequisites
2. Download the latest unitypackage from the releases page https://github.com/PupBrutus/BrutusTagSystem/releases
3. Import the unity package into your project
4. Drop the `BTS` prefab into your scene hierarchy
5. Right-click the BTS prefab and choose "Unpack" -- **do not choose `Unpack Completely`**
6. Review the steps below on Adding New Tags to add/update the tags shown.

## Adding New Tags

**Creating your tag**
1. Using the provided TEMPLATE.PSD or any existing tag within the `Assets/BrutusTagSystem2.0/example tags/`, create your new tag image.
2. When importing the new tag image into your project, ensure the "Texture Type" property is set to Sprite (2D and UI).

**Creating the tag button**
4. Duplicate a tag button from the scene hierarchy (`TagUIManager/UI-Canvas/ViewPort/Content/`) and rename the item to be the next integer available.
5. Update the Source Image of the Image component on your new button to your new tag sprite.
6. Ensure the Enabled Indicator and PlayerObjectAssigner variables are correctly set on the new button.

**Creating the tag object**
8. Right-click the TagDisplay prefab and choose `Prefab/Open Asset In Context`.
9. Duplicate a tag object from the scene hierarchy (`TagDisplay/TagGroup/LayoutPanel/`) and rename the new object to the same integer value from step 3.
10. Update the “Source Image” of the Image component on your new tag object to your new tag sprite.
11. Save the prefab and return to the main scene.

**Updating components for your new tag**
12. Select the TagDisplay object in the scene hierarchy and add both the new tagDisplay Object and the new tagButton Object to their appropriate arrays on the main TagDisplay prefab.
13. Select the "PlayerObjectAssigner" object in the scene hierarchy and select "Respawn All Pool Objects" to populate the object pool with the updated TagDisplay prefabs.

**Wrap-up**
14. Test your changes.

## Scripts

### TagGroupScript

This script is attached to the TagGroup prefab and is used to manage the position and rotation of the tag group above the owner's head in the world.

### TagButton

This script is attached to the TagButton prefab and is used to manage the tag button interactivity, manage which players have the tag selected, and handle data serialization.

### TagDisplay

This script is attached to the TagDisplay prefab and is used to update the tag display attached to each user based on the activeUsersJson data from the TagButtons. It ensures that the correct tags are displayed for the player.

## Interaction between TagButton and TagDisplay

- **TagButton**: When a player interacts with a TagButton, it updates the activeUsers list and serializes this data into a JSON string. This JSON string is then read to all TagDisplay objects when it performs the CallUpdate() function triggering TagUpdate on the TagDisplay objects.

- **TagDisplay**: Upon receiving the TagUpdate(int), each TagDisplay object reaches out to the correct button in the TagButtons array by index (tagIndex-1) for the activeUserListJSON, deserializes the data into a data-list and updates the tags displayed above the player's head accordingly if the displayname of the current owner appears in the list.

## Bugs/Troubleshooting

- Please uses [issues within GitHub](https://github.com/PupBrutus/BrutusTagSystem/issues) if you encounter problems using this asset.
  
- I've included a number of commented out debugging logging statements to aid in development which you may want to uncomment if you're developing/debugging this asset.

- **TagDisplay Arrays** Ensure that the TagObjects array and the TagButtons array appear in the same order for both arrays or there will be a mismatch between which tag is selected vs displayed.

- **TagButton PlayerObjectAssigner** Ensure that the PlayerObjectAssigner variable on the TagButton objects is assigned to the gameobject which acts as a parent to all pooled objects that will be updated when TagUpdate is called.

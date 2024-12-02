# BrutusTagSystem

BrutusTagSystem

## Requirements

Before using the Brutus Tag System, ensure that you have the [CyanPlayerObjectPool](https://github.com/CyanLaser/CyanPlayerObjectPool) installed.

## Scripts

### TagGroupScript

This script is attached to the TagGroup prefab and is used to manage the position and rotation of the tag group above the owner's head in the world.

### TagDisplay

This script is attached to the TagDisplay prefab and is used to update the tag display based on the activeUsersJson data from the TagButtons. It ensures that the correct tags are displayed for the player.

### TagButton

This script is attached to the TagButton prefab and is used to manage the tag button interactivity, manage which players have the tag selected, and handle data serialization for the Brutus Tag System 2.0.

## Interaction between TagButton and TagDisplay

- **TagButton**: When a player interacts with a TagButton, it updates the activeUsers list and serializes this data into a JSON string. This JSON string is then read to all TagDisplay objects when it performs the CallUpdate() function triggering TagUpdate on the TagDisplay objects.

- **TagDisplay**: Upon receiving the TagUpdate(int), each TagDisplay object reaches out to the correct button in the TagButtons array by index (tagIndex-1) for the activeUserListJSON, deserializes the data into a data-list and updates the tags displayed above the player's head accordingly if the displayname of the current owner appears in the list.

## Important Factors

- **TagDisplay Arrays** Ensure that the TagObjects array and the TagButtons array appear in the same order for both arrays or there will be a mismatch between which tag is selected vs displayed.

- **TagButton PlayerObjectAssigner** Ensure that the PlayerObjectAssigner variable on the TagButton objects is assigned to the gameobject which acts as a parent to all pooled objects that will be updated when TagUpdate is called.

## Adding New Tags

1. Using the provided TEMPLATE.PSD or any existing tag within the `Assets/BrutusTagSystem2.0/example tags/`, create your new tag image.
2. When importing the new tag image, ensure the Texture Type is set to Sprite (2D and UI).
3. Duplicate a tag button from the scene hierarchy (`TagUIManager/UI-Canvas/ViewPort/Content/`) and rename the item to be the next integer available.
4. Ensure the Enabled Indicator and PlayerObjectAssigner variables are correctly set on the new button.
5. Right-click the TagDisplay prefab and choose `Prefab/Open Asset In Context`.
6. Duplicate a tag display from the scene hierarchy (`TagDisplay/TagGroup/LayoutPanel/`) and rename the new object to the same integer value from step 3.
7. Save the prefab and return to the main scene.
8. Select the TagDisplay object in the scene hierarchy and add both the new tagDisplay Object and the new tagButton Object to their appropriate arrays on the main TagDisplay prefab.
9. Select the "PlayerObjectAssigner" object in the scene hierarchy and select "Respawn All Pool Objects" to populate the object pool with the updated TagDisplay prefabs.
10. Test your changes.




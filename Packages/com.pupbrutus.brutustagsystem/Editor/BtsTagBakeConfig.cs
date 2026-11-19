using UnityEngine;

// Editor-only source list for BtsTagMeshBaker: the original tag images, in tag-id order (the number stored in
// players' saved tag lists). Lives in an Editor folder, so it and the original images stay out of the world build;
// the build only carries the baked atlas, material and meshes. To add a tag, append its sprite here and re-bake.
[CreateAssetMenu(menuName = "Brutus Tag System/Tag Bake Config", fileName = "TagBakeConfig")]
public class BtsTagBakeConfig : ScriptableObject
{
    [Tooltip("Tag images in tag-id order. Never reorder or remove entries: saved player tags refer to these indices.")]
    public Sprite[] tagSprites;

    [Tooltip("Width of every tag in panel units (the UI images were 500)")]
    public float tagWidth = 500f;

    [Tooltip("Gap under the bottom tag in panel units (the layout group's bottom padding was 250)")]
    public float bottomPadding = 250f;
}

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Editor-only bake of the player tags into plain mesh objects that share one atlas texture and one material.
// Source: a TagBakeConfig asset (the original tag images, in tag-id order).
//   - Your world's config: the one BtsTagBakeConfig asset under Assets/ (any folder). Output goes to a Baked/ folder
//     next to it, so package updates never overwrite your tags.
//   - No config under Assets/ yet: the bake offers to copy the package's example config to
//     Assets/BrutusTagSystem Tags/ and bake that, or (package developers) to bake the example tags into the package.
// If the chosen config is missing or empty, it is filled from the tag images on the current (image) tag templates.
// Writes to <config folder>/Baked/ (the package's example bake: <package>/Runtime/Baked/):
//   TagAtlas.png    - every tag image packed into one texture (padding keeps mipmaps from bleeding between tags),
//                     also sliced into one sprite per tag for the tag picker buttons
//   TagAtlas.mat    - Sprites/Default (unlit, alpha blended, double sided)
//   TagMeshes.asset - one quad per tag, sized like the original UI image and mapped to its area of the atlas
// Then, in the open scene:
//   - rebuilds btsTagSourceMesh next to btsTagDisplay (inactive mesh templates, tag-id order) and links them to
//     btsTagDisplay, together with each tag's height, which btsTagDisplay uses to stack tags without a Canvas
//   - removes the old image templates (btsTagSource), the Canvas and layout components and the unused Camera
//   - points every Image that uses an original tag image (the picker buttons) at its atlas sprite
// After this the original images are only referenced by the config, so they stay out of the world build.
// To add a tag: append its sprite to the config (never reorder), re-bake, save the scene.
// The package folder is wherever this script's parent folder is, so it works from Packages/ or Assets/.
public static class BtsTagMeshBaker
{
    private const string MenuPath = "Tools/Brutus Tag System/Bake Tag Meshes";
    private const string UserConfigFolder = "Assets/BrutusTagSystem Tags";
    private const string ExampleConfigPath = "/Example Tags/TagBakeConfig.asset"; // under the package folder
    private const string ExampleBakedFolder = "/Runtime/Baked"; // under the package folder

    // Answers to the "no config under Assets" question, as DisplayDialogComplex returns them
    internal const int UseUserCopy = 0, CancelBake = 1, BakeIntoPackage = 2;
    private const string MeshTemplatesName = "btsTagSourceMesh";
    private const string ImageTemplatesName = "btsTagSource"; // the image templates from before the bake
    private const float ReferencePixelsPerUnit = 100f; // the tag Canvas's CanvasScaler value
    private const int AtlasPadding = 8;
    private const int AtlasMaxSize = 4096;

    private static string ConfigPath, BakedFolder, AtlasPath, MaterialPath, MeshesPath;

    [MenuItem(MenuPath)]
    public static void Bake() => Bake(-1);

    // packageConfigChoice: -1 asks when there is no config under Assets; otherwise one of the answers above
    // (lets scripted bakes, such as the package's own example bake, skip the dialog)
    internal static void Bake(int packageConfigChoice)
    {
        if (!FindPaths(packageConfigChoice)) return;

        UdonSharp.UdonSharpBehaviour found = FindDisplay();
        if (found == null) return;
        btsTagDisplay display = (btsTagDisplay)found;
        VRC.Udon.UdonBehaviour displayBehaviour = UdonSharpEditor.UdonSharpEditorUtility.GetBackingUdonBehaviour(display);
        Transform displayTransform = display.transform;
        Transform btsRoot = displayTransform.parent;
        if (btsRoot == null) { Debug.LogError("[BtsTagMeshBaker] btsTagDisplay needs a parent (the BTS object) to hold the mesh templates."); return; }

        BtsTagBakeConfig config = LoadOrSeedConfig(display);
        if (config == null) return;
        Sprite[] sprites = config.tagSprites;
        for (int i = 0; i < sprites.Length; i++)
            if (sprites[i] == null) { Debug.LogError($"[BtsTagMeshBaker] Tag {i} in the config has no sprite."); return; }

        // Images already on atlas sprites (from an earlier bake): remember their tag id, since re-importing the
        // atlas can replace the sprite objects they point at
        List<(Image image, int index)> onAtlas = new List<(Image, int)>();
        foreach (Image image in Object.FindObjectsOfType<Image>(true))
        {
            int index = AtlasSpriteIndex(image.sprite);
            if (index >= 0) onAtlas.Add((image, index));
        }

        // 1. Atlas: readable copies of each sprite's area, packed, saved, sliced into one sprite per tag
        Texture2D[] sources = new Texture2D[sprites.Length];
        for (int i = 0; i < sprites.Length; i++) sources[i] = ReadableCopy(sprites[i]);
        Texture2D atlas = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        Rect[] rects = atlas.PackTextures(sources, AtlasPadding, AtlasMaxSize, false);
        CleanUp(sources);
        if (rects == null) { Object.DestroyImmediate(atlas); Debug.LogError("[BtsTagMeshBaker] Packing the atlas failed."); return; }

        int atlasWidth = atlas.width, atlasHeight = atlas.height;
        Directory.CreateDirectory(BakedFolder);
        File.WriteAllBytes(AtlasPath, atlas.EncodeToPNG());
        Object.DestroyImmediate(atlas);
        AssetDatabase.ImportAsset(AtlasPath);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(AtlasPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = sprites[0].pixelsPerUnit;
        SpriteMetaData[] sheet = new SpriteMetaData[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            sheet[i] = new SpriteMetaData
            {
                name = SpriteName(i, sprites[i]),
                rect = new Rect(rects[i].x * atlasWidth, rects[i].y * atlasHeight, rects[i].width * atlasWidth, rects[i].height * atlasHeight),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
            };
        }
#pragma warning disable 618
        importer.spritesheet = sheet;
#pragma warning restore 618
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Trilinear;
        importer.maxTextureSize = AtlasMaxSize;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
        Texture2D atlasAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);

        // 2. One shared material. Instancing off: each tag is a different mesh, so instancing can't combine them,
        // and with it on Unity skips dynamic batching
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        material.shader = Shader.Find("Sprites/Default");
        material.mainTexture = atlasAsset;
        material.enableInstancing = false;
        EditorUtility.SetDirty(material);

        // 3. One centred quad per tag, the size the layout group gave the UI image: width as set, height from
        // the sprite's preferred height. All meshes live in one asset file.
        if (AssetDatabase.LoadAssetAtPath<Object>(MeshesPath) != null) AssetDatabase.DeleteAsset(MeshesPath);
        Mesh[] meshes = new Mesh[sprites.Length];
        float[] heights = new float[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            heights[i] = sprites[i].rect.height * ReferencePixelsPerUnit / sprites[i].pixelsPerUnit;
            meshes[i] = BuildQuad(i, sprites[i], new Vector2(config.tagWidth, heights[i]), rects[i]);
            if (i == 0) AssetDatabase.CreateAsset(meshes[i], MeshesPath);
            else AssetDatabase.AddObjectToAsset(meshes[i], MeshesPath);
        }
        AssetDatabase.SaveAssets();

        // 4. Mesh templates under an inactive parent so they never render themselves
        Transform old = btsRoot.Find(MeshTemplatesName);
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
        GameObject root = new GameObject(MeshTemplatesName);
        Undo.RegisterCreatedObjectUndo(root, "Bake tag meshes");
        root.transform.SetParent(btsRoot, false);
        root.layer = LayerMask.NameToLayer("UI");
        GameObject[] templates = new GameObject[sprites.Length];
        for (int i = 0; i < sprites.Length; i++) templates[i] = BuildTemplate(i, meshes[i], material, root.transform);
        root.SetActive(false);

        // 5. The tag template: no Canvas or layout, mesh templates and heights linked, old image templates gone
        int removed = StripCanvas(displayTransform);
        Undo.RecordObject(display, "Link mesh tag templates");
        display._tagTemplates = templates;
        display._tagHeights = heights;
        display.stackBottomPadding = config.bottomPadding;
        UdonSharpEditor.UdonSharpEditorUtility.CopyProxyToUdon(display);
        PrefabUtility.RecordPrefabInstancePropertyModifications(display);
        PrefabUtility.RecordPrefabInstancePropertyModifications(displayBehaviour);
        EditorUtility.SetDirty(display);
        Transform imageTemplates = btsRoot.Find(ImageTemplatesName);
        if (imageTemplates != null) { Undo.DestroyObjectImmediate(imageTemplates.gameObject); removed++; }

        // 6. Picker buttons (any Image showing an original tag image) use the matching atlas sprite
        Dictionary<string, Sprite> atlasSprites = new Dictionary<string, Sprite>();
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(AtlasPath)) if (o is Sprite s) atlasSprites[s.name] = s;
        Dictionary<Sprite, Sprite> toAtlas = new Dictionary<Sprite, Sprite>();
        for (int i = 0; i < sprites.Length; i++) toAtlas[sprites[i]] = atlasSprites[SpriteName(i, sprites[i])];
        int repointed = 0;
        void Point(Image image, Sprite atlasSprite)
        {
            if (image == null || image.sprite == atlasSprite) return;
            Undo.RecordObject(image, "Use atlas sprite");
            image.sprite = atlasSprite;
            PrefabUtility.RecordPrefabInstancePropertyModifications(image);
            repointed++;
        }
        foreach ((Image image, int index) in onAtlas)
            if (index < sprites.Length) Point(image, atlasSprites[SpriteName(index, sprites[index])]);
        foreach (Image image in Object.FindObjectsOfType<Image>(true))
            if (image.sprite != null && toAtlas.TryGetValue(image.sprite, out Sprite atlasSprite)) Point(image, atlasSprite);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(btsRoot.gameObject.scene);
        Debug.Log($"[BtsTagMeshBaker] Baked {meshes.Length} tags into {AtlasPath} ({atlasAsset.width}x{atlasAsset.height}); " +
                  $"{removed} image templates/Canvas/layout components removed; {repointed} Images moved to atlas sprites. Save the scene to keep it.");
    }

    // Picks the config (yours under Assets/, or the package's example) and puts the output next to it
    private static bool FindPaths(int packageConfigChoice)
    {
        string[] guids = AssetDatabase.FindAssets("BtsTagMeshBaker t:MonoScript");
        string scriptPath = guids.Length > 0 ? AssetDatabase.GUIDToAssetPath(guids[0]) : null;
        if (string.IsNullOrEmpty(scriptPath)) { Debug.LogError("[BtsTagMeshBaker] Can't find BtsTagMeshBaker.cs in the project."); return false; }
        string editorFolder = Path.GetDirectoryName(scriptPath).Replace('\\', '/');
        string packageFolder = Path.GetDirectoryName(editorFolder).Replace('\\', '/');
        string exampleConfig = packageFolder + ExampleConfigPath;

        string[] userConfigs = AssetDatabase.FindAssets("t:BtsTagBakeConfig", new[] { "Assets" });
        if (userConfigs.Length > 1)
        {
            string list = string.Join("\n", System.Array.ConvertAll(userConfigs, AssetDatabase.GUIDToAssetPath));
            Debug.LogError($"[BtsTagMeshBaker] More than one tag config under Assets; keep one (tag ids are positions in it):\n{list}");
            return false;
        }
        if (userConfigs.Length == 1)
        {
            ConfigPath = AssetDatabase.GUIDToAssetPath(userConfigs[0]);
            BakedFolder = Path.GetDirectoryName(ConfigPath).Replace('\\', '/') + "/Baked";
        }
        else
        {
            int choice = packageConfigChoice >= 0 ? packageConfigChoice : EditorUtility.DisplayDialogComplex(
                "Brutus Tag System",
                "There's no tag config under Assets yet. Baking now would write into the package folder, which a package update replaces.\n\n" +
                $"Create your own config in \"{UserConfigFolder}\" (starting with the example tags) and bake that?",
                "Create my config", "Cancel", "Bake into package");
            if (choice == CancelBake) return false;
            if (choice == UseUserCopy)
            {
                if (AssetDatabase.LoadAssetAtPath<BtsTagBakeConfig>(exampleConfig) == null)
                {
                    Debug.LogError($"[BtsTagMeshBaker] The example config {exampleConfig} is missing. Create one with Assets > Create > Brutus Tag System > Tag Bake Config under Assets.");
                    return false;
                }
                if (!AssetDatabase.IsValidFolder(UserConfigFolder)) AssetDatabase.CreateFolder("Assets", Path.GetFileName(UserConfigFolder));
                ConfigPath = UserConfigFolder + "/TagBakeConfig.asset";
                if (!AssetDatabase.CopyAsset(exampleConfig, ConfigPath)) { Debug.LogError($"[BtsTagMeshBaker] Couldn't copy {exampleConfig} to {ConfigPath}."); return false; }
                Debug.Log($"[BtsTagMeshBaker] Created {ConfigPath} from the example tags. Append your own tag sprites to it (never reorder).");
                BakedFolder = UserConfigFolder + "/Baked";
            }
            else
            {
                ConfigPath = exampleConfig;
                BakedFolder = packageFolder + ExampleBakedFolder;
            }
        }
        AtlasPath = BakedFolder + "/TagAtlas.png";
        MaterialPath = BakedFolder + "/TagAtlas.mat";
        MeshesPath = BakedFolder + "/TagMeshes.asset";
        return true;
    }

    // The btsTagDisplay in the open scene (the player-object template). Matched by script, not by name,
    // so a renamed BTS object still bakes.
    private static UdonSharp.UdonSharpBehaviour FindDisplay()
    {
        List<UdonSharp.UdonSharpBehaviour> displays = new List<UdonSharp.UdonSharpBehaviour>();
        foreach (VRC.Udon.UdonBehaviour behaviour in Object.FindObjectsOfType<VRC.Udon.UdonBehaviour>(true))
        {
            if (!behaviour.gameObject.scene.IsValid()) continue;
            if (UdonSharpEditor.UdonSharpEditorUtility.GetUdonSharpBehaviourType(behaviour) != typeof(btsTagDisplay)) continue;
            displays.Add(UdonSharpEditor.UdonSharpEditorUtility.GetProxyBehaviour(behaviour));
        }
        if (displays.Count == 1) return displays[0];
        if (displays.Count == 0) { Debug.LogError("[BtsTagMeshBaker] No btsTagDisplay in the open scene. Open the scene with the BTS prefab in it."); return null; }
        Debug.LogError($"[BtsTagMeshBaker] {displays.Count} btsTagDisplay objects in the open scene; expected one (the player-object template).");
        return null;
    }

    // The config, or a new one filled from the tag images on the current image templates (tag-id order)
    private static BtsTagBakeConfig LoadOrSeedConfig(btsTagDisplay display)
    {
        BtsTagBakeConfig config = AssetDatabase.LoadAssetAtPath<BtsTagBakeConfig>(ConfigPath);
        if (config != null && config.tagSprites != null && config.tagSprites.Length > 0) return config;

        GameObject[] templates = display._tagTemplates;
        if (templates == null || templates.Length == 0) { Debug.LogError($"[BtsTagMeshBaker] No tag sprites in {ConfigPath} and no tag templates on btsTagDisplay to read them from."); return null; }
        Sprite[] sprites = new Sprite[templates.Length];
        for (int i = 0; i < templates.Length; i++)
        {
            Image image = templates[i] != null ? templates[i].GetComponentInChildren<Image>(true) : null;
            if (image == null || image.sprite == null || AtlasSpriteIndex(image.sprite) >= 0)
            {
                Debug.LogError($"[BtsTagMeshBaker] No tag sprites in {ConfigPath}, and tag template {i} has no original tag image to read. Fill the config by hand (tag-id order).");
                return null;
            }
            sprites[i] = image.sprite;
        }

        if (config == null)
        {
            config = ScriptableObject.CreateInstance<BtsTagBakeConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
        }
        config.tagSprites = sprites;
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        Debug.Log($"[BtsTagMeshBaker] Filled {ConfigPath} with {sprites.Length} tag images from the current tag templates.");
        return config;
    }

    // Stable per-tag sprite name ("tag_<id>_<id>", as first baked), so picker references survive re-bakes
    private static string SpriteName(int index, Sprite source) => "tag_" + index + "_" + index;

    // Tag id of a sprite from any tag atlas (this bake's, or an earlier one such as the package's example bake) from
    // its name, or -1. Any atlas counts, so picker buttons move from the example atlas to yours on your first bake.
    private static int AtlasSpriteIndex(Sprite sprite)
    {
        if (sprite == null || !AssetDatabase.GetAssetPath(sprite).EndsWith("/Baked/TagAtlas.png")) return -1;
        string[] parts = sprite.name.Split('_');
        return parts.Length >= 2 && int.TryParse(parts[1], out int index) ? index : -1;
    }

    // Copies the sprite's area of its texture through a render target, so source import settings stay untouched.
    // ReadPixels and sprite rects both measure from the bottom-left.
    private static Texture2D ReadableCopy(Sprite sprite)
    {
        Rect r = sprite.rect;
        Texture source = sprite.texture;
        RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Graphics.Blit(source, rt);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D copy = new Texture2D((int)r.width, (int)r.height, TextureFormat.RGBA32, false);
        copy.ReadPixels(new Rect(r.x, r.y, r.width, r.height), 0, 0);
        copy.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        return copy;
    }

    // Centred quad in panel units, facing the same way as the UI image did (double-sided shader)
    private static Mesh BuildQuad(int index, Sprite sprite, Vector2 size, Rect uv)
    {
        Vector2 half = size * 0.5f;
        Mesh mesh = new Mesh { name = "Tag " + index + " " + sprite.name };
        mesh.vertices = new[]
        {
            new Vector3(-half.x, -half.y, 0), new Vector3(half.x, -half.y, 0),
            new Vector3(half.x, half.y, 0), new Vector3(-half.x, half.y, 0),
        };
        mesh.uv = new[]
        {
            new Vector2(uv.xMin, uv.yMin), new Vector2(uv.xMax, uv.yMin),
            new Vector2(uv.xMax, uv.yMax), new Vector2(uv.xMin, uv.yMax),
        };
        mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
        mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // Plain object (no RectTransform or layout), named by tag id like the old image templates
    private static GameObject BuildTemplate(int index, Mesh mesh, Material material, Transform parent)
    {
        GameObject go = new GameObject(index.ToString());
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        renderer.allowOcclusionWhenDynamic = false;
        return go;
    }

    // Removes the Canvas, its helpers, the layout components and the panel's invisible Image from the tag template,
    // and the unused Camera child. Dependants first, so Unity allows each removal.
    private static int StripCanvas(Transform display)
    {
        int removed = 0;
        Transform camera = display.Find("Camera");
        if (camera != null) { Undo.DestroyObjectImmediate(camera.gameObject); removed++; }
        Transform tagGroup = display.Find("TagGroup");
        if (tagGroup == null) return removed;
        foreach (System.Type type in new[]
                 {
                     typeof(ContentSizeFitter), typeof(VerticalLayoutGroup), typeof(Image), typeof(CanvasRenderer),
                     typeof(GraphicRaycaster), typeof(CanvasScaler), typeof(Canvas),
                 })
        {
            foreach (Component c in display.GetComponentsInChildren(type, true))
            {
                if (!c.transform.IsChildOf(tagGroup)) continue;
                Undo.DestroyObjectImmediate(c);
                removed++;
            }
        }
        return removed;
    }

    private static void CleanUp(Texture2D[] textures)
    {
        foreach (Texture2D t in textures) if (t != null) Object.DestroyImmediate(t);
    }
}

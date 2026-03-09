using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor tool — automatically creates a "Boss Portal NPC" in the Town scene.
/// Run from:  Tools ▶ Town Setup ▶ Add Boss Portal NPC
/// </summary>
public static class TownNPCSetup
{
    private const string TOWN_SCENE_PATH = "Assets/Scenes/Town.unity";
    private const string FONT_ASSET      = "Assets/Sprites/UI/Gixel SDF.asset";

    [MenuItem("Tools/Town Setup/Add Boss Portal NPC")]
    public static void AddBossPortalNPC()
    {
        // Open Town scene (save current scene first)
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene townScene = EditorSceneManager.OpenScene(TOWN_SCENE_PATH, OpenSceneMode.Single);

        // ── NPC root ──────────────────────────────────────────────────────────
        GameObject npcRoot = new GameObject("BossPortalNPC");

        // Sprite renderer (simple white square placeholder)
        SpriteRenderer sr = npcRoot.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        sr.color  = new Color(0.4f, 0.7f, 1f);    // light blue tint
        sr.sortingLayerName = "Characters";
        sr.sortingOrder     = 1;
        npcRoot.transform.localScale = new Vector3(0.8f, 1.2f, 1f);
        npcRoot.transform.position   = new Vector3(2f, 0f, 0f); // adjust in scene

        // Trigger collider for interaction range
        CircleCollider2D col = npcRoot.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 1.5f;

        // BossPortalNPC script
        BossPortalNPC npcScript = npcRoot.AddComponent<BossPortalNPC>();

        // ── Interact hint ("Press [F]") ────────────────────────────────────────
        GameObject hintObj = CreateTextChild(npcRoot, "InteractHint", "[F] Enter Boss Arena",
            new Vector3(0f, 1.4f, 0f), 3f, Color.yellow);
        hintObj.SetActive(false);

        // ── Dialogue text ──────────────────────────────────────────────────────
        GameObject dialogueObj = CreateTextChild(npcRoot, "DialogueText", "Help me save my world",
            new Vector3(0f, 1.9f, 0f), 2.8f, Color.white);
        dialogueObj.SetActive(false);

        // Wire references via SerializedObject
        SerializedObject so = new SerializedObject(npcScript);
        so.FindProperty("interactHintObject").objectReferenceValue = hintObj;
        so.FindProperty("dialogueObject").objectReferenceValue     = dialogueObj;
        so.ApplyModifiedProperties();

        // Register undo and mark scene dirty
        Undo.RegisterCreatedObjectUndo(npcRoot, "Add Boss Portal NPC");
        EditorSceneManager.MarkSceneDirty(townScene);
        EditorSceneManager.SaveScene(townScene);

        Selection.activeGameObject = npcRoot;
        Debug.Log("[TownNPCSetup] Boss Portal NPC created in Town scene. " +
                  "Move it to the desired position, then save the scene.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static GameObject CreateTextChild(GameObject parent, string name, string text,
                                               Vector3 localPos, float fontSize, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        obj.transform.localPosition = localPos;

        TextMeshPro tmp = obj.AddComponent<TextMeshPro>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.Center;

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_ASSET);
        if (font != null) tmp.font = font;

        // Size the text box
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(5f, 1.5f);

        return obj;
    }
}

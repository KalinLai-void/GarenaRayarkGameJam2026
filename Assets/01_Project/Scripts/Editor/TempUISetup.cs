using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Gameplay;

public static class TempUISetup
{
    [MenuItem("Tools/Setup UI Hierarchy")]
    public static void SetupUI()
    {
        // Load built-in UISprite
        Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        // Create MiniIcon Prefab first
        string prefabPath = "Assets/01_Project/Prefabs/MiniIcon.prefab";
        System.IO.Directory.CreateDirectory("Assets/01_Project/Prefabs");
        
        GameObject iconGo = new GameObject("MiniIcon", typeof(RectTransform), typeof(Image));
        RectTransform iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.sizeDelta = new Vector2(60f, 60f); // 放大為 60x60
        Image iconImg = iconGo.GetComponent<Image>();
        iconImg.sprite = uiSprite;
        iconImg.color = new Color(1f, 0.85f, 0f, 1f); // Gold
        GameObject miniIconPrefabAsset = PrefabUtility.SaveAsPrefabAsset(iconGo, prefabPath);
        Object.DestroyImmediate(iconGo);

        // Find or create Canvas
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("Canvas");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            
            canvasGo.AddComponent<GraphicRaycaster>();
        }
        GameObject canvasRoot = canvas.gameObject;

        // Setup EventSystem (Compatible with New Input System)
        UnityEngine.EventSystems.EventSystem eventSystem = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            GameObject esGo = new GameObject("EventSystem");
            eventSystem = esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
        }
        
        // Remove legacy StandaloneInputModule if present to prevent InvalidOperationException
        UnityEngine.EventSystems.StandaloneInputModule legacyModule = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        if (legacyModule != null)
        {
            Object.DestroyImmediate(legacyModule);
        }
        
        // Add InputSystemUIInputModule for New Input System
        UnityEngine.InputSystem.UI.InputSystemUIInputModule newModule = eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        if (newModule == null)
        {
            eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // Find or create UIManager
        GameObject uiManagerGo = GameObject.Find("UIManager");
        if (uiManagerGo == null)
        {
            uiManagerGo = new GameObject("UIManager");
        }
        SkillUIManager uiManager = uiManagerGo.GetComponent<SkillUIManager>();
        if (uiManager == null)
        {
            uiManager = uiManagerGo.AddComponent<SkillUIManager>();
        }

        void SetupRectTransform(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
        }

        // ───────────────────────────────────────────
        // 1. PlayerStatus_HUD (Top-Left)
        // ───────────────────────────────────────────
        GameObject playerStatusGo = GameObject.Find("PlayerStatus_HUD");
        if (playerStatusGo != null) Object.DestroyImmediate(playerStatusGo);

        playerStatusGo = new GameObject("PlayerStatus_HUD", typeof(RectTransform));
        playerStatusGo.transform.SetParent(canvasRoot.transform, false);
        SetupRectTransform(playerStatusGo.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(380f, 180f));

        // HP_Bar_Group
        GameObject hpBarGroup = new GameObject("HP_Bar_Group", typeof(RectTransform));
        hpBarGroup.transform.SetParent(playerStatusGo.transform, false);
        SetupRectTransform(hpBarGroup.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(300f, 30f));

        // HP_Slider
        GameObject sliderGo = new GameObject("HP_Slider", typeof(RectTransform));
        sliderGo.transform.SetParent(hpBarGroup.transform, false);
        SetupRectTransform(sliderGo.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Slider hpSlider = sliderGo.AddComponent<Slider>();

        // Slider Background
        GameObject bgGo = new GameObject("Background", typeof(RectTransform));
        bgGo.transform.SetParent(sliderGo.transform, false);
        SetupRectTransform(bgGo.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image bgImage = bgGo.AddComponent<Image>();
        bgImage.sprite = uiSprite;
        bgImage.type = Image.Type.Sliced;
        bgImage.color = new Color(0.35f, 0f, 0f, 1f); // Dark Red

        // Slider Fill Area
        GameObject fillAreaGo = new GameObject("Fill Area", typeof(RectTransform));
        fillAreaGo.transform.SetParent(sliderGo.transform, false);
        SetupRectTransform(fillAreaGo.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        // Slider Fill
        GameObject fillGo = new GameObject("Fill", typeof(RectTransform));
        fillGo.transform.SetParent(fillAreaGo.transform, false);
        SetupRectTransform(fillGo.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image fillImage = fillGo.AddComponent<Image>();
        fillImage.sprite = uiSprite;
        fillImage.type = Image.Type.Sliced;
        fillImage.color = new Color(0.9f, 0.1f, 0.1f, 1f); // Bright Red

        hpSlider.targetGraphic = bgImage;
        hpSlider.fillRect = fillGo.GetComponent<RectTransform>();
        hpSlider.minValue = 0;
        hpSlider.maxValue = 100;
        hpSlider.value = 100;

        // HP_Text
        GameObject hpTextGo = new GameObject("HP_Text", typeof(RectTransform));
        hpTextGo.transform.SetParent(hpBarGroup.transform, false);
        SetupRectTransform(hpTextGo.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI hpText = hpTextGo.AddComponent<TextMeshProUGUI>();
        hpText.text = "100 / 100";
        hpText.fontSize = 16f;
        hpText.alignment = TextAlignmentOptions.Center;
        hpText.color = Color.white;
        hpText.fontStyle = FontStyles.Bold;

        // Skill_Hotbar_Group
        GameObject skillHotbarGo = new GameObject("Skill_Hotbar_Group", typeof(RectTransform));
        skillHotbarGo.transform.SetParent(playerStatusGo.transform, false);
        SetupRectTransform(skillHotbarGo.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -40f), new Vector2(350f, 85f));
        HorizontalLayoutGroup skillLayout = skillHotbarGo.AddComponent<HorizontalLayoutGroup>();
        skillLayout.spacing = 10f;
        skillLayout.childControlWidth = false;
        skillLayout.childControlHeight = false;
        skillLayout.childForceExpandWidth = false;
        skillLayout.childForceExpandHeight = false;
        skillLayout.childAlignment = TextAnchor.MiddleLeft; // Align vertically

        TextMeshProUGUI[] skillTexts = new TextMeshProUGUI[4];
        Image activeCooldownMask = null;
        TextMeshProUGUI activeCooldownText = null;

        for (int i = 0; i < 4; i++)
        {
            GameObject slotGo = new GameObject($"Slot_Skill_{i + 1}", typeof(RectTransform));
            slotGo.transform.SetParent(skillHotbarGo.transform, false);
            RectTransform slotRt = slotGo.GetComponent<RectTransform>();
            Image slotImg = slotGo.AddComponent<Image>();
            slotImg.sprite = uiSprite;
            slotImg.type = Image.Type.Sliced;

            if (i == 0)
            {
                // Slot 1: Active Skill (Different size and color)
                slotRt.sizeDelta = new Vector2(80f, 80f);
                slotImg.color = new Color(0.45f, 0.15f, 0.2f, 0.95f); // Distinct Dark Red-Purple

                // CooldownMask
                GameObject maskGo = new GameObject("CooldownMask", typeof(RectTransform));
                maskGo.transform.SetParent(slotGo.transform, false);
                SetupRectTransform(maskGo.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                Image maskImg = maskGo.AddComponent<Image>();
                maskImg.sprite = uiSprite;
                maskImg.color = new Color(0f, 0f, 0f, 0.58f); // Half-transparent black
                maskImg.type = Image.Type.Filled;
                maskImg.fillMethod = Image.FillMethod.Radial360;
                maskImg.fillOrigin = (int)Image.Origin360.Top;
                maskImg.fillAmount = 0f;
                maskGo.SetActive(false);
                activeCooldownMask = maskImg;

                // CooldownText
                GameObject cdTextGo = new GameObject("CooldownText", typeof(RectTransform));
                cdTextGo.transform.SetParent(slotGo.transform, false);
                SetupRectTransform(cdTextGo.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                TextMeshProUGUI cdText = cdTextGo.AddComponent<TextMeshProUGUI>();
                cdText.text = "5";
                cdText.fontSize = 26f;
                cdText.alignment = TextAlignmentOptions.Center;
                cdText.color = Color.white;
                cdText.fontStyle = FontStyles.Bold;
                cdTextGo.SetActive(false);
                activeCooldownText = cdText;
            }
            else
            {
                // Slots 2, 3, 4: Passive Skills (Normal style)
                slotRt.sizeDelta = new Vector2(65f, 65f);
                slotImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            }

            // LV_Badge
            GameObject badgeGo = new GameObject("LV_Badge", typeof(RectTransform));
            badgeGo.transform.SetParent(slotGo.transform, false);
            SetupRectTransform(badgeGo.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(5f, -5f), new Vector2(30f, 22f));
            Image badgeImg = badgeGo.AddComponent<Image>();
            badgeImg.sprite = uiSprite;
            badgeImg.type = Image.Type.Sliced;
            badgeImg.color = new Color(0.1f, 0.1f, 0.1f, 1.0f);

            // LV_Text
            GameObject lvTextGo = new GameObject("LV_Text", typeof(RectTransform));
            lvTextGo.transform.SetParent(badgeGo.transform, false);
            SetupRectTransform(lvTextGo.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI lvText = lvTextGo.AddComponent<TextMeshProUGUI>();
            lvText.text = "1";
            lvText.fontSize = 11f;
            lvText.alignment = TextAlignmentOptions.Center;
            lvText.color = Color.yellow;
            lvText.fontStyle = FontStyles.Bold;

            skillTexts[i] = lvText;
        }

        // ───────────────────────────────────────────
        // 2. ActiveSkill_HUD (Bottom-Left) -> REMOVED
        // ───────────────────────────────────────────
        GameObject activeStatusGo = GameObject.Find("ActiveSkill_HUD");
        if (activeStatusGo != null) Object.DestroyImmediate(activeStatusGo);

        // ───────────────────────────────────────────
        // 3. Inventory_HUD (Top-Right - Stacking to the Left)
        // ───────────────────────────────────────────
        GameObject inventoryHUDGo = GameObject.Find("Inventory_HUD");
        if (inventoryHUDGo != null) Object.DestroyImmediate(inventoryHUDGo);

        inventoryHUDGo = new GameObject("Inventory_HUD", typeof(RectTransform));
        inventoryHUDGo.transform.SetParent(canvasRoot.transform, false);
        SetupRectTransform(inventoryHUDGo.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(300f, 50f));

        // MiniIcon_Container
        GameObject miniContainerGo = new GameObject("MiniIcon_Container", typeof(RectTransform));
        miniContainerGo.transform.SetParent(inventoryHUDGo.transform, false);
        SetupRectTransform(miniContainerGo.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero);
        HorizontalLayoutGroup containerLayout = miniContainerGo.AddComponent<HorizontalLayoutGroup>();
        containerLayout.spacing = 8f;
        containerLayout.childControlWidth = false;
        containerLayout.childControlHeight = false;
        containerLayout.childForceExpandWidth = false;
        containerLayout.childForceExpandHeight = false;
        containerLayout.childAlignment = TextAnchor.MiddleRight; // Dynamic items appear from right going leftwards

        // Spawn 2 placeholders: Map and Key
        GameObject mapGo = new GameObject("Slot_Item_Map", typeof(RectTransform));
        mapGo.transform.SetParent(miniContainerGo.transform, false);
        mapGo.GetComponent<RectTransform>().sizeDelta = new Vector2(60f, 60f);
        Image mapImg = mapGo.AddComponent<Image>();
        mapImg.sprite = uiSprite;
        mapImg.color = new Color(1f, 0.85f, 0f, 1f);

        GameObject keyGo = new GameObject("Slot_Item_Key", typeof(RectTransform));
        keyGo.transform.SetParent(miniContainerGo.transform, false);
        keyGo.GetComponent<RectTransform>().sizeDelta = new Vector2(60f, 60f);
        Image keyImg = keyGo.AddComponent<Image>();
        keyImg.sprite = uiSprite;
        keyImg.color = new Color(1f, 0.85f, 0f, 1f);

        // ───────────────────────────────────────────
        // Serialized Object reference wiring
        // ───────────────────────────────────────────
        SerializedObject so = new SerializedObject(uiManager);
        so.FindProperty("hpSlider").objectReferenceValue = hpSlider;
        so.FindProperty("hpText").objectReferenceValue = hpText;

        SerializedProperty skillTextsProp = so.FindProperty("skillLevelTexts");
        skillTextsProp.ClearArray();
        skillTextsProp.arraySize = 4;
        for (int i = 0; i < 4; i++)
        {
            skillTextsProp.GetArrayElementAtIndex(i).objectReferenceValue = skillTexts[i];
        }

        so.FindProperty("activeCooldownMask").objectReferenceValue = activeCooldownMask;
        so.FindProperty("activeCooldownText").objectReferenceValue = activeCooldownText;

        so.FindProperty("inventoryIconContainer").objectReferenceValue = miniContainerGo.transform;
        so.FindProperty("miniIconPrefab").objectReferenceValue = miniIconPrefabAsset;

        so.ApplyModifiedProperties();

        // Save the scene
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("Successfully created UI elements, prefab, and successfully wired up SkillUIManager references!");
    }
}

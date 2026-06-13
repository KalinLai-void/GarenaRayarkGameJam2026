using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Gameplay;

public static class TinderUISetup
{
    [MenuItem("Tools/Setup Tinder UI")]
    public static void SetupTinderUI()
    {
        // 1. Get built-in UI Sprite
        Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        // 2. Generate or Load Procedural Circle and Capsule Sprites
        Sprite circleSprite = GetOrCreateCircleSprite(64);
        Sprite capsuleSprite = GetOrCreateCapsuleSprite(128, 32);

        // 3. Find or create Canvas
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

        // 4. Find and destroy old HUD if exists to prevent duplicates
        GameObject oldHud = GameObject.Find("LevelUp_Tinder_HUD");
        if (oldHud != null)
        {
            Object.DestroyImmediate(oldHud);
        }

        // Helper function for setting RectTransform parameters
        void SetupRectTransform(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
        }

        // 5. Create LevelUp_Tinder_HUD (Root)
        GameObject rootGo = new GameObject("LevelUp_Tinder_HUD", typeof(RectTransform));
        rootGo.transform.SetParent(canvasRoot.transform, false);
        RectTransform rootRt = rootGo.GetComponent<RectTransform>();
        SetupRectTransform(rootRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460f, 900f));

        // Attach the TinderSwipeManager to the root
        TinderSwipeManager swipeManager = rootGo.AddComponent<TinderSwipeManager>();

        // 6. Phone_Frame (Image: Light Purple Casing)
        GameObject phoneGo = new GameObject("Phone_Frame", typeof(RectTransform), typeof(Image));
        phoneGo.transform.SetParent(rootGo.transform, false);
        SetupRectTransform(phoneGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image phoneImg = phoneGo.GetComponent<Image>();
        phoneImg.sprite = uiSprite;
        phoneImg.type = Image.Type.Sliced;
        phoneImg.color = new Color(0.58f, 0.45f, 0.65f, 1f); // Casing Purple

        // 6.1 Speaker_Slot (Pill Shape Top Casing)
        GameObject speakerGo = new GameObject("Speaker_Slot", typeof(RectTransform), typeof(Image));
        speakerGo.transform.SetParent(phoneGo.transform, false);
        SetupRectTransform(speakerGo.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(70f, 18f));
        Image speakerImg = speakerGo.GetComponent<Image>();
        speakerImg.sprite = capsuleSprite;
        speakerImg.color = new Color(0.35f, 0.25f, 0.4f, 1f); // Darker Phone Detail

        // 6.2 Home_Button (Circle Bottom Casing)
        GameObject homeGo = new GameObject("Home_Button", typeof(RectTransform), typeof(Image));
        homeGo.transform.SetParent(phoneGo.transform, false);
        SetupRectTransform(homeGo.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 45f), new Vector2(65f, 65f));
        Image homeImg = homeGo.GetComponent<Image>();
        homeImg.sprite = circleSprite;
        homeImg.color = new Color(0.35f, 0.25f, 0.4f, 1f); // Darker Phone Detail

        // 6.2.1 Timer_Text (Centered Inside Home Button)
        GameObject timerGo = new GameObject("Timer_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        timerGo.transform.SetParent(homeGo.transform, false);
        SetupRectTransform(timerGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI timerText = timerGo.GetComponent<TextMeshProUGUI>();
        timerText.text = "5";
        timerText.fontSize = 28f;
        timerText.alignment = TextAlignmentOptions.Center;
        timerText.color = Color.white;
        timerText.fontStyle = FontStyles.Bold;

        // 7. Screen_Mask_Area (Image + Mask: Screen, Solid White Background)
        GameObject maskGo = new GameObject("Screen_Mask_Area", typeof(RectTransform), typeof(Image), typeof(Mask));
        maskGo.transform.SetParent(phoneGo.transform, false);
        RectTransform maskRt = maskGo.GetComponent<RectTransform>();
        SetupRectTransform(maskRt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        // Margins: Left/Right 25, Bottom 90, Top 80
        maskRt.offsetMin = new Vector2(25f, 90f);
        maskRt.offsetMax = new Vector2(-25f, -80f);
        
        Image maskImg = maskGo.GetComponent<Image>();
        maskImg.sprite = uiSprite;
        maskImg.type = Image.Type.Sliced;
        maskImg.color = Color.white; // White Screen Background (mockup)
        
        Mask maskComponent = maskGo.GetComponent<Mask>();
        maskComponent.showMaskGraphic = true;

        // 8. Card_Stack_Container (Fill Screen Mask)
        GameObject containerGo = new GameObject("Card_Stack_Container", typeof(RectTransform));
        containerGo.transform.SetParent(maskGo.transform, false);
        SetupRectTransform(containerGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        // 9. UI_TinderCard_Prefab (Card Template)
        GameObject cardGo = null;
        string prefabFolderPath = "Assets/01_Project/Prefabs";
        string cardPrefabPath = $"{prefabFolderPath}/UI_TinderCard.prefab";
        GameObject cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(cardPrefabPath);

        if (cardPrefab != null)
        {
            // If the prefab already exists, instantiate it to preserve user customizations!
            cardGo = (GameObject)PrefabUtility.InstantiatePrefab(cardPrefab);
            cardGo.transform.SetParent(containerGo.transform, false);
            cardGo.name = "UI_TinderCard_Prefab";
        }
        else
        {
            // Otherwise, construct a default template card
            cardGo = new GameObject("UI_TinderCard_Prefab", typeof(RectTransform));
            cardGo.transform.SetParent(containerGo.transform, false);
            RectTransform cardRt = cardGo.GetComponent<RectTransform>();
            SetupRectTransform(cardRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -15f), new Vector2(380f, 380f));

            // Add TinderCardDragHandler script to the prefab
            cardGo.AddComponent<TinderCardDragHandler>();

            // 9.1 Card_Background (Image: Bright Cyan Square, Stretches to fill the card)
            GameObject cardBgGo = new GameObject("Card_Background", typeof(RectTransform), typeof(Image));
            cardBgGo.transform.SetParent(cardGo.transform, false);
            SetupRectTransform(cardBgGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Image cardBgImg = cardBgGo.GetComponent<Image>();
            cardBgImg.sprite = uiSprite;
            cardBgImg.type = Image.Type.Sliced;
            cardBgImg.color = new Color(0.35f, 0.82f, 0.92f, 1f); // Bright Cyan Box (mockup)

            // 9.2 Character_Illustration (Image: Gray placeholder, top portion 48% to 95% using percentage anchors)
            GameObject charGo = new GameObject("Character_Illustration", typeof(RectTransform), typeof(Image));
            charGo.transform.SetParent(cardGo.transform, false);
            RectTransform charRt = charGo.GetComponent<RectTransform>();
            SetupRectTransform(charRt, new Vector2(0.05f, 0.48f), new Vector2(0.95f, 0.95f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Image charImg = charGo.GetComponent<Image>();
            charImg.sprite = uiSprite;
            charImg.type = Image.Type.Sliced;
            charImg.color = new Color(0.85f, 0.85f, 0.85f, 1f); // Character Portrait

            // 9.3 Dialog_Bubble (Image: Semi-transparent Black, bottom portion 4% to 44% using percentage anchors)
            GameObject bubbleGo = new GameObject("Dialog_Bubble", typeof(RectTransform), typeof(Image));
            bubbleGo.transform.SetParent(cardGo.transform, false);
            RectTransform bubbleRt = bubbleGo.GetComponent<RectTransform>();
            SetupRectTransform(bubbleRt, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.44f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Image bubbleImg = bubbleGo.GetComponent<Image>();
            bubbleImg.sprite = uiSprite;
            bubbleImg.type = Image.Type.Sliced;
            bubbleImg.color = new Color(0.08f, 0.08f, 0.08f, 0.88f); // Dialog Box

            // 9.3.1 Skill_Name_Text (Inside Bubble, percentage anchors)
            GameObject skillNameGo = new GameObject("Skill_Name_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            skillNameGo.transform.SetParent(bubbleGo.transform, false);
            RectTransform nameRt = skillNameGo.GetComponent<RectTransform>();
            SetupRectTransform(nameRt, new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.92f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI skillNameText = skillNameGo.GetComponent<TextMeshProUGUI>();
            skillNameText.text = "主動技能: 閃爍彈";
            skillNameText.fontSize = 16f;
            skillNameText.alignment = TextAlignmentOptions.Center;
            skillNameText.color = Color.yellow;
            skillNameText.fontStyle = FontStyles.Bold;

            // 9.3.2 Skill_Desc_Text (Inside Bubble, percentage anchors)
            GameObject skillDescGo = new GameObject("Skill_Desc_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            skillDescGo.transform.SetParent(bubbleGo.transform, false);
            RectTransform descRt = skillDescGo.GetComponent<RectTransform>();
            SetupRectTransform(descRt, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.68f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI skillDescText = skillDescGo.GetComponent<TextMeshProUGUI>();
            skillDescText.text = "朝鼠標位置發射一枚會爆炸並致盲周圍敵人的閃光彈，冷卻時間 8 秒。";
            skillDescText.fontSize = 12f;
            skillDescText.alignment = TextAlignmentOptions.Left;
            skillDescText.color = Color.white;

            // Save card as Prefab
            System.IO.Directory.CreateDirectory(prefabFolderPath);
            cardPrefab = PrefabUtility.SaveAsPrefabAsset(cardGo, cardPrefabPath);
        }

        // 10. Button_Group (HorizontalLayoutGroup - Parented to Phone_Frame to sit OUTSIDE Screen_Mask_Area!)
        GameObject btnGroupGo = new GameObject("Button_Group", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        btnGroupGo.transform.SetParent(phoneGo.transform, false);
        // Anchor to bottom, Y offset of 165f relative to bottom of Phone_Frame centers it perfectly in white screen's bottom area
        SetupRectTransform(btnGroupGo.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 165f), new Vector2(300f, 90f));
        
        HorizontalLayoutGroup layout = btnGroupGo.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 50f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.MiddleCenter;

        // 10.1 Btn_Nope (Gray Circle Button with "X")
        GameObject btnNopeGo = new GameObject("Btn_Nope", typeof(RectTransform), typeof(Image), typeof(Button));
        btnNopeGo.transform.SetParent(btnGroupGo.transform, false);
        SetupRectTransform(btnNopeGo.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(85f, 85f));
        Image nopeImg = btnNopeGo.GetComponent<Image>();
        nopeImg.sprite = circleSprite;
        nopeImg.color = new Color(0.68f, 0.64f, 0.64f, 1f); // Gray Circle (mockup Nope)
        
        GameObject nopeLabelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        nopeLabelGo.transform.SetParent(btnNopeGo.transform, false);
        SetupRectTransform(nopeLabelGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI nopeLabel = nopeLabelGo.GetComponent<TextMeshProUGUI>();
        nopeLabel.text = "X";
        nopeLabel.fontSize = 32f;
        nopeLabel.alignment = TextAlignmentOptions.Center;
        nopeLabel.color = Color.white;
        nopeLabel.fontStyle = FontStyles.Bold;

        // 10.2 Btn_Like (Pink Circle Button with "❤" Heart)
        GameObject btnLikeGo = new GameObject("Btn_Like", typeof(RectTransform), typeof(Image), typeof(Button));
        btnLikeGo.transform.SetParent(btnGroupGo.transform, false);
        SetupRectTransform(btnLikeGo.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(85f, 85f));
        Image likeImg = btnLikeGo.GetComponent<Image>();
        likeImg.sprite = circleSprite;
        likeImg.color = new Color(0.84f, 0.32f, 0.54f, 1f); // Pink Circle (mockup Like)
        
        GameObject likeLabelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        likeLabelGo.transform.SetParent(btnLikeGo.transform, false);
        SetupRectTransform(likeLabelGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI likeLabel = likeLabelGo.GetComponent<TextMeshProUGUI>();
        likeLabel.text = "❤";
        likeLabel.fontSize = 32f;
        likeLabel.alignment = TextAlignmentOptions.Center;
        likeLabel.color = Color.white;
        likeLabel.fontStyle = FontStyles.Bold;


        // 12. Wire TinderSwipeManager References
        SerializedObject so = new SerializedObject(swipeManager);
        so.FindProperty("cardPrefab").objectReferenceValue = cardPrefab;
        so.FindProperty("cardContainer").objectReferenceValue = containerGo.transform;
        so.FindProperty("nopeButton").objectReferenceValue = btnNopeGo.transform;
        so.FindProperty("likeButton").objectReferenceValue = btnLikeGo.transform;
        so.FindProperty("timerText").objectReferenceValue = timerText;
        so.ApplyModifiedProperties();

        // 13. Save scene
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("Successfully generated Tinder UI with proportional percentage-based card layout, outer Button_Group, and Home Button timer!");
    }

    private static Sprite GetOrCreateCircleSprite(int radius)
    {
        string path = "Assets/01_Project/Sprites/ProceduralCircle.png";
        System.IO.Directory.CreateDirectory("Assets/01_Project/Sprites");
        
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;
        
        int size = radius * 2;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        
        Color transparent = new Color(0, 0, 0, 0);
        Color white = Color.white;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - radius + 0.5f;
                float dy = y - radius + 0.5f;
                float distSq = dx * dx + dy * dy;
                
                if (distSq <= radius * radius)
                {
                    float dist = Mathf.Sqrt(distSq);
                    float edgeWidth = 1.0f;
                    if (radius - dist < edgeWidth)
                    {
                        float alpha = (radius - dist) / edgeWidth;
                        tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, white);
                    }
                }
                else
                {
                    tex.SetPixel(x, y, transparent);
                }
            }
        }
        tex.Apply();
        
        byte[] bytes = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, bytes);
        Object.DestroyImmediate(tex);
        
        AssetDatabase.ImportAsset(path);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.SaveAndReimport();
        }
        
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Sprite GetOrCreateCapsuleSprite(int width, int height)
    {
        string path = "Assets/01_Project/Sprites/ProceduralCapsule.png";
        System.IO.Directory.CreateDirectory("Assets/01_Project/Sprites");
        
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;
        
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        
        Color transparent = new Color(0, 0, 0, 0);
        Color white = Color.white;
        float radius = height / 2f;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = 0;
                if (x < radius)
                {
                    dx = x - radius + 0.5f;
                }
                else if (x > width - radius)
                {
                    dx = x - (width - radius) + 0.5f;
                }
                else
                {
                    dx = 0;
                }
                
                float dy = y - radius + 0.5f;
                float distSq = dx * dx + dy * dy;
                
                if (distSq <= radius * radius)
                {
                    float dist = Mathf.Sqrt(distSq);
                    float edgeWidth = 1.0f;
                    if (radius - dist < edgeWidth)
                    {
                        float alpha = (radius - dist) / edgeWidth;
                        tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, white);
                    }
                }
                else
                {
                    tex.SetPixel(x, y, transparent);
                }
            }
        }
        tex.Apply();
        
        byte[] bytes = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, bytes);
        Object.DestroyImmediate(tex);
        
        AssetDatabase.ImportAsset(path);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.SaveAndReimport();
        }
        
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}

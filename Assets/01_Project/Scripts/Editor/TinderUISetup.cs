using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

        // 6. Phone_Frame (Image: Light Purple)
        GameObject phoneGo = new GameObject("Phone_Frame", typeof(RectTransform), typeof(Image));
        phoneGo.transform.SetParent(rootGo.transform, false);
        SetupRectTransform(phoneGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image phoneImg = phoneGo.GetComponent<Image>();
        phoneImg.sprite = uiSprite;
        phoneImg.type = Image.Type.Sliced;
        phoneImg.color = new Color(0.58f, 0.45f, 0.65f, 1f); // Lighter Purple (matching mockup casing)

        // 6.1 Speaker_Slot (Pill Shape Top)
        GameObject speakerGo = new GameObject("Speaker_Slot", typeof(RectTransform), typeof(Image));
        speakerGo.transform.SetParent(phoneGo.transform, false);
        SetupRectTransform(speakerGo.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(70f, 18f));
        Image speakerImg = speakerGo.GetComponent<Image>();
        speakerImg.sprite = capsuleSprite;
        speakerImg.color = new Color(0.35f, 0.25f, 0.4f, 1f); // Darker Phone Detail Purple

        // 6.2 Home_Button (Circle Bottom)
        GameObject homeGo = new GameObject("Home_Button", typeof(RectTransform), typeof(Image));
        homeGo.transform.SetParent(phoneGo.transform, false);
        SetupRectTransform(homeGo.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 45f), new Vector2(65f, 65f));
        Image homeImg = homeGo.GetComponent<Image>();
        homeImg.sprite = circleSprite;
        homeImg.color = new Color(0.35f, 0.25f, 0.4f, 1f); // Darker Phone Detail Purple

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
        maskImg.color = Color.white; // White Screen Background
        
        Mask maskComponent = maskGo.GetComponent<Mask>();
        maskComponent.showMaskGraphic = true;

        // 8. Refresh_Count_Text (Text: Top Sub inside screen area)
        GameObject refreshGo = new GameObject("Refresh_Count_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        refreshGo.transform.SetParent(maskGo.transform, false);
        SetupRectTransform(refreshGo.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -25f), new Vector2(300f, 25f));
        TextMeshProUGUI refreshText = refreshGo.GetComponent<TextMeshProUGUI>();
        refreshText.text = "刷新剩餘: 3";
        refreshText.fontSize = 15f;
        refreshText.alignment = TextAlignmentOptions.Center;
        refreshText.color = new Color(0.3f, 0.6f, 0.3f, 1f); // Green text on white screen
        refreshText.fontStyle = FontStyles.Bold;

        // 9. Card_Stack_Container
        GameObject containerGo = new GameObject("Card_Stack_Container", typeof(RectTransform));
        containerGo.transform.SetParent(maskGo.transform, false);
        SetupRectTransform(containerGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        // 10. UI_TinderCard_Prefab (Card Root)
        GameObject cardGo = new GameObject("UI_TinderCard_Prefab", typeof(RectTransform));
        cardGo.transform.SetParent(containerGo.transform, false);
        RectTransform cardRt = cardGo.GetComponent<RectTransform>();
        SetupRectTransform(cardRt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        // Margins: 15 all around, bottom is 150 to leave space for Nope/Like buttons
        cardRt.offsetMin = new Vector2(15f, 150f);
        cardRt.offsetMax = new Vector2(-15f, -70f);

        // 10.1 Card_Background (Image: Bright Cyan Box)
        GameObject cardBgGo = new GameObject("Card_Background", typeof(RectTransform), typeof(Image));
        cardBgGo.transform.SetParent(cardGo.transform, false);
        SetupRectTransform(cardBgGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image cardBgImg = cardBgGo.GetComponent<Image>();
        cardBgImg.sprite = uiSprite;
        cardBgImg.type = Image.Type.Sliced;
        cardBgImg.color = new Color(0.35f, 0.82f, 0.92f, 1f); // Bright Cyan Box (matching mockup)

        // 10.2 Character_Illustration (Image: Gray placeholder)
        GameObject charGo = new GameObject("Character_Illustration", typeof(RectTransform), typeof(Image));
        charGo.transform.SetParent(cardGo.transform, false);
        RectTransform charRt = charGo.GetComponent<RectTransform>();
        SetupRectTransform(charRt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        charRt.offsetMin = new Vector2(20f, 180f);
        charRt.offsetMax = new Vector2(-20f, -20f);
        Image charImg = charGo.GetComponent<Image>();
        charImg.sprite = uiSprite;
        charImg.type = Image.Type.Sliced;
        charImg.color = new Color(0.85f, 0.85f, 0.85f, 1f); // Character Illustration (Light Gray)

        // 10.3 Dialog_Bubble (Image: Semi-transparent Black)
        GameObject bubbleGo = new GameObject("Dialog_Bubble", typeof(RectTransform), typeof(Image));
        bubbleGo.transform.SetParent(cardGo.transform, false);
        RectTransform bubbleRt = bubbleGo.GetComponent<RectTransform>();
        SetupRectTransform(bubbleRt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        bubbleRt.offsetMin = new Vector2(15f, 15f);
        bubbleRt.offsetMax = new Vector2(-15f, -310f); // Height of bubble leaves space for portrait
        Image bubbleImg = bubbleGo.GetComponent<Image>();
        bubbleImg.sprite = uiSprite;
        bubbleImg.type = Image.Type.Sliced;
        bubbleImg.color = new Color(0.08f, 0.08f, 0.08f, 0.88f); // Dialog Box (Dark Gray/Black Translucent)

        // 10.3.1 Skill_Name_Text
        GameObject skillNameGo = new GameObject("Skill_Name_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        skillNameGo.transform.SetParent(bubbleGo.transform, false);
        RectTransform nameRt = skillNameGo.GetComponent<RectTransform>();
        SetupRectTransform(nameRt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(0f, 25f));
        nameRt.offsetMin = new Vector2(15f, nameRt.offsetMin.y);
        nameRt.offsetMax = new Vector2(-15f, nameRt.offsetMax.y);
        TextMeshProUGUI skillNameText = skillNameGo.GetComponent<TextMeshProUGUI>();
        skillNameText.text = "主動技能: 閃爍彈";
        skillNameText.fontSize = 16f;
        skillNameText.alignment = TextAlignmentOptions.Center;
        skillNameText.color = Color.yellow;
        skillNameText.fontStyle = FontStyles.Bold;

        // 10.3.2 Skill_Desc_Text
        GameObject skillDescGo = new GameObject("Skill_Desc_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        skillDescGo.transform.SetParent(bubbleGo.transform, false);
        RectTransform descRt = skillDescGo.GetComponent<RectTransform>();
        SetupRectTransform(descRt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        descRt.offsetMin = new Vector2(15f, 10f);
        descRt.offsetMax = new Vector2(-15f, -38f);
        TextMeshProUGUI skillDescText = skillDescGo.GetComponent<TextMeshProUGUI>();
        skillDescText.text = "朝鼠標位置發射一枚會爆炸並致盲周圍敵人的閃光彈，冷卻時間 8 秒。";
        skillDescText.fontSize = 12f;
        skillDescText.alignment = TextAlignmentOptions.Left;
        skillDescText.color = Color.white;

        // 11. Button_Group (HorizontalLayoutGroup - positioned inside white screen lower area)
        GameObject btnGroupGo = new GameObject("Button_Group", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        btnGroupGo.transform.SetParent(maskGo.transform, false);
        SetupRectTransform(btnGroupGo.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 75f), new Vector2(300f, 90f));
        
        HorizontalLayoutGroup layout = btnGroupGo.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 50f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.MiddleCenter;

        // 11.1 Btn_Nope (Gray Circle Button)
        GameObject btnNopeGo = new GameObject("Btn_Nope", typeof(RectTransform), typeof(Image), typeof(Button));
        btnNopeGo.transform.SetParent(btnGroupGo.transform, false);
        SetupRectTransform(btnNopeGo.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(85f, 85f));
        Image nopeImg = btnNopeGo.GetComponent<Image>();
        nopeImg.sprite = circleSprite;
        nopeImg.color = new Color(0.68f, 0.64f, 0.64f, 1f); // Gray circle (matching mockup Nope)
        btnNopeGo.AddComponent<Button>();
        
        GameObject nopeLabelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        nopeLabelGo.transform.SetParent(btnNopeGo.transform, false);
        SetupRectTransform(nopeLabelGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI nopeLabel = nopeLabelGo.GetComponent<TextMeshProUGUI>();
        nopeLabel.text = "X";
        nopeLabel.fontSize = 32f;
        nopeLabel.alignment = TextAlignmentOptions.Center;
        nopeLabel.color = Color.white;
        nopeLabel.fontStyle = FontStyles.Bold;

        // 11.2 Btn_Like (Pink Circle Button + Timer_Text Countdown overlay)
        GameObject btnLikeGo = new GameObject("Btn_Like", typeof(RectTransform), typeof(Image), typeof(Button));
        btnLikeGo.transform.SetParent(btnGroupGo.transform, false);
        SetupRectTransform(btnLikeGo.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(85f, 85f));
        Image likeImg = btnLikeGo.GetComponent<Image>();
        likeImg.sprite = circleSprite;
        likeImg.color = new Color(0.84f, 0.32f, 0.54f, 1f); // Pink circle (matching mockup Like)
        btnLikeGo.AddComponent<Button>();
        
        // Timer_Text is centered on the Like Button showing numbers
        GameObject timerGo = new GameObject("Timer_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        timerGo.transform.SetParent(btnLikeGo.transform, false);
        SetupRectTransform(timerGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI timerText = timerGo.GetComponent<TextMeshProUGUI>();
        timerText.text = "5"; // Numerical countdown timer
        timerText.fontSize = 32f;
        timerText.alignment = TextAlignmentOptions.Center;
        timerText.color = Color.white;
        timerText.fontStyle = FontStyles.Bold;

        // 12. Save card as Prefab
        string prefabFolderPath = "Assets/01_Project/Prefabs";
        System.IO.Directory.CreateDirectory(prefabFolderPath);
        string cardPrefabPath = $"{prefabFolderPath}/UI_TinderCard.prefab";
        PrefabUtility.SaveAsPrefabAsset(cardGo, cardPrefabPath);

        // 13. Save scene
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("Successfully created Tinder Level Up UI hierarchy with Timer on Like button, saved prefab, and saved scene!");
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

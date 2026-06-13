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

        // 2. Find or create Canvas
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

        // 3. Find and destroy old HUD if exists to prevent duplicates
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

        // 4. Create LevelUp_Tinder_HUD (Root)
        GameObject rootGo = new GameObject("LevelUp_Tinder_HUD", typeof(RectTransform));
        rootGo.transform.SetParent(canvasRoot.transform, false);
        RectTransform rootRt = rootGo.GetComponent<RectTransform>();
        SetupRectTransform(rootRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(450f, 800f));

        // 5. Phone_Frame (Image: Purple)
        GameObject phoneGo = new GameObject("Phone_Frame", typeof(RectTransform), typeof(Image));
        phoneGo.transform.SetParent(rootGo.transform, false);
        SetupRectTransform(phoneGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image phoneImg = phoneGo.GetComponent<Image>();
        phoneImg.sprite = uiSprite;
        phoneImg.type = Image.Type.Sliced;
        phoneImg.color = new Color(0.24f, 0.12f, 0.38f, 1f); // Dark Violet/Purple

        // 6. Timer_Text (Text: Top)
        GameObject timerGo = new GameObject("Timer_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        timerGo.transform.SetParent(phoneGo.transform, false);
        SetupRectTransform(timerGo.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(200f, 40f));
        TextMeshProUGUI timerText = timerGo.GetComponent<TextMeshProUGUI>();
        timerText.text = "5.0s";
        timerText.fontSize = 28f;
        timerText.alignment = TextAlignmentOptions.Center;
        timerText.color = Color.white;
        timerText.fontStyle = FontStyles.Bold;

        // 7. Refresh_Count_Text (Text: Top Sub)
        GameObject refreshGo = new GameObject("Refresh_Count_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        refreshGo.transform.SetParent(phoneGo.transform, false);
        SetupRectTransform(refreshGo.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(300f, 30f));
        TextMeshProUGUI refreshText = refreshGo.GetComponent<TextMeshProUGUI>();
        refreshText.text = "刷新剩餘: 3";
        refreshText.fontSize = 16f;
        refreshText.alignment = TextAlignmentOptions.Center;
        refreshText.color = new Color(0.4f, 0.9f, 0.4f, 1f);
        refreshText.fontStyle = FontStyles.Bold;

        // 8. Screen_Mask_Area (Image + Mask: Screen)
        GameObject maskGo = new GameObject("Screen_Mask_Area", typeof(RectTransform), typeof(Image), typeof(Mask));
        maskGo.transform.SetParent(phoneGo.transform, false);
        // Margins: Left/Right 25, Bottom 150, Top 120
        RectTransform maskRt = maskGo.GetComponent<RectTransform>();
        SetupRectTransform(maskRt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        maskRt.offsetMin = new Vector2(25f, 150f);
        maskRt.offsetMax = new Vector2(-25f, -120f);
        
        Image maskImg = maskGo.GetComponent<Image>();
        maskImg.sprite = uiSprite;
        maskImg.type = Image.Type.Sliced;
        maskImg.color = new Color(0.1f, 0.08f, 0.12f, 1f); // Dark screen background
        
        Mask maskComponent = maskGo.GetComponent<Mask>();
        maskComponent.showMaskGraphic = true;

        // 9. Card_Stack_Container
        GameObject containerGo = new GameObject("Card_Stack_Container", typeof(RectTransform));
        containerGo.transform.SetParent(maskGo.transform, false);
        SetupRectTransform(containerGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        // 10. UI_TinderCard_Prefab (Card Root)
        GameObject cardGo = new GameObject("UI_TinderCard_Prefab", typeof(RectTransform));
        cardGo.transform.SetParent(containerGo.transform, false);
        RectTransform cardRt = cardGo.GetComponent<RectTransform>();
        SetupRectTransform(cardRt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        // Margins: 15 all around the card
        cardRt.offsetMin = new Vector2(15f, 15f);
        cardRt.offsetMax = new Vector2(-15f, -15f);

        // 10.1 Card_Background (Image: Blue)
        GameObject cardBgGo = new GameObject("Card_Background", typeof(RectTransform), typeof(Image));
        cardBgGo.transform.SetParent(cardGo.transform, false);
        SetupRectTransform(cardBgGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image cardBgImg = cardBgGo.GetComponent<Image>();
        cardBgImg.sprite = uiSprite;
        cardBgImg.type = Image.Type.Sliced;
        cardBgImg.color = new Color(0.18f, 0.35f, 0.65f, 1f); // Card Background (Blue)

        // 10.2 Character_Illustration (Image: Gray)
        GameObject charGo = new GameObject("Character_Illustration", typeof(RectTransform), typeof(Image));
        charGo.transform.SetParent(cardGo.transform, false);
        RectTransform charRt = charGo.GetComponent<RectTransform>();
        SetupRectTransform(charRt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        charRt.offsetMin = new Vector2(20f, 220f);
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
        bubbleRt.offsetMax = new Vector2(-15f, -380f);
        Image bubbleImg = bubbleGo.GetComponent<Image>();
        bubbleImg.sprite = uiSprite;
        bubbleImg.type = Image.Type.Sliced;
        bubbleImg.color = new Color(0.08f, 0.08f, 0.08f, 0.88f); // Dialog Box (Dark Gray/Black Translucent)

        // 10.3.1 Skill_Name_Text
        GameObject skillNameGo = new GameObject("Skill_Name_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        skillNameGo.transform.SetParent(bubbleGo.transform, false);
        RectTransform nameRt = skillNameGo.GetComponent<RectTransform>();
        SetupRectTransform(nameRt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(0f, 30f));
        nameRt.offsetMin = new Vector2(15f, nameRt.offsetMin.y);
        nameRt.offsetMax = new Vector2(-15f, nameRt.offsetMax.y);
        TextMeshProUGUI skillNameText = skillNameGo.GetComponent<TextMeshProUGUI>();
        skillNameText.text = "主動技能: 閃爍彈";
        skillNameText.fontSize = 18f;
        skillNameText.alignment = TextAlignmentOptions.Center;
        skillNameText.color = Color.yellow;
        skillNameText.fontStyle = FontStyles.Bold;

        // 10.3.2 Skill_Desc_Text
        GameObject skillDescGo = new GameObject("Skill_Desc_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        skillDescGo.transform.SetParent(bubbleGo.transform, false);
        RectTransform descRt = skillDescGo.GetComponent<RectTransform>();
        SetupRectTransform(descRt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        descRt.offsetMin = new Vector2(15f, 15f);
        descRt.offsetMax = new Vector2(-15f, -45f);
        TextMeshProUGUI skillDescText = skillDescGo.GetComponent<TextMeshProUGUI>();
        skillDescText.text = "朝鼠標位置發射一枚會爆炸並致盲周圍敵人的閃光彈，冷卻時間 8 秒。";
        skillDescText.fontSize = 14f;
        skillDescText.alignment = TextAlignmentOptions.Left;
        skillDescText.color = Color.white;

        // 11. Button_Group (HorizontalLayoutGroup)
        GameObject btnGroupGo = new GameObject("Button_Group", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        btnGroupGo.transform.SetParent(phoneGo.transform, false);
        SetupRectTransform(btnGroupGo.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 25f), new Vector2(300f, 90f));
        
        HorizontalLayoutGroup layout = btnGroupGo.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 50f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.MiddleCenter;

        // 11.1 Btn_Nope (Red Button)
        GameObject btnNopeGo = new GameObject("Btn_Nope", typeof(RectTransform), typeof(Image), typeof(Button));
        btnNopeGo.transform.SetParent(btnGroupGo.transform, false);
        SetupRectTransform(btnNopeGo.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80f, 80f));
        Image nopeImg = btnNopeGo.GetComponent<Image>();
        nopeImg.sprite = uiSprite;
        nopeImg.type = Image.Type.Sliced;
        nopeImg.color = new Color(0.9f, 0.25f, 0.25f, 1f); // Red
        
        GameObject nopeLabelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        nopeLabelGo.transform.SetParent(btnNopeGo.transform, false);
        SetupRectTransform(nopeLabelGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI nopeLabel = nopeLabelGo.GetComponent<TextMeshProUGUI>();
        nopeLabel.text = "X";
        nopeLabel.fontSize = 28f;
        nopeLabel.alignment = TextAlignmentOptions.Center;
        nopeLabel.color = Color.white;
        nopeLabel.fontStyle = FontStyles.Bold;

        // 11.2 Btn_Like (Green Button)
        GameObject btnLikeGo = new GameObject("Btn_Like", typeof(RectTransform), typeof(Image), typeof(Button));
        btnLikeGo.transform.SetParent(btnGroupGo.transform, false);
        SetupRectTransform(btnLikeGo.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80f, 80f));
        Image likeImg = btnLikeGo.GetComponent<Image>();
        likeImg.sprite = uiSprite;
        likeImg.type = Image.Type.Sliced;
        likeImg.color = new Color(0.25f, 0.85f, 0.45f, 1f); // Green
        
        GameObject likeLabelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        likeLabelGo.transform.SetParent(btnLikeGo.transform, false);
        SetupRectTransform(likeLabelGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI likeLabel = likeLabelGo.GetComponent<TextMeshProUGUI>();
        likeLabel.text = "❤";
        likeLabel.fontSize = 28f;
        likeLabel.alignment = TextAlignmentOptions.Center;
        likeLabel.color = Color.white;
        likeLabel.fontStyle = FontStyles.Bold;

        // 12. Save card as Prefab
        string prefabFolderPath = "Assets/01_Project/Prefabs";
        System.IO.Directory.CreateDirectory(prefabFolderPath);
        string cardPrefabPath = $"{prefabFolderPath}/UI_TinderCard.prefab";
        PrefabUtility.SaveAsPrefabAsset(cardGo, cardPrefabPath);

        // 13. Save scene
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("Successfully created Tinder Level Up UI hierarchy, saved UI_TinderCard prefab, and saved active scene!");
    }
}

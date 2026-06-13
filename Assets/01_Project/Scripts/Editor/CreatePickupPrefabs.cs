using UnityEditor;
using UnityEngine;
using Gameplay;

public static class CreatePickupPrefabs
{
    [MenuItem("Tools/Create Pickup Prefabs")]
    public static void CreatePrefabs()
    {
        Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        string folderPath = "Assets/01_Project/Prefabs";
        System.IO.Directory.CreateDirectory(folderPath);

        string[] names = new string[] { "Pickup_Map", "Pickup_Key", "Pickup_Badge", "Pickup_Gem", "Pickup_Potion" };
        Color[] colors = new Color[] {
            new Color(1.0f, 0.85f, 0.0f, 1.0f), // Yellow for Map
            new Color(0.9f, 0.5f, 0.1f, 1.0f),  // Orange/Bronze for Key
            new Color(0.1f, 0.6f, 1.0f, 1.0f),  // Blue for Badge
            new Color(0.1f, 0.8f, 0.2f, 1.0f),  // Green for Gem
            new Color(0.9f, 0.2f, 0.2f, 1.0f)   // Red for Potion
        };

        for (int j = 0; j < names.Length; j++)
        {
            string pName = names[j];
            Color pColor = colors[j];

            // 1. Create root GameObject
            GameObject rootGo = new GameObject(pName);
            
            // 2. Add Trigger Collider
            BoxCollider2D col = rootGo.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.8f, 0.8f);

            // 3. Add PickupItem script
            PickupItem pickup = rootGo.AddComponent<PickupItem>();
            
            // Use SerializedObject to safely assign values
            SerializedObject so = new SerializedObject(pickup);
            so.FindProperty("itemName").stringValue = pName.Replace("Pickup_", "");
            
            // 4. Create Visual child
            GameObject visualGo = new GameObject("Visual");
            visualGo.transform.SetParent(rootGo.transform, false);
            
            // 5. Add SpriteRenderer and set color
            SpriteRenderer sr = visualGo.AddComponent<SpriteRenderer>();
            sr.sprite = uiSprite;
            sr.color = pColor;

            // 6. Add HoverEffect script
            visualGo.AddComponent<HoverEffect>();

            // Save the sprite to the PickupItem component's itemSprite field
            so.FindProperty("itemSprite").objectReferenceValue = uiSprite;
            so.ApplyModifiedProperties();

            // 7. Save as prefab
            string prefabFullPath = $"{folderPath}/{pName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(rootGo, prefabFullPath);

            // 8. Clean up scene
            Object.DestroyImmediate(rootGo);
        }

        Debug.Log("Successfully created 5 pickup item prefabs in Assets/01_Project/Prefabs/");
    }
}

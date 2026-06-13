using Common;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Common
{
    [System.Serializable]
    public class SceneReference : ISerializationCallbackReceiver
    {
#if UNITY_EDITOR
        [SerializeField] private UnityEditor.SceneAsset sceneAsset;
#endif

        [SerializeField, HideInInspector] private string sceneName = "";

        public string SceneName => sceneName;

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (sceneAsset != null)
            {
                sceneName = sceneAsset.name;
            }
            else
            {
                sceneName = "";
            }
#endif
        }

        public void OnAfterDeserialize() { }
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(SceneReference))]
public class SceneReferenceDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty sceneAssetProp = property.FindPropertyRelative("sceneAsset");

        if (sceneAssetProp != null)
        {
            EditorGUI.PropertyField(position, sceneAssetProp, label);
        }

        EditorGUI.EndProperty();
    }
}
#endif
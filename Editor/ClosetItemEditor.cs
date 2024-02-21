using UnityEngine;
using UnityEditor;
using dog.miruku.ndcloset.runtime;

namespace dog.miruku.ndcloset
{
    [CustomEditor(typeof(ClosetItem))]
    public class ClosetItemEditor : Editor
    {
        private SerializedProperty _itemNameProperty;
        private SerializedProperty _defaultProperty;
        private SerializedProperty _additionalObjectsProperty;
        private SerializedProperty _customIconProperty;


        static ClosetItemEditor()
        {
            EditorApplication.hierarchyWindowItemOnGUI += DrawIconOnWindowItem;
        }

        public void OnEnable()
        {
            _itemNameProperty = serializedObject.FindProperty("_itemName");
            _defaultProperty = serializedObject.FindProperty("_default");
            _additionalObjectsProperty = serializedObject.FindProperty("_additionalObjects");
            _customIconProperty = serializedObject.FindProperty("_customIcon");
        }

        private static void DrawIconOnWindowItem(int instanceID, Rect rect)
        {
            GameObject gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

            if (gameObject == null)
            {
                return;
            }

            if (gameObject.TryGetComponent<ClosetItem>(out var item))
            {
                var labelRect = new Rect(rect.xMax - 75,
                                       rect.yMin,
                                       rect.width,
                                       rect.height);
                var labelText = Localization.Get(Localization.Get("item"));
                if (item.Default) labelText += " ✔";
                EditorGUI.LabelField(labelRect, labelText);
            }
        }
        public override void OnInspectorGUI()
        {
            var item = (ClosetItem)target;
            var closet = item.Closet;

            serializedObject.Update();
            ClosetEditorUtil.Default();

            if (closet == null)
            {
                EditorGUILayout.HelpBox(Localization.Get("noCloset"), MessageType.Warning);
            }


            EditorGUILayout.PropertyField(_itemNameProperty, new GUIContent(Localization.Get("name")));
            EditorGUILayout.PropertyField(_defaultProperty, new GUIContent(Localization.Get("default")));
            if (closet != null && closet.IsUnique)
            {
                EditorGUILayout.PropertyField(_additionalObjectsProperty, new GUIContent(Localization.Get("additionalObjects")));
            }
            EditorGUILayout.PropertyField(_customIconProperty, new GUIContent(Localization.Get("customIcon")));
            if (item.CustomIcon != null)
            {
                var texture = AssetPreview.GetAssetPreview(item.CustomIcon);
                GUILayout.Label(texture);
            }
            if (GUILayout.Button(Localization.Get("generateIcon")))
            {
                var icon = ClosetUtil.GenerateIcon(item);
                item.CustomIcon = icon;
            }
            serializedObject.ApplyModifiedProperties();
            item.Validate();
        }
    }
}
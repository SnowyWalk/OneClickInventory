using System.Collections.Generic;
using System.Linq;
using dog.miruku.inventory.runtime;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace dog.miruku.inventory
{
    public class AvatarHierarchyFolding
    {
        public bool show = false;
        public Dictionary<string, bool> nodesShow = new Dictionary<string, bool>();
    }
    public class InventoryEditorUtil
    {
        private static int SelectedLanguage { get; set; }

        public static GUIStyle HeaderStyle => new GUIStyle(EditorStyles.boldLabel);

        private static void AvatarHierarchy(InventoryNode node, int level, AvatarHierarchyFolding folding)
        {
            if (!folding.nodesShow.ContainsKey(node.Key)) folding.nodesShow[node.Key] = false;
            var menuItemsToInstall = node.MenuItemsToInstall.ToArray();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space(level * 10, false);
            if (node.HasChildren || menuItemsToInstall.Length > 0)
            {
                folding.nodesShow[node.Key] = EditorGUILayout.BeginFoldoutHeaderGroup(folding.nodesShow[node.Key], GUIContent.none, new GUIStyle(EditorStyles.foldoutHeader) { padding = new RectOffset(0, 0, 0, 0), stretchWidth = false });
            }
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(node.Value, typeof(Inventory), true);
            EditorGUI.EndDisabledGroup();
            if (node.HasChildren || menuItemsToInstall.Length > 0)
            {
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
            EditorGUILayout.EndHorizontal();
            if (folding.nodesShow[node.Key])
            {
                foreach (var child in node.Children)
                {
                    AvatarHierarchy(child, level + 1, folding);
                }
                foreach (var menuItem in menuItemsToInstall)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.Space((level + 1) * 10, false);
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(menuItem, typeof(Inventory), true);
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        private static void AvatarHierarchy(VRCAvatarDescriptor avatar, AvatarHierarchyFolding folding)
        {
            var rootNodes = InventoryNode.GetRootNodes(avatar);
            foreach (var node in rootNodes)
            {
                AvatarHierarchy(node, 0, folding);
            }
        }

        public static void Footer(VRCAvatarDescriptor avatar, AvatarHierarchyFolding folding)
        {
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Localization.Get("avatar"), HeaderStyle);

            folding.show = EditorGUILayout.Foldout(folding.show, Localization.Get("avatarHierarchy"));
            if (folding.show)
            {
                AvatarHierarchy(avatar, folding);
            }

            var usedParameterMemory = InventoryNode.GetRootNodes(avatar).Select(e => e.UsedParameterMemory).Sum();
            EditorGUILayout.LabelField($"{Localization.Get("usedParameterMemory")} : {usedParameterMemory}");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Localization.Get("etc"), HeaderStyle);
            var selectedLanguage = EditorGUILayout.Popup(Localization.Get("language"), SelectedLanguage, Localization.Languages.Select(e => e.Item2).ToArray());
            if (selectedLanguage != SelectedLanguage)
            {
                SelectedLanguage = selectedLanguage;
                Localization.Language = Localization.Languages[SelectedLanguage].Item1;
                Localization.Get(Localization.Languages[SelectedLanguage].Item1);
            }
        }
    }
}
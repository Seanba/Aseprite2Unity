using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace Aseprite2Unity.Editor
{
    [CustomEditor(typeof(AsepriteImporter))]
    [CanEditMultipleObjects]
    public class AsepriteImporterEditor : ScriptedImporterEditor
    {
        private readonly string[] m_AnimatorCullingModeNames = EnumExtensions.GetUpToDateEnumNames<AnimatorCullingMode>();

        public override void OnEnable()
        {
            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
#if UNITY_2019_2_OR_NEWER
            serializedObject.Update();
#endif
            var importer = serializedObject.targetObject as AsepriteImporter;

            if (importer.Errors.Any())
            {
                var asset = Path.GetFileName(importer.assetPath);
                EditorGUILayout.LabelField("There were errors importing " + asset, EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(string.Join("\n\n", importer.Errors.Take(10).ToArray()), MessageType.Error);
                EditorGUILayout.Separator();
            }

            EditorGUILayout.LabelField("Aseprite2Unity Version: " + AsepriteImporter.Version);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Sprite Settings", EditorStyles.boldLabel);
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_PixelsPerUnit"),
                    new GUIContent("Pixels Per Unit", "How many pixels make up a unit. Default is 100. Use this the same as you would in the Texture Importer settings for sprites."));

                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_SpriteAtlas"),
                    new GUIContent("Sprite Atlas", "The sprites created by this import will be made part of this sprite atlas."));

                DisplayStringChoiceProperty(serializedObject.FindProperty("m_SortingLayerName"),
                    SortingLayer.layers.Select(l => l.name).ToArray(),
                    new GUIContent("Sorting Layer", "Name of the SpriteRenderer's sorting layer."));

                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_SortingOrder"),
                    new GUIContent("Order in Layer", "SpriteRenderer's order within a sorting layer."));

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Animator Settings", EditorStyles.boldLabel);
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FrameRate"),
                    new GUIContent("Frame Rate", "How often sprite animations are sampled."));

                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_AnimatorController"),
                    new GUIContent("Animator Controller", "Animator Controller to use with the imported sprite."));

                DisplayEnumProperty(serializedObject.FindProperty("m_AnimatorCullingMode"),
                    m_AnimatorCullingModeNames,
                    new GUIContent("Culling Mode", "Controls how the animation is updated when the object is culled."));

                EditorGUI.indentLevel--;
            }

#if UNITY_2019_2_OR_NEWER
            serializedObject.ApplyModifiedProperties();
#endif
            ApplyRevertGUI();

            EditorGUILayout.HelpBox("Tip: You can change sprite pivot by adding a pivot slice named unity:pivot to your first frame in Aseprite.", MessageType.Info);
            EditorGUILayout.HelpBox("Tip: Animations in Aseprite loop by default. Surround Frame Tag names with square brackets to disable looping. For example, [MyAnimName].", MessageType.Info);
        }

        static void DisplayEnumProperty(SerializedProperty prop, string[] displayNames, GUIContent guicontent)
        {
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, guicontent, prop);
            EditorGUI.showMixedValue = prop.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();

            GUIContent[] options = new GUIContent[displayNames.Length];
            for (int i = 0; i < options.Length; ++i)
            {
                options[i] = new GUIContent(ObjectNames.NicifyVariableName(displayNames[i]), "");
            }

            var selection = EditorGUI.Popup(rect, guicontent, prop.intValue, options);
            if (EditorGUI.EndChangeCheck())
            {
                prop.intValue = selection;
            }

            EditorGUI.showMixedValue = false;
            EditorGUI.EndProperty();
        }

        static void DisplayStringChoiceProperty(SerializedProperty prop, string[] choices, GUIContent content)
        {
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, content, prop);
            EditorGUI.showMixedValue = prop.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();

            GUIContent[] options = new GUIContent[choices.Length];
            for (int i = 0; i < options.Length; ++i)
            {
                options[i] = new GUIContent(choices[i], "");
            }

            int selection = Array.IndexOf(choices, prop.stringValue);
            if (selection == -1)
            {
                selection = 0;
            }

            selection = EditorGUI.Popup(rect, content, selection, options);
            if (EditorGUI.EndChangeCheck())
            {
                prop.stringValue = choices[selection];
            }

            EditorGUI.showMixedValue = false;
            EditorGUI.EndProperty();
        }
    }
}

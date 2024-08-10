﻿using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.AssetImporters;
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
            serializedObject.Update();

            var importer = serializedObject.targetObject as AsepriteImporter;

            if (importer.Errors.Any())
            {
                var asset = Path.GetFileName(importer.assetPath);
                EditorGUILayout.LabelField("There were errors importing " + asset, EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(string.Join("\n\n", importer.Errors.Take(10).ToArray()), MessageType.Error);
                EditorGUILayout.Separator();
            }

            EditorGUILayout.LabelField($"Aseprite2Unity Version: {Config.Version}");
            EditorGUILayout.Space();

            ExportAnimatorControllerGui();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Sprite Settings", EditorStyles.boldLabel);
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AsepriteImporter.m_PixelsPerUnit)),
                    new GUIContent("Pixels Per Unit", "How many pixels make up a unit. Default is 100. Use this the same as you would in the Texture Importer settings for sprites."));

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AsepriteImporter.m_InstantiatedPrefab)),
                    new GUIContent("Instantiated Prefab", "Prefab that animated sprite is configured with. Use this to create sprites with additional scripting or components."));

                DisplayStringChoiceProperty(serializedObject.FindProperty(nameof(AsepriteImporter.m_SortingLayerName)),
                    SortingLayer.layers.Select(l => l.name).ToArray(),
                    new GUIContent("Sorting Layer", "Name of the SpriteRenderer's sorting layer. If Instantiated Prefab has a Sprite Renderer then this will not be used."));

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AsepriteImporter.m_SortingOrder)),
                    new GUIContent("Order in Layer", "SpriteRenderer's order within a sorting layer. If Instantiated Prefab has a Sprite Renderer then this will not be used."));

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Animator Settings", EditorStyles.boldLabel);
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AsepriteImporter.m_FrameRate)),
                    new GUIContent("Frame Rate", "How often sprite animations are sampled."));

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AsepriteImporter.m_AnimatorController)),
                    new GUIContent("Animator Controller", "Animator Controller to use with the imported sprite. If Instantiated Prefab has an Animator component then this will not be used."));

                DisplayEnumProperty(serializedObject.FindProperty(nameof(AsepriteImporter.m_AnimatorCullingMode)),
                    m_AnimatorCullingModeNames,
                    new GUIContent("Culling Mode", "Controls how the animation is updated when the object is culled. If Instantiated Prefab has an Animator component then this will not be used."));

                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();

            ApplyRevertGUI();

            EditorGUILayout.HelpBox("Tip: You can change sprite pivot by adding a pivot slice named unity:pivot to your first frame in Aseprite.", MessageType.Info);
            EditorGUILayout.HelpBox("Tip: Animations in Aseprite loop by default. Surround Frame Tag names with square brackets to disable looping. For example, [MyAnimName].", MessageType.Info);
        }

        private void ExportAnimatorControllerGui()
        {
            if (serializedObject.targetObject is AsepriteImporter importer)
            {
                if (GUILayout.Button("Export Default Animator Controller"))
                {
                    // Creates the controller and prompts the user in case they are about to overwrite and existing file
                    var animationControllerAssetPath = EditorUtility.SaveFilePanelInProject("Save Animator Controller", $"{Path.GetFileNameWithoutExtension(importer.assetPath)}.AnimatorController",
                        "controller",
                        "Chose location for Animation Controller",
                        Path.GetDirectoryName(importer.assetPath));

                    if (!string.IsNullOrEmpty(animationControllerAssetPath))
                    {
                        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(animationControllerAssetPath);

                        if (controller == null)
                        {
                            // We're using a brand new animator controller
                            controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(animationControllerAssetPath);
                        }
                        else
                        {
                            // Remove old states from animator controller that already exists
                            var machine = controller.layers[0].stateMachine;
                            foreach (var state in machine.states.ToArray())
                            {
                                machine.RemoveState(state.state);
                            }
                        }

                        // Add a state for every animation clip in our Aseprite asset
                        var fsm = controller.layers[0].stateMachine;
                        var position = fsm.entryPosition;
                        position.x += 200;

                        var prefix = $"{Path.GetFileNameWithoutExtension(importer.assetPath)}.Animations.";
                        var clips = AssetDatabase.LoadAllAssetsAtPath(importer.assetPath).OfType<AnimationClip>().OrderBy(a => a.name);
                        foreach (var clip in clips)
                        {
                            var name = clip.name;
                            if (name.StartsWith(prefix))
                            {
                                name = name.Substring(prefix.Length);
                            }

                            name = name.Replace('.', '_');
                            var state = fsm.AddState(name, position);
                            state.motion = clip;
                            position.y += 80;
                        }
                    }
                }
            }
        }

        private static void DisplayEnumProperty(SerializedProperty prop, string[] displayNames, GUIContent guicontent)
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

        private static void DisplayStringChoiceProperty(SerializedProperty prop, string[] choices, GUIContent content)
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

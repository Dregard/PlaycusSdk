using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;

namespace Playcus
{
    public class PlaycusSettingsEditorWindow : EditorWindow
    {
        private List<SettingsEntry> _entries = new List<SettingsEntry>();
        private List<AnimBool> _anims = new List<AnimBool>();
        private Vector2 _scrollPos;

        [MenuItem ("Playcus/Settings")]
        public static void ShowWindow () 
        {
            var window = GetWindowWithRect<PlaycusSettingsEditorWindow>(new Rect(0, 0, 600, 600));
            var icon = Resources.Load<Texture>("playcus_icon");
            GUIContent titleContent = new GUIContent ("Playcus Settings", icon);
            window.titleContent = titleContent;
        }

        private void OnEnable()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var instances = new List<SettingsEntry>();

            foreach (var assembly in assemblies)
            {
                var derivedTypes = assembly.GetTypes()
                    .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(SettingsEntry)));

                foreach (var type in derivedTypes)
                {
                    if (type.GetConstructor(Type.EmptyTypes) != null)
                    {
                        var instance = (SettingsEntry)Activator.CreateInstance(type);
                        instance.Initialize();
                        instances.Add(instance);
                    }
                }
            }

            foreach (var instance in instances)
            {
                _entries.Add(instance);
                var anim = new AnimBool(false);
                anim.valueChanged.AddListener(Repaint);
                _anims.Add(anim);
            }
        }

        void OnGUI ()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            
            for (var i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (entry.DrawInSettingsWindow == false)
                {
                    continue;
                }
                
                var anim = _anims[i];
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                var headerRect = GUILayoutUtility.GetRect(16f, 28f, GUILayout.ExpandWidth(true));
                GUI.Box(headerRect, GUIContent.none); // Draws background

                var newFoldout = EditorGUI.Foldout(
                    new Rect(headerRect.x + 12f, headerRect.y + 4f, headerRect.width, headerRect.height),
                    anim.target, 
                    entry.Label, 
                    true, 
                    EditorStyles.foldoutHeader);

                if (newFoldout != anim.target)
                {
                    anim.target = newFoldout;
                }

                EditorGUILayout.BeginFadeGroup(anim.faded);
                if (anim.target)
                {
                    entry.Draw();
                }
                EditorGUILayout.EndFadeGroup();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
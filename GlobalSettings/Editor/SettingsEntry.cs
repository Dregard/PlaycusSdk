using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Playcus
{
    public abstract class SettingsEntry
    {
        private readonly bool _drawInSettingsWindow;
        public bool DrawInSettingsWindow => _drawInSettingsWindow;

        public SettingsEntry()
        {
            // Get the attribute from the class itself
            var attribute = (HideInSettingsWindowAttribute)Attribute.GetCustomAttribute(
                this.GetType(), typeof(HideInSettingsWindowAttribute));

            _drawInSettingsWindow = attribute == null;
        }

        internal void Initialize()
        {
            OnInitialize();
        }

        protected abstract void OnInitialize();
        
        public abstract string Label { get; }
        protected abstract void DrawLayout();

        internal void Draw()
        {
            DrawLayout();
        }

        /// <summary>
        /// Set value to the property with the private setter.
        /// </summary>
        protected void SetPrivateProperty(object obj, string propertyName, object value)
        {
            var type = obj.GetType();
            var prop = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(obj, value);
            }
            else
            {
                Debug.LogWarning($"Property '{propertyName}' not found or not writable on {type.Name}");
            }
        }
    }
    
    public class DefineSymbolSwitch
    {
        private readonly string _label;
        private readonly string _symbol;
        private bool _isEnabled;

        public bool IsEnabled => _isEnabled;
        
        public DefineSymbolSwitch(string label, string symbol)
        {
            _label = label;
            _symbol = symbol;
            _isEnabled = DefineSymbolUtility.DefineSymbolExists(symbol);
        }

        public void DrawToggle()
        {
            var isEnabled = EditorGUILayout.ToggleLeft(_label, _isEnabled);
            if (isEnabled != _isEnabled)
            {
                if (isEnabled)
                {
                    DefineSymbolUtility.AddDefineSymbol(_symbol);
                }
                else
                {
                    DefineSymbolUtility.RemoveDefineSymbol(_symbol);
                }

                _isEnabled = isEnabled;
            }
        }
    }
    
    /// <summary>
    /// For settings entries used only for initialization.
    /// </summary>
    public class HideInSettingsWindowAttribute : Attribute {}
}
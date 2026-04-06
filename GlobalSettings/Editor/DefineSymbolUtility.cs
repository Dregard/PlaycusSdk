using UnityEditor;

namespace Playcus
{
    public static class DefineSymbolUtility
    {
        private enum SymbolOperation
        {
            ADD,
            REMOVE,
            CHECK
        }
        
        private static readonly BuildTargetGroup[] _buildTargetGroups = new[]
        {
            BuildTargetGroup.Android,
            BuildTargetGroup.iOS
        };

        public static void AddDefineSymbol(string symbol)
        {
            DefineSymbolOperation(symbol, SymbolOperation.ADD);
        }
        
        public static void RemoveDefineSymbol(string symbol)
        {
            DefineSymbolOperation(symbol, SymbolOperation.REMOVE);
        }
        
        public static bool DefineSymbolExists(string symbol)
        {
            return DefineSymbolOperation(symbol, SymbolOperation.CHECK);
        }

        private static bool DefineSymbolOperation(string symbol, SymbolOperation operation)
        {
            if (string.IsNullOrEmpty(symbol))
            {
                return false;
            }

            var result = false;
            
            // Apply symbols to all valid build target groups
            foreach (BuildTargetGroup buildTargetGroup in _buildTargetGroups)
            {
                if (!IsValidBuildTargetGroup(buildTargetGroup))
                    continue;

                try
                {
                    string currentSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);

                    try
                    {
                        switch (operation)
                        {
                            case SymbolOperation.ADD:
                                if (!currentSymbols.Contains(symbol))
                                {
                                    currentSymbols = string.IsNullOrEmpty(currentSymbols) ? symbol : $"{currentSymbols};{symbol}";
                                    result = true;
                                    // Debug.Log($"Added scripting define symbol '{symbol}' to {buildTargetGroup}");
                                }
                                break;
                            case SymbolOperation.REMOVE:
                                if (currentSymbols.Contains(symbol))
                                {
                                    currentSymbols = currentSymbols.Replace(symbol, string.Empty);
                                    result = true;
                                    // Debug.Log($"Removed scripting define symbol '{symbol}' from {buildTargetGroup}");
                                }
                                break;
                            case SymbolOperation.CHECK:
                                result = currentSymbols.Contains(symbol);
                                break;
                        }
                    }
                    catch // (Exception e)
                    {
                        // Debug.LogError($"Failed add symbol '{symbol}' to platform '{buildTargetGroup}': {e.Message}");
                    }

                    PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, currentSymbols);
                }
                catch // (Exception e)
                {
                    // Debug.LogError($"Failed to get symbols for platform '{buildTargetGroup}': {e.Message}");
                }
            }

            if (operation != SymbolOperation.CHECK && result)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
#if UNITY_2019_3_OR_NEWER
                UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
#endif
            }

            return result;
        }

        private static bool IsValidBuildTargetGroup(BuildTargetGroup group)
        {
            string groupName = group.ToString();
            return !string.IsNullOrEmpty(groupName) &&
                   group != BuildTargetGroup.Unknown &&
                   !groupName.StartsWith("Obsolete");
        }
    }
}
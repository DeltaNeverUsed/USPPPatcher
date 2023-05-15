#if UNITY_EDITOR && !COMPILER_UDONSHARP
using HarmonyLib;
using UdonSharp.Compiler;
using UnityEditor;

namespace USPPPatcher.Editor
{
    [InitializeOnLoad]
    public static class Patcher
    {
        public static void Postfix(string filePath, float timeoutSeconds, ref string __result)
        {
            if (__result == "")
                return;
            
            // Normalize new lines
            __result = __result.Replace("\r\n", "\n").Replace('\r', '\n');

            // Do PreProcessor stuff
            __result = PPHandler.Parse(__result);
        }

        static Patcher() {
            var assembly = typeof(UdonSharpCompilerV1).Assembly; // Getting the assembly of the UdonSharp compiler
            
            var readMethod = assembly.GetType("UdonSharp.UdonSharpUtils").GetMethod("ReadFileTextSync");
            var harmony = new Harmony("USPPPatcher.DeltaNeverUsed.patch");
            harmony.Patch(readMethod, null, new HarmonyMethod(typeof(Patcher), "Postfix"));
        }
    }
}
#endif
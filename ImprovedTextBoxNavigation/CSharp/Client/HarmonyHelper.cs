
using System.Reflection;
using HarmonyLib;

namespace ImprovedTextBoxNavigation;

public static class HarmonyHelper
{
    public static HashSet<MethodInfo> PatchAll(this Harmony harmony, Type rootType)
    {
        var patchTypes = GetPatchTypes(rootType);
        var hashSet = new HashSet<MethodInfo>();
        foreach (var patchType in patchTypes)
        {
            var patchedMethods = harmony.CreateClassProcessor(patchType).Patch();
            if (patchedMethods == null) continue;
            
            foreach (var patchedMethod in patchedMethods)
            {
                hashSet.Add(patchedMethod);
            }
        }

        return hashSet;
    }

    public static IEnumerable<Type> GetPatchTypes(Type type)
    {
        if (type.GetCustomAttributes<HarmonyPatch>().Any())
        {
            yield return type;
        }
        
        foreach (var nestedType in type.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
        {
            foreach (var patchType in GetPatchTypes(nestedType))
            {
                yield return patchType;
            }
        }
    }
}
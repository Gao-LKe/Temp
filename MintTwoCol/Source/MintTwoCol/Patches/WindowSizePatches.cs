using System.Reflection;
using DubsMintMenus;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace MintTwoCol;

internal static class WindowSizePatches
{
    internal static void Install(Harmony harmony)
    {
        harmony.Patch(MintBridge.RequestedTabSize, postfix: Get(nameof(RequestedTabSizePostfix)));
        harmony.Patch(MintBridge.SetInitialSizeAndPosition, postfix: Get(nameof(SetInitialSizeAndPositionPostfix)));

        MethodInfo preClose = MintBridge.WindowType.GetMethod("PreClose", BindingFlags.Instance | BindingFlags.Public);
        if (preClose != null)
        {
            harmony.Patch(preClose, prefix: Get(nameof(PreClosePrefix)), postfix: Get(nameof(PreClosePostfix)));
        }
    }

    private static HarmonyMethod Get(string name)
    {
        return new HarmonyMethod(typeof(WindowSizePatches).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic));
    }

    private static void RequestedTabSizePostfix(ref Vector2 __result)
    {
        __result.x += CategoryRailDrawer.RightRailWidth;
    }

    private static void SetInitialSizeAndPositionPostfix(MainTabWindow_MintArchitect __instance)
    {
        if (DubsMintMenus.Settings.BuildMenuSize == default(Vector2))
        {
            return;
        }

        Rect rect = __instance.windowRect;
        rect.width += CategoryRailDrawer.RightRailWidth;
        if (rect.xMax > (float)UI.screenWidth)
        {
            rect.x = Mathf.Max(0f, (float)UI.screenWidth - rect.width);
        }
        __instance.windowRect = rect;
    }

    private static void PreClosePrefix(MainTabWindow_MintArchitect __instance)
    {
        Rect rect = __instance.windowRect;
        rect.width = Mathf.Max(0f, rect.width - CategoryRailDrawer.RightRailWidth);
        __instance.windowRect = rect;
    }

    private static void PreClosePostfix(MainTabWindow_MintArchitect __instance)
    {
        Rect rect = __instance.windowRect;
        rect.width += CategoryRailDrawer.RightRailWidth;
        __instance.windowRect = rect;
        CategoryRailDrawer.ResetDrag();
    }
}

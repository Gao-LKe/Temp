using System;
using System.Reflection;
using DubsMintMenus;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace MintTwoCol;

internal static class CategoryRailPatches
{
    private static bool reportedLeftFailure;
    private static bool reportedRightFailure;
    private static bool rightRailVisible;

    internal static void Install(Harmony harmony)
    {
        harmony.Patch(MintBridge.DrawArchitectList, prefix: GetPrefix(nameof(DrawArchitectListPrefix)));
        harmony.Patch(MintBridge.ScrollBox, prefix: GetPrefix(nameof(ScrollBoxPrefix)));
        harmony.Patch(MintBridge.DrawGridHeader, prefix: GetPrefix(nameof(DrawGridHeaderPrefix)));
    }

    private static HarmonyMethod GetPrefix(string name)
    {
        MethodInfo method = typeof(CategoryRailPatches).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
        return new HarmonyMethod(method);
    }

    private static bool DrawArchitectListPrefix(MainTabWindow_MintArchitect __instance, Rect box)
    {
        if (!MintBridge.Available)
        {
            return true;
        }

        try
        {
            CategoryRailDrawer.DrawLeft(__instance, box);
            return false;
        }
        catch (Exception exception)
        {
            if (!reportedLeftFailure)
            {
                reportedLeftFailure = true;
                Log.Error($"[{MintTwoColMod.HarmonyId}] Custom category rail failed, falling back to Dubs Mint Menus layout: {exception}");
            }
            return true;
        }
    }

    private static bool ScrollBoxPrefix(MainTabWindow_MintArchitect __instance, ref Rect canvas)
    {
        rightRailVisible = false;
        if (!MintBridge.Available)
        {
            return true;
        }
        if (canvas.width - CategoryRailDrawer.RightRailWidth - CategoryRailDrawer.RailGap < CategoryRailDrawer.MinGridWidth)
        {
            return true;
        }

        Rect rail = new Rect(
            canvas.xMax - CategoryRailDrawer.RightRailWidth,
            canvas.y - CategoryRailDrawer.HeaderHeight,
            CategoryRailDrawer.RightRailWidth,
            canvas.height + CategoryRailDrawer.HeaderHeight);

        try
        {
            CategoryRailDrawer.DrawRight(__instance, rail);
            rightRailVisible = true;
            canvas.width -= CategoryRailDrawer.RightRailWidth + CategoryRailDrawer.RailGap;
        }
        catch (Exception exception)
        {
            rightRailVisible = false;
            if (!reportedRightFailure)
            {
                reportedRightFailure = true;
                Log.Error($"[{MintTwoColMod.HarmonyId}] Custom mod category rail failed, grid keeps full width: {exception}");
            }
        }
        return true;
    }

    private static void DrawGridHeaderPrefix(ref Rect header)
    {
        if (rightRailVisible)
        {
            header.width -= CategoryRailDrawer.RightRailWidth + CategoryRailDrawer.RailGap;
        }
    }
}

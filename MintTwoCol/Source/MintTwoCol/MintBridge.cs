using System;
using System.Collections.Generic;
using System.Reflection;
using DubsMintMenus;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MintTwoCol;

internal static class MintBridge
{
    private const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static bool Available { get; private set; }

    internal static Type WindowType => typeof(MainTabWindow_MintArchitect);

    internal static MethodInfo DrawArchitectList { get; private set; }
    internal static MethodInfo ScrollBox { get; private set; }
    internal static MethodInfo DrawGridHeader { get; private set; }
    internal static MethodInfo RequestedTabSize { get; private set; }
    internal static MethodInfo SetInitialSizeAndPosition { get; private set; }
    internal static MethodInfo Parc { get; private set; }
    internal static FieldInfo SearchString { get; private set; }

    private static PropertyInfo cacheDesPanels;
    private static FieldInfo selectedTab;

    internal static void Initialize(Harmony harmony)
    {
        Available = false;

        DrawArchitectList = ResolveMethod("DrawArchitectList", typeof(Rect));
        ScrollBox = ResolveMethod("ScrollBox", typeof(Rect));
        DrawGridHeader = ResolveMethod("DrawGridHeader", typeof(Rect));
        RequestedTabSize = WindowType.GetProperty("RequestedTabSize", InstanceMembers)?.GetGetMethod(true);
        SetInitialSizeAndPosition = WindowType.GetMethod("SetInitialSizeAndPosition", InstanceMembers);
        cacheDesPanels = WindowType.GetProperty("CacheDesPanels", InstanceMembers);
        selectedTab = WindowType.GetField("SelectedTab", InstanceMembers);
        Parc = ResolveMethod("parc", typeof(Designator));
        SearchString = WindowType.GetField("searchString", BindingFlags.Static | BindingFlags.NonPublic);

        if (DrawArchitectList == null || ScrollBox == null || DrawGridHeader == null || RequestedTabSize == null || SetInitialSizeAndPosition == null || cacheDesPanels == null || selectedTab == null)
        {
            Log.Warning($"[{MintTwoColMod.HarmonyId}] Dubs Mint Menus internals not found, no patch applied. List={DrawArchitectList != null}, Grid={ScrollBox != null}, Header={DrawGridHeader != null}, Size={RequestedTabSize != null}, Position={SetInitialSizeAndPosition != null}, Cache={cacheDesPanels != null}, Selection={selectedTab != null}.");
            return;
        }

        if (selectedTab.FieldType != typeof(ArchitectCategoryTab))
        {
            Log.Warning($"[{MintTwoColMod.HarmonyId}] Unexpected SelectedTab type {selectedTab.FieldType}, no patch applied.");
            return;
        }

        Available = true;
        PatchAll(harmony);
        Log.Message($"[{MintTwoColMod.HarmonyId}] Dubs Mint Menus internals resolved, patches applied.");
    }

    private static MethodInfo ResolveMethod(string name, Type parameterType)
    {
        return WindowType.GetMethod(name, InstanceMembers, null, new[] { parameterType }, null);
    }

    private static void PatchAll(Harmony harmony)
    {
        CategoryRailPatches.Install(harmony);
        WindowSizePatches.Install(harmony);
        try
        {
            PinyinSearchPatches.Install(harmony);
        }
        catch (Exception exception)
        {
            Log.Warning($"[{MintTwoColMod.HarmonyId}] Pinyin search patch skipped: {exception}");
        }
    }

    internal static string GetSearchString()
    {
        return SearchString?.GetValue(null) as string;
    }

    internal static List<ArchitectCategoryTab> GetCategories(MainTabWindow_MintArchitect window)
    {
        return cacheDesPanels?.GetValue(window) as List<ArchitectCategoryTab>;
    }

    internal static ArchitectCategoryTab GetSelected(MainTabWindow_MintArchitect window)
    {
        return selectedTab?.GetValue(window) as ArchitectCategoryTab;
    }

    internal static void SetSelected(MainTabWindow_MintArchitect window, ArchitectCategoryTab tab)
    {
        selectedTab?.SetValue(window, tab);
    }

    internal static bool IsNative(ArchitectCategoryTab tab)
    {
        ModContentPack pack = tab?.def?.modContentPack;
        return pack != null && pack.IsOfficialMod;
    }
}

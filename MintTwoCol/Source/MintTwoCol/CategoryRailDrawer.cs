using System;
using System.Collections.Generic;
using System.Linq;
using DubsMintMenus;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace MintTwoCol;

internal static class CategoryRailDrawer
{
    internal const float RowHeight = 30f;
    internal const float RightRailWidth = 200f;
    internal const float RailGap = 8f;
    internal const float HeaderHeight = 32f;
    internal const float MinGridWidth = 200f;

    private const float DragThresholdSqr = 36f;

    private static Vector2 leftScroll;
    private static Vector2 rightScroll;

    private static bool pressActive;
    private static ArchitectCategoryTab pressTab;
    private static bool pressNative;
    private static bool pressDraggable;
    private static Vector2 pressPosition;

    private static bool dragging;
    private static bool dragNative;
    private static int dropSlot;

    internal static void ResetDrag()
    {
        pressActive = false;
        pressTab = null;
        dragging = false;
        dropSlot = 0;
    }

    private static bool loggedSummary;

    internal static void DrawLeft(MainTabWindow_MintArchitect window, Rect box)
    {
        LogSummaryOnce(window);
        DrawRail(window, box, native: true, ref leftScroll);
    }

    private static void LogSummaryOnce(MainTabWindow_MintArchitect window)
    {
        if (loggedSummary)
        {
            return;
        }
        loggedSummary = true;

        List<ArchitectCategoryTab> all = MintBridge.GetCategories(window);
        if (all == null)
        {
            Log.Warning($"[{MintTwoColMod.HarmonyId}] Category cache is null.");
            return;
        }

        int nativeCount = all.Count(tab => tab != null && MintBridge.IsNative(tab));
        int moddedCount = all.Count(tab => tab != null && !MintBridge.IsNative(tab));
        Log.Message($"[{MintTwoColMod.HarmonyId}] Categories: native={nativeCount}, modded={moddedCount}.");
        foreach (IGrouping<string, ArchitectCategoryTab> group in all
                     .Where(tab => tab != null)
                     .GroupBy(tab => tab.def?.modContentPack?.PackageId ?? "null")
                     .OrderByDescending(group => group.Count())
                     .Take(12))
        {
            Log.Message($"[{MintTwoColMod.HarmonyId}]   {group.Key}: {group.Count()} ({string.Join(", ", group.Take(3).Select(tab => tab.def.defName))})");
        }
    }

    internal static void DrawRight(MainTabWindow_MintArchitect window, Rect box)
    {
        DrawRail(window, box, native: false, ref rightScroll);
    }

    private static void DrawRail(MainTabWindow_MintArchitect window, Rect box, bool native, ref Vector2 scroll)
    {
        Text.Font = GameFont.Small;
        Widgets.DrawMenuSection(box);
        Rect inner = box.ContractedBy(4f);
        if (inner.width < 20f || inner.height < 10f)
        {
            return;
        }

        List<ArchitectCategoryTab> rows = Ordered(window, native);
        ArchitectCategoryTab selected = MintBridge.GetSelected(window);
        int leadingRows = native ? 1 : 0;
        float contentHeight = RowHeight * (rows.Count + leadingRows);
        Rect viewRect = new Rect(0f, 0f, inner.width - 16f, contentHeight);

        GUI.BeginGroup(inner);
        try
        {
            Widgets.BeginScrollView(new Rect(0f, 0f, inner.width, inner.height), ref scroll, viewRect, true);
            try
            {
                GUI.BeginGroup(viewRect);
                try
                {
                    float y = 0f;
                    if (native)
                    {
                        DrawRow(new Rect(0f, y, viewRect.width, RowHeight), null, selected == null);
                        y += RowHeight;
                    }
                    foreach (ArchitectCategoryTab tab in rows)
                    {
                        DrawRow(new Rect(0f, y, viewRect.width, RowHeight), tab, selected == tab);
                        y += RowHeight;
                    }

                    HandleInput(window, viewRect, native, rows);

                    if (dragging && dragNative == native && rows.Count > 0)
                    {
                        float lineY = RowHeight * (dropSlot + leadingRows);
                        Widgets.DrawLineHorizontal(0f, lineY, viewRect.width);
                    }
                }
                finally
                {
                    GUI.EndGroup();
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }
        }
        finally
        {
            GUI.EndGroup();
        }
    }

    private static void DrawRow(Rect rect, ArchitectCategoryTab tab, bool selected)
    {
        if (Mouse.IsOver(rect))
        {
            Widgets.DrawHighlight(rect);
        }
        else if (selected)
        {
            Widgets.DrawHighlightSelected(rect);
        }

        string label = tab == null ? Translator.Translate("All").CapitalizeFirst() : tab.def.LabelCap;
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(rect.ContractedBy(6f, 0f), label);
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private static void HandleInput(MainTabWindow_MintArchitect window, Rect viewRect, bool native, List<ArchitectCategoryTab> rows)
    {
        Event current = Event.current;
        if (current == null)
        {
            return;
        }

        switch (current.type)
        {
            case EventType.MouseDown:
                if (current.button != 0 || !viewRect.Contains(current.mousePosition) || pressActive)
                {
                    return;
                }
                {
                    int rowIndex = Mathf.FloorToInt(current.mousePosition.y / RowHeight);
                    int totalRows = rows.Count + (native ? 1 : 0);
                    if (rowIndex < 0 || rowIndex >= totalRows)
                    {
                        return;
                    }
                    int tabIndex = rowIndex - (native ? 1 : 0);
                    ArchitectCategoryTab tab = tabIndex >= 0 && tabIndex < rows.Count ? rows[tabIndex] : null;
                    pressActive = true;
                    pressTab = tab;
                    pressNative = native;
                    pressDraggable = tab != null;
                    pressPosition = current.mousePosition;
                    current.Use();
                }
                break;

            case EventType.MouseDrag:
                if (!pressActive || pressNative != native || dragging || !pressDraggable)
                {
                    return;
                }
                if ((current.mousePosition - pressPosition).sqrMagnitude > DragThresholdSqr)
                {
                    dragging = true;
                    dragNative = native;
                    dropSlot = DragSlot(current.mousePosition, native, rows.Count);
                    current.Use();
                }
                break;

            case EventType.MouseUp:
                if (!pressActive || pressNative != native)
                {
                    return;
                }
                if (dragging)
                {
                    if (viewRect.Contains(current.mousePosition) && rows.Count > 0)
                    {
                        ApplyReorder(native, rows, pressTab, DragSlot(current.mousePosition, native, rows.Count));
                    }
                }
                else
                {
                    SoundStarter.PlayOneShotOnCamera(SoundDefOf.Click, (Map)null);
                    MintBridge.SetSelected(window, pressTab);
                }
                ResetDrag();
                current.Use();
                break;
        }
    }

    private static int DragSlot(Vector2 mousePosition, bool native, int rowCount)
    {
        int slot = Mathf.FloorToInt(mousePosition.y / RowHeight) - (native ? 1 : 0);
        return Mathf.Clamp(slot, 0, rowCount);
    }

    private static void ApplyReorder(bool native, List<ArchitectCategoryTab> rows, ArchitectCategoryTab moved, int slot)
    {
        if (moved == null)
        {
            return;
        }

        int from = rows.IndexOf(moved);
        if (from < 0)
        {
            return;
        }
        rows.RemoveAt(from);
        if (from < slot)
        {
            slot--;
        }
        slot = Mathf.Clamp(slot, 0, rows.Count);
        rows.Insert(slot, moved);

        List<string> order = native ? MintTwoColMod.Settings.LeftOrder : MintTwoColMod.Settings.RightOrder;
        order.Clear();
        order.AddRange(rows.Select(tab => tab.def.defName));
        MintTwoColMod.Settings.Write();
    }

    internal static List<ArchitectCategoryTab> Ordered(MainTabWindow_MintArchitect window, bool native)
    {
        List<ArchitectCategoryTab> all = MintBridge.GetCategories(window);
        if (all == null)
        {
            return new List<ArchitectCategoryTab>();
        }

        List<ArchitectCategoryTab> filtered = all
            .Where(tab => tab != null && tab.Visible && MintBridge.IsNative(tab) == native)
            .OrderByDescending(tab => tab.def.order)
            .ThenBy(tab => tab.def.defName, StringComparer.Ordinal)
            .ToList();
        List<string> order = native ? MintTwoColMod.Settings.LeftOrder : MintTwoColMod.Settings.RightOrder;
        if (order.Count == 0)
        {
            return filtered;
        }

        Dictionary<string, int> index = new Dictionary<string, int>();
        for (int i = 0; i < order.Count; i++)
        {
            index[order[i]] = i;
        }
        return filtered.OrderBy(tab => index.TryGetValue(tab.def.defName, out int position) ? position : int.MaxValue).ToList();
    }
}

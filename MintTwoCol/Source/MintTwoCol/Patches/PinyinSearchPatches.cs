using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MintTwoCol;

// Mint 的建筑搜索不走原版 QuickSearchWidget，而是自己的 parc(Designator)：
// 对 label、分类 label、Mod 名、材料类别 label 做 OrdinalIgnoreCase 包含判断。
// 这里只补拼音匹配，原逻辑保持不动。
internal static class PinyinSearchPatches
{
    internal static void Install(Harmony harmony)
    {
        if (MintBridge.Parc == null || MintBridge.SearchString == null)
        {
            Log.Warning($"[{MintTwoColMod.HarmonyId}] Pinyin search unavailable: parc/searchString not found.");
            return;
        }

        MethodInfo postfix = typeof(PinyinSearchPatches).GetMethod(nameof(ParcPostfix), BindingFlags.Static | BindingFlags.NonPublic);
        harmony.Patch(MintBridge.Parc, postfix: new HarmonyMethod(postfix));
    }

    private static void ParcPostfix(Designator p, ref bool __result)
    {
        if (__result || p == null)
        {
            return;
        }

        string query = MintBridge.GetSearchString();
        if (string.IsNullOrEmpty(query))
        {
            return;
        }

        // 英文单词跨语言命中：中文界面下 def.label 已本地化，用 defName 兜底。
        if (DefNameMatches(p, query))
        {
            __result = true;
            return;
        }

        if (PinyinSearch.Available && MatchesAnyLabel(p, query))
        {
            __result = true;
        }
    }

    private static bool DefNameMatches(Designator designator, string query)
    {
        if (!(designator is Designator_Place place) || place.PlacingDef == null)
        {
            return false;
        }

        string defName = place.PlacingDef.defName;
        if (string.IsNullOrEmpty(defName))
        {
            return false;
        }
        if (defName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        // "dining chair" 这类带空格输入：去掉空格后与驼峰 defName（DiningChair）比较。
        string compactQuery = query.Replace(" ", string.Empty);
        return compactQuery.Length != query.Length
            && compactQuery.Length > 1
            && defName.Replace(" ", string.Empty).IndexOf(compactQuery, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool MatchesAnyLabel(Designator designator, string query)
    {
        if (PinyinSearch.Matches(((Command)designator).Label, query))
        {
            return true;
        }

        if (!(designator is Designator_Place place) || place.PlacingDef == null)
        {
            return false;
        }

        BuildableDef def = place.PlacingDef;
        if (def.designationCategory != null && PinyinSearch.Matches(def.designationCategory.label, query))
        {
            return true;
        }
        if (def.modContentPack != null && PinyinSearch.Matches(def.modContentPack.Name, query))
        {
            return true;
        }
        if (def.stuffCategories != null)
        {
            foreach (StuffCategoryDef category in def.stuffCategories)
            {
                if (PinyinSearch.Matches(category.label, query))
                {
                    return true;
                }
            }
        }

        return false;
    }
}

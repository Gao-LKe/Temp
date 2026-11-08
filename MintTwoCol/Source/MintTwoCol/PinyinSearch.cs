using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace MintTwoCol;

// 软依赖：仅在检测到 Pinyin Quick Search（cedaro.PinyinQuickSearch）时启用。
// 该 Mod 公开了 PinyinQuickSearch.PinyinTool.GetSearchTerm(string)，返回
// "全拼|首字母|原文"，可直接复用其拼音转换与设置。
internal static class PinyinSearch
{
    private static bool resolved;
    private static MethodInfo getSearchTerm;
    private static bool failureLogged;

    internal static bool Available
    {
        get
        {
            Resolve();
            return getSearchTerm != null;
        }
    }

    private static void Resolve()
    {
        if (resolved)
        {
            return;
        }
        resolved = true;
        try
        {
            Type type = AccessTools.TypeByName("PinyinQuickSearch.PinyinTool");
            if (type == null)
            {
                return;
            }
            getSearchTerm = type.GetMethod("GetSearchTerm", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
            if (getSearchTerm != null)
            {
                Log.Message($"[{MintTwoColMod.HarmonyId}] Pinyin Quick Search detected, build search accepts pinyin.");
            }
        }
        catch (Exception exception)
        {
            Log.Warning($"[{MintTwoColMod.HarmonyId}] Pinyin Quick Search probe failed: {exception.Message}");
        }
    }

    internal static bool Matches(string label, string query)
    {
        if (string.IsNullOrEmpty(label) || string.IsNullOrEmpty(query))
        {
            return false;
        }

        try
        {
            if (!(getSearchTerm.Invoke(null, new object[] { label }) is string term))
            {
                return false;
            }
            return term.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }
        catch (Exception exception)
        {
            if (!failureLogged)
            {
                failureLogged = true;
                Log.Warning($"[{MintTwoColMod.HarmonyId}] Pinyin lookup failed, pinyin search disabled: {exception.Message}");
            }
            getSearchTerm = null;
            return false;
        }
    }
}

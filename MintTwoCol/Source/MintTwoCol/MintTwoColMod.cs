using HarmonyLib;
using Verse;

namespace MintTwoCol;

public sealed class MintTwoColMod : Mod
{
    public const string HarmonyId = "GK.MintTwoCol";

    internal static MintTwoColMod Instance { get; private set; }
    internal static TwoColSettings Settings { get; private set; }
    internal static Harmony Harmony { get; private set; }

    public MintTwoColMod(ModContentPack content) : base(content)
    {
        Instance = this;
        Settings = GetSettings<TwoColSettings>();
        Harmony = new Harmony(HarmonyId);

        LongEventHandler.QueueLongEvent(ApplyPatches, "MintTwoCol.LoadingLabel", false, null);

        Log.Message($"[{HarmonyId}] Loaded.");
    }

    private static void ApplyPatches()
    {
        try
        {
            MintBridge.Initialize(Harmony);
        }
        catch (System.Exception exception)
        {
            Log.Error($"[{HarmonyId}] Patch setup failed, Dubs Mint Menus stays unchanged: {exception}");
        }
    }
}

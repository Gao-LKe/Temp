using System;
using System.Collections.Generic;
using Verse;

namespace MintTwoCol;

public sealed class TwoColSettings : ModSettings
{
    public List<string> LeftOrder = new List<string>();
    public List<string> RightOrder = new List<string>();

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref LeftOrder, "leftOrder", LookMode.Value, Array.Empty<object>());
        Scribe_Collections.Look(ref RightOrder, "rightOrder", LookMode.Value, Array.Empty<object>());
        if (LeftOrder == null)
        {
            LeftOrder = new List<string>();
        }
        if (RightOrder == null)
        {
            RightOrder = new List<string>();
        }
    }
}

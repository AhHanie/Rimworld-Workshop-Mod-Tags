using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace WSModTags
{
    public class Mod: Verse.Mod
    {
        public Mod(ModContentPack content) : base(content)
        {
            LongEventHandler.QueueLongEvent(Init, "WSModTags.LoadingLabel", doAsynchronously: true, null);
        }

        private void Init()
        {
            new Harmony("sk.wsmodtags").PatchAll();
        }
    }
}

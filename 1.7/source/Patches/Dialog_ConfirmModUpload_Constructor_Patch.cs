using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WSModTags.Patches
{
    [HarmonyPatch(typeof(Dialog_ConfirmModUpload), MethodType.Constructor, typeof(ModMetaData), typeof(Action))]
    public static class Dialog_ConfirmModUpload_Constructor_Patch
    {
        public static void Postfix(ModMetaData mod)
        {
            if (mod == null)
            {
                return;
            }

            List<Version> versions = mod.SupportedVersionsReadOnly;
            mod.uploadSkipSourceTextures = versions != null && versions.Count > 0 && versions.All(IsAtLeast17);
        }

        private static bool IsAtLeast17(Version version)
        {
            return version != null && (version.Major > 1 || (version.Major == 1 && version.Minor >= 7));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;

namespace WSModTags.Patches
{
    [HarmonyPatch(typeof(ModMetaData), nameof(ModMetaData.GetWorkshopTags))]
    public static class ModMetaData_GetWorkshopTags_Patch
    {
        public static void Postfix(ModMetaData __instance, ref IList<string> __result)
        {
            List<string> mergedTags = __result == null ? new List<string>() : __result.ToList();
            List<string> customTags = WorkshopTagLoader.GetCustomTagsForMerge(__instance);
            WorkshopTagValidationResult validation = WorkshopTagValidator.ValidateRawTags(customTags);

            if (!validation.IsValid)
            {
                foreach (string error in validation.Errors)
                {
                    Logger.Warning(error);
                }

                __result = Deduplicate(mergedTags);
                return;
            }

            mergedTags.AddRange(validation.ValidTags);
            __result = Deduplicate(mergedTags);
        }

        private static List<string> Deduplicate(IEnumerable<string> tags)
        {
            List<string> result = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string tag in tags)
            {
                if (!string.IsNullOrWhiteSpace(tag) && seen.Add(tag.Trim()))
                {
                    result.Add(tag.Trim());
                }
            }

            return result;
        }
    }
}

using System.Reflection;
using System.Linq;
using HarmonyLib;
using Verse;
using Verse.Steam;

namespace WSModTags.Patches
{
    [HarmonyPatch]
    public static class Workshop_Upload_Patch
    {
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Workshop), "Upload");
        }

        public static bool Prefix(WorkshopUploadable item)
        {
            ModMetaData mod = item as ModMetaData;
            return mod == null || WorkshopUploadGuard.AllowUpload(mod);
        }
    }

    [HarmonyPatch(typeof(Workshop), "BakeAndUpload", typeof(ModMetaData))]
    public static class Workshop_BakeAndUpload_Patch
    {
        public static bool Prefix(ModMetaData mod)
        {
            return WorkshopUploadGuard.AllowUpload(mod);
        }
    }

    public static class WorkshopUploadGuard
    {
        public static bool AllowUpload(ModMetaData mod)
        {
            WorkshopTagValidationResult validation = WorkshopTagValidator.ValidateConfiguredTags(mod);
            if (validation.IsValid)
            {
                return true;
            }

            string text = "WSModTags.InvalidTagsBlocked".Translate() + "\n\n" +
                validation.Errors.Select(error => "  - " + error).ToLineList();
            Find.WindowStack.Add(new Dialog_MessageBox(text));
            return false;
        }
    }
}

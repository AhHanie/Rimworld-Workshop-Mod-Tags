using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace WSModTags.Patches
{
    [HarmonyPatch(typeof(Dialog_ConfirmModUpload), nameof(Dialog_ConfirmModUpload.DoWindowContents))]
    public static class Dialog_ConfirmModUpload_DoWindowContents_Patch
    {
        public static void Postfix(ModMetaData ___mod, Rect inRect)
        {
            if (___mod == null)
            {
                return;
            }

            WorkshopTagValidationResult validation = WorkshopTagValidator.ValidateConfiguredTags(___mod);
            Rect buttonRect = new Rect(inRect.xMax - 210f, inRect.height - 35f - 24f - 10f, 200f, 24f);
            string label = validation.IsValid ? "WSModTags.EditTags".Translate() : "WSModTags.EditTagsInvalid".Translate();

            Color oldColor = GUI.color;
            if (!validation.IsValid)
            {
                GUI.color = ColorLibrary.RedReadable;
            }

            bool clicked = Widgets.ButtonText(buttonRect, label);
            GUI.color = oldColor;

            if (clicked)
            {
                Find.WindowStack.Add(new WorkshopTagsDialog(___mod));
            }
        }
    }
}

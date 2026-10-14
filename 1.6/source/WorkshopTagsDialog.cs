using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace WSModTags
{
    public class WorkshopTagsDialog : Window
    {
        private const float ChipHeight = 28f;
        private const float ChipGap = 6f;
        private const float SectionGap = 12f;

        private static readonly string[] SuggestedTags =
        {
            "Quality of Life",
            "Gameplay",
            "Balance",
            "Biotech",
            "Ideology",
            "Royalty",
            "Anomaly",
            "Odyssey"
        };

        private readonly ModMetaData mod;
        private readonly List<string> tags;
        private readonly List<string> loadErrors = new List<string>();
        private string newTag = "";

        public override Vector2 InitialSize
        {
            get { return new Vector2(680f, 560f); }
        }

        public WorkshopTagsDialog(ModMetaData mod)
        {
            this.mod = mod;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;

            List<string> loadedTags;
            List<string> errors;
            WorkshopTagLoader.TryLoadCustomTags(mod, out loadedTags, out errors);
            tags = loadedTags;
            loadErrors.AddRange(errors);
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;

            Rect titleRect = new Rect(inRect.x, inRect.y, inRect.width, 30f);
            Widgets.Label(titleRect, "WSModTags.EditTagsTitle".Translate(mod.Name));

            float curY = titleRect.yMax + SectionGap;

            curY = DrawSectionLabel(inRect, curY, "WSModTags.AutomaticTags".Translate());
            curY = DrawChips(new Rect(inRect.x, curY, inRect.width, 64f), AutomaticTags(), removable: false) + SectionGap;

            curY = DrawSectionLabel(inRect, curY, "WSModTags.CustomTags".Translate());
            Rect customRect = new Rect(inRect.x, curY, inRect.width, 112f);
            if (tags.Count == 0)
            {
                GUI.color = Color.gray;
                Widgets.Label(customRect, "WSModTags.NoCustomTags".Translate());
                GUI.color = Color.white;
            }
            else
            {
                DrawChips(customRect, tags, removable: true);
            }
            curY = customRect.yMax + SectionGap;

            curY = DrawAddTagControls(inRect, curY);

            curY = DrawSectionLabel(inRect, curY + SectionGap, "WSModTags.Suggestions".Translate());
            curY = DrawSuggestions(new Rect(inRect.x, curY, inRect.width, 72f)) + SectionGap;

            Rect infoRect = new Rect(inRect.x, curY, inRect.width, 82f);
            DrawValidationInfo(infoRect);

            Rect pathRect = new Rect(inRect.x, inRect.yMax - 70f, inRect.width, 24f);
            GUI.color = Color.gray;
            Widgets.Label(pathRect, "WSModTags.SavesTo".Translate("About/WorkshopTags.xml"));
            GUI.color = Color.white;

            Rect buttonRect = new Rect(inRect.x, inRect.yMax - 35f, inRect.width, 35f);
            if (Widgets.ButtonText(new Rect(buttonRect.xMax - 210f, buttonRect.y, 100f, buttonRect.height), "Cancel".Translate()))
            {
                Close();
            }

            WorkshopTagValidationResult validation = WorkshopTagValidator.ValidateRawTags(tags);
            bool canSave = validation.IsValid;
            if (Widgets.ButtonText(new Rect(buttonRect.xMax - 100f, buttonRect.y, 100f, buttonRect.height), "Save".Translate(), active: canSave))
            {
                Save(validation);
            }
        }

        private float DrawSectionLabel(Rect inRect, float y, string label)
        {
            Rect rect = new Rect(inRect.x, y, inRect.width, 24f);
            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            Widgets.Label(rect, label);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            return rect.yMax + 4f;
        }

        private float DrawAddTagControls(Rect inRect, float y)
        {
            Rect addRect = new Rect(inRect.x, y, inRect.width, 30f);
            Rect addFieldRect = new Rect(addRect.x, addRect.y, addRect.width - 110f, addRect.height);
            Rect addButtonRect = new Rect(addFieldRect.xMax + 10f, addRect.y, 100f, addRect.height);
            newTag = Widgets.TextField(addFieldRect, newTag);

            string inputError = CurrentInputError();
            bool canAdd = !string.IsNullOrWhiteSpace(newTag) && inputError == null;
            if (Widgets.ButtonText(addButtonRect, "WSModTags.AddTag".Translate(), active: canAdd))
            {
                AddTag(newTag);
                newTag = "";
                loadErrors.Clear();
            }

            Rect hintRect = new Rect(inRect.x, addRect.yMax + 4f, inRect.width, 24f);
            if (inputError == null)
            {
                GUI.color = Color.gray;
                Widgets.Label(hintRect, "WSModTags.TagHelp".Translate());
                GUI.color = Color.white;
            }
            else if (!string.IsNullOrWhiteSpace(newTag))
            {
                GUI.color = ColorLibrary.RedReadable;
                Widgets.Label(hintRect, inputError);
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = Color.gray;
                Widgets.Label(hintRect, "WSModTags.TagHelp".Translate());
                GUI.color = Color.white;
            }

            return hintRect.yMax;
        }

        private float DrawSuggestions(Rect rect)
        {
            List<string> suggestions = SuggestedTags.Where(tag => !HasTag(tag) && !AutomaticTags().Contains(tag, StringComparer.OrdinalIgnoreCase)).ToList();
            if (suggestions.Count == 0)
            {
                GUI.color = Color.gray;
                Widgets.Label(rect, "WSModTags.NoSuggestions".Translate());
                GUI.color = Color.white;
                return rect.yMax;
            }

            return DrawChips(rect, suggestions, removable: false, suggestion: true);
        }

        private float DrawChips(Rect rect, IList<string> chipTags, bool removable, bool suggestion = false)
        {
            float x = rect.x;
            float y = rect.y;
            float maxY = y + ChipHeight;

            for (int i = 0; i < chipTags.Count; i++)
            {
                string tag = chipTags[i];
                string label = removable ? tag + "  x" : tag;
                float width = Mathf.Min(rect.width, Mathf.Max(70f, Text.CalcSize(label).x + 22f));

                if (x + width > rect.xMax)
                {
                    x = rect.x;
                    y += ChipHeight + ChipGap;
                }

                if (y + ChipHeight > rect.yMax)
                {
                    break;
                }

                Rect chipRect = new Rect(x, y, width, ChipHeight);
                bool clicked = Widgets.ButtonText(chipRect, label, active: !(!removable && !suggestion));
                if (clicked && removable)
                {
                    tags.RemoveAt(i);
                    loadErrors.Clear();
                    i--;
                }
                else if (clicked && suggestion)
                {
                    AddTag(tag);
                    loadErrors.Clear();
                }

                x += width + ChipGap;
                maxY = Mathf.Max(maxY, chipRect.yMax);
            }

            return maxY;
        }

        private void DrawValidationInfo(Rect rect)
        {
            List<string> messages = new List<string>();
            messages.AddRange(loadErrors);
            messages.AddRange(WorkshopTagValidator.ValidateRawTags(tags).Errors);

            if (messages.Count == 0)
            {
                GUI.color = Color.gray;
                Widgets.Label(rect, "WSModTags.SaveRequired".Translate());
                GUI.color = Color.white;
                return;
            }

            GUI.color = ColorLibrary.RedReadable;
            Widgets.Label(rect, messages.Select(error => "- " + error).ToLineList());
            GUI.color = Color.white;
        }

        private void AddTag(string tag)
        {
            if (!string.IsNullOrWhiteSpace(tag) && !HasTag(tag))
            {
                tags.Add(tag.Trim());
            }
        }

        private void Save(WorkshopTagValidationResult validation)
        {
            string path = WorkshopTagLoader.WorkshopTagsPathFor(mod);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            XDocument document = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement("WorkshopTags", validation.ValidTags.Select(tag => new XElement("li", tag))));
            document.Save(path);

            tags.Clear();
            tags.AddRange(validation.ValidTags);
            loadErrors.Clear();
            WorkshopTagLoader.Invalidate(mod);
            Messages.Message("WSModTags.TagsSaved".Translate(), MessageTypeDefOf.TaskCompletion, historical: false);
            Close();
        }

        private string CurrentInputError()
        {
            if (string.IsNullOrWhiteSpace(newTag))
            {
                return null;
            }

            if (HasTag(newTag))
            {
                return "WSModTags.DuplicateTag".Translate(newTag.Trim());
            }

            WorkshopTagValidationResult validation = WorkshopTagValidator.ValidateRawTags(new[] { newTag });
            return validation.IsValid ? null : validation.Errors.FirstOrDefault();
        }

        private bool HasTag(string tag)
        {
            string trimmed = tag == null ? "" : tag.Trim();
            return tags.Any(existing => existing.Equals(trimmed, StringComparison.OrdinalIgnoreCase));
        }

        private List<string> AutomaticTags()
        {
            List<string> automaticTags = new List<string> { mod.translationMod ? "Translation" : "Mod" };
            foreach (System.Version version in mod.SupportedVersionsReadOnly)
            {
                string tag = version.Major + "." + version.Minor;
                if (!automaticTags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                {
                    automaticTags.Add(tag);
                }
            }

            return automaticTags;
        }
    }
}

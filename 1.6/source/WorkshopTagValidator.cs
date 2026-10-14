using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Verse;

namespace WSModTags
{
    public static class WorkshopTagValidator
    {
        private static readonly Regex VersionTagRegex = new Regex(@"^\d+(\.\d+)+$", RegexOptions.Compiled);

        public static WorkshopTagValidationResult ValidateConfiguredTags(ModMetaData mod)
        {
            List<string> rawTags;
            List<string> loadErrors;

            if (!WorkshopTagLoader.TryLoadCustomTags(mod, out rawTags, out loadErrors))
            {
                return new WorkshopTagValidationResult(new List<string>(), loadErrors);
            }

            WorkshopTagValidationResult validation = ValidateRawTags(rawTags);
            if (loadErrors.Count > 0)
            {
                validation.Errors.AddRange(loadErrors);
            }

            return validation;
        }

        public static WorkshopTagValidationResult ValidateRawTags(IEnumerable<string> rawTags)
        {
            List<string> validTags = new List<string>();
            List<string> errors = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (rawTags == null)
            {
                return new WorkshopTagValidationResult(validTags, errors);
            }

            foreach (string rawTag in rawTags)
            {
                string tag = rawTag == null ? null : rawTag.Trim();

                if (string.IsNullOrEmpty(tag))
                {
                    errors.Add("Tag cannot be empty.");
                    continue;
                }

                if (tag.Length > 255)
                {
                    errors.Add("Tag '" + TruncateForMessage(tag) + "' is longer than 255 characters.");
                    continue;
                }

                if (tag.Contains(","))
                {
                    errors.Add("Tag '" + tag + "' contains a comma, which Steam does not allow.");
                    continue;
                }

                if (tag.Any(char.IsControl))
                {
                    errors.Add("Tag '" + TruncateForMessage(tag) + "' contains a control character.");
                    continue;
                }

                if (VersionTagRegex.IsMatch(tag))
                {
                    errors.Add("Tag '" + tag + "' looks like a RimWorld version. Version tags are added automatically.");
                    continue;
                }

                if (seen.Add(tag))
                {
                    validTags.Add(tag);
                }
            }

            return new WorkshopTagValidationResult(validTags, errors);
        }

        private static string TruncateForMessage(string value)
        {
            if (value == null || value.Length <= 60)
            {
                return value;
            }

            return value.Substring(0, 60) + "...";
        }
    }
}

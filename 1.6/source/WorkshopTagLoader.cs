using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Verse;

namespace WSModTags
{
    public static class WorkshopTagLoader
    {
        private const string AboutFolderName = "About";
        private const string AboutFileName = "About.xml";
        private const string WorkshopTagsFileName = "WorkshopTags.xml";

        private static readonly Dictionary<string, CacheEntry> Cache = new Dictionary<string, CacheEntry>();

        public static bool TryLoadCustomTags(ModMetaData mod, out List<string> tags, out List<string> errors)
        {
            tags = new List<string>();
            errors = new List<string>();

            if (mod == null || mod.RootDir == null)
            {
                return true;
            }

            string rootPath = mod.RootDir.FullName;
            string workshopTagsPath = Path.Combine(rootPath, AboutFolderName, WorkshopTagsFileName);
            string aboutPath = Path.Combine(rootPath, AboutFolderName, AboutFileName);
            DateTime workshopWriteTime = GetWriteTimeUtc(workshopTagsPath);
            DateTime aboutWriteTime = GetWriteTimeUtc(aboutPath);

            CacheEntry cached;
            if (Cache.TryGetValue(rootPath, out cached) &&
                cached.WorkshopTagsWriteTimeUtc == workshopWriteTime &&
                cached.AboutWriteTimeUtc == aboutWriteTime)
            {
                tags = new List<string>(cached.Tags);
                errors = new List<string>(cached.Errors);
                return errors.Count == 0;
            }

            List<string> loadedTags;
            List<string> loadedErrors;
            bool ok = LoadUncached(workshopTagsPath, aboutPath, out loadedTags, out loadedErrors);

            Cache[rootPath] = new CacheEntry(workshopWriteTime, aboutWriteTime, loadedTags, loadedErrors);
            tags = loadedTags;
            errors = loadedErrors;
            return ok;
        }

        public static List<string> GetCustomTagsForMerge(ModMetaData mod)
        {
            List<string> tags;
            List<string> errors;
            if (!TryLoadCustomTags(mod, out tags, out errors))
            {
                foreach (string error in errors)
                {
                    Logger.Warning(error);
                }

                return new List<string>();
            }

            return tags;
        }

        public static void Invalidate(ModMetaData mod)
        {
            if (mod != null && mod.RootDir != null)
            {
                Cache.Remove(mod.RootDir.FullName);
            }
        }

        public static string WorkshopTagsPathFor(ModMetaData mod)
        {
            return Path.Combine(mod.RootDir.FullName, AboutFolderName, WorkshopTagsFileName);
        }

        private static bool LoadUncached(string workshopTagsPath, string aboutPath, out List<string> tags, out List<string> errors)
        {
            tags = new List<string>();
            errors = new List<string>();

            if (File.Exists(workshopTagsPath))
            {
                return TryReadStandaloneTags(workshopTagsPath, out tags, out errors);
            }

            if (File.Exists(aboutPath))
            {
                return TryReadAboutTags(aboutPath, out tags, out errors);
            }

            return true;
        }

        private static bool TryReadStandaloneTags(string path, out List<string> tags, out List<string> errors)
        {
            tags = new List<string>();
            errors = new List<string>();

            try
            {
                XDocument document = XDocument.Load(path, LoadOptions.PreserveWhitespace);
                XElement root = document.Root;
                if (root == null)
                {
                    return true;
                }

                tags = root.Elements().Where(e => e.Name.LocalName.Equals("li", StringComparison.OrdinalIgnoreCase))
                    .Select(e => e.Value).ToList();
                return true;
            }
            catch (Exception exception)
            {
                errors.Add("Could not read " + path + ": " + exception.Message);
                Logger.Exception(exception, "WorkshopTags.xml");
                return false;
            }
        }

        private static bool TryReadAboutTags(string path, out List<string> tags, out List<string> errors)
        {
            tags = new List<string>();
            errors = new List<string>();

            try
            {
                XDocument document = XDocument.Load(path, LoadOptions.PreserveWhitespace);
                XElement root = document.Root;
                if (root == null)
                {
                    return true;
                }

                XElement tagNode = root.Elements()
                    .FirstOrDefault(e => e.Name.LocalName.Equals("workshopTags", StringComparison.OrdinalIgnoreCase));

                if (tagNode == null)
                {
                    return true;
                }

                tags = tagNode.Elements().Where(e => e.Name.LocalName.Equals("li", StringComparison.OrdinalIgnoreCase))
                    .Select(e => e.Value).ToList();
                return true;
            }
            catch (Exception exception)
            {
                errors.Add("Could not read workshopTags from " + path + ": " + exception.Message);
                Logger.Exception(exception, "About.xml workshopTags");
                return false;
            }
        }

        private static DateTime GetWriteTimeUtc(string path)
        {
            return File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
        }

        private class CacheEntry
        {
            public CacheEntry(DateTime workshopTagsWriteTimeUtc, DateTime aboutWriteTimeUtc, List<string> tags, List<string> errors)
            {
                WorkshopTagsWriteTimeUtc = workshopTagsWriteTimeUtc;
                AboutWriteTimeUtc = aboutWriteTimeUtc;
                Tags = new List<string>(tags);
                Errors = new List<string>(errors);
            }

            public DateTime WorkshopTagsWriteTimeUtc { get; private set; }

            public DateTime AboutWriteTimeUtc { get; private set; }

            public List<string> Tags { get; private set; }

            public List<string> Errors { get; private set; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using Verse;

namespace MainButtonBarSystem
{
    /// <summary>
    /// Reads the themes of a theme project that is not an active mod, with the game's own def parser
    /// (<c>DirectXmlToObject</c>), and adds them to or removes them from <c>DefDatabase&lt;BarAppearanceDef&gt;</c>
    /// while the game runs (design spec 2026-11-07 §5.2). Problems are warnings, never errors: an author saving a
    /// half-typed XML must not fill the log with red text.
    /// </summary>
    internal static class ThemeProjectLoader
    {
        private const string DefNode = "MainButtonBarSystem.BarAppearanceDef";

        /// <summary>Defs that could not be taken out of the database (its Remove is private): kept but no longer listed.</summary>
        private static readonly HashSet<BarAppearanceDef> hidden = new HashSet<BarAppearanceDef>();

        private static readonly MethodInfo RemoveMethod = typeof(DefDatabase<BarAppearanceDef>).GetMethod("Remove", BindingFlags.Static | BindingFlags.NonPublic);

        internal static bool IsHidden(BarAppearanceDef def) => def != null && hidden.Contains(def);

        /// <summary>After a play-data reload the hidden defs are gone with the old database.</summary>
        internal static void ForgetHidden() => hidden.Clear();

        /// <summary>
        /// Every BarAppearanceDef in the folder's Defs (sub-folders too), with <see cref="BarAppearanceDef.projectFolder"/>
        /// set. Null when a file cannot be read right now (being written, or not valid XML yet): try again later.
        /// </summary>
        internal static List<BarAppearanceDef> ParseFolder(string folder, string projectName)
        {
            var result = new List<BarAppearanceDef>();
            string defsFolder = Path.Combine(folder, "Defs");
            if (!Directory.Exists(defsFolder)) return result;
            string[] files;
            try
            {
                files = Directory.GetFiles(defsFolder, "*.xml", SearchOption.AllDirectories);
            }
            catch (Exception)
            {
                return null;
            }
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            foreach (var file in files)
            {
                var doc = new XmlDocument { XmlResolver = null };
                try
                {
                    using (var reader = XmlReader.Create(file, settings))
                        doc.Load(reader);
                }
                catch (XmlException e)
                {
                    Warn(projectName, Path.GetFileName(file) + ": not valid XML yet (" + e.Message + "); the theme keeps its last good version.");
                    return null;
                }
                catch (Exception)
                {
                    return null;
                }
                if (doc.DocumentElement == null) continue;
                foreach (XmlNode child in doc.DocumentElement.ChildNodes)
                {
                    if (child is not XmlElement element || element.Name != DefNode) continue;
                    var def = ParseDef(element, folder, projectName, Path.GetFileName(file));
                    if (def != null) result.Add(def);
                }
            }
            return result;
        }

        private static BarAppearanceDef ParseDef(XmlElement element, string folder, string projectName, string file)
        {
            if (string.Equals(element.GetAttribute("Abstract"), "true", StringComparison.OrdinalIgnoreCase)) return null;
            if (element.HasAttribute("ParentName"))
            {
                Warn(projectName, file + ": a theme with ParentName is skipped; inheritance works once the theme is an active mod.");
                return null;
            }
            var node = (XmlElement)element.CloneNode(true);
            if (!KeepNode(node, typeof(BarAppearanceDef), out var defType, projectName, file, DefNode)) return null;
            Sanitize(node, defType, projectName, file);
            BarAppearanceDef def;
            try
            {
                def = DirectXmlToObject.ObjectFromXml<BarAppearanceDef>(node, true);
            }
            catch (Exception e)
            {
                Warn(projectName, file + ": could not read a theme: " + e.Message);
                return null;
            }
            if (def == null || def.defName.NullOrEmpty() || def.defName == Def.DefaultDefName)
            {
                Warn(projectName, file + ": a theme without a defName is skipped.");
                return null;
            }
            // The game's own check indexes an empty description and throws; an empty one simply means none.
            def.description = def.description?.Trim();
            if (def.description.NullOrEmpty()) def.description = null;
            // A worker that cannot be made would throw on every frame the bar is drawn.
            var worker = def.workerClass;
            if (worker == null || !typeof(BarAppearanceWorker).IsAssignableFrom(worker) || worker.IsAbstract || worker.GetConstructor(Type.EmptyTypes) == null)
            {
                Warn(projectName, $"{file}: {def.defName}: workerClass {worker?.FullName ?? "(none)"} is not a usable {nameof(BarAppearanceWorker)}; the standard one is used.");
                def.workerClass = typeof(BarAppearanceWorker);
            }
            def.ResolveDefNameHash();
            def.projectFolder = folder;
            return def;
        }

        /// <summary>
        /// Takes out of a theme's XML, with a warning, everything the game's parser would log as an error (an author
        /// saving a file being edited must not get red text): nodes that are not fields of <paramref name="type"/> (by
        /// name or LoadAlias), a field written twice, a Class that does not exist, and values that do not parse (a
        /// misspelt enum, a number or colour typed wrong, an unknown type name). Recursive, lists included.
        /// </summary>
        private static void Sanitize(XmlNode node, Type type, string projectName, string file)
        {
            if (type == null || type == typeof(string) || type.IsPrimitive || type.IsEnum) return;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var item = type.GetGenericArguments()[0];
                foreach (var li in node.ChildNodes.OfType<XmlElement>().ToList())
                {
                    if (!KeepNode(li, item, out var liType, projectName, file, node.Name)) node.RemoveChild(li);
                    else if (HasElementChildren(li)) Sanitize(li, liType, projectName, file);
                }
                return;
            }
            if (type.IsGenericType) return;   // dictionaries and the like: left to the game
            var seen = new HashSet<string>();
            foreach (var child in node.ChildNodes.OfType<XmlElement>().ToList())
            {
                var field = FieldFor(type, child.Name);
                if (field == null)
                {
                    Warn(projectName, $"{file}: <{child.Name}> is not a field of {type.Name}; it is skipped.");
                    node.RemoveChild(child);
                    continue;
                }
                if (!seen.Add(field.Name))
                {
                    Warn(projectName, $"{file}: <{child.Name}> is written twice in {type.Name}; the later one is skipped.");
                    node.RemoveChild(child);
                    continue;
                }
                if (!KeepNode(child, field.FieldType, out var childType, projectName, file, child.Name)) node.RemoveChild(child);
                else if (HasElementChildren(child)) Sanitize(child, childType, projectName, file);
            }
        }

        /// <summary>
        /// Whether a node can be read as <paramref name="declared"/>: its Class (if any) exists and fits, and a plain
        /// text value parses. <paramref name="type"/> is the type it is read as.
        /// </summary>
        private static bool KeepNode(XmlElement node, Type declared, out Type type, string projectName, string file, string what)
        {
            type = declared;
            string classAttr = node.GetAttribute("Class");
            if (!classAttr.NullOrEmpty())
            {
                var named = GenTypes.GetTypeInAnyAssembly(classAttr);
                if (named == null || !declared.IsAssignableFrom(named))
                {
                    Warn(projectName, $"{file}: <{what}> has Class=\"{classAttr}\", which is not a {declared.Name}; it is skipped.");
                    return false;
                }
                type = named;
            }
            if (HasElementChildren(node)) return true;
            string text = node.InnerText?.Trim();
            if (text.NullOrEmpty() || type == typeof(string)) return true;
            if (!ValueParses(text, type))
            {
                Warn(projectName, $"{file}: <{what}> \"{text}\" is not a valid {type.Name}; it is skipped.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Whether the game reads <paramref name="text"/> as <paramref name="type"/> without an error. Type names are
        /// looked up directly, because the game's type parser logs an error instead of throwing.
        /// </summary>
        private static bool ValueParses(string text, Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (type == typeof(Type)) return text == "null" || text == "Null" || GenTypes.GetTypeInAnyAssembly(text) != null;
            // Def references and types with their own XML loader: left to the game.
            if (GenTypes.IsDef(type) || type.GetMethod("LoadDataFromXmlCustom", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != null) return true;
            // A whole object or list written as plain text cannot be read.
            if (!ParseHelper.HandlesType(type)) return false;
            try
            {
                ParseHelper.FromString(text, type);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool HasElementChildren(XmlNode node) => node.ChildNodes.OfType<XmlElement>().Any();

        private static FieldInfo FieldFor(Type type, string name)
        {
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
            {
                foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    bool match = field.Name == name ||
                                 field.GetCustomAttributes<LoadAliasAttribute>(true).Any(a => a.alias.EqualsIgnoreCase(name));
                    if (!match) continue;
                    var unsaved = field.GetCustomAttribute<UnsavedAttribute>(true);
                    return unsaved != null && !unsaved.allowLoading ? null : field;
                }
            }
            return null;
        }

        /// <summary>
        /// Adds a parsed theme to the def database. A defName that is already taken (by another mod's theme or another
        /// project) is skipped with a warning; a def hidden earlier under the same name comes back instead. Returns the
        /// def now in the database, or null.
        /// </summary>
        internal static BarAppearanceDef TryAdd(BarAppearanceDef def, string projectName)
        {
            var existing = DefDatabase<BarAppearanceDef>.GetNamedSilentFail(def.defName);
            if (existing != null)
            {
                if (hidden.Remove(existing))
                {
                    existing.projectFolder = def.projectFolder;
                    existing.CopyFrom(def);
                    existing.ClearCaches();
                    existing.ReleaseTextures();
                    Check(existing, projectName);
                    return existing;
                }
                Warn(projectName, $"the theme {def.defName} is skipped: another theme already has that defName (a copied project needs its own packageId and defName).");
                return null;
            }
            def.shortHash = FreeShortHash(def.defName);
            DefDatabase<BarAppearanceDef>.Add(def);
            DefDatabase<BarAppearanceDef>.InitializeShortHashDictionary();
            Check(def, projectName);
            return def;
        }

        private static void Check(BarAppearanceDef def, string projectName)
        {
            try
            {
                def.ResolveReferences();
            }
            catch (Exception e)
            {
                Warn(projectName, def.defName + ": " + e.Message);
            }
            try
            {
                foreach (var error in def.ConfigErrors())
                    Warn(projectName, def.defName + ": " + error);
            }
            catch (Exception e)
            {
                Warn(projectName, def.defName + ": " + e.Message);
            }
        }

        /// <summary>A short hash no other theme has, the way the game gives them (it gives none to defs added later).</summary>
        private static ushort FreeShortHash(string defName)
        {
            ushort hash = (ushort)(GenText.StableStringHash(defName) % 65535);
            for (int tries = 0; tries < 65536 && (hash == 0 || DefDatabase<BarAppearanceDef>.GetByShortHash(hash) != null); tries++)
                hash++;
            return hash;
        }

        /// <summary>Takes a theme out of the def database (the game's own private Remove), else hides it from every list.</summary>
        internal static void Remove(BarAppearanceDef def)
        {
            def.ReleaseTextures();
            if (RemoveMethod != null)
            {
                try
                {
                    RemoveMethod.Invoke(null, new object[] { def });
                    return;
                }
                catch (Exception e)
                {
                    Log.Warning(MainButtonBarSystemMod.LogPrefix + "Could not remove the theme " + def.defName + " from the def database (" + e.Message + "); it is hidden instead.");
                }
            }
            hidden.Add(def);
        }

        /// <summary>
        /// The game is about to clear play data (a language change). 1.7's ClearAllPlayData clears the def database of
        /// every Def type it still finds after unloading the mods, which no longer includes this mod's BarAppearanceDef, so
        /// that database keeps every theme. The mod's own themes are replaced when the mods load again; a project's theme
        /// would stay with its short hash and make the game's short-hash pass log "already has short hash". Runs on the
        /// loading thread: no textures and no log here (the scan after the reload releases the textures).
        /// </summary>
        internal static void TakeOutBeforeReload(BarAppearanceDef def)
        {
            def.shortHash = 0;
            if (RemoveMethod == null || DefDatabase<BarAppearanceDef>.GetNamedSilentFail(def.defName) != def) return;
            try
            {
                RemoveMethod.Invoke(null, new object[] { def });
            }
            catch (Exception)
            {
                // Still in the database, but with no short hash: the game gives it a new one, and the scan after the
                // reload takes it out.
            }
        }

        /// <summary>Themes that could only be hidden are still in the database: the same as <see cref="TakeOutBeforeReload"/>.</summary>
        internal static void TakeOutHiddenBeforeReload()
        {
            foreach (var def in hidden.ToList()) TakeOutBeforeReload(def);
        }

        internal static void Warn(string projectName, string message) =>
            Log.WarningOnce($"{MainButtonBarSystemMod.LogPrefix}Theme project {projectName}: {message}", (projectName + "|" + message).GetHashCode());
    }
}

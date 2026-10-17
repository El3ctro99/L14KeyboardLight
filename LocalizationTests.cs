using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace L14KeyboardLight
{
    public static class LocalizationTests
    {
        public static int Run(TextWriter output)
        {
            if (output == null) throw new ArgumentNullException("output");
            string previous = UiText.Language;
            int passed = 0;
            int failed = 0;
            try
            {
                RunOne(output, "First run uses Hungarian Windows display language", delegate
                {
                    Equal("hu", UiText.ResolveLanguage(null, CultureInfo.GetCultureInfo("hu-HU")));
                    Equal("hu", UiText.ResolveLanguage("", CultureInfo.GetCultureInfo("hu")));
                }, ref passed, ref failed);

                RunOne(output, "First run falls back to English for other Windows languages", delegate
                {
                    string[] cultures = { "en-US", "en-GB", "de-DE", "ja-JP" };
                    foreach (string culture in cultures)
                        Equal("en", UiText.ResolveLanguage(null, CultureInfo.GetCultureInfo(culture)));
                    Equal("en", UiText.ResolveLanguage(null, CultureInfo.InvariantCulture));
                    Equal("en", UiText.ResolveLanguage(null, null));
                }, ref passed, ref failed);

                RunOne(output, "Saved English choice overrides Hungarian Windows", delegate
                {
                    Equal("en", UiText.ResolveLanguage("en", CultureInfo.GetCultureInfo("hu-HU")));
                }, ref passed, ref failed);

                RunOne(output, "Saved Hungarian choice overrides English Windows", delegate
                {
                    Equal("hu", UiText.ResolveLanguage("hu", CultureInfo.GetCultureInfo("en-US")));
                }, ref passed, ref failed);

                RunOne(output, "Saved language codes are case-insensitive and normalized", delegate
                {
                    Equal("en", UiText.ResolveLanguage(" EN ", CultureInfo.GetCultureInfo("hu-HU")));
                    Equal("hu", UiText.ResolveLanguage("HU", CultureInfo.GetCultureInfo("en-US")));
                }, ref passed, ref failed);

                RunOne(output, "Old settings gain the Windows language without changing existing choices", delegate
                {
                    const string oldJson = "{\"PreferredLevel\":1,\"AutoEnabled\":false}";
                    using (MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(oldJson)))
                    {
                        Settings settings = (Settings)new DataContractJsonSerializer(typeof(Settings)).ReadObject(input);
                        settings.NormalizeLanguage(CultureInfo.GetCultureInfo("hu-HU"));
                        Equal("hu", settings.Language);
                        True(settings.PreferredLevel == 1, "Old brightness choice changed");
                        True(!settings.AutoEnabled, "Old automatic fullscreen choice changed");
                    }
                }, ref passed, ref failed);

                RunOne(output, "Saved language survives a settings round-trip on a different Windows language", delegate
                {
                    Settings original = new Settings { PreferredLevel = 2, AutoEnabled = true, Language = "en" };
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Settings));
                    using (MemoryStream stream = new MemoryStream())
                    {
                        serializer.WriteObject(stream, original);
                        stream.Position = 0;
                        Settings restored = (Settings)serializer.ReadObject(stream);
                        restored.NormalizeLanguage(CultureInfo.GetCultureInfo("hu-HU"));
                        Equal("en", restored.Language);
                        True(restored.PreferredLevel == 2 && restored.AutoEnabled, "Other settings changed during language round-trip");
                    }
                }, ref passed, ref failed);

                RunOne(output, "Unsupported or blank saved choices fall back to Windows", delegate
                {
                    Equal("hu", UiText.ResolveLanguage("fr", CultureInfo.GetCultureInfo("hu-HU")));
                    Equal("en", UiText.ResolveLanguage("unknown", CultureInfo.GetCultureInfo("de-DE")));
                    Equal("hu", UiText.ResolveLanguage("  ", CultureInfo.GetCultureInfo("hu-HU")));
                }, ref passed, ref failed);

                RunOne(output, "Switching language updates all brightness and boolean labels", delegate
                {
                    UiText.SetLanguage("en");
                    Equal("Off", UiText.LevelName(0)); Equal("Low", UiText.LevelName(1)); Equal("High", UiText.LevelName(2));
                    Equal("Yes", UiText.YesNo(true)); Equal("No", UiText.YesNo(false));
                    Equal("On", UiText.OnOff(true)); Equal("Off", UiText.OnOff(false));
                    UiText.SetLanguage("hu");
                    Equal("Ki", UiText.LevelName(0)); Equal("Halvány", UiText.LevelName(1)); Equal("Erős", UiText.LevelName(2));
                    Equal("igen", UiText.YesNo(true)); Equal("nem", UiText.YesNo(false));
                    Equal("be", UiText.OnOff(true)); Equal("ki", UiText.OnOff(false));
                }, ref passed, ref failed);

                RunOne(output, "Both catalogs have the same nonempty key set", delegate
                {
                    UiText.SetLanguage("en");
                    string[] english = UiText.SupportedKeys();
                    UiText.SetLanguage("hu");
                    string[] hungarian = UiText.SupportedKeys();
                    Equal(String.Join("|", english), String.Join("|", hungarian));
                    True(english.Length > 0, "Empty localization catalog");
                    foreach (string language in new string[] { "en", "hu" })
                    {
                        UiText.SetLanguage(language);
                        foreach (string key in english)
                            True(!String.IsNullOrWhiteSpace(UiText.Get(key)), "Empty translation: " + language + "/" + key);
                    }
                }, ref passed, ref failed);

                RunOne(output, "Translated format strings preserve every argument", delegate
                {
                    UiText.SetLanguage("en");
                    string[] keys = UiText.SupportedKeys();
                    foreach (string key in keys)
                    {
                        UiText.SetLanguage("en");
                        string english = UiText.Get(key);
                        UiText.SetLanguage("hu");
                        string hungarian = UiText.Get(key);
                        Equal(Placeholders(english), Placeholders(hungarian));
                        foreach (string language in new string[] { "en", "hu" })
                        {
                            UiText.SetLanguage(language);
                            string formatted = UiText.Format(key, "ARGUMENT_ZERO", "ARGUMENT_ONE");
                            foreach (Match match in Regex.Matches(english, @"\{([0-9]+)\}"))
                                True(formatted.Contains(match.Groups[1].Value == "0" ? "ARGUMENT_ZERO" : "ARGUMENT_ONE"),
                                    "Missing formatted argument: " + language + "/" + key);
                        }
                    }
                    UiText.SetLanguage("en");
                    Equal("Machine: ThinkPad L14 Gen 3 (21C2)", UiText.Format("StatusMachine", "ThinkPad L14 Gen 3", "21C2"));
                    Equal("Unknown Lenovo response: 0x12345678. Control stopped.", UiText.Format("ErrorDriverUnknownResponse", "12345678"));
                }, ref passed, ref failed);

                RunOne(output, "Language selector remains recognizable in both languages", delegate
                {
                    foreach (string language in new string[] { "en", "hu" })
                    {
                        UiText.SetLanguage(language);
                        Equal("Language / Nyelv", UiText.Get("LanguageMenu"));
                        Equal("English", UiText.Get("English"));
                        Equal("Magyar", UiText.Get("Hungarian"));
                    }
                }, ref passed, ref failed);

                RunOne(output, "Tray tooltips fit the Windows Forms 63-character limit", delegate
                {
                    foreach (string language in new string[] { "en", "hu" })
                    {
                        UiText.SetLanguage(language);
                        True(UiText.Get("TooltipSuppressed").Length <= 63, "Fullscreen tooltip is too long");
                        True(UiText.Get("TooltipError").Length <= 63, "Error tooltip is too long");
                    }
                }, ref passed, ref failed);

                RunOne(output, "Missing keys and invalid levels fail explicitly", delegate
                {
                    Throws<ArgumentException>(delegate { UiText.Get("DoesNotExist"); });
                    Throws<ArgumentException>(delegate { UiText.Format("DoesNotExist", "x"); });
                    Throws<ArgumentOutOfRangeException>(delegate { UiText.LevelName(-1); });
                    Throws<ArgumentOutOfRangeException>(delegate { UiText.LevelName(3); });
                }, ref passed, ref failed);

                RunOne(output, "Recorded driver errors follow language changes without losing native details", delegate
                {
                    UiText.SetLanguage("hu");
                    Win32Exception original = new Win32Exception(5, "Original native diagnostic");
                    Win32Exception tagged = UiText.WithMessage(original, "ErrorDriverOpen");
                    True(Object.ReferenceEquals(original, tagged), "Tagging replaced the original exception");
                    Equal(UiText.Get("ErrorDriverOpen"), UiText.ErrorMessage(tagged));
                    UiText.SetLanguage("en");
                    Equal("The Lenovo Power Management driver (IBMPmDrv) is unavailable.", UiText.ErrorMessage(tagged));
                    True(tagged.NativeErrorCode == 5, "Native Win32 error code changed");
                    Equal("Original native diagnostic", tagged.Message);
                    Exception plain = new InvalidOperationException("Untranslated native diagnostic");
                    Equal(plain.Message, UiText.ErrorMessage(plain));
                }, ref passed, ref failed);

                RunOne(output, "Tagged format arguments are retained after language switching", delegate
                {
                    UiText.SetLanguage("hu");
                    object[] arguments = { "12345678" };
                    NotSupportedException tagged = UiText.WithMessage(new NotSupportedException("Original"), "ErrorDriverUnknownResponse", arguments);
                    arguments[0] = "changed externally";
                    Equal("Ismeretlen Lenovo-válasz: 0x12345678. A vezérlés leállt.", UiText.ErrorMessage(tagged));
                    UiText.SetLanguage("en");
                    Equal("Unknown Lenovo response: 0x12345678. Control stopped.", UiText.ErrorMessage(tagged));
                }, ref passed, ref failed);

                RunOne(output, "Supported key snapshots cannot mutate the catalog", delegate
                {
                    UiText.SetLanguage("en");
                    string[] first = UiText.SupportedKeys();
                    string original = first[0];
                    first[0] = "mutated";
                    Equal(original, UiText.SupportedKeys()[0]);
                    True(!String.IsNullOrEmpty(UiText.Get(original)), "Catalog changed through key snapshot");
                }, ref passed, ref failed);
            }
            finally { UiText.SetLanguage(previous); }
            output.WriteLine("LOCALIZATION: " + passed + " passed, " + failed + " failed");
            return failed == 0 ? 0 : 1;
        }

        private static string Placeholders(string text)
        {
            List<string> values = new List<string>();
            foreach (Match match in Regex.Matches(text, @"\{([0-9]+)\}")) values.Add(match.Groups[1].Value);
            values.Sort(StringComparer.Ordinal);
            return String.Join(",", values.ToArray());
        }

        private static void RunOne(TextWriter output, string name, Action test, ref int passed, ref int failed)
        {
            try { test(); passed++; output.WriteLine("PASS " + name); }
            catch (Exception error) { failed++; output.WriteLine("FAIL " + name + ": " + error.Message); }
        }

        private static void Equal(string expected, string actual)
        {
            if (!String.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException("Expected '" + expected + "', got '" + actual + "'.");
        }

        private static void True(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }
    }
}

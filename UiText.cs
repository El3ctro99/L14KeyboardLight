using System;
using System.Collections.Generic;
using System.Globalization;

namespace L14KeyboardLight
{
    public static class UiText
    {
        private const string ErrorKey = "L14KeyboardLight.UiText.Key";
        private const string ErrorArguments = "L14KeyboardLight.UiText.Arguments";
        private static readonly Dictionary<string, string> English = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "LevelOff", "Off" },
            { "LevelLow", "Low" },
            { "LevelHigh", "High" },
            { "AutoFullscreen", "Turn off automatically in fullscreen" },
            { "Startup", "Start with Windows" },
            { "StatusMenu", "Status…" },
            { "Exit", "Exit" },
            { "LanguageMenu", "Language / Nyelv" },
            { "English", "English" },
            { "Hungarian", "Magyar" },
            { "StatusMachine", "Machine: {0} ({1})" },
            { "StatusPreferred", "Preferred brightness: {0}" },
            { "StatusFullscreen", "Fullscreen: {0}" },
            { "StatusSuppressed", "Automatically switched off: {0}" },
            { "StatusManualOverride", "Manual override: {0}" },
            { "StatusStartup", "Startup: {0}" },
            { "StatusOk", "Status: OK" },
            { "StatusLastError", "Last error: {0}" },
            { "TooltipSuppressed", "L14 Keyboard Light – fullscreen: Off" },
            { "TooltipError", "L14 Keyboard Light – control error" },
            { "ErrorModelRead", "The machine model could not be read." },
            { "ErrorUnsupportedModel", "This release supports only the verified ThinkPad L14 Gen 3 (21C2) model." },
            { "ErrorDriverOpen", "The Lenovo Power Management driver (IBMPmDrv) is unavailable." },
            { "ErrorDriverRead", "The keyboard brightness could not be read." },
            { "ErrorDriverShortResponse", "The Lenovo driver returned an incomplete response." },
            { "ErrorDriverUnknownResponse", "Unknown Lenovo response: 0x{0}. Control stopped." },
            { "ErrorDriverWrite", "The keyboard brightness could not be set." },
            { "ErrorPowerEvents", "Could not subscribe to Windows session and display events." },
            { "ErrorInvalidLevel", "Brightness must be Off, Low or High." },
            { "ErrorInvalidRead", "The driver returned an invalid brightness level." },
            { "LogSettingsRead", "Reading settings: {0}" },
            { "Yes", "Yes" },
            { "No", "No" },
            { "On", "On" },
            { "Off", "Off" }
        };

        private static readonly Dictionary<string, string> Hungarian = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "LevelOff", "Ki" },
            { "LevelLow", "Halvány" },
            { "LevelHigh", "Erős" },
            { "AutoFullscreen", "Teljes képernyőn automatikusan kikapcsol" },
            { "Startup", "Indítás a Windowszal" },
            { "StatusMenu", "Állapot…" },
            { "Exit", "Kilépés" },
            { "LanguageMenu", "Language / Nyelv" },
            { "English", "English" },
            { "Hungarian", "Magyar" },
            { "StatusMachine", "Gép: {0} ({1})" },
            { "StatusPreferred", "Kívánt fényerő: {0}" },
            { "StatusFullscreen", "Teljes képernyő: {0}" },
            { "StatusSuppressed", "Automatikusan lekapcsolva: {0}" },
            { "StatusManualOverride", "Kézi felülbírálás: {0}" },
            { "StatusStartup", "Automatikus indulás: {0}" },
            { "StatusOk", "Állapot: rendben" },
            { "StatusLastError", "Utolsó hiba: {0}" },
            { "TooltipSuppressed", "L14 Keyboard Light – teljes képernyő: Ki" },
            { "TooltipError", "L14 Keyboard Light – vezérlési hiba" },
            { "ErrorModelRead", "Nem olvasható a gép típusa." },
            { "ErrorUnsupportedModel", "Ez a kiadás csak az ellenőrzött ThinkPad L14 Gen 3 (21C2) típushoz készült." },
            { "ErrorDriverOpen", "Nem érhető el a Lenovo energiagazdálkodási drivere (IBMPmDrv)." },
            { "ErrorDriverRead", "Nem sikerült kiolvasni a billentyűzet fényerejét." },
            { "ErrorDriverShortResponse", "A Lenovo-driver hiányos választ adott." },
            { "ErrorDriverUnknownResponse", "Ismeretlen Lenovo-válasz: 0x{0}. A vezérlés leállt." },
            { "ErrorDriverWrite", "Nem sikerült beállítani a billentyűzet fényerejét." },
            { "ErrorPowerEvents", "Nem sikerült feliratkozni a Windows munkamenet- és kijelzőeseményeire." },
            { "ErrorInvalidLevel", "A fényerő csak Ki, Halvány vagy Erős lehet." },
            { "ErrorInvalidRead", "A driver érvénytelen fényerőértéket adott vissza." },
            { "LogSettingsRead", "Beállítások olvasása: {0}" },
            { "Yes", "igen" },
            { "No", "nem" },
            { "On", "be" },
            { "Off", "ki" }
        };

        private static string language = ResolveLanguage(null, CultureInfo.CurrentUICulture);

        public static string Language { get { return language; } }

        // Settings store only canonical language codes. Older or invalid settings
        // use the Windows display language, with English for all other languages.
        public static string ResolveLanguage(string requested, CultureInfo systemCulture)
        {
            if (!String.IsNullOrWhiteSpace(requested))
            {
                string normalized = requested.Trim();
                if (normalized.Equals("hu", StringComparison.OrdinalIgnoreCase)) return "hu";
                if (normalized.Equals("en", StringComparison.OrdinalIgnoreCase)) return "en";
            }
            return systemCulture != null &&
                systemCulture.TwoLetterISOLanguageName.Equals("hu", StringComparison.OrdinalIgnoreCase) ? "hu" : "en";
        }

        public static void SetLanguage(string code)
        {
            language = ResolveLanguage(code, CultureInfo.CurrentUICulture);
        }

        private static Dictionary<string, string> Current
        {
            get { return language == "hu" ? Hungarian : English; }
        }

        public static string Get(string key)
        {
            if (key == null) throw new ArgumentNullException("key");
            string value;
            if (!Current.TryGetValue(key, out value))
                throw new ArgumentException("Unknown UI text key: " + key, "key");
            return value;
        }

        public static string Format(string key, params object[] args)
        {
            return String.Format(CultureInfo.GetCultureInfo(language), Get(key), args);
        }

        // Keep the original exception type and native error code while allowing
        // an already recorded error to follow a later language-menu choice.
        public static T WithMessage<T>(T exception, string key, params object[] args) where T : Exception
        {
            if (exception == null) throw new ArgumentNullException("exception");
            Format(key, args); // Validate keys and placeholders when tagging.
            exception.Data[ErrorKey] = key;
            exception.Data[ErrorArguments] = (object[])args.Clone();
            return exception;
        }

        public static string ErrorMessage(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException("exception");
            string key = exception.Data[ErrorKey] as string;
            object[] args = exception.Data[ErrorArguments] as object[];
            return key != null && args != null ? Format(key, args) : exception.Message;
        }

        public static string LevelName(int level)
        {
            switch (level)
            {
                case 0: return Get("LevelOff");
                case 1: return Get("LevelLow");
                case 2: return Get("LevelHigh");
                default: throw new ArgumentOutOfRangeException("level", Get("ErrorInvalidLevel"));
            }
        }

        public static string YesNo(bool value) { return Get(value ? "Yes" : "No"); }
        public static string OnOff(bool value) { return Get(value ? "On" : "Off"); }

        public static string[] SupportedKeys()
        {
            string[] keys = new string[Current.Count];
            Current.Keys.CopyTo(keys, 0);
            Array.Sort(keys, StringComparer.Ordinal);
            return keys;
        }
    }
}

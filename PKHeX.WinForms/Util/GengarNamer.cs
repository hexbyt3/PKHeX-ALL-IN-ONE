using PKHeX.Core;

namespace PKHeX.WinForms
{
    public sealed class GengarNamer : IFileNamer<PKM>
    {
        public string Name => "Gengar Namer";

        // Use underscore as the main delimiter between sections
        private const string Delimiter = "_";

        public string GetName(PKM obj)
        {
            if (obj is GBPKM gb)
                return GetGBPKM(gb);
            return GetRegular(obj);
        }

        private static string GetRegular(PKM pk)
        {
            // Build each section, ensuring each is always present (even if empty)
            string speciesSection = GetSpeciesSection(pk);
            string levelSection = GetLevelSection(pk);
            string shinySection = GetShinySection(pk);
            string teraSection = GetTeraSection(pk);
            string natureSection = GetNature(pk);
            string abilitySection = GetAbility(pk);
            string ivSection = GetIVSection(pk);
            string yearSection = GetYearSection(pk);
            string versionSection = GetVersion(pk);

            // Combine all sections with delimiter
            return string.Join(Delimiter,
                speciesSection,
                levelSection,
                shinySection,
                teraSection,
                natureSection,
                abilitySection,
                ivSection,
                yearSection,
                versionSection
            );
        }

        private static string GetSpeciesSection(PKM pk)
        {
            // Get base species name
            string speciesName = SpeciesName.GetSpeciesNameGeneration(pk.Species, (int)LanguageID.English, pk.Format);

            // Get form name using ShowdownParsing
            string formName = GetFormName(pk);
            if (!string.IsNullOrEmpty(formName))
                speciesName = $"{speciesName}-{formName}";

            // Add Gigantamax suffix if applicable
            if (pk is IGigantamax { CanGigantamax: true })
                speciesName += "-Gmax";

            return speciesName;
        }

        private static string GetLevelSection(PKM pk)
        {
            return pk.CurrentLevel.ToString();
        }

        private static string GetShinySection(PKM pk)
        {
            if (!pk.IsShiny)
                return "Normal";

            // Square shiny
            if (pk.Format >= 8 && (pk.ShinyXor == 0 || pk.FatefulEncounter || pk.Version == GameVersion.GO))
                return "Square";

            // Star shiny
            return "Star";
        }

        private static string GetTeraSection(PKM pk)
        {
            if (pk is not ITeraType t)
                return "None";

            var type = t.GetTeraType();
            return ((byte)type == TeraTypeUtil.Stellar) ? "Stellar" : type.ToString();
        }

        private static string GetIVSection(PKM pk)
        {
            return $"{pk.IV_HP}.{pk.IV_ATK}.{pk.IV_DEF}.{pk.IV_SPA}.{pk.IV_SPD}.{pk.IV_SPE}";
        }

        private static string GetYearSection(PKM pk)
        {
            int metYear = pk.MetYear;
            return metYear > 0 ? $"{metYear + 2000}" : "Unknown";
        }

        private static string GetFormName(PKM pk)
        {
            if (pk.Form == 0)
                return string.Empty;

            var strings = GameInfo.Strings;
            return ShowdownParsing.GetStringFromForm(pk.Form, strings, pk.Species, pk.Context);
        }

        private static string GetVersion(PKM pk)
        {
            return pk.Version switch
            {
                GameVersion.S or GameVersion.R => "RS",
                GameVersion.E => "Emerald",
                GameVersion.FR or GameVersion.LG => "FRLG",
                GameVersion.D or GameVersion.P => "DP",
                GameVersion.Pt => "Pt",
                GameVersion.HG or GameVersion.SS => "HGSS",
                GameVersion.B or GameVersion.W => "BW",
                GameVersion.B2 or GameVersion.W2 => "B2W2",
                GameVersion.X or GameVersion.Y => "XY",
                GameVersion.AS or GameVersion.OR => "ORAS",
                GameVersion.SN or GameVersion.MN => "SM",
                GameVersion.US or GameVersion.UM => "USUM",
                GameVersion.GO => "GO",
                GameVersion.RD or GameVersion.BU or GameVersion.YW or GameVersion.GN => "VC1",
                GameVersion.GD or GameVersion.SI or GameVersion.C => "VC2",
                GameVersion.GP or GameVersion.GE => "LGPE",
                GameVersion.SW or GameVersion.SH => "SWSH",
                GameVersion.BD or GameVersion.SP => "BDSP",
                GameVersion.PLA => "PLA",
                GameVersion.SL or GameVersion.VL => "SV",
                _ => $"Gen{pk.Format}"
            };
        }

        private static string GetNature(PKM pk)
        {
            var nature = pk.Nature;
            var strings = Util.GetNaturesList("en");
            if ((uint)nature >= strings.Length)
                nature = 0;
            return strings[(uint)nature];
        }

        private static string GetAbility(PKM pk)
        {
            int abilityIndex = pk.Ability;
            var abilityStrings = Util.GetAbilitiesList("en");
            if ((uint)abilityIndex >= abilityStrings.Length)
                abilityIndex = 0;
            return abilityStrings[abilityIndex];
        }

        private static string GetGBPKM(GBPKM gb)
        {
            // Build sections for GB Pokémon
            string speciesSection = GetSpeciesSection(gb);
            string levelSection = GetLevelSection(gb);
            string shinySection = gb.IsShiny ? "Star" : "Normal";
            string ivSection = GetIVSection(gb);
            string yearSection = GetYearSection(gb);

            // GB Pokémon don't have: Tera types, Abilities, or specific game versions
            // Using simplified format for compatibility
            return string.Join(Delimiter,
                speciesSection,
                levelSection,
                shinySection,
                "None",     // No Tera type
                GetNature(gb),
                "None",     // No ability
                ivSection,
                yearSection,
                $"Gen{gb.Format}"
            );
        }
    }
}

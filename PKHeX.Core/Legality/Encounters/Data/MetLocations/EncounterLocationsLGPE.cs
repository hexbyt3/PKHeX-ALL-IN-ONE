using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace PKHeX.Core.Legality.Encounters.Data.MetLocations;

/// <summary>
/// Generates encounter location data for Let's Go Pikachu and Let's Go Eevee games.
/// </summary>
public static class EncounterLocationsLGPE
{
    /// <summary>
    /// Generates JSON data for all encounters in Let's Go Pikachu and Let's Go Eevee.
    /// </summary>
    /// <param name="outputPath">Path where the JSON file will be saved</param>
    /// <param name="errorLogPath">Path for logging errors during generation</param>
    public static void GenerateEncounterDataJSON(string outputPath, string errorLogPath)
    {
        try
        {
            using var errorLogger = new StreamWriter(errorLogPath, false, Encoding.UTF8);
            errorLogger.WriteLine($"[{DateTime.Now}] Starting JSON generation process for encounters in Let's Go Pikachu/Eevee.");

            var gameStrings = GameInfo.GetStrings("en");
            errorLogger.WriteLine($"[{DateTime.Now}] Game strings loaded.");

            var pt = PersonalTable.GG;
            errorLogger.WriteLine($"[{DateTime.Now}] PersonalTable for LGPE loaded.");

            var encounterData = new Dictionary<string, List<EncounterInfo>>();

            ProcessEncounterSlots(Encounters7GG.SlotsGP, "Let's Go Pikachu", encounterData, gameStrings, errorLogger);
            ProcessEncounterSlots(Encounters7GG.SlotsGE, "Let's Go Eevee", encounterData, gameStrings, errorLogger);

            ProcessStaticEncounters(Encounters7GG.Encounter_GG, "Both", encounterData, gameStrings, errorLogger);
            ProcessStaticEncounters(Encounters7GG.StaticGP, "Let's Go Pikachu", encounterData, gameStrings, errorLogger);
            ProcessStaticEncounters(Encounters7GG.StaticGE, "Let's Go Eevee", encounterData, gameStrings, errorLogger);

            ProcessTradeEncounters(Encounters7GG.TradeGift_GG, "Both", encounterData, gameStrings, errorLogger);
            ProcessTradeEncounters(Encounters7GG.TradeGift_GP, "Let's Go Pikachu", encounterData, gameStrings, errorLogger);
            ProcessTradeEncounters(Encounters7GG.TradeGift_GE, "Let's Go Eevee", encounterData, gameStrings, errorLogger);

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            string jsonString = JsonSerializer.Serialize(encounterData, jsonOptions);

            File.WriteAllText(outputPath, jsonString, new UTF8Encoding(false));

            errorLogger.WriteLine($"[{DateTime.Now}] JSON file generated successfully without BOM at: {outputPath}");
        }
        catch (Exception ex)
        {
            using var errorLogger = new StreamWriter(errorLogPath, true, Encoding.UTF8);
            errorLogger.WriteLine($"[{DateTime.Now}] An error occurred: {ex.Message}");
            errorLogger.WriteLine($"Stack Trace: {ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// Processes encounter slots for a specific game version.
    /// </summary>
    private static void ProcessEncounterSlots(EncounterArea7b[] areas, string versionName, Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings, StreamWriter errorLogger)
    {
        foreach (var area in areas)
        {
            var locationId = area.Location;
            var locationName = gameStrings.GetLocationName(false, (ushort)locationId, 7, 7, GameVersion.GG);
            if (string.IsNullOrEmpty(locationName))
                locationName = $"Unknown Location {locationId}";

            foreach (var slot in area.Slots)
            {
                AddEncounterInfoWithEvolutions(encounterData, gameStrings, errorLogger, slot.Species, slot.Form,
                    locationName, locationId, slot.LevelMin, slot.LevelMax, "Wild", false, string.Empty, versionName, 0, string.Empty);
            }
        }
    }

    /// <summary>
    /// Processes static encounters for a specific game version.
    /// </summary>
    private static void ProcessStaticEncounters(EncounterStatic7b[] encounters, string versionName, Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings, StreamWriter errorLogger)
    {
        foreach (var encounter in encounters)
        {
            var locationId = encounter.Location;
            var locationName = gameStrings.GetLocationName(false, (ushort)locationId, 7, 7, GameVersion.GG);
            if (string.IsNullOrEmpty(locationName))
                locationName = $"Unknown Location {locationId}";

            string setIVs = string.Empty;
            int flawlessIVCount = encounter.FlawlessIVCount;

            if (encounter.IVs.IsSpecified)
            {
                setIVs = FormatIVs(encounter.IVs);
                flawlessIVCount = 0; // If specific IVs are set, don't use FlawlessIVCount
            }

            AddEncounterInfoWithEvolutions(encounterData, gameStrings, errorLogger, encounter.Species, encounter.Form,
                locationName, locationId, encounter.Level, encounter.Level, "Static",
                encounter.Shiny == Shiny.Never,
                encounter.FixedBall != Ball.None ? encounter.FixedBall.ToString() : string.Empty,
                versionName, flawlessIVCount, setIVs);
        }
    }

    /// <summary>
    /// Processes trade encounters for a specific game version.
    /// </summary>
    private static void ProcessTradeEncounters(EncounterTrade7b[] encounters, string versionName, Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings, StreamWriter errorLogger)
    {
        const int tradeLocationId = 30001;
        const string tradeLocationName = "a Link Trade (NPC)";

        foreach (var encounter in encounters)
        {
            string setIVs = string.Empty;
            int flawlessIVCount = 0;

            if (encounter.IVs.IsSpecified)
            {
                setIVs = FormatIVs(encounter.IVs);
            }

            AddEncounterInfoWithEvolutions(encounterData, gameStrings, errorLogger, encounter.Species, encounter.Form,
                tradeLocationName, tradeLocationId, encounter.Level, encounter.Level, "Trade",
                encounter.Shiny == Shiny.Never, encounter.FixedBall.ToString(), versionName, flawlessIVCount, setIVs);
        }
    }

    /// <summary>
    /// Adds encounter information for a Pokémon and all its possible evolutions.
    /// </summary>
    private static void AddEncounterInfoWithEvolutions(Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings,
        StreamWriter errorLogger, int speciesIndex, int form, string locationName, int locationId,
        int minLevel, int maxLevel, string encounterType, bool isShinyLocked, string fixedBall, string encounterVersion,
        int flawlessIVCount, string setIVs)
    {
        AddSingleEncounterInfo(encounterData, gameStrings, errorLogger, speciesIndex, form, locationName, locationId,
            minLevel, maxLevel, encounterType, isShinyLocked, fixedBall, encounterVersion, null, flawlessIVCount, setIVs);

        var processedForms = new HashSet<(int Species, int Form)> { ((int)speciesIndex, form) };

        ProcessEvolutions(speciesIndex, form, minLevel, locationId, locationName, isShinyLocked,
            fixedBall, encounterVersion, encounterType, encounterData, gameStrings, errorLogger, processedForms, flawlessIVCount, setIVs);
    }

    /// <summary>
    /// Adds a single encounter information entry to the encounter data dictionary.
    /// </summary>
    private static void AddSingleEncounterInfo(Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings,
        StreamWriter errorLogger, int speciesIndex, int form, string locationName, int locationId,
        int minLevel, int maxLevel, string encounterType, bool isShinyLocked, string fixedBall, string encounterVersion,
        int? metLevel = null, int flawlessIVCount = 0, string setIVs = "")
    {
        form = form == 255 ? 0 : form;

        string dexNumber = speciesIndex.ToString();
        if (form > 0)
            dexNumber += $"-{form}";

        if (!encounterData.ContainsKey(dexNumber))
            encounterData[dexNumber] = [];

        var speciesName = gameStrings.specieslist[speciesIndex];
        if (string.IsNullOrEmpty(speciesName))
        {
            errorLogger.WriteLine($"[{DateTime.Now}] Empty species name for index {speciesIndex}. Skipping.");
            return;
        }

        var encounterInfo = new EncounterInfo
        {
            SpeciesName = speciesName,
            SpeciesIndex = speciesIndex,
            Form = form,
            LocationName = locationName,
            LocationId = locationId,
            MinLevel = minLevel,
            MaxLevel = maxLevel,
            MetLevel = metLevel ?? minLevel,
            EncounterType = encounterType,
            IsShinyLocked = isShinyLocked,
            FixedBall = fixedBall,
            EncounterVersion = encounterVersion,
            FlawlessIVCount = flawlessIVCount,
            SetIVs = setIVs
        };

        // Set legal balls for this encounter
        SetLegalBalls(encounterInfo, errorLogger);

        bool isDuplicate = encounterData[dexNumber].Exists(e =>
            e.LocationId == locationId &&
            e.EncounterType == encounterType &&
            e.EncounterVersion == encounterVersion &&
            e.MinLevel == minLevel &&
            e.MaxLevel == maxLevel);

        if (!isDuplicate)
        {
            encounterData[dexNumber].Add(encounterInfo);
            errorLogger.WriteLine($"[{DateTime.Now}] Processed encounter: {speciesName} (Dex: {dexNumber}) at {locationName} (ID: {locationId}), " +
                $"Levels {minLevel}-{maxLevel}, Type: {encounterType}, IVs: {(flawlessIVCount > 0 ? $"{flawlessIVCount} perfect IVs" : setIVs)}, " +
                $"Legal Balls: {string.Join(", ", encounterInfo.LegalBalls)}");
        }
    }

    /// <summary>
    /// Sets the legal balls for an encounter in Let's Go Pikachu/Eevee games.
    /// </summary>
    private static void SetLegalBalls(EncounterInfo encounter, StreamWriter errorLogger)
    {
        var legalBalls = new List<int>();
        GameVersion gameVersion = DetermineGameVersion(encounter.EncounterVersion);

        // Check if there's a fixed ball first
        if (!string.IsNullOrEmpty(encounter.FixedBall))
        {
            if (Enum.TryParse(encounter.FixedBall, out Ball fixedBall))
            {
                legalBalls.Add(ConvertBallToImageId(fixedBall));
                encounter.LegalBalls = [.. legalBalls];
                errorLogger.WriteLine($"[{DateTime.Now}] Fixed ball: {encounter.FixedBall}");
                return;
            }
        }

        // Different legal balls based on encounter type
        if (encounter.EncounterType == "Trade")
        {
            // Trades typically come in a standard Poké Ball
            legalBalls.Add(ConvertBallToImageId(Ball.Poke));
        }
        else if (encounter.EncounterType == "Static" || encounter.EncounterType == "Wild" ||
                 encounter.EncounterType.Contains("Evolved"))
        {
            // For wild and static encounters in LGPE, use the appropriate wild balls
            // LGPE only allows Poké Ball, Great Ball, Ultra Ball, and Premier Ball for wild catches
            ulong wildBallsMask = BallUseLegality.GetWildBalls(7, GameVersion.GG);

            for (byte ballId = 1; ballId < 64; ballId++)
            {
                if (BallUseLegality.IsBallPermitted(wildBallsMask, ballId))
                {
                    var ball = (Ball)ballId;
                    legalBalls.Add(ConvertBallToImageId(ball));
                }
            }
        }
        else
        {
            // Default to only Poké Ball for other encounter types
            legalBalls.Add(ConvertBallToImageId(Ball.Poke));
        }

        encounter.LegalBalls = [.. legalBalls];
    }

    /// <summary>
    /// Determines the game version based on the version string.
    /// </summary>
    private static GameVersion DetermineGameVersion(string versionString)
    {
        return versionString switch
        {
            "Let's Go Pikachu" => GameVersion.GP,
            "Let's Go Eevee" => GameVersion.GE,
            _ => GameVersion.GG, // Both
        };
    }

    /// <summary>
    /// Converts a Ball enum value to the corresponding image ID in the ballImageMap.
    /// </summary>
    private static int ConvertBallToImageId(Ball ball)
    {
        return ball switch
        {
            Ball.Master => 1,
            Ball.Ultra => 12,
            Ball.Great => 13,
            Ball.Poke => 14,
            Ball.Premier => 22,
            _ => 14, // Default to Poké Ball for any unmapped balls
        };
    }

    /// <summary>
    /// Gets the minimum level required for evolution based on the evolution method.
    /// </summary>
    private static int GetEvolutionLevel(EvolutionMethod evo, int baseLevel)
    {
        if (evo.Level > 0)
            return evo.Level;
        if (evo.LevelUp > 0)
            return baseLevel + evo.LevelUp;
        if (evo.Method == EvolutionType.LevelUp && evo.Argument > 0)
            return evo.Argument;
        return baseLevel;
    }

    /// <summary>
    /// Processes all possible evolutions of a Pokémon and adds them to the encounter data.
    /// </summary>
    private static void ProcessEvolutions(int speciesIndex, int form, int baseLevel, int locationId, string locationName,
        bool isShinyLocked, string fixedBall, string versionName, string encounterType,
        Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings,
        StreamWriter errorLogger, HashSet<(int Species, int Form)> processedForms, int flawlessIVCount, string setIVs)
    {
        var tree = EvolutionTree.GetEvolutionTree(EntityContext.Gen7b);
        var evos = tree.Forward.GetForward((ushort)speciesIndex, (byte)form);

        foreach (var evo in evos.Span)
        {
            int evolvedSpecies = evo.Species;
            int evolvedForm = evo.Form == 255 ? 0 : evo.Form;

            if (!processedForms.Add((evolvedSpecies, evolvedForm)))
                continue;

            var evolvedSpeciesName = gameStrings.specieslist[evolvedSpecies];
            if (string.IsNullOrEmpty(evolvedSpeciesName))
            {
                errorLogger.WriteLine($"[{DateTime.Now}] Empty species name for evolved index {evolvedSpecies}. Skipping.");
                continue;
            }

            int evolutionLevel = GetEvolutionLevel(evo, baseLevel);
            int minLevel = Math.Max(baseLevel, evolutionLevel);

            evolvedForm = evolvedForm == 255 ? 0 : evolvedForm;

            string evolvedDexNumber = evolvedSpecies.ToString();
            if (evolvedForm > 0)
                evolvedDexNumber += $"-{evolvedForm}";

            if (!encounterData.ContainsKey(evolvedDexNumber))
                encounterData[evolvedDexNumber] = [];

            string evolvedEncounterType = $"{encounterType} (Evolved)";

            AddSingleEncounterInfo(encounterData, gameStrings, errorLogger, evolvedSpecies, evolvedForm, locationName, locationId,
                minLevel, 100, evolvedEncounterType, isShinyLocked, fixedBall, versionName, baseLevel, flawlessIVCount, setIVs);

            ProcessEvolutions(evolvedSpecies, evolvedForm, minLevel, locationId, locationName,
                isShinyLocked, fixedBall, versionName, encounterType, encounterData, gameStrings, errorLogger, processedForms, flawlessIVCount, setIVs);
        }
    }

    /// <summary>
    /// Formats IVs as a string for display and storage.
    /// </summary>
    private static string FormatIVs(IndividualValueSet ivs)
    {
        var ivParts = new List<string>(6);

        if (ivs.HP >= 0) ivParts.Add($"HP:{ivs.HP}");
        if (ivs.ATK >= 0) ivParts.Add($"Atk:{ivs.ATK}");
        if (ivs.DEF >= 0) ivParts.Add($"Def:{ivs.DEF}");
        if (ivs.SPA >= 0) ivParts.Add($"SpA:{ivs.SPA}");
        if (ivs.SPD >= 0) ivParts.Add($"SpD:{ivs.SPD}");
        if (ivs.SPE >= 0) ivParts.Add($"Spe:{ivs.SPE}");

        return string.Join(", ", ivParts);
    }

    /// <summary>
    /// Contains information about a single encounter.
    /// </summary>
    private sealed class EncounterInfo
    {
        /// <summary>
        /// Name of the Pokémon species.
        /// </summary>
        public required string SpeciesName { get; set; }

        /// <summary>
        /// National Pokédex index of the species.
        /// </summary>
        public required int SpeciesIndex { get; set; }

        /// <summary>
        /// Form number of the Pokémon.
        /// </summary>
        public required int Form { get; set; }

        /// <summary>
        /// Name of the location where the Pokémon can be encountered.
        /// </summary>
        public required string LocationName { get; set; }

        /// <summary>
        /// Internal ID of the location.
        /// </summary>
        public required int LocationId { get; set; }

        /// <summary>
        /// Minimum level of the Pokémon at this encounter.
        /// </summary>
        public required int MinLevel { get; set; }

        /// <summary>
        /// Maximum level of the Pokémon at this encounter.
        /// </summary>
        public required int MaxLevel { get; set; }

        /// <summary>
        /// Level recorded as the "met level" for this encounter.
        /// </summary>
        public required int MetLevel { get; set; }

        /// <summary>
        /// Type of encounter (Wild, Static, Trade, etc.).
        /// </summary>
        public required string EncounterType { get; set; }

        /// <summary>
        /// Indicates whether the Pokémon cannot be shiny in this encounter.
        /// </summary>
        public required bool IsShinyLocked { get; set; }

        /// <summary>
        /// The fixed Poké Ball if required for this encounter.
        /// </summary>
        public required string FixedBall { get; set; }

        /// <summary>
        /// Version of the game where this encounter is available.
        /// </summary>
        public required string EncounterVersion { get; set; }

        /// <summary>
        /// Number of guaranteed perfect IVs (31) for this encounter.
        /// </summary>
        public int FlawlessIVCount { get; set; }

        /// <summary>
        /// String representation of fixed IV values for this encounter.
        /// </summary>
        public string SetIVs { get; set; } = string.Empty;
        /// <summary>
        /// Legal balls that can be used for this encounter.
        /// </summary>
        public int[] LegalBalls { get; set; } = [];
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace PKHeX.Core.Legality.Encounters.Data.MetLocations;

/// <summary>
/// Generates encounter location data for Pokémon Sword and Shield games.
/// </summary>
public static class EncounterLocationsSWSH
{
    private const ushort MaxLair = 244;

    /// <summary>
    /// Generates a JSON file containing all encounter data for Sword and Shield.
    /// </summary>
    /// <param name="outputPath">Path where the JSON file will be saved</param>
    /// <param name="errorLogPath">Path to the error log file</param>
    public static void GenerateEncounterDataJSON(string outputPath, string errorLogPath)
    {
        try
        {
            using var errorLogger = new StreamWriter(errorLogPath, false, Encoding.UTF8);
            errorLogger.WriteLine($"[{DateTime.Now}] Starting JSON generation process for encounters in Sword/Shield.");

            var gameStrings = GameInfo.GetStrings("en");
            var encounterData = new Dictionary<string, List<EncounterInfo>>();

            ProcessEncounterSlots(Encounters8.SlotsSW_Symbol, encounterData, gameStrings, errorLogger, "Sword Symbol");
            ProcessEncounterSlots(Encounters8.SlotsSW_Hidden, encounterData, gameStrings, errorLogger, "Sword Hidden");
            ProcessEncounterSlots(Encounters8.SlotsSH_Symbol, encounterData, gameStrings, errorLogger, "Shield Symbol");
            ProcessEncounterSlots(Encounters8.SlotsSH_Hidden, encounterData, gameStrings, errorLogger, "Shield Hidden");

            ProcessStaticEncounters(Encounters8.StaticSWSH, "Both", encounterData, gameStrings, errorLogger);
            ProcessStaticEncounters(Encounters8.StaticSW, "Sword", encounterData, gameStrings, errorLogger);
            ProcessStaticEncounters(Encounters8.StaticSH, "Shield", encounterData, gameStrings, errorLogger);

            ProcessEggMetLocations(encounterData, gameStrings, errorLogger);
            ProcessDenEncounters(encounterData, gameStrings, errorLogger);
            ProcessMaxLairEncounters(encounterData, gameStrings, errorLogger);

            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
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
    /// Processes slot-based encounters for the given areas.
    /// </summary>
    /// <param name="areas">Encounter areas to process</param>
    /// <param name="encounterData">Dictionary to store encounter data</param>
    /// <param name="gameStrings">Game strings for localization</param>
    /// <param name="errorLogger">Error logger for logging issues</param>
    /// <param name="slotType">Type of slot (Symbol/Hidden)</param>
    private static void ProcessEncounterSlots(EncounterArea8[] areas, Dictionary<string, List<EncounterInfo>> encounterData,
        GameStrings gameStrings, StreamWriter errorLogger, string slotType)
    {
        foreach (var area in areas)
        {
            var locationName = gameStrings.GetLocationName(false, (ushort)area.Location, 8, 8, GameVersion.SWSH)
                ?? $"Unknown Location {area.Location}";

            // Get weather for this location from static method
            var weather = EncounterArea8.GetWeather(area.Location);

            foreach (var slot in area.Slots)
            {
                bool canGigantamax = Gigantamax.CanToggle(slot.Species, slot.Form);

                // Consider slot-specific weather; combine base location weather with slot's specific weather
                var slotWeather = slot.Weather & weather;

                AddEncounterInfoWithEvolutions(encounterData, gameStrings, errorLogger, slot.Species, slot.Form,
                    locationName, area.Location, slot.LevelMin, slot.LevelMax, $"Wild {slotType}",
                    false, false, string.Empty, "Both", canGigantamax, 0, string.Empty, slotWeather);
            }
        }
    }

    /// <summary>
    /// Processes egg-related met locations.
    /// </summary>
    /// <param name="encounterData">Dictionary to store encounter data</param>
    /// <param name="gameStrings">Game strings for localization</param>
    /// <param name="errorLogger">Error logger for logging issues</param>
    private static void ProcessEggMetLocations(Dictionary<string, List<EncounterInfo>> encounterData,
        GameStrings gameStrings, StreamWriter errorLogger)
    {
        const int eggMetLocationId = 60002;
        const string locationName = "a Nursery Worker";

        errorLogger.WriteLine($"[{DateTime.Now}] Processing egg met locations with location ID: {eggMetLocationId} ({locationName})");

        var pt = PersonalTable.SWSH;

        // Eggs don't have weather since they're not encountered in the wild
        // They're explicitly set to None for clarity
        var weather = AreaWeather8.None;

        for (ushort species = 1; species < pt.MaxSpeciesID; species++)
        {
            var personalInfo = pt.GetFormEntry(species, 0);
            if (personalInfo is null || !personalInfo.IsPresentInGame)
                continue;

            if (!Breeding.CanHatchAsEgg(species))
                continue;

            byte formCount = personalInfo.FormCount;
            for (byte form = 0; form < formCount; form++)
            {
                var formInfo = pt.GetFormEntry(species, form);
                if (formInfo is null || !formInfo.IsPresentInGame)
                    continue;

                // Check if this specific form can be hatched (handles special form restrictions)
                if (!Breeding.CanHatchAsEgg(species, form, EntityContext.Gen8))
                    continue;

                bool canGigantamax = Gigantamax.CanToggle(species, form);

                AddSingleEncounterInfo(
                    encounterData,
                    gameStrings,
                    errorLogger,
                    species,
                    form,
                    locationName,
                    eggMetLocationId,
                    1,
                    1,
                    1,
                    "Egg",
                    false,
                    true,
                    string.Empty,
                    "Both",
                    canGigantamax,
                    0,
                    string.Empty,
                    weather
                );
            }
        }
    }

    /// <summary>
    /// Processes static encounters from the specified encounter array.
    /// </summary>
    /// <param name="encounters">Static encounters to process</param>
    /// <param name="versionName">Version name (Sword/Shield/Both)</param>
    /// <param name="encounterData">Dictionary to store encounter data</param>
    /// <param name="gameStrings">Game strings for localization</param>
    /// <param name="errorLogger">Error logger for logging issues</param>
    private static void ProcessStaticEncounters(EncounterStatic8[] encounters, string versionName,
        Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings, StreamWriter errorLogger)
    {
        foreach (var encounter in encounters)
        {
            var locationName = gameStrings.GetLocationName(false, (ushort)encounter.Location, 8, 8, GameVersion.SWSH)
                ?? $"Unknown Location {encounter.Location}";

            bool canGigantamax = Gigantamax.CanToggle(encounter.Species, encounter.Form) || encounter.CanGigantamax;
            string fixedBall = encounter.FixedBall != Ball.None ? encounter.FixedBall.ToString() : string.Empty;

            // Get IV information
            int flawlessIVCount = encounter.FlawlessIVCount;
            string setIVs = string.Empty;

            // If the encounter has specific IVs set, create a formatted IV string
            if (IsIVsSpecified(encounter.IVs))
            {
                setIVs = FormatIVs(encounter.IVs);
            }

            // Get location's base weather if the encounter doesn't specify weather
            var weather = encounter.Weather != AreaWeather8.None
                ? encounter.Weather
                : EncounterArea8.GetWeather((byte)encounter.Location);

            AddEncounterInfoWithEvolutions(
                encounterData, gameStrings, errorLogger, encounter.Species, encounter.Form,
                locationName, encounter.Location, encounter.Level, encounter.Level, "Static",
                encounter.Shiny == Shiny.Never, encounter.Gift, fixedBall, versionName, canGigantamax,
                flawlessIVCount, setIVs, weather);
        }
    }

    /// <summary>
    /// Processes den-based encounters.
    /// </summary>
    /// <param name="encounterData">Dictionary to store encounter data</param>
    /// <param name="gameStrings">Game strings for localization</param>
    /// <param name="errorLogger">Error logger for logging issues</param>
    private static void ProcessDenEncounters(Dictionary<string, List<EncounterInfo>> encounterData,
        GameStrings gameStrings, StreamWriter errorLogger)
    {
        const int denLocationId = Encounters8Nest.SharedNest;
        var locationName = gameStrings.GetLocationName(false, (ushort)denLocationId, 8, 8, GameVersion.SWSH)
            ?? $"Unknown Location {denLocationId}";

        errorLogger.WriteLine($"[{DateTime.Now}] Processing Pokémon Den encounters with location ID: {denLocationId} ({locationName})");

        ProcessNestEncounters(Encounters8Nest.Nest_SW, "Sword", encounterData, gameStrings, errorLogger, denLocationId, locationName);
        ProcessNestEncounters(Encounters8Nest.Nest_SH, "Shield", encounterData, gameStrings, errorLogger, denLocationId, locationName);
        ProcessDistributionEncounters(Encounters8Nest.Dist_SW, "Sword", encounterData, gameStrings, errorLogger, denLocationId, locationName);
        ProcessDistributionEncounters(Encounters8Nest.Dist_SH, "Shield", encounterData, gameStrings, errorLogger, denLocationId, locationName);
        ProcessCrystalEncounters(Encounters8Nest.Crystal_SWSH, encounterData, gameStrings, errorLogger, denLocationId, locationName);
    }

    /// <summary>
    /// Processes standard nest (raid den) encounters.
    /// </summary>
    /// <param name="encounters">Nest encounters to process</param>
    /// <param name="versionName">Version name (Sword/Shield)</param>
    /// <param name="encounterData">Dictionary to store encounter data</param>
    /// <param name="gameStrings">Game strings for localization</param>
    /// <param name="errorLogger">Error logger for logging issues</param>
    /// <param name="locationId">Location ID for the den</param>
    /// <param name="locationName">Location name for the den</param>
    private static void ProcessNestEncounters(EncounterStatic8N[] encounters, string versionName,
        Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings,
        StreamWriter errorLogger, int locationId, string locationName)
    {
        // Max Raid encounters don't depend on weather
        // But we'll use the location's weather for reference in case needed
        var weather = EncounterArea8.GetWeather((byte)locationId);

        foreach (var encounter in encounters)
        {
            bool canGigantamax = Gigantamax.CanToggle(encounter.Species, encounter.Form) || encounter.CanGigantamax;
            int flawlessIVCount = encounter.FlawlessIVCount;

            AddEncounterInfoWithEvolutions(
                encounterData,
                gameStrings,
                errorLogger,
                encounter.Species,
                encounter.Form,
                locationName,
                locationId,
                encounter.Level,
                encounter.Level,
                "Max Raid",
                encounter.Shiny == Shiny.Never,
                false,
                string.Empty,
                versionName,
                canGigantamax,
                flawlessIVCount,
                string.Empty,
                weather
            );
        }
    }

    /// <summary>
    /// Processes distribution raid encounters.
    /// </summary>
    /// <param name="encounters">Distribution encounters to process</param>
    /// <param name="versionName">Version name (Sword/Shield)</param>
    /// <param name="encounterData">Dictionary to store encounter data</param>
    /// <param name="gameStrings">Game strings for localization</param>
    /// <param name="errorLogger">Error logger for logging issues</param>
    /// <param name="locationId">Location ID for the den</param>
    /// <param name="locationName">Location name for the den</param>
    private static void ProcessDistributionEncounters(EncounterStatic8ND[] encounters, string versionName,
        Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings,
        StreamWriter errorLogger, int locationId, string locationName)
    {
        // Distribution raids don't depend on weather
        // But we'll use the location's weather for reference in case needed  
        var weather = EncounterArea8.GetWeather((byte)locationId);

        foreach (var encounter in encounters)
        {
            bool canGigantamax = Gigantamax.CanToggle(encounter.Species, encounter.Form) || encounter.CanGigantamax;
            int flawlessIVCount = encounter.FlawlessIVCount;

            AddEncounterInfoWithEvolutions(
                encounterData,
                gameStrings,
                errorLogger,
                encounter.Species,
                encounter.Form,
                locationName,
                locationId,
                encounter.Level,
                encounter.Level,
                "Max Raid",
                encounter.Shiny == Shiny.Never,
                false,
                string.Empty,
                versionName,
                canGigantamax,
                flawlessIVCount,
                string.Empty,
                weather
            );
        }
    }

    /// <summary>
    /// Processes crystal raid encounters.
    /// </summary>
    /// <param name="encounters">Crystal encounters to process</param>
    /// <param name="encounterData">Dictionary to store encounter data</param>
    /// <param name="gameStrings">Game strings for localization</param>
    /// <param name="errorLogger">Error logger for logging issues</param>
    /// <param name="locationId">Location ID for the den</param>
    /// <param name="locationName">Location name for the den</param>
    private static void ProcessCrystalEncounters(EncounterStatic8NC[] encounters,
        Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings,
        StreamWriter errorLogger, int locationId, string locationName)
    {
        // Crystal raids don't depend on weather
        // But we'll use the location's weather for reference in case needed
        var weather = EncounterArea8.GetWeather((byte)locationId);

        foreach (var encounter in encounters)
        {
            string versionName = encounter.Version switch
            {
                GameVersion.SW => "Sword",
                GameVersion.SH => "Shield",
                _ => "Both"
            };

            bool canGigantamax = Gigantamax.CanToggle(encounter.Species, encounter.Form) || encounter.CanGigantamax;
            int flawlessIVCount = encounter.FlawlessIVCount;

            AddEncounterInfoWithEvolutions(
                encounterData,
                gameStrings,
                errorLogger,
                encounter.Species,
                encounter.Form,
                locationName,
                locationId,
                encounter.Level,
                encounter.Level,
                "Max Raid",
                encounter.Shiny == Shiny.Never,
                false,
                string.Empty,
                versionName,
                canGigantamax,
                flawlessIVCount,
                string.Empty,
                weather
            );
        }
    }

    /// <summary>
    /// Processes Max Lair encounters from the DLC.
    /// </summary>
    /// <param name="encounterData">Dictionary to store encounter data</param>
    /// <param name="gameStrings">Game strings for localization</param>
    /// <param name="errorLogger">Error logger for logging issues</param>
    private static void ProcessMaxLairEncounters(Dictionary<string, List<EncounterInfo>> encounterData,
        GameStrings gameStrings, StreamWriter errorLogger)
    {
        var locationName = gameStrings.GetLocationName(false, MaxLair, 8, 8, GameVersion.SWSH)
            ?? $"Unknown Location {MaxLair}";

        // Get weather for Max Lair location
        var weather = EncounterArea8.GetWeather((byte)MaxLair);

        foreach (var encounter in Encounters8Nest.DynAdv_SWSH)
        {
            bool canGigantamax = Gigantamax.CanToggle(encounter.Species, encounter.Form) || encounter.CanGigantamax;
            int flawlessIVCount = encounter.FlawlessIVCount;

            AddEncounterInfoWithEvolutions(
                encounterData, gameStrings, errorLogger, encounter.Species, encounter.Form,
                locationName, MaxLair, encounter.Level, encounter.Level, "Max Lair",
                encounter.Shiny == Shiny.Never, false, string.Empty, "Both", canGigantamax,
                flawlessIVCount, string.Empty, weather);
        }
    }

    /// <summary>
    /// Adds encounter information for a species and all its possible evolutions.
    /// </summary>
    /// <param name="encounterData">Dictionary to store encounter data</param>
    /// <param name="gameStrings">Game strings for localization</param>
    /// <param name="errorLogger">Error logger for logging issues</param>
    /// <param name="speciesIndex">Species Pokédex index</param>
    /// <param name="form">Form number</param>
    /// <param name="locationName">Location name</param>
    /// <param name="locationId">Location ID</param>
    /// <param name="minLevel">Minimum level</param>
    /// <param name="maxLevel">Maximum level</param>
    /// <param name="encounterType">Encounter type description</param>
    /// <param name="isShinyLocked">Whether the encounter is shiny-locked</param>
    /// <param name="isGift">Whether the encounter is a gift</param>
    /// <param name="fixedBall">Fixed ball for the encounter, if any</param>
    /// <param name="encounterVersion">Game version for the encounter</param>
    /// <param name="canGigantamax">Whether the Pokémon can Gigantamax</param>
    /// <param name="flawlessIVCount">Number of guaranteed perfect IVs</param>
    /// <param name="setIVs">Fixed IVs if specified</param>
    /// <param name="weather">Weather conditions for the encounter</param>
    private static void AddEncounterInfoWithEvolutions(
        Dictionary<string, List<EncounterInfo>> encounterData,
        GameStrings gameStrings,
        StreamWriter errorLogger,
        ushort speciesIndex,
        byte form,
        string locationName,
        int locationId,
        int minLevel,
        int maxLevel,
        string encounterType,
        bool isShinyLocked = false,
        bool isGift = false,
        string fixedBall = "",
        string encounterVersion = "Both",
        bool canGigantamax = false,
        int flawlessIVCount = 0,
        string setIVs = "",
        AreaWeather8 weather = AreaWeather8.None)
    {
        var pt = PersonalTable.SWSH;
        var personalInfo = pt.GetFormEntry(speciesIndex, form);
        if (personalInfo is null || !personalInfo.IsPresentInGame)
        {
            errorLogger.WriteLine($"[{DateTime.Now}] Species {speciesIndex} form {form} not present in SWSH. Skipping.");
            return;
        }

        AddSingleEncounterInfo(encounterData, gameStrings, errorLogger, speciesIndex, form, locationName, locationId,
            minLevel, maxLevel, minLevel, encounterType, isShinyLocked, isGift, fixedBall, encounterVersion,
            canGigantamax, flawlessIVCount, setIVs, weather);

        var processedForms = new HashSet<(ushort Species, byte Form)> { (speciesIndex, form) };

        ProcessEvolutionLine(encounterData, gameStrings, pt, errorLogger, speciesIndex, form, locationName, locationId,
            minLevel, maxLevel, minLevel, encounterType, isShinyLocked, isGift, fixedBall, encounterVersion,
            canGigantamax, flawlessIVCount, setIVs, processedForms, weather);
    }

    /// <summary>
    /// Processes the evolution line for a species to add all possible evolved forms.
    /// </summary>
    /// <param name="encounterData">Dictionary to store encounter data</param>
    /// <param name="gameStrings">Game strings for localization</param>
    /// <param name="pt">Personal table containing species data</param>
    /// <param name="errorLogger">Error logger for logging issues</param>
    /// <param name="species">Species Pokédex index</param>
    /// <param name="form">Form number</param>
    /// <param name="locationName">Location name</param>
    /// <param name="locationId">Location ID</param>
    /// <param name="baseLevel">Base level of the Pokémon</param>
    /// <param name="maxLevel">Maximum level</param>
    /// <param name="metLevel">Met level</param>
    /// <param name="encounterType">Encounter type description</param>
    /// <param name="isShinyLocked">Whether the encounter is shiny-locked</param>
    /// <param name="isGift">Whether the encounter is a gift</param>
    /// <param name="fixedBall">Fixed ball for the encounter, if any</param>
    /// <param name="encounterVersion">Game version for the encounter</param>
    /// <param name="baseCanGigantamax">Whether the base form can Gigantamax</param>
    /// <param name="flawlessIVCount">Number of guaranteed perfect IVs</param>
    /// <param name="setIVs">Fixed IVs if specified</param>
    /// <param name="processedForms">Set of already processed species/form combinations</param>
    /// <param name="weather">Weather conditions for the encounter</param>
    private static void ProcessEvolutionLine(
        Dictionary<string, List<EncounterInfo>> encounterData,
        GameStrings gameStrings,
        PersonalTable8SWSH pt,
        StreamWriter errorLogger,
        ushort species,
        byte form,
        string locationName,
        int locationId,
        int baseLevel,
        int maxLevel,
        int metLevel,
        string encounterType,
        bool isShinyLocked,
        bool isGift,
        string fixedBall,
        string encounterVersion,
        bool baseCanGigantamax,
        int flawlessIVCount,
        string setIVs,
        HashSet<(ushort Species, byte Form)> processedForms,
        AreaWeather8 weather)
    {
        var nextEvolutions = GetImmediateEvolutions(species, form, pt, processedForms);

        foreach (var (evoSpecies, evoForm) in nextEvolutions)
        {
            if (!processedForms.Add((evoSpecies, evoForm)))
                continue;

            var evoPersonalInfo = pt.GetFormEntry(evoSpecies, evoForm);
            if (evoPersonalInfo is null || !evoPersonalInfo.IsPresentInGame)
                continue;

            // Get the minimum level required for evolution with correct form parameters
            var evolutionMinLevel = GetMinEvolutionLevel(species, form, evoSpecies, evoForm);
            // The minimum level for the evolved form is the maximum of the base level and the evolution level
            var minLevel = Math.Max(baseLevel, evolutionMinLevel);

            bool evoCanGigantamax = baseCanGigantamax || Gigantamax.CanToggle(evoSpecies, evoForm);

            AddSingleEncounterInfo(
                encounterData, gameStrings, errorLogger, evoSpecies, evoForm, locationName, locationId,
                minLevel, Math.Max(minLevel, maxLevel), metLevel, $"{encounterType} (Evolved)",
                isShinyLocked, isGift, fixedBall, encounterVersion, evoCanGigantamax, flawlessIVCount, setIVs, weather);

            ProcessEvolutionLine(
                encounterData, gameStrings, pt, errorLogger, evoSpecies, evoForm, locationName, locationId,
                minLevel, Math.Max(minLevel, maxLevel), metLevel, encounterType, isShinyLocked, isGift, fixedBall,
                encounterVersion, evoCanGigantamax, flawlessIVCount, setIVs, processedForms, weather);
        }
    }

    /// <summary>
    /// Gets the next possible evolutions for a species and form.
    /// </summary>
    /// <param name="species">Species Pokédex index</param>
    /// <param name="form">Form number</param>
    /// <param name="pt">Personal table containing species data</param>
    /// <param name="processedForms">Set of already processed species/form combinations</param>
    /// <returns>List of species and form combinations that are immediate evolutions</returns>
    private static List<(ushort Species, byte Form)> GetImmediateEvolutions(
        ushort species,
        byte form,
        PersonalTable8SWSH pt,
        HashSet<(ushort Species, byte Form)> processedForms)
    {
        var results = new List<(ushort Species, byte Form)>();

        var tree = EvolutionTree.GetEvolutionTree(EntityContext.Gen8);
        var evos = tree.Forward.GetForward(species, form);

        foreach (var evo in evos.Span)
        {
            ushort evoSpecies = (ushort)evo.Species;
            byte evoForm = (byte)evo.Form;

            if (processedForms.Contains((evoSpecies, evoForm)))
                continue;

            var personalInfo = pt.GetFormEntry(evoSpecies, evoForm);
            if (personalInfo is null || !personalInfo.IsPresentInGame)
                continue;

            results.Add((evoSpecies, evoForm));
        }

        return results;
    }

    /// <summary>
    /// Gets the minimum level required for evolution.
    /// </summary>
    /// <param name="baseSpecies">Base species Pokédex index</param>
    /// <param name="baseForm">Base form number</param>
    /// <param name="evolvedSpecies">Evolved species Pokédex index</param>
    /// <param name="evolvedForm">Evolved form number</param>
    /// <returns>Minimum level required for evolution</returns>
    private static int GetMinEvolutionLevel(ushort baseSpecies, byte baseForm, ushort evolvedSpecies, byte evolvedForm)
    {
        var tree = EvolutionTree.GetEvolutionTree(EntityContext.Gen8);
        int minLevel = 1;

        var evos = tree.Forward.GetForward(baseSpecies, baseForm);
        foreach (var evo in evos.Span)
        {
            if (evo.Species == evolvedSpecies && evo.Form == evolvedForm)
            {
                int levelRequirement = GetEvolutionLevel(evo);
                minLevel = Math.Max(minLevel, levelRequirement);
                return minLevel;
            }

            var secondaryEvos = tree.Forward.GetForward((ushort)evo.Species, (byte)evo.Form);
            foreach (var secondEvo in secondaryEvos.Span)
            {
                if (secondEvo.Species == evolvedSpecies && secondEvo.Form == evolvedForm)
                {
                    int firstEvolutionLevel = GetEvolutionLevel(evo);
                    int secondEvolutionLevel = GetEvolutionLevel(secondEvo);

                    minLevel = Math.Max(minLevel, Math.Max(firstEvolutionLevel, secondEvolutionLevel));
                    return minLevel;
                }
            }
        }

        return minLevel;
    }

    /// <summary>
    /// Gets the level requirement from an evolution method.
    /// </summary>
    /// <param name="evo">Evolution method</param>
    /// <returns>Level requirement or 0 if not applicable</returns>
    private static int GetEvolutionLevel(EvolutionMethod evo)
    {
        if (evo.Level > 0)
            return evo.Level;
        if (evo.Method == EvolutionType.LevelUp && evo.Argument > 0)
            return evo.Argument;
        return 0;
    }

    // SWSH Gen8 Ribbons
    private static readonly string[] Gen8Ribbons = ["ChampionGalar", "TowerMaster", "MasterRank"];
    private static readonly string[] PreviousGenRibbons =
    [
        "ChampionKalos", "ChampionG3", "ChampionSinnoh", "BestFriends", "Training",
        "BattlerSkillful", "BattlerExpert", "Effort", "Alert", "Shock", "Downcast",
        "Careless", "Relax", "Snooze", "Smile", "Gorgeous", "Royal", "GorgeousRoyal",
        "Artist", "Footprint", "Record", "Legend", "Country", "National", "Earth",
        "World", "Classic", "Premier", "Event", "Birthday", "Special", "Souvenir",
        "Wishing", "ChampionBattle", "ChampionRegional", "ChampionNational",
        "ChampionWorld", "ChampionG6Hoenn", "ContestStar", "MasterCoolness",
        "MasterBeauty", "MasterCuteness", "MasterCleverness", "MasterToughness",
        "ChampionAlola", "BattleRoyale", "BattleTreeGreat", "BattleTreeMaster"
    ];

    // Gen8 mark names (time-based)
    private static readonly string[] TimeBasedMarks = ["MarkLunchtime", "MarkSleepyTime", "MarkDusk", "MarkDawn"];

    // Gen8 mark names (weather-based)
    private static readonly string[] WeatherBasedMarks = ["MarkCloudy", "MarkRainy", "MarkStormy", "MarkSnowy",
        "MarkBlizzard", "MarkDry", "MarkSandstorm", "MarkMisty"];

    // Gen8 mark names (special condition)
    private static readonly string[] SpecialConditionMarks = ["MarkFishing", "MarkCurry", "MarkUncommon", "MarkRare", "MarkDestiny"];

    // Gen8 mark names (personality-based)
    private static readonly string[] PersonalityMarks = [
        "MarkRowdy", "MarkAbsentMinded", "MarkJittery", "MarkExcited", "MarkCharismatic", "MarkCalmness",
        "MarkIntense", "MarkZonedOut", "MarkJoyful", "MarkAngry", "MarkSmiley", "MarkTeary",
        "MarkUpbeat", "MarkPeeved", "MarkIntellectual", "MarkFerocious", "MarkCrafty", "MarkScowling",
        "MarkKindly", "MarkFlustered", "MarkPumpedUp", "MarkZeroEnergy", "MarkPrideful", "MarkUnsure",
        "MarkHumble", "MarkThorny", "MarkVigor", "MarkSlump"
    ];

    /// <summary>
    /// Adds a single encounter info entry to the encounter data dictionary.
    /// </summary>
    /// <param name="encounterData">Dictionary to store encounter data</param>
    /// <param name="gameStrings">Game strings for localization</param>
    /// <param name="errorLogger">Error logger for logging issues</param>
    /// <param name="speciesIndex">Species Pokédex index</param>
    /// <param name="form">Form number</param>
    /// <param name="locationName">Location name</param>
    /// <param name="locationId">Location ID</param>
    /// <param name="minLevel">Minimum level</param>
    /// <param name="maxLevel">Maximum level</param>
    /// <param name="metLevel">Met level</param>
    /// <param name="encounterType">Encounter type description</param>
    /// <param name="isShinyLocked">Whether the encounter is shiny-locked</param>
    /// <param name="isGift">Whether the encounter is a gift</param>
    /// <param name="fixedBall">Fixed ball for the encounter, if any</param>
    /// <param name="encounterVersion">Game version for the encounter</param>
    /// <param name="canGigantamax">Whether the Pokémon can Gigantamax</param>
    /// <param name="flawlessIVCount">Number of guaranteed perfect IVs</param>
    /// <param name="setIVs">Fixed IVs if specified</param>
    /// <param name="weather">Weather conditions for the encounter</param>
    private static void AddSingleEncounterInfo(
        Dictionary<string, List<EncounterInfo>> encounterData,
        GameStrings gameStrings,
        StreamWriter errorLogger,
        ushort speciesIndex,
        byte form,
        string locationName,
        int locationId,
        int minLevel,
        int maxLevel,
        int metLevel,
        string encounterType,
        bool isShinyLocked,
        bool isGift,
        string fixedBall,
        string encounterVersion,
        bool canGigantamax,
        int flawlessIVCount,
        string setIVs,
        AreaWeather8 weather)
    {
        string dexNumber = form > 0 ? $"{speciesIndex}-{form}" : speciesIndex.ToString();

        var speciesName = gameStrings.specieslist[speciesIndex];
        if (string.IsNullOrEmpty(speciesName))
        {
            errorLogger.WriteLine($"[{DateTime.Now}] Empty species name for index {speciesIndex}. Skipping.");
            return;
        }

        var personalInfo = PersonalTable.SWSH.GetFormEntry(speciesIndex, form);
        if (personalInfo is null)
        {
            errorLogger.WriteLine($"[{DateTime.Now}] Personal info not found for species {speciesIndex} form {form}. Skipping.");
            return;
        }

        string genderRatio = DetermineGenderRatio(personalInfo);

        if (!encounterData.TryGetValue(dexNumber, out var encounterList))
        {
            encounterList = [];
            encounterData[dexNumber] = encounterList;
        }

        var existingEncounter = encounterList.FirstOrDefault(e =>
            e.LocationId == locationId &&
            e.SpeciesIndex == speciesIndex &&
            e.Form == form &&
            e.EncounterType == encounterType &&
            e.CanGigantamax == canGigantamax &&
            e.Gender == genderRatio);

        if (existingEncounter is not null)
        {
            existingEncounter.MinLevel = Math.Min(existingEncounter.MinLevel, minLevel);
            existingEncounter.MaxLevel = Math.Max(existingEncounter.MaxLevel, maxLevel);
            existingEncounter.MetLevel = Math.Min(existingEncounter.MetLevel, metLevel);
            existingEncounter.Weather |= weather; // Combine weather conditions

            string existingVersion = existingEncounter.EncounterVersion ?? string.Empty;
            string newVersion = encounterVersion ?? string.Empty;
            existingEncounter.EncounterVersion = CombineVersions(existingVersion, newVersion);

            // Update IV requirements with higher priority value (if any)
            if (flawlessIVCount > existingEncounter.FlawlessIVCount)
            {
                existingEncounter.FlawlessIVCount = flawlessIVCount;
                // Clear specific IVs if we're using flawless count
                if (flawlessIVCount > 0)
                    existingEncounter.SetIVs = string.Empty;
            }

            // Only set specific IVs if we don't have flawless count and new IVs are provided
            if (existingEncounter.FlawlessIVCount == 0 && !string.IsNullOrEmpty(setIVs) &&
                string.IsNullOrEmpty(existingEncounter.SetIVs))
            {
                existingEncounter.SetIVs = setIVs;
            }

            errorLogger.WriteLine($"[{DateTime.Now}] Updated existing encounter: {speciesName} " +
                $"(Dex: {dexNumber}) at {locationName} (ID: {locationId}), Levels {existingEncounter.MinLevel}-{existingEncounter.MaxLevel}, " +
                $"Met Level: {existingEncounter.MetLevel}, Type: {encounterType}, Version: {existingEncounter.EncounterVersion}, " +
                $"Can Gigantamax: {canGigantamax}, Gender: {genderRatio}, " +
                $"IVs: {(flawlessIVCount > 0 ? $"{flawlessIVCount} perfect IVs" : setIVs)}");
        }
        else
        {
            var newEncounter = new EncounterInfo
            {
                SpeciesName = speciesName,
                SpeciesIndex = speciesIndex,
                Form = form,
                LocationName = locationName,
                LocationId = locationId,
                MinLevel = minLevel,
                MaxLevel = maxLevel,
                MetLevel = metLevel,
                EncounterType = encounterType,
                IsShinyLocked = isShinyLocked,
                IsGift = isGift,
                FixedBall = fixedBall,
                EncounterVersion = encounterVersion,
                CanGigantamax = canGigantamax,
                Gender = genderRatio,
                FlawlessIVCount = flawlessIVCount,
                SetIVs = setIVs,
                Weather = weather
            };

            SetEncounterMarksAndRibbons(newEncounter, errorLogger);
            SetLegalBalls(newEncounter, errorLogger);

            encounterList.Add(newEncounter);

            errorLogger.WriteLine($"[{DateTime.Now}] Processed new encounter: {speciesName} " +
                $"(Dex: {dexNumber}) at {locationName} (ID: {locationId}), Levels {minLevel}-{maxLevel}, " +
                $"Met Level: {metLevel}, Type: {encounterType}, Version: {encounterVersion}, " +
                $"Can Gigantamax: {canGigantamax}, Gender: {genderRatio}, " +
                $"IVs: {(flawlessIVCount > 0 ? $"{flawlessIVCount} perfect IVs" : setIVs)}, " +
                $"Required Marks: {string.Join(", ", newEncounter.RequiredMarks)}, " +
                $"Possible Marks: {(newEncounter.PossibleMarks.Length > 5 ? string.Join(", ", newEncounter.PossibleMarks.Take(5)) + "..." : string.Join(", ", newEncounter.PossibleMarks))}, " +
                $"Valid Ribbons: {(newEncounter.ValidRibbons.Length > 5 ? string.Join(", ", newEncounter.ValidRibbons.Take(5)) + "..." : string.Join(", ", newEncounter.ValidRibbons))}, " +
                $"Legal Balls: {string.Join(", ", newEncounter.LegalBalls)}");
        }
    }

    /// <summary>
    /// Sets the legal balls for an encounter based on encounter type and game version.
    /// </summary>
    private static void SetLegalBalls(EncounterInfo encounter, StreamWriter errorLogger)
    {
        var legalBalls = new List<int>();
        GameVersion gameVersion = DetermineGameVersion(encounter.EncounterVersion);

        // Handle fixed ball encounters first (highest priority)
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

        // Handle egg encounters (special inheritance rules)
        if (encounter.EncounterType == "Egg")
        {
            // For eggs, Master Ball and Cherish Ball can never be inherited
            for (byte ballId = 1; ballId < 64; ballId++)
            {
                var ball = (Ball)ballId;
                if (ball != Ball.Master && ball != Ball.Cherish &&
                    BallContextHOME.Instance.CanBreedWithBall((ushort)encounter.SpeciesIndex, (byte)encounter.Form, ball))
                {
                    legalBalls.Add(ConvertBallToImageId(ball));
                }
            }

            encounter.LegalBalls = [.. legalBalls];
            errorLogger.WriteLine($"[{DateTime.Now}] Legal balls for egg: {string.Join(", ", legalBalls)}");
            return;
        }

        // Handle curry mark encounters (limited ball set)
        if (encounter.EncounterType.Contains("Curry") || (encounter.PossibleMarks != null && encounter.PossibleMarks.Contains("MarkCurry")))
        {
            // For curry encounters, only Poké Ball, Great Ball, and Ultra Ball are legal
            legalBalls.Add(ConvertBallToImageId(Ball.Poke));
            legalBalls.Add(ConvertBallToImageId(Ball.Great));
            legalBalls.Add(ConvertBallToImageId(Ball.Ultra));

            encounter.LegalBalls = [.. legalBalls];
            errorLogger.WriteLine($"[{DateTime.Now}] Legal balls for curry encounter: {string.Join(", ", legalBalls)}");
            return;
        }

        // Determine ball permission mask based on encounter type
        ulong ballPermitMask;

        if (encounter.IsGift)
        {
            // Gift Pokémon typically come in a standard Poké Ball
            ballPermitMask = 1ul << (int)Ball.Poke;
        }
        else if (encounter.EncounterType.Contains("Static"))
        {
            // Static encounters use wild catch balls
            ballPermitMask = BallUseLegality.GetWildBalls(8, gameVersion);
        }
        else if (encounter.EncounterType.Contains("Wild") || encounter.EncounterType.Contains("Symbol") || encounter.EncounterType.Contains("Hidden"))
        {
            // Wild encounters use the standard ball set for Gen 8
            ballPermitMask = BallUseLegality.GetWildBalls(8, gameVersion);
        }
        else if (encounter.EncounterType.Contains("Evolved"))
        {
            // Evolved encounters use the same balls as their base forms
            ballPermitMask = BallUseLegality.GetWildBalls(8, gameVersion);
        }
        else
        {
            // Default to standard Gen 8 wild balls for any other encounter type
            ballPermitMask = BallUseLegality.WildPokeballs8;
        }

        // Convert the bitmask to a list of legal balls
        for (byte ballId = 1; ballId < 64; ballId++)
        {
            if (BallUseLegality.IsBallPermitted(ballPermitMask, ballId))
            {
                var ball = (Ball)ballId;

                // Special case: Heavy Ball can't be used for certain species in Alola
                // (This check is retained for completeness, though it's more relevant for Gen 7)
                if (ball == Ball.Heavy && BallUseLegality.IsAlolanCaptureNoHeavyBall((ushort)encounter.SpeciesIndex))
                {
                    continue;
                }

                legalBalls.Add(ConvertBallToImageId(ball));
            }
        }

        encounter.LegalBalls = [.. legalBalls];
        errorLogger.WriteLine($"[{DateTime.Now}] Legal balls: {string.Join(", ", legalBalls)}");
    }

    /// <summary>
    /// Determines the game version based on the version string.
    /// </summary>
    private static GameVersion DetermineGameVersion(string versionString)
    {
        return versionString switch
        {
            "Sword" => GameVersion.SW,
            "Shield" => GameVersion.SH,
            _ => GameVersion.SWSH, // Both
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
            Ball.LAPoke => 2,
            Ball.LAUltra => 3,
            Ball.Dream => 4,
            Ball.LAWing => 5,
            Ball.LAJet => 6,
            Ball.LALeaden => 7,
            Ball.LAOrigin => 8, // Origin Ball appears to be 8 in your image map
            Ball.LAGigaton => 9,
            Ball.Strange => 10,
            Ball.Beast => 11,
            Ball.Ultra => 12,
            Ball.Great => 13,
            Ball.Poke => 14,
            Ball.Safari => 15,
            Ball.Net => 16,
            Ball.Dive => 17,
            Ball.Nest => 18,
            Ball.Repeat => 19,
            Ball.Timer => 20,
            Ball.Luxury => 21,
            Ball.Premier => 22,
            Ball.Dusk => 23,
            Ball.Heal => 24,
            Ball.Quick => 25,
            Ball.Cherish => 26,
            Ball.Fast => 27,
            Ball.Level => 28,
            Ball.Lure => 29,
            Ball.Heavy => 30,
            Ball.Love => 31,
            Ball.Friend => 32,
            Ball.Moon => 33,
            Ball.Sport => 34,
            Ball.LAGreat => 36,
            Ball.LAHeavy => 37,
            Ball.LAFeather => 38,
            _ => 14, // Default to Poké Ball for any unmapped balls
        };
    }

    /// <summary>
    /// Sets the required marks, possible marks and valid ribbons for an encounter.
    /// </summary>
    /// <param name="encounter">Encounter info to update</param>
    /// <param name="errorLogger">Error logger for logging issues</param>
    private static void SetEncounterMarksAndRibbons(EncounterInfo encounter, StreamWriter errorLogger)
    {
        var requiredMarks = new List<string>();
        var possibleMarks = new List<string>();
        var validRibbons = new List<string>();

        // Process possible marks
        if (CanHaveEncounterMarks(encounter))
        {
            // Add possible marks based on encounter conditions
            if (CanHaveWeatherMarks(encounter))
            {
                // Weather-based marks for wild encounters
                possibleMarks.AddRange(GetPossibleWeatherMarks(encounter.Weather));
            }

            if (CanHaveTimeMarks(encounter))
            {
                // Time-based marks for wild encounters
                possibleMarks.AddRange(TimeBasedMarks);
            }

            // Special condition marks
            if (encounter.EncounterType.Contains("Fishing") || encounter.Weather.HasFlag(AreaWeather8.Fishing))
            {
                possibleMarks.Add("MarkFishing");
            }

            if (encounter.EncounterType.Contains("Curry"))
            {
                possibleMarks.Add("MarkCurry");
            }

            // Destiny mark (birthday encounters)
            possibleMarks.Add("MarkDestiny");

            // Rarity marks
            possibleMarks.Add("MarkUncommon");
            possibleMarks.Add("MarkRare");

            // Personality marks (always applicable for wild encounters)
            possibleMarks.AddRange(PersonalityMarks);
        }

        // Add all valid ribbons for Gen8
        validRibbons.AddRange(Gen8Ribbons);
        validRibbons.AddRange(PreviousGenRibbons);

        encounter.RequiredMarks = [.. requiredMarks];
        encounter.PossibleMarks = [.. possibleMarks.Except(requiredMarks).ToArray()];
        encounter.ValidRibbons = [.. validRibbons];

        errorLogger.WriteLine($"[{DateTime.Now}] Mark analysis - Required: {string.Join(", ", requiredMarks)}, " +
            $"Possible: {(possibleMarks.Count > 5 ? string.Join(", ", possibleMarks.Take(5)) + "..." : string.Join(", ", possibleMarks))}");
    }

    /// <summary>
    /// Gets possible weather-based marks based on the weather conditions.
    /// </summary>
    /// <param name="weather">Weather conditions</param>
    /// <returns>List of possible weather marks</returns>
    private static List<string> GetPossibleWeatherMarks(AreaWeather8 weather)
    {
        var weatherMarks = new List<string>();

        if (weather.HasFlag(AreaWeather8.Overcast))
            weatherMarks.Add("MarkCloudy");
        if (weather.HasFlag(AreaWeather8.Raining))
            weatherMarks.Add("MarkRainy");
        if (weather.HasFlag(AreaWeather8.Thunderstorm))
            weatherMarks.Add("MarkStormy");
        if (weather.HasFlag(AreaWeather8.Snowing))
            weatherMarks.Add("MarkSnowy");
        if (weather.HasFlag(AreaWeather8.Snowstorm))
            weatherMarks.Add("MarkBlizzard");
        if (weather.HasFlag(AreaWeather8.Intense_Sun))
            weatherMarks.Add("MarkDry");
        if (weather.HasFlag(AreaWeather8.Sandstorm))
            weatherMarks.Add("MarkSandstorm");
        if (weather.HasFlag(AreaWeather8.Heavy_Fog))
            weatherMarks.Add("MarkMisty");

        return weatherMarks;
    }

    /// <summary>
    /// Determines if the encounter type can have encounter marks from Gen 8.
    /// </summary>
    /// <param name="encounter">The encounter info to check</param>
    /// <returns>True if the encounter can have encounter marks</returns>
    private static bool CanHaveEncounterMarks(EncounterInfo encounter)
    {
        // Based on IsEncounterMarkAllowed in MarkRules
        // Egg, gift, and static encounters typically don't have marks
        if (encounter.EncounterType is "Egg" or "Gift" or "Static")
            return false;

        // Raid encounters don't have encounter marks
        if (encounter.EncounterType.Contains("Raid") || encounter.EncounterType.Contains("Max Lair"))
            return false;

        // Wild and fishing encounters can have marks
        return encounter.EncounterType.Contains("Wild") ||
               encounter.EncounterType.Contains("Fishing") ||
               encounter.EncounterType.Contains("Curry");
    }

    /// <summary>
    /// Determines if the encounter can have weather-based marks.
    /// </summary>
    /// <param name="encounter">The encounter info to check</param>
    /// <returns>True if the encounter can have weather-based marks</returns>
    private static bool CanHaveWeatherMarks(EncounterInfo encounter)
    {
        // Weather marks apply to wild encounters
        return encounter.EncounterType.Contains("Wild") ||
               encounter.Weather != AreaWeather8.None;
    }

    /// <summary>
    /// Determines if the encounter can have time-based marks.
    /// </summary>
    /// <param name="encounter">The encounter info to check</param>
    /// <returns>True if the encounter can have time-based marks</returns>
    private static bool CanHaveTimeMarks(EncounterInfo encounter)
    {
        // Time marks apply to wild encounters
        return encounter.EncounterType.Contains("Wild");
    }

    /// <summary>
    /// Combines two version strings into a single one.
    /// </summary>
    /// <param name="version1">First version string</param>
    /// <param name="version2">Second version string</param>
    /// <returns>Combined version string</returns>
    private static string CombineVersions(string version1, string version2)
    {
        if (version1 == "Both" || version2 == "Both")
            return "Both";

        if ((version1 == "Sword" && version2 == "Shield") ||
            (version1 == "Shield" && version2 == "Sword"))
        {
            return "Both";
        }

        return version1;
    }

    /// <summary>
    /// Determines the gender ratio of a Pokémon.
    /// </summary>
    /// <param name="personalInfo">Personal info of the Pokémon</param>
    /// <returns>Gender ratio string</returns>
    private static string DetermineGenderRatio(IPersonalInfo personalInfo) => personalInfo switch
    {
        { Genderless: true } => "Genderless",
        { OnlyFemale: true } => "Female",
        { OnlyMale: true } => "Male",
        { Gender: 0 } => "Male",
        { Gender: 254 } => "Female",
        { Gender: 255 } => "Genderless",
        _ => "Male, Female"
    };

    /// <summary>
    /// Checks if an IV set has any specified values.
    /// </summary>
    /// <param name="ivs">IV set to check</param>
    /// <returns>True if any IVs are specified, false otherwise</returns>
    private static bool IsIVsSpecified(IndividualValueSet ivs)
    {
        bool hasNonDefaultIV = ivs.HP != -1 || ivs.ATK != -1 || ivs.DEF != -1 ||
                               ivs.SPA != -1 || ivs.SPD != -1 || ivs.SPE != -1;

        bool hasMixedValues = (ivs.HP != ivs.ATK || ivs.ATK != ivs.DEF || ivs.DEF != ivs.SPA ||
                               ivs.SPA != ivs.SPD || ivs.SPD != ivs.SPE);

        return hasNonDefaultIV && hasMixedValues;
    }

    /// <summary>
    /// Formats an IV set into a readable string.
    /// </summary>
    /// <param name="ivs">IV set to format</param>
    /// <returns>Formatted IV string</returns>
    private static string FormatIVs(IndividualValueSet ivs)
    {
        var ivParts = new List<string>();

        if (ivs.HP != -1) ivParts.Add($"HP:{ivs.HP}");
        if (ivs.ATK != -1) ivParts.Add($"Atk:{ivs.ATK}");
        if (ivs.DEF != -1) ivParts.Add($"Def:{ivs.DEF}");
        if (ivs.SPA != -1) ivParts.Add($"SpA:{ivs.SPA}");
        if (ivs.SPD != -1) ivParts.Add($"SpD:{ivs.SPD}");
        if (ivs.SPE != -1) ivParts.Add($"Spe:{ivs.SPE}");

        return string.Join(", ", ivParts);
    }

    /// <summary>
    /// Represents encounter information for a Pokémon.
    /// </summary>
    private sealed class EncounterInfo
    {
        /// <summary>
        /// Gets or sets the English species name.
        /// </summary>
        public required string SpeciesName { get; set; }

        /// <summary>
        /// Gets or sets the species Pokédex index.
        /// </summary>
        public required int SpeciesIndex { get; set; }

        /// <summary>
        /// Gets or sets the form number.
        /// </summary>
        public required int Form { get; set; }

        /// <summary>
        /// Gets or sets the location name.
        /// </summary>
        public required string LocationName { get; set; }

        /// <summary>
        /// Gets or sets the location ID.
        /// </summary>
        public required int LocationId { get; set; }

        /// <summary>
        /// Gets or sets the minimum level.
        /// </summary>
        public required int MinLevel { get; set; }

        /// <summary>
        /// Gets or sets the maximum level.
        /// </summary>
        public required int MaxLevel { get; set; }

        /// <summary>
        /// Gets or sets the met level.
        /// </summary>
        public required int MetLevel { get; set; }

        /// <summary>
        /// Gets or sets the encounter type description.
        /// </summary>
        public required string EncounterType { get; set; }

        /// <summary>
        /// Gets or sets whether the encounter is shiny-locked.
        /// </summary>
        public required bool IsShinyLocked { get; set; }

        /// <summary>
        /// Gets or sets whether the encounter is a gift.
        /// </summary>
        public required bool IsGift { get; set; }

        /// <summary>
        /// Gets or sets the fixed ball for the encounter, if any.
        /// </summary>
        public required string FixedBall { get; set; }

        /// <summary>
        /// Gets or sets the game version for the encounter.
        /// </summary>
        public required string EncounterVersion { get; set; }

        /// <summary>
        /// Gets or sets whether the Pokémon can Gigantamax.
        /// </summary>
        public required bool CanGigantamax { get; set; }

        /// <summary>
        /// Gets or sets the gender ratio.
        /// </summary>
        public required string Gender { get; set; }

        /// <summary>
        /// Gets or sets the number of guaranteed perfect IVs.
        /// </summary>
        public int FlawlessIVCount { get; set; }

        /// <summary>
        /// Gets or sets the fixed IV values if specified.
        /// </summary>
        public string SetIVs { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the weather conditions for the encounter.
        /// </summary>
        public AreaWeather8 Weather { get; set; } = AreaWeather8.None;

        /// <summary>
        /// Required Marks that an encounter must have.
        /// </summary>
        public string[] RequiredMarks { get; set; } = [];

        /// <summary>
        /// Possible Marks that an encounter can have, but are not guaranteed.
        /// </summary>
        public string[] PossibleMarks { get; set; } = [];

        /// <summary>
        /// Valid Ribbons that an encounter can have.
        /// </summary>
        public string[] ValidRibbons { get; set; } = [];
        /// <summary>
        /// Legal balls that can be used for this encounter.
        /// </summary>
        public int[] LegalBalls { get; set; } = [];
    }
}

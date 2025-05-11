using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using static PKHeX.Core.Ball;

namespace PKHeX.Core.Legality.Encounters.Data.MetLocations;

/// <summary>
/// Generates encounter location data for Pokémon Scarlet and Violet games.
/// </summary>
public static class EncounterLocationsSV
{
    /// <summary>
    /// Generates encounter location data JSON for Scarlet and Violet games.
    /// </summary>
    /// <param name="outputPath">Path where the JSON file will be saved</param>
    /// <param name="errorLogPath">Path where error logs will be written</param>
    public static void GenerateEncounterDataJSON(string outputPath, string errorLogPath)
    {
        try
        {
            using var errorLogger = new StreamWriter(errorLogPath, false, Encoding.UTF8);
            errorLogger.WriteLine($"[{DateTime.Now}] Starting JSON generation process for encounters in Scarlet/Violet.");

            var gameStrings = GameInfo.GetStrings("en");
            errorLogger.WriteLine($"[{DateTime.Now}] Game strings loaded.");

            var pt = PersonalTable.SV;
            errorLogger.WriteLine($"[{DateTime.Now}] PersonalTable for SV loaded.");

            var encounterData = new Dictionary<string, List<EncounterInfo>>();

            ProcessRegularEncounters(encounterData, gameStrings, pt, errorLogger);
            ProcessEggMetLocations(encounterData, gameStrings, pt, errorLogger);
            ProcessSevenStarRaids(encounterData, gameStrings, pt, errorLogger);

            ProcessStaticEncounters(Encounters9.Encounter_SV, "Both", encounterData, gameStrings, pt, errorLogger);
            ProcessStaticEncounters(Encounters9.StaticSL, "Scarlet", encounterData, gameStrings, pt, errorLogger);
            ProcessStaticEncounters(Encounters9.StaticVL, "Violet", encounterData, gameStrings, pt, errorLogger);

            ProcessFixedEncounters(encounterData, gameStrings, pt, errorLogger);
            ProcessTeraRaidEncounters(encounterData, gameStrings, pt, errorLogger);
            ProcessDistributionEncounters(encounterData, gameStrings, pt, errorLogger);
            ProcessOutbreakEncounters(encounterData, gameStrings, pt, errorLogger);

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
    /// Processes and adds egg met locations to the encounter data.
    /// </summary>
    private static void ProcessEggMetLocations(Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings,
        PersonalTable9SV pt, StreamWriter errorLogger)
    {
        const int eggMetLocationId = 60005;
        const string locationName = "a Picnic";

        errorLogger.WriteLine($"[{DateTime.Now}] Processing egg met locations with location ID: {eggMetLocationId} ({locationName})");

        for (ushort species = 1; species < pt.MaxSpeciesID; species++)
        {
            var personalInfo = pt.GetFormEntry(species, 0);
            if (personalInfo is null or { IsPresentInGame: false })
                continue;

            if (personalInfo.EggGroup1 == 15 || personalInfo.EggGroup2 == 15)
                continue;

            byte formCount = personalInfo.FormCount;
            for (byte form = 0; form < formCount; form++)
            {
                var formInfo = pt.GetFormEntry(species, form);
                if (formInfo is null or { IsPresentInGame: false })
                    continue;

                if (formInfo.EggGroup1 == 15 || formInfo.EggGroup2 == 15)
                    continue;

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
                    SizeType9.RANDOM,
                    0
                );
            }
        }
    }

    /// <summary>
    /// Processes and adds regular wild encounters to the encounter data.
    /// </summary>
    private static void ProcessRegularEncounters(Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings,
        PersonalTable9SV pt, StreamWriter errorLogger)
    {
        foreach (var area in Encounters9.Slots)
        {
            var locationId = area.Location;
            var locationName = gameStrings.GetLocationName(false, (ushort)locationId, 9, 9, GameVersion.SV)
                ?? $"Unknown Location {locationId}";

            foreach (var slot in area.Slots)
            {
                AddEncounterInfoWithEvolutions(encounterData, gameStrings, pt, errorLogger, slot.Species, slot.Form,
                    locationName, locationId, slot.LevelMin, slot.LevelMax, "Wild", false, false,
                    string.Empty, "Both", SizeType9.RANDOM, 0);
            }
        }
    }

    /// <summary>
    /// Processes and adds seven star raid encounters to the encounter data.
    /// </summary>
    private static void ProcessSevenStarRaids(Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings,
        PersonalTable9SV pt, StreamWriter errorLogger)
    {
        foreach (var encounter in Encounters9.Might)
        {
            var locationName = gameStrings.GetLocationName(false, (ushort)EncounterMight9.Location, 9, 9, GameVersion.SV)
                ?? "A Crystal Cavern";

            AddEncounterInfoWithEvolutions(encounterData, gameStrings, pt, errorLogger, encounter.Species, encounter.Form,
                locationName, EncounterMight9.Location, encounter.Level, encounter.Level, "7-Star Raid",
                encounter.Shiny == Shiny.Never, false, string.Empty, "Both", encounter.ScaleType, encounter.Scale,
                encounter.FlawlessIVCount);
        }
    }

    /// <summary>
    /// Processes and adds static encounters to the encounter data.
    /// </summary>
    private static void ProcessStaticEncounters(EncounterStatic9[] encounters, string versionName,
        Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings, PersonalTable9SV pt, StreamWriter errorLogger)
    {
        foreach (var encounter in encounters)
        {
            var locationId = encounter.Location;
            var locationName = gameStrings.GetLocationName(false, (ushort)locationId, 9, 9, GameVersion.SV)
                ?? $"Unknown Location {locationId}";

            string fixedBall = encounter.FixedBall != Ball.None ? encounter.FixedBall.ToString() : string.Empty;

            string setIVs = string.Empty;
            int flawlessIVCount = encounter.FlawlessIVCount;

            if (encounter.IVs is { IsSpecified: true })
            {
                var ivParts = new List<string>();

                if (encounter.IVs.HP >= 0) ivParts.Add($"HP:{encounter.IVs.HP}");
                if (encounter.IVs.ATK >= 0) ivParts.Add($"Atk:{encounter.IVs.ATK}");
                if (encounter.IVs.DEF >= 0) ivParts.Add($"Def:{encounter.IVs.DEF}");
                if (encounter.IVs.SPA >= 0) ivParts.Add($"SpA:{encounter.IVs.SPA}");
                if (encounter.IVs.SPD >= 0) ivParts.Add($"SpD:{encounter.IVs.SPD}");
                if (encounter.IVs.SPE >= 0) ivParts.Add($"Spe:{encounter.IVs.SPE}");

                setIVs = string.Join(", ", ivParts);
            }

            // Use "Titan" encounter type for encounters with IsTitan = true
            string encounterType = encounter.IsTitan ? "Titan" : "Static";

            AddEncounterInfoWithEvolutions(encounterData, gameStrings, pt, errorLogger, encounter.Species, encounter.Form,
                locationName, locationId, encounter.Level, encounter.Level, encounterType,
                encounter.Shiny == Shiny.Never, false, fixedBall, versionName, SizeType9.RANDOM, 0,
                flawlessIVCount, setIVs);
        }
    }

    /// <summary>
    /// Processes and adds fixed encounters to the encounter data.
    /// </summary>
    private static void ProcessFixedEncounters(Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings,
        PersonalTable9SV pt, StreamWriter errorLogger)
    {
        foreach (var encounter in Encounters9.Fixed)
        {
            var locationName = gameStrings.GetLocationName(false, (ushort)encounter.Location, 9, 9, GameVersion.SV)
                ?? $"Unknown Location {encounter.Location}";

            AddEncounterInfoWithEvolutions(encounterData, gameStrings, pt, errorLogger, encounter.Species, encounter.Form,
                locationName, encounter.Location, encounter.Level, encounter.Level, "Fixed",
                false, false, string.Empty, "Both", SizeType9.RANDOM, 0,
                encounter.FlawlessIVCount);
        }
    }

    /// <summary>
    /// Processes and adds tera raid encounters to the encounter data.
    /// </summary>
    private static void ProcessTeraRaidEncounters(Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings,
        PersonalTable9SV pt, StreamWriter errorLogger)
    {
        ProcessTeraRaidEncountersForGroup(Encounters9.TeraBase, encounterData, gameStrings, pt, errorLogger, "Paldea");
        ProcessTeraRaidEncountersForGroup(Encounters9.TeraDLC1, encounterData, gameStrings, pt, errorLogger, "Kitakami");
        ProcessTeraRaidEncountersForGroup(Encounters9.TeraDLC2, encounterData, gameStrings, pt, errorLogger, "Blueberry");
    }

    /// <summary>
    /// Processes and adds tera raid encounters for a specific group to the encounter data.
    /// </summary>
    private static void ProcessTeraRaidEncountersForGroup(EncounterTera9[] encounters,
       Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings,
       PersonalTable9SV pt, StreamWriter errorLogger, string groupName)
    {
        foreach (var encounter in encounters)
        {
            var locationName = gameStrings.GetLocationName(false, (ushort)EncounterTera9.Location, 9, 9, GameVersion.SV)
                ?? "Tera Raid Den";

            string versionAvailability = (encounter.IsAvailableHostScarlet, encounter.IsAvailableHostViolet) switch
            {
                (true, true) => "Both",
                (true, false) => "Scarlet",
                (false, true) => "Violet",
                _ => "Unknown"
            };

            AddEncounterInfoWithEvolutions(encounterData, gameStrings, pt, errorLogger, encounter.Species, encounter.Form,
                locationName, EncounterTera9.Location, encounter.Level, encounter.Level,
                $"{encounter.Stars}★ Tera Raid {groupName}", encounter.Shiny == Shiny.Never, false,
                string.Empty, versionAvailability, SizeType9.RANDOM, 0, encounter.FlawlessIVCount);
        }
    }

    /// <summary>
    /// Processes and adds distribution encounters to the encounter data.
    /// </summary>
    private static void ProcessDistributionEncounters(Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings,
        PersonalTable9SV pt, StreamWriter errorLogger)
    {
        foreach (var encounter in Encounters9.Dist)
        {
            var locationName = gameStrings.GetLocationName(false, (ushort)EncounterDist9.Location, 9, 9, GameVersion.SV)
                ?? "Distribution Raid Den";

            var versionAvailability = GetVersionAvailability(encounter);

            AddEncounterInfoWithEvolutions(encounterData, gameStrings, pt, errorLogger, encounter.Species, encounter.Form,
                locationName, EncounterDist9.Location, encounter.Level, encounter.Level,
                $"Distribution Raid {encounter.Stars}★", encounter.Shiny == Shiny.Never, false,
                string.Empty, versionAvailability, encounter.ScaleType, encounter.Scale,
                encounter.FlawlessIVCount);
        }
    }

    /// <summary>
    /// Determines version availability for a distribution encounter.
    /// </summary>
    private static string GetVersionAvailability(EncounterDist9 encounter)
    {
        bool availableInScarlet = encounter.RandRate0TotalScarlet > 0 || encounter.RandRate1TotalScarlet > 0 ||
                                  encounter.RandRate2TotalScarlet > 0 || encounter.RandRate3TotalScarlet > 0;

        bool availableInViolet = encounter.RandRate0TotalViolet > 0 || encounter.RandRate1TotalViolet > 0 ||
                                encounter.RandRate2TotalViolet > 0 || encounter.RandRate3TotalViolet > 0;

        return (availableInScarlet, availableInViolet) switch
        {
            (true, true) => "Both",
            (true, false) => "Scarlet",
            (false, true) => "Violet",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Processes and adds outbreak encounters to the encounter data.
    /// </summary>
    private static void ProcessOutbreakEncounters(Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings,
        PersonalTable9SV pt, StreamWriter errorLogger)
    {
        foreach (var encounter in Encounters9.Outbreak)
        {
            var locationName = gameStrings.GetLocationName(false, encounter.Location, 9, 9, GameVersion.SV)
                ?? $"Unknown Location {encounter.Location}";

            SizeType9 sizeType = encounter.IsForcedScaleRange ? SizeType9.VALUE : SizeType9.RANDOM;
            byte sizeValue = encounter.IsForcedScaleRange ? encounter.ScaleMin : (byte)0;

            AddEncounterInfoWithEvolutions(encounterData, gameStrings, pt, errorLogger, encounter.Species, encounter.Form,
                locationName, encounter.Location, encounter.LevelMin, encounter.LevelMax, "Outbreak",
                encounter.Shiny == Shiny.Never, false, string.Empty, "Both", sizeType, sizeValue);
        }
    }

    /// <summary>
    /// Adds encounter information with evolutions to the encounter data.
    /// </summary>
    private static void AddEncounterInfoWithEvolutions(Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings,
        PersonalTable9SV pt, StreamWriter errorLogger, ushort speciesIndex, byte form, string locationName, int locationId,
        int minLevel, int maxLevel, string encounterType, bool isShinyLocked, bool isGift, string fixedBall,
        string encounterVersion, SizeType9 sizeType, byte sizeValue, int flawlessIVCount = 0, string setIVs = "")
    {
        var personalInfo = pt.GetFormEntry(speciesIndex, form);
        if (personalInfo is null or { IsPresentInGame: false })
        {
            errorLogger.WriteLine($"[{DateTime.Now}] Species {speciesIndex} form {form} not present in SV. Skipping.");
            return;
        }

        AddSingleEncounterInfo(encounterData, gameStrings, errorLogger, speciesIndex, form, locationName, locationId,
            minLevel, maxLevel, minLevel, encounterType, isShinyLocked, isGift, fixedBall, encounterVersion,
            sizeType, sizeValue, flawlessIVCount, setIVs);

        var processedForms = new HashSet<(ushort Species, byte Form)> { (speciesIndex, form) };

        ProcessEvolutionLine(encounterData, gameStrings, pt, errorLogger, speciesIndex, form, locationName, locationId,
            minLevel, maxLevel, minLevel, encounterType, isShinyLocked, isGift, fixedBall, encounterVersion,
            sizeType, sizeValue, flawlessIVCount, setIVs, processedForms);
    }

    /// <summary>
    /// Gets the minimum evolution level required for a Pokémon to evolve.
    /// </summary>
    private static int GetMinEvolutionLevel(ushort baseSpecies, byte baseForm, ushort evolvedSpecies, byte evolvedForm)
    {
        var tree = EvolutionTree.GetEvolutionTree(EntityContext.Gen9);
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
    /// Gets the level at which a Pokémon evolves using the specified evolution method.
    /// </summary>
    private static int GetEvolutionLevel(EvolutionMethod evo)
    {
        if (evo.Level > 0)
            return evo.Level;
        if (evo.Method == EvolutionType.LevelUp && evo.Argument > 0)
            return evo.Argument;
        return 0;
    }

    /// <summary>
    /// Processes the evolution line of a Pokémon and adds encounters for evolved forms.
    /// </summary>
    private static void ProcessEvolutionLine(Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings,
        PersonalTable9SV pt, StreamWriter errorLogger, ushort species, byte form, string locationName, int locationId,
        int baseLevel, int maxLevel, int metLevel, string encounterType, bool isShinyLocked, bool isGift, string fixedBall,
        string encounterVersion, SizeType9 sizeType, byte sizeValue, int flawlessIVCount, string setIVs,
        HashSet<(ushort Species, byte Form)> processedForms)
    {
        var personalInfo = pt.GetFormEntry(species, form);
        if (personalInfo is null or { IsPresentInGame: false })
            return;

        var nextEvolutions = GetImmediateEvolutions(species, form, pt, processedForms);
        foreach (var (evoSpecies, evoForm) in nextEvolutions)
        {
            if (!processedForms.Add((evoSpecies, evoForm)))
                continue;

            var evoPersonalInfo = pt.GetFormEntry(evoSpecies, evoForm);
            if (evoPersonalInfo is null or { IsPresentInGame: false })
                continue;

            // Get minimum level for evolution
            var evolutionMinLevel = GetMinEvolutionLevel(species, form, evoSpecies, evoForm);
            var minLevel = Math.Max(baseLevel, evolutionMinLevel);

            AddSingleEncounterInfo(encounterData, gameStrings, errorLogger, evoSpecies, evoForm, locationName, locationId,
                minLevel, Math.Max(minLevel, maxLevel), metLevel, $"{encounterType} (Evolved)", isShinyLocked, isGift,
                fixedBall, encounterVersion, sizeType, sizeValue, flawlessIVCount, setIVs);

            ProcessEvolutionLine(encounterData, gameStrings, pt, errorLogger, evoSpecies, evoForm, locationName, locationId,
                minLevel, Math.Max(minLevel, maxLevel), metLevel, encounterType, isShinyLocked, isGift, fixedBall,
                encounterVersion, sizeType, sizeValue, flawlessIVCount, setIVs, processedForms);
        }
    }

    /// <summary>
    /// Gets immediate evolutions for a Pokémon species and form.
    /// </summary>
    private static List<(ushort Species, byte Form)> GetImmediateEvolutions(
        ushort species,
        byte form,
        PersonalTable9SV pt,
        HashSet<(ushort Species, byte Form)> processedForms)
    {
        List<(ushort Species, byte Form)> results = [];

        var tree = EvolutionTree.GetEvolutionTree(EntityContext.Gen9);
        var evos = tree.Forward.GetForward(species, form);

        foreach (var evo in evos.Span)
        {
            ushort evoSpecies = (ushort)evo.Species;
            byte evoForm = (byte)evo.Form;

            if (processedForms.Contains((evoSpecies, evoForm)))
                continue;

            var personalInfo = pt.GetFormEntry(evoSpecies, evoForm);
            if (personalInfo is null or { IsPresentInGame: false })
                continue;

            results.Add((evoSpecies, evoForm));
        }

        return results;
    }

    /// <summary>
    /// Combines version availability from two sources.
    /// </summary>
    private static string CombineVersions(string version1, string version2)
    {
        if (version1 == "Both" || version2 == "Both")
            return "Both";

        if ((version1 == "Scarlet" && version2 == "Violet") ||
            (version1 == "Violet" && version2 == "Scarlet"))
        {
            return "Both";
        }

        return version1;
    }

    private static readonly string[] Gen9Ribbons = ["ChampionPaldea", "OnceInALifetime", "Partner"];
    private static readonly string[] PreviousGenRibbons =
    [
        "Alert", "BeautyMaster", "BestFriends", "Careless",
    "ClevernessMaster", "ContestStar", "CoolnessMaster", "CutenessMaster",
    "Downcast", "Effort", "GalarChampion", "Gorgeous",
    "GorgeousRoyal", "Hisui", "MasterRank",
    "Relax", "Royal", "Shock", "SinnohChampion",
    "Smile", "Snooze", "ToughnessMaster", "TowerMaster",
    "TwinklingStar"
    ];

    // Gen 8 mark names (time-based)
    private static readonly string[] TimeBasedMarks = ["MarkLunchtime", "MarkSleepyTime", "MarkDusk", "MarkDawn"];

    // Gen 8 mark names (weather-based)
    private static readonly string[] WeatherBasedMarks = ["MarkCloudy", "MarkRainy", "MarkStormy", "MarkSnowy",
        "MarkBlizzard", "MarkDry", "MarkSandstorm", "MarkMisty"];

    // Gen 8 mark names (personality-based)
    private static readonly string[] PersonalityMarks = [
        "MarkRowdy", "MarkAbsentMinded", "MarkJittery", "MarkExcited", "MarkCharismatic", "MarkCalmness",
        "MarkIntense", "MarkZonedOut", "MarkJoyful", "MarkAngry", "MarkSmiley", "MarkTeary",
        "MarkUpbeat", "MarkPeeved", "MarkIntellectual", "MarkFerocious", "MarkCrafty", "MarkScowling",
        "MarkKindly", "MarkFlustered", "MarkPumpedUp", "MarkZeroEnergy", "MarkPrideful", "MarkUnsure",
        "MarkHumble", "MarkThorny", "MarkVigor", "MarkSlump"
    ];

    /// <summary>
    /// Adds guaranteed marks, possible marks, and valid ribbons to the encounter
    /// </summary>
    /// <param name="encounter">The encounter info to update with mark and ribbon data</param>
    /// <param name="errorLogger">Logger for recording processing information</param>
    private static void SetEncounterMarksAndRibbons(EncounterInfo encounter, StreamWriter errorLogger)
    {
        var requiredMarks = new List<string>();
        var possibleMarks = new List<string>();
        var validRibbons = new List<string>();

        // Process required marks based on encounter type
        if (encounter.EncounterType.Contains("7-Star Raid"))
        {
            requiredMarks.Add("MarkMightiest");
        }
        else if (encounter.EncounterType.Contains("Titan"))
        {
            requiredMarks.Add("MarkTitan");
        }

        // Process guaranteed size-based marks
        if (encounter.SizeType == SizeType9.VALUE)
        {
            if (encounter.SizeValue == byte.MaxValue)
                requiredMarks.Add("MarkJumbo");
            else if (encounter.SizeValue == byte.MinValue)
                requiredMarks.Add("MarkMini");
        }

        // Process possible marks
        if (CanHaveEncounterMarks(encounter))
        {
            // Add possible marks based on encounter conditions
            if (CanHaveWeatherMarks(encounter))
            {
                // Weather-based marks for wild encounters
                possibleMarks.AddRange(WeatherBasedMarks);
            }

            if (CanHaveTimeMarks(encounter))
            {
                // Time-based marks for wild encounters
                possibleMarks.AddRange(TimeBasedMarks);
            }

            // Special condition marks
            if (encounter.EncounterType.Contains("Fishing"))
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

        // Gen 9 specific obtainable marks
        if (CanHaveGen9Marks(encounter))
        {
            if (!requiredMarks.Contains("MarkJumbo") &&
                !requiredMarks.Contains("MarkMini") &&
                !encounter.EncounterType.Contains("7-Star Raid"))
            {
                possibleMarks.Add("MarkItemfinder");
            }

            if (!encounter.EncounterType.Contains("7-Star Raid") &&
                !encounter.EncounterType.Contains("Titan"))
            {
                possibleMarks.Add("MarkPartner");
            }

            possibleMarks.Add("MarkGourmand");
        }

        // Add all valid ribbons for Gen 9
        validRibbons.AddRange(Gen9Ribbons);
        validRibbons.AddRange(PreviousGenRibbons);

        encounter.RequiredMarks = [.. requiredMarks];
        encounter.PossibleMarks = [.. possibleMarks.Except(requiredMarks).ToArray()];
        encounter.ValidRibbons = [.. validRibbons];

        errorLogger.WriteLine($"[{DateTime.Now}] Mark analysis - Required: {string.Join(", ", requiredMarks)}, " +
            $"Possible: {(possibleMarks.Count > 5 ? string.Join(", ", possibleMarks.Take(5)) + "..." : string.Join(", ", possibleMarks))}");
    }

    /// <summary>
    /// Determines if the encounter type can have encounter marks from Gen 8/9.
    /// </summary>
    /// <param name="encounter">The encounter info to check</param>
    /// <returns>True if the encounter can have encounter marks</returns>
    private static bool CanHaveEncounterMarks(EncounterInfo encounter)
    {
        // Based on IsEncounterMarkAllowed in MarkRules
        // Egg, gift, and static encounters typically don't have marks
        if (encounter.EncounterType is "Egg" or "Gift" or "Static")
            return false;

        // Tera raids don't have Gen8 encounter marks
        if (encounter.EncounterType.Contains("Raid") || encounter.EncounterType.Contains("Distribution"))
            return false;

        // Wild and outbreak encounters can have marks
        return encounter.EncounterType.Contains("Wild") ||
               encounter.EncounterType.Contains("Outbreak") ||
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
               encounter.EncounterType.Contains("Outbreak");
    }

    /// <summary>
    /// Determines if the encounter can have time-based marks.
    /// </summary>
    /// <param name="encounter">The encounter info to check</param>
    /// <returns>True if the encounter can have time-based marks</returns>
    private static bool CanHaveTimeMarks(EncounterInfo encounter)
    {
        // Time marks apply to wild encounters
        return encounter.EncounterType.Contains("Wild") ||
               encounter.EncounterType.Contains("Outbreak");
    }

    /// <summary>
    /// Determines if the encounter can have Gen 9 specific marks.
    /// </summary>
    /// <param name="encounter">The encounter info to check</param>
    /// <returns>True if the encounter can have Gen 9 specific marks</returns>
    private static bool CanHaveGen9Marks(EncounterInfo encounter)
    {
        // Most Gen 9 marks are obtainable through activities, not tied to encounter
        return encounter.EncounterType != "Egg";
    }

    /// <summary>
    /// Adds a single encounter to the encounter data.
    /// </summary>
    private static void AddSingleEncounterInfo(Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings,
        StreamWriter errorLogger, ushort speciesIndex, byte form, string locationName, int locationId, int minLevel, int maxLevel,
        int metLevel, string encounterType, bool isShinyLocked, bool isGift, string fixedBall, string encounterVersion,
        SizeType9 sizeType, byte sizeValue, int flawlessIVCount = 0, string setIVs = "")
    {
        string dexNumber = form > 0 ? $"{speciesIndex}-{form}" : speciesIndex.ToString();

        var speciesName = gameStrings.specieslist[speciesIndex];
        if (string.IsNullOrEmpty(speciesName))
        {
            errorLogger.WriteLine($"[{DateTime.Now}] Empty species name for index {speciesIndex}. Skipping.");
            return;
        }

        var personalInfo = PersonalTable.SV.GetFormEntry(speciesIndex, form);
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
            e.Gender == genderRatio);

        if (existingEncounter is not null)
        {
            existingEncounter.MinLevel = Math.Min(existingEncounter.MinLevel, minLevel);
            existingEncounter.MaxLevel = Math.Max(existingEncounter.MaxLevel, maxLevel);
            existingEncounter.MetLevel = Math.Min(existingEncounter.MetLevel, metLevel);

            string existingVersion = existingEncounter.EncounterVersion ?? string.Empty;
            string newEncounterVersion = encounterVersion ?? string.Empty;
            existingEncounter.EncounterVersion = CombineVersions(existingVersion, newEncounterVersion);

            // Update IV requirements with higher priority value (if any)
            if (flawlessIVCount > existingEncounter.FlawlessIVCount)
            {
                existingEncounter.FlawlessIVCount = flawlessIVCount;
                // Clear specific IVs if we're using flawless count
                if (flawlessIVCount > 0)
                    existingEncounter.SetIVs = string.Empty;
            }

            // Only set specific IVs if we don't have flawless count and new IVs are provided
            if (existingEncounter.FlawlessIVCount == 0 && !string.IsNullOrEmpty(setIVs) && string.IsNullOrEmpty(existingEncounter.SetIVs))
            {
                existingEncounter.SetIVs = setIVs;
            }

            errorLogger.WriteLine($"[{DateTime.Now}] Updated existing encounter: {speciesName} " +
                $"(Dex: {dexNumber}) at {locationName} (ID: {locationId}), Levels {existingEncounter.MinLevel}-{existingEncounter.MaxLevel}, " +
                $"Met Level: {existingEncounter.MetLevel}, Type: {encounterType}, Version: {existingEncounter.EncounterVersion}, Gender: {genderRatio}, " +
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
                SizeType = sizeType,
                SizeValue = sizeValue,
                Gender = genderRatio,
                FlawlessIVCount = flawlessIVCount,
                SetIVs = setIVs
            };

            SetEncounterMarksAndRibbons(newEncounter, errorLogger);
            SetLegalBalls(newEncounter, errorLogger);

            encounterList.Add(newEncounter);

            errorLogger.WriteLine($"[{DateTime.Now}] Processed new encounter: {speciesName} " +
                $"(Dex: {dexNumber}) at {locationName} (ID: {locationId}), Levels {minLevel}-{maxLevel}, " +
                $"Met Level: {metLevel}, Type: {encounterType}, Version: {encounterVersion}, Gender: {genderRatio}, " +
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

        // Determine ball permission mask based on encounter type
        ulong ballPermitMask;

        if (encounter.IsGift)
        {
            // Gift Pokémon typically come in a standard Poké Ball
            ballPermitMask = 1ul << (int)Ball.Poke;
        }
        else if (encounter.EncounterType.Contains("Raid") || encounter.EncounterType.Contains("Distribution"))
        {
            // All raid-type encounters use standard Gen 9 balls
            ballPermitMask = BallUseLegality.WildPokeballs9;
        }
        else
        {
            // All other wild-type encounters use standard wild ball set for Gen 9
            ballPermitMask = BallUseLegality.GetWildBalls(9, gameVersion);
        }

        // Convert the bitmask to a list of legal balls
        for (byte ballId = 1; ballId < 64; ballId++)
        {
            if (BallUseLegality.IsBallPermitted(ballPermitMask, ballId))
            {
                var ball = (Ball)ballId;
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
            "Scarlet" => GameVersion.SL,
            "Violet" => GameVersion.VL,
            _ => GameVersion.SV, // Both
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
            Ball.LAOrigin => 8, 
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
            _ => 14, 
        };
    }

    /// <summary>
    /// Determines the gender ratio of a Pokémon based on its personal info.
    /// </summary>
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
    /// Represents encounter information for a specific Pokémon.
    /// </summary>
    private sealed record EncounterInfo
    {
        /// <summary>
        /// The name of the Pokémon species.
        /// </summary>
        public required string SpeciesName { get; set; }

        /// <summary>
        /// The species index (national dex number).
        /// </summary>
        public required int SpeciesIndex { get; set; }

        /// <summary>
        /// The form number of the Pokémon.
        /// </summary>
        public required int Form { get; set; }

        /// <summary>
        /// The name of the location where the Pokémon is encountered.
        /// </summary>
        public required string LocationName { get; set; }

        /// <summary>
        /// The ID of the location where the Pokémon is encountered.
        /// </summary>
        public required int LocationId { get; set; }

        /// <summary>
        /// The minimum level at which the Pokémon can be encountered.
        /// </summary>
        public required int MinLevel { get; set; }

        /// <summary>
        /// The maximum level at which the Pokémon can be encountered.
        /// </summary>
        public required int MaxLevel { get; set; }

        /// <summary>
        /// The met level for the Pokémon.
        /// </summary>
        public required int MetLevel { get; set; }

        /// <summary>
        /// The type of encounter (e.g., Wild, Egg, Raid).
        /// </summary>
        public required string EncounterType { get; set; }

        /// <summary>
        /// Indicates whether the Pokémon is shiny locked.
        /// </summary>
        public required bool IsShinyLocked { get; set; }

        /// <summary>
        /// Indicates whether the Pokémon is a gift.
        /// </summary>
        public required bool IsGift { get; set; }

        /// <summary>
        /// The fixed ball type for the encounter, if any.
        /// </summary>
        public required string FixedBall { get; set; }

        /// <summary>
        /// The game version(s) where the encounter is available.
        /// </summary>
        public required string EncounterVersion { get; set; }

        /// <summary>
        /// The size type of the Pokémon.
        /// </summary>
        public required SizeType9 SizeType { get; set; }

        /// <summary>
        /// The size value of the Pokémon.
        /// </summary>
        public required byte SizeValue { get; set; }

        /// <summary>
        /// The gender ratio of the Pokémon.
        /// </summary>
        public required string Gender { get; set; }

        /// <summary>
        /// Specific IVs for the encounter.
        /// </summary>
        public string SetIVs { get; set; } = string.Empty;

        /// <summary>
        /// The number of guaranteed perfect (31) IVs.
        /// </summary>
        public int FlawlessIVCount { get; set; }
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

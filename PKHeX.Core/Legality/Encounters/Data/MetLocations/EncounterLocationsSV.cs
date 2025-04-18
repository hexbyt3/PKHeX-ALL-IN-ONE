using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace PKHeX.Core.Legality.Encounters.Data.MetLocations;
public static class EncounterLocationsSV
{
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

    private static void ProcessEggMetLocations(Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings,
        PersonalTable9SV pt, StreamWriter errorLogger)
    {
        const int eggMetLocationId = 60005;
        const string locationName = "a Picnic";

        errorLogger.WriteLine($"[{DateTime.Now}] Processing egg met locations with location ID: {eggMetLocationId} ({locationName})");

        for (ushort species = 1; species < pt.MaxSpeciesID; species++)
        {
            var personalInfo = pt.GetFormEntry(species, 0);
            if (personalInfo is null || !personalInfo.IsPresentInGame)
                continue;

            if (personalInfo.EggGroup1 == 15 || personalInfo.EggGroup2 == 15)
                continue;

            byte formCount = personalInfo.FormCount;
            for (byte form = 0; form < formCount; form++)
            {
                var formInfo = pt.GetFormEntry(species, form);
                if (formInfo is null || !formInfo.IsPresentInGame)
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

    private static void ProcessSevenStarRaids(Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings,
        PersonalTable9SV pt, StreamWriter errorLogger)
    {
        foreach (var encounter in Encounters9.Might)
        {
            var locationName = gameStrings.GetLocationName(false, (ushort)EncounterMight9.Location, 9, 9, GameVersion.SV)
                ?? "A Crystal Cavern";

            AddEncounterInfoWithEvolutions(encounterData, gameStrings, pt, errorLogger, encounter.Species, encounter.Form,
                locationName, EncounterMight9.Location, encounter.Level, encounter.Level, "7-Star Raid",
                encounter.Shiny == Shiny.Never, false, string.Empty, "Both", encounter.ScaleType, encounter.Scale);
        }
    }

    private static void ProcessStaticEncounters(EncounterStatic9[] encounters, string versionName,
        Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings, PersonalTable9SV pt, StreamWriter errorLogger)
    {
        foreach (var encounter in encounters)
        {
            var locationId = encounter.Location;
            var locationName = gameStrings.GetLocationName(false, (ushort)locationId, 9, 9, GameVersion.SV)
                ?? $"Unknown Location {locationId}";

            string fixedBall = encounter.FixedBall != Ball.None ? encounter.FixedBall.ToString() : string.Empty;
            AddEncounterInfoWithEvolutions(encounterData, gameStrings, pt, errorLogger, encounter.Species, encounter.Form,
                locationName, locationId, encounter.Level, encounter.Level, "Static",
                encounter.Shiny == Shiny.Never, false, fixedBall, versionName, SizeType9.RANDOM, 0);
        }
    }

    private static void ProcessFixedEncounters(Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings,
        PersonalTable9SV pt, StreamWriter errorLogger)
    {
        foreach (var encounter in Encounters9.Fixed)
        {
            var locationName = gameStrings.GetLocationName(false, (ushort)encounter.Location, 9, 9, GameVersion.SV)
                ?? $"Unknown Location {encounter.Location}";

            AddEncounterInfoWithEvolutions(encounterData, gameStrings, pt, errorLogger, encounter.Species, encounter.Form,
                locationName, encounter.Location, encounter.Level, encounter.Level, "Fixed",
                false, false, string.Empty, "Both", SizeType9.RANDOM, 0);
        }
    }

    private static void ProcessTeraRaidEncounters(Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings,
        PersonalTable9SV pt, StreamWriter errorLogger)
    {
        ProcessTeraRaidEncountersForGroup(Encounters9.TeraBase, encounterData, gameStrings, pt, errorLogger, "Paldea");
        ProcessTeraRaidEncountersForGroup(Encounters9.TeraDLC1, encounterData, gameStrings, pt, errorLogger, "Kitakami");
        ProcessTeraRaidEncountersForGroup(Encounters9.TeraDLC2, encounterData, gameStrings, pt, errorLogger, "Blueberry");
    }

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
                string.Empty, versionAvailability, SizeType9.RANDOM, 0);
        }
    }

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
                string.Empty, versionAvailability, encounter.ScaleType, encounter.Scale);
        }
    }

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

    private static void AddEncounterInfoWithEvolutions(Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings,
        PersonalTable9SV pt, StreamWriter errorLogger, ushort speciesIndex, byte form, string locationName, int locationId,
        int minLevel, int maxLevel, string encounterType, bool isShinyLocked, bool isGift, string fixedBall,
        string encounterVersion, SizeType9 sizeType, byte sizeValue)
    {
        var personalInfo = pt.GetFormEntry(speciesIndex, form);
        if (personalInfo is null || !personalInfo.IsPresentInGame)
        {
            errorLogger.WriteLine($"[{DateTime.Now}] Species {speciesIndex} form {form} not present in SV. Skipping.");
            return;
        }

        AddSingleEncounterInfo(encounterData, gameStrings, errorLogger, speciesIndex, form, locationName, locationId,
            minLevel, maxLevel, minLevel, encounterType, isShinyLocked, isGift, fixedBall, encounterVersion, sizeType, sizeValue);

        var processedForms = new HashSet<(ushort Species, byte Form)> { (speciesIndex, form) };

        ProcessEvolutionLine(encounterData, gameStrings, pt, errorLogger, speciesIndex, form, locationName, locationId,
            minLevel, maxLevel, minLevel, encounterType, isShinyLocked, isGift, fixedBall, encounterVersion, sizeType, sizeValue, processedForms);
    }

    private static int GetMinEvolutionLevel(ushort baseSpecies, ushort evolvedSpecies)
    {
        var tree = EvolutionTree.GetEvolutionTree(EntityContext.Gen9);
        int minLevel = 1;

        var evos = tree.Forward.GetForward(baseSpecies, 0);
        foreach (var evo in evos.Span)
        {
            if (evo.Species == evolvedSpecies)
            {
                int levelRequirement = evo.LevelUp > 0 ? evo.LevelUp :
                                       evo.Method == EvolutionType.LevelUp ? evo.Argument : 1;
                minLevel = Math.Max(minLevel, levelRequirement);
                return minLevel;
            }

            var secondaryEvos = tree.Forward.GetForward((ushort)evo.Species, 0);
            foreach (var secondEvo in secondaryEvos.Span)
            {
                if (secondEvo.Species == evolvedSpecies)
                {
                    int firstEvolutionLevel = evo.LevelUp > 0 ? evo.LevelUp :
                                              evo.Method == EvolutionType.LevelUp ? evo.Argument : 1;
                    int secondEvolutionLevel = secondEvo.LevelUp > 0 ? secondEvo.LevelUp :
                                               secondEvo.Method == EvolutionType.LevelUp ? secondEvo.Argument : 1;

                    minLevel = Math.Max(minLevel, Math.Max(firstEvolutionLevel, secondEvolutionLevel));
                    return minLevel;
                }
            }
        }

        return minLevel;
    }

    private static void ProcessEvolutionLine(Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings,
        PersonalTable9SV pt, StreamWriter errorLogger, ushort species, byte form, string locationName, int locationId,
        int baseLevel, int maxLevel, int metLevel, string encounterType, bool isShinyLocked, bool isGift, string fixedBall, string encounterVersion,
        SizeType9 sizeType, byte sizeValue, HashSet<(ushort Species, byte Form)> processedForms)
    {
        var personalInfo = pt.GetFormEntry(species, form);
        if (personalInfo is null || !personalInfo.IsPresentInGame)
            return;

        var nextEvolutions = GetImmediateEvolutions(species, form, pt, processedForms);
        foreach (var (evoSpecies, evoForm) in nextEvolutions)
        {
            if (!processedForms.Add((evoSpecies, evoForm)))
                continue;

            var evoPersonalInfo = pt.GetFormEntry(evoSpecies, evoForm);
            if (evoPersonalInfo is null || !evoPersonalInfo.IsPresentInGame)
                continue;

            var evolutionMinLevel = GetMinEvolutionLevel(species, evoSpecies);
            var minLevel = Math.Max(baseLevel, evolutionMinLevel);

            AddSingleEncounterInfo(encounterData, gameStrings, errorLogger, evoSpecies, evoForm, locationName, locationId,
                minLevel, Math.Max(minLevel, maxLevel), metLevel, $"{encounterType} (Evolved)", isShinyLocked, isGift,
                fixedBall, encounterVersion, sizeType, sizeValue);

            ProcessEvolutionLine(encounterData, gameStrings, pt, errorLogger, evoSpecies, evoForm, locationName, locationId,
                minLevel, Math.Max(minLevel, maxLevel), metLevel, encounterType, isShinyLocked, isGift, fixedBall, encounterVersion, sizeType, sizeValue, processedForms);
        }
    }

    private static List<(ushort Species, byte Form)> GetImmediateEvolutions(
        ushort species,
        byte form,
        PersonalTable9SV pt,
        HashSet<(ushort Species, byte Form)> processedForms)
    {
        var results = new List<(ushort Species, byte Form)>();

        var tree = EvolutionTree.GetEvolutionTree(EntityContext.Gen9);
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

    private static void AddSingleEncounterInfo(Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings,
        StreamWriter errorLogger, ushort speciesIndex, byte form, string locationName, int locationId, int minLevel, int maxLevel,
        int metLevel, string encounterType, bool isShinyLocked, bool isGift, string fixedBall, string encounterVersion,
        SizeType9 sizeType, byte sizeValue)
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

            errorLogger.WriteLine($"[{DateTime.Now}] Updated existing encounter: {speciesName} " +
                $"(Dex: {dexNumber}) at {locationName} (ID: {locationId}), Levels {existingEncounter.MinLevel}-{existingEncounter.MaxLevel}, " +
                $"Met Level: {existingEncounter.MetLevel}, Type: {encounterType}, Version: {existingEncounter.EncounterVersion}, Gender: {genderRatio}");
        }
        else
        {
            encounterList.Add(new EncounterInfo
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
                Gender = genderRatio
            });

            errorLogger.WriteLine($"[{DateTime.Now}] Processed new encounter: {speciesName} " +
                $"(Dex: {dexNumber}) at {locationName} (ID: {locationId}), Levels {minLevel}-{maxLevel}, " +
                $"Met Level: {metLevel}, Type: {encounterType}, Version: {encounterVersion}, Gender: {genderRatio}");
        }
    }

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

    private sealed class EncounterInfo
    {
        public required string SpeciesName { get; set; }
        public required int SpeciesIndex { get; set; }
        public required int Form { get; set; }
        public required string LocationName { get; set; }
        public required int LocationId { get; set; }
        public required int MinLevel { get; set; }
        public required int MaxLevel { get; set; }
        public required int MetLevel { get; set; }
        public required string EncounterType { get; set; }
        public required bool IsShinyLocked { get; set; }
        public required bool IsGift { get; set; }
        public required string FixedBall { get; set; }
        public required string EncounterVersion { get; set; }
        public required SizeType9 SizeType { get; set; }
        public required byte SizeValue { get; set; }
        public required string Gender { get; set; }
    }
}

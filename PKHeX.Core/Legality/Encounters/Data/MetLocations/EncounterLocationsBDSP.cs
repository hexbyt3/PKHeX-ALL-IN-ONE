using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace PKHeX.Core.Legality.Encounters.Data.MetLocations;
public static class EncounterLocationsBDSP
{
    public static void GenerateEncounterDataJSON(string outputPath, string errorLogPath)
    {
        try
        {
            using var errorLogger = new StreamWriter(errorLogPath, false, Encoding.UTF8);
            errorLogger.WriteLine($"[{DateTime.Now}] Starting JSON generation process for encounters in BDSP.");

            var gameStrings = GameInfo.GetStrings("en");
            errorLogger.WriteLine($"[{DateTime.Now}] Game strings loaded.");

            var pt = PersonalTable.BDSP;
            errorLogger.WriteLine($"[{DateTime.Now}] PersonalTable for BDSP loaded.");

            var encounterData = new Dictionary<string, List<EncounterInfo>>();

            ProcessWildEncounters(encounterData, gameStrings, errorLogger);
            ProcessEggMetLocations(encounterData, gameStrings, errorLogger);
            ProcessStaticEncounters(encounterData, gameStrings, errorLogger);

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

    private static void ProcessWildEncounters(Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings,
        StreamWriter errorLogger)
    {
        ProcessEncounterAreas(Encounters8b.SlotsBD, GameVersion.BD, encounterData, gameStrings, errorLogger);
        ProcessEncounterAreas(Encounters8b.SlotsSP, GameVersion.SP, encounterData, gameStrings, errorLogger);
    }

    private static void ProcessEncounterAreas(EncounterArea8b[] areas, GameVersion version,
        Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings, StreamWriter errorLogger)
    {
        foreach (var area in areas)
        {
            var locationName = gameStrings.GetLocationName(false, area.Location, 8, 8, GameVersion.BDSP)
                ?? $"Unknown Location {area.Location}";

            foreach (var slot in area.Slots)
            {
                var speciesName = gameStrings.specieslist[slot.Species];
                if (string.IsNullOrEmpty(speciesName))
                {
                    errorLogger.WriteLine($"[{DateTime.Now}] Empty species name for index {slot.Species}. Skipping.");
                    continue;
                }

                AddEncounterInfoWithEvolutions(
                    encounterData, gameStrings, errorLogger,
                    slot.Species, slot.Form, locationName, area.Location,
                    slot.LevelMin, slot.LevelMax, area.Type.ToString(),
                    false, false, string.Empty,
                    version.ToString(), slot.IsUnderground);
            }
        }
    }

    private static void ProcessEggMetLocations(Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings,
        StreamWriter errorLogger)
    {
        ProcessEggLocationForNursery(60006, "a Nursery Couple", encounterData, gameStrings, errorLogger);
        ProcessEggLocationForNursery(60010, "a Nursery Couple (2)", encounterData, gameStrings, errorLogger);
    }

    private static void ProcessEggLocationForNursery(ushort eggMetLocationId, string locationName,
        Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings, StreamWriter errorLogger)
    {
        errorLogger.WriteLine($"[{DateTime.Now}] Processing egg met locations with location ID: {eggMetLocationId} ({locationName})");

        var pt = PersonalTable.BDSP;

        for (ushort species = 1; species < pt.MaxSpeciesID; species++)
        {
            var personalInfo = pt.GetFormEntry(species, 0);
            if (personalInfo is null || !personalInfo.IsPresentInGame)
            {
                continue;
            }

            if (personalInfo.EggGroup1 == 15 || personalInfo.EggGroup2 == 15)
            {
                continue;
            }

            byte formCount = personalInfo.FormCount;
            for (byte form = 0; form < formCount; form++)
            {
                var formInfo = pt.GetFormEntry(species, form);
                if (formInfo is null || !formInfo.IsPresentInGame)
                {
                    continue;
                }

                if (formInfo.EggGroup1 == 15 || formInfo.EggGroup2 == 15)
                {
                    continue;
                }

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
                    false
                );
            }
        }
    }

    private static void ProcessStaticEncounters(Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings,
        StreamWriter errorLogger)
    {
        ProcessStaticEncounterArray(Encounters8b.Encounter_BDSP, "Both", encounterData, gameStrings, errorLogger);
        ProcessStaticEncounterArray(Encounters8b.StaticBD, "Brilliant Diamond", encounterData, gameStrings, errorLogger);
        ProcessStaticEncounterArray(Encounters8b.StaticSP, "Shining Pearl", encounterData, gameStrings, errorLogger);
    }

    private static void ProcessStaticEncounterArray(EncounterStatic8b[] encounters, string versionName,
        Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings, StreamWriter errorLogger)
    {
        foreach (var encounter in encounters)
        {
            var speciesName = gameStrings.specieslist[encounter.Species];
            if (string.IsNullOrEmpty(speciesName))
            {
                errorLogger.WriteLine($"[{DateTime.Now}] Empty species name for index {encounter.Species}. Skipping.");
                continue;
            }

            var locationName = gameStrings.GetLocationName(false, encounter.Location, 8, 8, GameVersion.BDSP)
                ?? $"Unknown Location {encounter.Location}";

            string fixedBall = encounter.FixedBall != Ball.None ? encounter.FixedBall.ToString() : string.Empty;

            AddEncounterInfoWithEvolutions(
                encounterData, gameStrings, errorLogger,
                encounter.Species, encounter.Form, locationName,
                encounter.Location, encounter.Level, encounter.Level, "Static",
                encounter.Shiny == Shiny.Never, encounter.FixedBall != Ball.None,
                fixedBall, versionName, false);
        }
    }

    private static void AddEncounterInfoWithEvolutions(Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings,
        StreamWriter errorLogger, ushort speciesIndex, byte form, string locationName, ushort locationId, byte minLevel, byte maxLevel,
        string encounterType, bool isShinyLocked, bool isGift, string fixedBall, string version, bool isUnderground)
    {
        var pt = PersonalTable.BDSP;
        var personalInfo = pt.GetFormEntry(speciesIndex, form);

        if (personalInfo is null || !personalInfo.IsPresentInGame)
        {
            errorLogger.WriteLine($"[{DateTime.Now}] Species {speciesIndex} form {form} not present in BDSP. Skipping.");
            return;
        }

        AddSingleEncounterInfo(
            encounterData, gameStrings, errorLogger,
            speciesIndex, form, locationName, locationId,
            minLevel, maxLevel, minLevel, encounterType,
            isShinyLocked, isGift, fixedBall,
            version, isUnderground);

        var processedForms = new HashSet<(ushort Species, byte Form)> { (speciesIndex, form) };

        ProcessEvolutionLine(
            encounterData, gameStrings, errorLogger,
            speciesIndex, form, locationName, locationId,
            maxLevel, maxLevel, minLevel, encounterType,
            isShinyLocked, isGift, fixedBall,
            version, isUnderground, pt, processedForms);
    }

    private static void ProcessEvolutionLine(Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings,
        StreamWriter errorLogger, ushort baseSpecies, byte form, string locationName, ushort locationId,
        byte baseLevel, byte maxLevel, byte metLevel, string encounterType, bool isShinyLocked, bool isGift, string fixedBall,
        string version, bool isUnderground, PersonalTable8BDSP pt, HashSet<(ushort Species, byte Form)> processedForms)
    {
        if (pt.GetFormEntry(baseSpecies, form)?.IsPresentInGame != true)
        {
            return;
        }

        var nextEvolutions = GetImmediateEvolutions(baseSpecies, form, pt, processedForms);
        foreach (var (evoSpecies, evoForm) in nextEvolutions)
        {
            if (!processedForms.Add((evoSpecies, evoForm)))
            {
                continue;
            }

            var evoInfo = pt.GetFormEntry(evoSpecies, evoForm);
            if (evoInfo is null || !evoInfo.IsPresentInGame)
            {
                continue;
            }

            int evolutionMinLevel = GetMinEvolutionLevel(baseSpecies, evoSpecies);
            int minLevel = Math.Max(baseLevel, evolutionMinLevel);

            AddSingleEncounterInfo(
                encounterData, gameStrings, errorLogger,
                evoSpecies, evoForm, locationName, locationId,
                (byte)minLevel, (byte)maxLevel, metLevel, $"{encounterType} (Evolved)",
                isShinyLocked, isGift, fixedBall,
                version, isUnderground);

            ProcessEvolutionLine(
                encounterData, gameStrings, errorLogger,
                evoSpecies, evoForm, locationName, locationId,
                (byte)minLevel, (byte)maxLevel, metLevel, encounterType,
                isShinyLocked, isGift, fixedBall,
                version, isUnderground, pt, processedForms);
        }
    }

    private static List<(ushort Species, byte Form)> GetImmediateEvolutions(
        ushort species,
        byte form,
        PersonalTable8BDSP pt,
        HashSet<(ushort Species, byte Form)> processedForms)
    {
        var results = new List<(ushort Species, byte Form)>();

        var tree = EvolutionTree.GetEvolutionTree(EntityContext.Gen8b);
        var evos = tree.Forward.GetForward(species, form);

        foreach (var evo in evos.Span)
        {
            ushort evoSpecies = (ushort)evo.Species;
            byte evoForm = (byte)evo.Form;

            if (processedForms.Contains((evoSpecies, evoForm)))
            {
                continue;
            }

            var personalInfo = pt.GetFormEntry(evoSpecies, evoForm);
            if (personalInfo is null || !personalInfo.IsPresentInGame)
            {
                continue;
            }

            results.Add((evoSpecies, evoForm));
        }

        return results;
    }

    private static int GetMinEvolutionLevel(ushort baseSpecies, ushort evolvedSpecies)
    {
        var tree = EvolutionTree.GetEvolutionTree(EntityContext.Gen8b);
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

    private static void AddSingleEncounterInfo(Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings,
        StreamWriter errorLogger, ushort speciesIndex, byte form, string locationName, ushort locationId, byte minLevel, byte maxLevel,
        byte metLevel, string encounterType, bool isShinyLocked, bool isGift, string fixedBall, string version, bool isUnderground)
    {
        string dexNumber = form > 0 ? $"{speciesIndex}-{form}" : speciesIndex.ToString();

        var speciesName = gameStrings.specieslist[speciesIndex];
        if (string.IsNullOrEmpty(speciesName))
        {
            errorLogger.WriteLine($"[{DateTime.Now}] Empty species name for index {speciesIndex}. Skipping.");
            return;
        }

        var personalInfo = PersonalTable.BDSP.GetFormEntry(speciesIndex, form);
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
            e.IsUnderground == isUnderground &&
            e.Gender == genderRatio);

        if (existingEncounter is not null)
        {
            existingEncounter.MinLevel = Math.Min(existingEncounter.MinLevel, minLevel);
            existingEncounter.MaxLevel = Math.Max(existingEncounter.MaxLevel, maxLevel);
            existingEncounter.MetLevel = Math.Min(existingEncounter.MetLevel, metLevel);

            string existingVersion = existingEncounter.Version ?? string.Empty;
            string newVersion = version ?? string.Empty;
            existingEncounter.Version = CombineVersions(existingVersion, newVersion);

            errorLogger.WriteLine($"[{DateTime.Now}] Updated existing encounter: {speciesName} " +
                $"(Dex: {dexNumber}) at {locationName} (ID: {locationId}), Levels {existingEncounter.MinLevel}-{existingEncounter.MaxLevel}, " +
                $"Met Level: {existingEncounter.MetLevel}, Type: {encounterType}, Version: {existingEncounter.Version}, Gender: {genderRatio}");
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
                IsUnderground = isUnderground,
                IsShinyLocked = isShinyLocked,
                IsGift = isGift,
                FixedBall = fixedBall,
                Version = version,
                Gender = genderRatio
            });

            errorLogger.WriteLine($"[{DateTime.Now}] Processed new encounter: {speciesName} " +
                $"(Dex: {dexNumber}) at {locationName} (ID: {locationId}), Levels {minLevel}-{maxLevel}, " +
                $"Met Level: {metLevel}, Type: {encounterType}, Version: {version}, Gender: {genderRatio}");
        }
    }

    private static string CombineVersions(string version1, string version2)
    {
        if (version1 == "Both" || version2 == "Both")
        {
            return "Both";
        }

        if ((version1 == "Brilliant Diamond" && version2 == "Shining Pearl") ||
            (version1 == "Shining Pearl" && version2 == "Brilliant Diamond"))
        {
            return "Both";
        }

        if (version1 == "BD" && version2 == "SP" || version1 == "SP" && version2 == "BD")
        {
            return "Both";
        }

        return version1;
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
        public required ushort SpeciesIndex { get; set; }
        public required byte Form { get; set; }
        public required string LocationName { get; set; }
        public required ushort LocationId { get; set; }
        public required byte MinLevel { get; set; }
        public required byte MaxLevel { get; set; }
        public required byte MetLevel { get; set; }
        public required string EncounterType { get; set; }
        public required bool IsUnderground { get; set; }
        public required bool IsShinyLocked { get; set; }
        public required bool IsGift { get; set; }
        public required string FixedBall { get; set; }
        public required string Version { get; set; }
        public required string Gender { get; set; }
    }
}

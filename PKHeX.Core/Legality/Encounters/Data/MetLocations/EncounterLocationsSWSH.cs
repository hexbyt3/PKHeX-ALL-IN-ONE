using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace PKHeX.Core.Legality.Encounters.Data.MetLocations;

public static class EncounterLocationsSWSH
{
    private const ushort MaxLair = 244;

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

    private static void ProcessEncounterSlots(EncounterArea8[] areas, Dictionary<string, List<EncounterInfo>> encounterData,
        GameStrings gameStrings, StreamWriter errorLogger, string slotType)
    {
        foreach (var area in areas)
        {
            var locationName = gameStrings.GetLocationName(false, (ushort)area.Location, 8, 8, GameVersion.SWSH)
                ?? $"Unknown Location {area.Location}";

            foreach (var slot in area.Slots)
            {
                bool canGigantamax = Gigantamax.CanToggle(slot.Species, slot.Form);

                AddEncounterInfoWithEvolutions(encounterData, gameStrings, errorLogger, slot.Species, slot.Form,
                    locationName, area.Location, slot.LevelMin, slot.LevelMax, $"Wild {slotType}",
                    false, false, string.Empty, "Both", canGigantamax);
            }
        }
    }

    private static void ProcessEggMetLocations(Dictionary<string, List<EncounterInfo>> encounterData,
        GameStrings gameStrings, StreamWriter errorLogger)
    {
        const int eggMetLocationId = 60002;
        const string locationName = "a Nursery Worker";

        errorLogger.WriteLine($"[{DateTime.Now}] Processing egg met locations with location ID: {eggMetLocationId} ({locationName})");

        var pt = PersonalTable.SWSH;

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
                    canGigantamax
                );
            }
        }
    }

    private static void ProcessStaticEncounters(EncounterStatic8[] encounters, string versionName,
        Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings, StreamWriter errorLogger)
    {
        foreach (var encounter in encounters)
        {
            var locationName = gameStrings.GetLocationName(false, (ushort)encounter.Location, 8, 8, GameVersion.SWSH)
                ?? $"Unknown Location {encounter.Location}";

            bool canGigantamax = Gigantamax.CanToggle(encounter.Species, encounter.Form) || encounter.CanGigantamax;
            string fixedBall = encounter.FixedBall != Ball.None ? encounter.FixedBall.ToString() : string.Empty;

            AddEncounterInfoWithEvolutions(
                encounterData, gameStrings, errorLogger, encounter.Species, encounter.Form,
                locationName, encounter.Location, encounter.Level, encounter.Level, "Static",
                encounter.Shiny == Shiny.Never, encounter.Gift, fixedBall, versionName, canGigantamax);
        }
    }

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

    private static void ProcessNestEncounters(EncounterStatic8N[] encounters, string versionName,
        Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings,
        StreamWriter errorLogger, int locationId, string locationName)
    {
        foreach (var encounter in encounters)
        {
            bool canGigantamax = Gigantamax.CanToggle(encounter.Species, encounter.Form) || encounter.CanGigantamax;

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
                canGigantamax
            );
        }
    }

    private static void ProcessDistributionEncounters(EncounterStatic8ND[] encounters, string versionName,
        Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings,
        StreamWriter errorLogger, int locationId, string locationName)
    {
        foreach (var encounter in encounters)
        {
            bool canGigantamax = Gigantamax.CanToggle(encounter.Species, encounter.Form) || encounter.CanGigantamax;

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
                canGigantamax
            );
        }
    }

    private static void ProcessCrystalEncounters(EncounterStatic8NC[] encounters,
        Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings,
        StreamWriter errorLogger, int locationId, string locationName)
    {
        foreach (var encounter in encounters)
        {
            string versionName = encounter.Version switch
            {
                GameVersion.SW => "Sword",
                GameVersion.SH => "Shield",
                _ => "Both"
            };

            bool canGigantamax = Gigantamax.CanToggle(encounter.Species, encounter.Form) || encounter.CanGigantamax;

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
                canGigantamax
            );
        }
    }

    private static void ProcessMaxLairEncounters(Dictionary<string, List<EncounterInfo>> encounterData,
        GameStrings gameStrings, StreamWriter errorLogger)
    {
        var locationName = gameStrings.GetLocationName(false, MaxLair, 8, 8, GameVersion.SWSH)
            ?? $"Unknown Location {MaxLair}";

        foreach (var encounter in Encounters8Nest.DynAdv_SWSH)
        {
            bool canGigantamax = Gigantamax.CanToggle(encounter.Species, encounter.Form) || encounter.CanGigantamax;

            AddEncounterInfoWithEvolutions(
                encounterData, gameStrings, errorLogger, encounter.Species, encounter.Form,
                locationName, MaxLair, encounter.Level, encounter.Level, "Max Lair",
                encounter.Shiny == Shiny.Never, false, string.Empty, "Both", canGigantamax);
        }
    }

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
        bool canGigantamax = false)
    {
        var pt = PersonalTable.SWSH;
        var personalInfo = pt.GetFormEntry(speciesIndex, form);
        if (personalInfo is null || !personalInfo.IsPresentInGame)
        {
            errorLogger.WriteLine($"[{DateTime.Now}] Species {speciesIndex} form {form} not present in SWSH. Skipping.");
            return;
        }

        AddSingleEncounterInfo(encounterData, gameStrings, errorLogger, speciesIndex, form, locationName, locationId,
            minLevel, maxLevel, minLevel, encounterType, isShinyLocked, isGift, fixedBall, encounterVersion, canGigantamax);

        var processedForms = new HashSet<(ushort Species, byte Form)> { (speciesIndex, form) };

        ProcessEvolutionLine(encounterData, gameStrings, pt, errorLogger, speciesIndex, form, locationName, locationId,
            minLevel, maxLevel, minLevel, encounterType, isShinyLocked, isGift, fixedBall, encounterVersion, canGigantamax, processedForms);
    }

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
        HashSet<(ushort Species, byte Form)> processedForms)
    {
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

            bool evoCanGigantamax = baseCanGigantamax || Gigantamax.CanToggle(evoSpecies, evoForm);

            AddSingleEncounterInfo(
                encounterData, gameStrings, errorLogger, evoSpecies, evoForm, locationName, locationId,
                minLevel, 100, metLevel, $"{encounterType} (Evolved)",
                isShinyLocked, isGift, fixedBall, encounterVersion, evoCanGigantamax);

            ProcessEvolutionLine(
                encounterData, gameStrings, pt, errorLogger, evoSpecies, evoForm, locationName, locationId,
                minLevel, 100, metLevel, encounterType, isShinyLocked, isGift, fixedBall, encounterVersion,
                evoCanGigantamax, processedForms);
        }
    }

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

    private static int GetMinEvolutionLevel(ushort baseSpecies, ushort evolvedSpecies)
    {
        var tree = EvolutionTree.GetEvolutionTree(EntityContext.Gen8);
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
        bool canGigantamax)
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

            string existingVersion = existingEncounter.EncounterVersion ?? string.Empty;
            string newVersion = encounterVersion ?? string.Empty;
            existingEncounter.EncounterVersion = CombineVersions(existingVersion, newVersion);

            errorLogger.WriteLine($"[{DateTime.Now}] Updated existing encounter: {speciesName} " +
                $"(Dex: {dexNumber}) at {locationName} (ID: {locationId}), Levels {existingEncounter.MinLevel}-{existingEncounter.MaxLevel}, " +
                $"Met Level: {existingEncounter.MetLevel}, Type: {encounterType}, Version: {existingEncounter.EncounterVersion}, " +
                $"Can Gigantamax: {canGigantamax}, Gender: {genderRatio}");
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
                CanGigantamax = canGigantamax,
                Gender = genderRatio
            });

            errorLogger.WriteLine($"[{DateTime.Now}] Processed new encounter: {speciesName} " +
                $"(Dex: {dexNumber}) at {locationName} (ID: {locationId}), Levels {minLevel}-{maxLevel}, " +
                $"Met Level: {metLevel}, Type: {encounterType}, Version: {encounterVersion}, " +
                $"Can Gigantamax: {canGigantamax}, Gender: {genderRatio}");
        }
    }

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
        public required bool CanGigantamax { get; set; }
        public required string Gender { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace PKHeX.Core.Legality.Encounters.Data.MetLocations;
public static class EncounterLocationsLA
{
    public static void GenerateEncounterDataJSON(string outputPath, string errorLogPath)
    {
        ArgumentNullException.ThrowIfNull(outputPath);
        ArgumentNullException.ThrowIfNull(errorLogPath);

        try
        {
            using var errorLogger = new StreamWriter(errorLogPath, false, Encoding.UTF8);
            errorLogger.WriteLine($"[{DateTime.Now}] Starting JSON generation process for encounters in Legends Arceus.");

            var gameStrings = GameInfo.GetStrings("en");
            errorLogger.WriteLine($"[{DateTime.Now}] Game strings loaded.");

            var encounterData = new Dictionary<string, List<EncounterInfo>>();

            ProcessEncounterSlots(Encounters8a.SlotsLA, encounterData, gameStrings, errorLogger);
            ProcessStaticEncounters(Encounters8a.StaticLA, encounterData, gameStrings, errorLogger);

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

    private static void ProcessEncounterSlots(EncounterArea8a[] areas, Dictionary<string, List<EncounterInfo>> encounterData,
        GameStrings gameStrings, StreamWriter errorLogger)
    {
        ArgumentNullException.ThrowIfNull(areas);
        ArgumentNullException.ThrowIfNull(encounterData);
        ArgumentNullException.ThrowIfNull(gameStrings);
        ArgumentNullException.ThrowIfNull(errorLogger);

        foreach (var area in areas)
        {
            foreach (var slot in area.Slots)
            {
                AddEncounterInfoWithEvolutions(slot, area.Location, area.Type.ToString(), encounterData, gameStrings, errorLogger);
                AddAlternateFormEncounters(slot, area.Location, area.Type.ToString(), encounterData, gameStrings, errorLogger);
            }
        }
    }

    private static void ProcessStaticEncounters(EncounterStatic8a[] encounters, Dictionary<string, List<EncounterInfo>> encounterData,
        GameStrings gameStrings, StreamWriter errorLogger)
    {
        ArgumentNullException.ThrowIfNull(encounters);
        ArgumentNullException.ThrowIfNull(encounterData);
        ArgumentNullException.ThrowIfNull(gameStrings);
        ArgumentNullException.ThrowIfNull(errorLogger);

        foreach (var encounter in encounters)
        {
            AddEncounterInfoWithEvolutions(encounter, encounter.Location, "Static", encounterData, gameStrings, errorLogger);
            AddAlternateFormEncounters(encounter, encounter.Location, "Static", encounterData, gameStrings, errorLogger);
        }
    }

    private static void AddAlternateFormEncounters(ISpeciesForm baseEncounter, ushort locationId, string encounterType,
        Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings, StreamWriter errorLogger)
    {
        ArgumentNullException.ThrowIfNull(baseEncounter);
        ArgumentNullException.ThrowIfNull(encounterType);
        ArgumentNullException.ThrowIfNull(encounterData);
        ArgumentNullException.ThrowIfNull(gameStrings);
        ArgumentNullException.ThrowIfNull(errorLogger);

        var species = baseEncounter.Species;
        var originalForm = baseEncounter.Form;
        var pt = PersonalTable.LA;

        var baseFormInfo = pt.GetFormEntry(species, 0);
        if (baseFormInfo is null)
            return;

        var formCount = baseFormInfo.FormCount;
        if (formCount <= 1)
            return;

        bool isFormChangeable = FormInfo.IsFormChangeable(species, originalForm, 0, EntityContext.Gen8a, EntityContext.Gen8a);
        if (!isFormChangeable)
            return;

        for (byte form = 0; form < formCount; form++)
        {
            if (form == originalForm)
                continue;

            var formEntry = pt.GetFormEntry(species, form);
            if (formEntry is null || !formEntry.IsPresentInGame)
                continue;

            var formVariantEncounter = CreateFormVariantEncounter(baseEncounter, form);
            errorLogger.WriteLine($"[{DateTime.Now}] Adding alternate form: Species {species}-{form} based on original form {originalForm}");

            int metLevel = formVariantEncounter switch
            {
                EncounterSlot8a slot => slot.LevelMin,
                EncounterStatic8a static8a => static8a.LevelMin,
                _ => 1
            };

            string formVariantType = $"{encounterType} (Form Variant)";
            AddSingleEncounterInfo(formVariantEncounter, locationId,
                gameStrings.GetLocationName(false, (byte)(locationId & 0xFF), 8, 8, GameVersion.PLA),
                formVariantType, encounterData, gameStrings, errorLogger, metLevel);

            var personalInfo = pt.GetFormEntry(species, form);
            if (personalInfo is null || !personalInfo.IsPresentInGame)
                continue;

            var processedForms = new HashSet<(ushort Species, byte Form)> { (species, form) };

            ProcessEvolutionLine(formVariantEncounter, locationId,
                gameStrings.GetLocationName(false, (byte)(locationId & 0xFF), 8, 8, GameVersion.PLA),
                formVariantType, encounterData, gameStrings, errorLogger,
                species, form, pt, processedForms, metLevel);
        }
    }

    private static ISpeciesForm CreateFormVariantEncounter(ISpeciesForm baseEncounter, byte newForm)
    {
        ArgumentNullException.ThrowIfNull(baseEncounter);

        return baseEncounter switch
        {
            EncounterSlot8a slot => new EncounterSlot8a(
                slot.Parent,
                slot.Species,
                newForm,
                slot.LevelMin,
                slot.LevelMax,
                slot.AlphaType,
                slot.FlawlessIVCount,
                slot.Gender),

            EncounterStatic8a static8a => new EncounterStatic8a(
                static8a.Species,
                newForm,
                static8a.LevelMin,
                static8a.HeightScalar,
                static8a.WeightScalar)
            {
                Location = static8a.Location,
                LevelMax = static8a.LevelMax,
                Gender = static8a.Gender,
                Shiny = static8a.Shiny,
                FlawlessIVCount = static8a.FlawlessIVCount,
                IsAlpha = static8a.IsAlpha,
                FixedBall = static8a.FixedBall,
                FatefulEncounter = static8a.FatefulEncounter,
                Moves = static8a.Moves,
                Method = static8a.Method
            },

            _ => baseEncounter
        };
    }

    private static void AddEncounterInfoWithEvolutions(ISpeciesForm encounter, ushort locationId, string encounterType,
        Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings, StreamWriter errorLogger)
    {
        ArgumentNullException.ThrowIfNull(encounter);
        ArgumentNullException.ThrowIfNull(encounterType);
        ArgumentNullException.ThrowIfNull(encounterData);
        ArgumentNullException.ThrowIfNull(gameStrings);
        ArgumentNullException.ThrowIfNull(errorLogger);

        var speciesIndex = encounter.Species;
        var form = encounter.Form;
        var pt = PersonalTable.LA;
        var personalInfo = pt.GetFormEntry(speciesIndex, form);

        if (personalInfo is null || !personalInfo.IsPresentInGame)
        {
            errorLogger.WriteLine($"[{DateTime.Now}] Species {speciesIndex} form {form} not present in LA. Skipping.");
            return;
        }

        var locationName = gameStrings.GetLocationName(false, (byte)(locationId & 0xFF), 8, 8, GameVersion.PLA);
        if (string.IsNullOrEmpty(locationName))
        {
            errorLogger.WriteLine($"[{DateTime.Now}] Unknown location ID: {locationId} for species {gameStrings.specieslist[speciesIndex]} " +
                $"(Index: {speciesIndex}, Form: {form}). Skipping this encounter.");
            return;
        }

        int metLevel = encounter switch
        {
            EncounterSlot8a slot => slot.LevelMin,
            EncounterStatic8a static8a => static8a.LevelMin,
            _ => 1
        };

        AddSingleEncounterInfo(encounter, locationId, locationName, encounterType, encounterData, gameStrings, errorLogger, metLevel);

        var processedForms = new HashSet<(ushort Species, byte Form)> { (speciesIndex, form) };

        ProcessEvolutionLine(encounter, locationId, locationName, encounterType, encounterData, gameStrings, errorLogger,
            speciesIndex, form, pt, processedForms, metLevel);
    }

    private static void ProcessEvolutionLine(ISpeciesForm baseEncounter, ushort locationId, string locationName, string encounterType,
        Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings, StreamWriter errorLogger,
        ushort species, byte form, PersonalTable8LA pt, HashSet<(ushort Species, byte Form)> processedForms, int metLevel)
    {
        ArgumentNullException.ThrowIfNull(baseEncounter);
        ArgumentNullException.ThrowIfNull(locationName);
        ArgumentNullException.ThrowIfNull(encounterType);
        ArgumentNullException.ThrowIfNull(encounterData);
        ArgumentNullException.ThrowIfNull(gameStrings);
        ArgumentNullException.ThrowIfNull(errorLogger);
        ArgumentNullException.ThrowIfNull(pt);
        ArgumentNullException.ThrowIfNull(processedForms);

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

            int baseLevel = baseEncounter switch
            {
                EncounterSlot8a slot => slot.LevelMin,
                EncounterStatic8a static8a => static8a.LevelMin,
                _ => 1
            };

            var evolutionMinLevel = GetMinEvolutionLevel(species, evoSpecies);
            var minLevel = Math.Max(baseLevel, evolutionMinLevel);

            var evoEncounter = CreateEvolvedEncounter(baseEncounter, evoSpecies, evoForm, minLevel);

            string evolvedEncounterType = $"{encounterType} (Evolved)";
            AddSingleEncounterInfo(evoEncounter, locationId, locationName, evolvedEncounterType, encounterData, gameStrings, errorLogger, metLevel);

            ProcessEvolutionLine(evoEncounter, locationId, locationName, encounterType, encounterData, gameStrings, errorLogger,
                evoSpecies, evoForm, pt, processedForms, metLevel);
        }
    }

    private static List<(ushort Species, byte Form)> GetImmediateEvolutions(
        ushort species,
        byte form,
        PersonalTable8LA pt,
        HashSet<(ushort Species, byte Form)> processedForms)
    {
        ArgumentNullException.ThrowIfNull(pt);
        ArgumentNullException.ThrowIfNull(processedForms);

        var results = new List<(ushort Species, byte Form)>();

        var tree = EvolutionTree.GetEvolutionTree(EntityContext.Gen8a);
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
        var tree = EvolutionTree.GetEvolutionTree(EntityContext.Gen8a);
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

    private static ISpeciesForm CreateEvolvedEncounter(ISpeciesForm baseEncounter, ushort evoSpecies, byte evoForm, int minLevel)
    {
        ArgumentNullException.ThrowIfNull(baseEncounter);

        return baseEncounter switch
        {
            EncounterSlot8a slot => new EncounterSlot8a(
                slot.Parent,
                evoSpecies,
                evoForm,
                (byte)minLevel,
                (byte)minLevel,
                slot.AlphaType,
                slot.FlawlessIVCount,
                slot.Gender),

            EncounterStatic8a static8a => new EncounterStatic8a(
                evoSpecies,
                evoForm,
                (byte)minLevel,
                static8a.HeightScalar,
                static8a.WeightScalar)
            {
                LevelMax = (byte)minLevel,
                Location = static8a.Location,
                Shiny = static8a.Shiny,
                Gender = static8a.Gender,
                IsAlpha = static8a.IsAlpha,
                FixedBall = static8a.FixedBall,
                FatefulEncounter = static8a.FatefulEncounter,
                FlawlessIVCount = static8a.FlawlessIVCount,
                Moves = static8a.Moves,
                Method = static8a.Method
            },

            _ => baseEncounter
        };
    }

    private static void AddSingleEncounterInfo(ISpeciesForm encounter, ushort locationId, string locationName, string encounterType,
        Dictionary<string, List<EncounterInfo>> encounterData, GameStrings gameStrings, StreamWriter errorLogger, int metLevel)
    {
        ArgumentNullException.ThrowIfNull(encounter);
        ArgumentNullException.ThrowIfNull(locationName);
        ArgumentNullException.ThrowIfNull(encounterType);
        ArgumentNullException.ThrowIfNull(encounterData);
        ArgumentNullException.ThrowIfNull(gameStrings);
        ArgumentNullException.ThrowIfNull(errorLogger);

        string dexNumber = encounter.Form > 0 ? $"{encounter.Species}-{encounter.Form}" : encounter.Species.ToString();

        var speciesName = gameStrings.specieslist[encounter.Species];
        if (string.IsNullOrEmpty(speciesName))
        {
            errorLogger.WriteLine($"[{DateTime.Now}] Empty species name for index {encounter.Species}. Skipping.");
            return;
        }

        var personalInfo = PersonalTable.LA.GetFormEntry(encounter.Species, encounter.Form);
        if (personalInfo is null)
        {
            errorLogger.WriteLine($"[{DateTime.Now}] Personal info not found for species {encounter.Species} form {encounter.Form}. Skipping.");
            return;
        }

        string genderRatio = DetermineGenderRatio(personalInfo);
        bool isAlpha = encounter is EncounterSlot8a slotCheck ? slotCheck.IsAlpha :
                     encounter is EncounterStatic8a staticCheck && staticCheck.IsAlpha;

        if (!encounterData.TryGetValue(dexNumber, out var encounterList))
        {
            encounterList = [];
            encounterData[dexNumber] = encounterList;
        }

        var existingEncounter = encounterList.FirstOrDefault(e =>
            e.LocationId == locationId &&
            e.SpeciesIndex == encounter.Species &&
            e.Form == encounter.Form &&
            e.EncounterType == encounterType &&
            e.IsAlpha == isAlpha &&
            e.Gender == genderRatio);

        if (existingEncounter is not null)
        {
            switch (encounter)
            {
                case EncounterSlot8a slotUpdate:
                    existingEncounter.MinLevel = Math.Min(existingEncounter.MinLevel, slotUpdate.LevelMin);
                    existingEncounter.MaxLevel = Math.Max(existingEncounter.MaxLevel, slotUpdate.LevelMax);
                    existingEncounter.MetLevel = Math.Min(existingEncounter.MetLevel, metLevel);
                    break;

                case EncounterStatic8a staticUpdate:
                    existingEncounter.MinLevel = Math.Min(existingEncounter.MinLevel, staticUpdate.LevelMin);
                    existingEncounter.MaxLevel = Math.Max(existingEncounter.MaxLevel, staticUpdate.LevelMax);
                    existingEncounter.MetLevel = Math.Min(existingEncounter.MetLevel, metLevel);
                    break;
            }

            errorLogger.WriteLine($"[{DateTime.Now}] Updated existing encounter: {existingEncounter.SpeciesName} " +
                $"(Dex: {dexNumber}) at {locationName} (ID: {locationId}), Levels {existingEncounter.MinLevel}-{existingEncounter.MaxLevel}, " +
                $"Met Level: {existingEncounter.MetLevel}");
        }
        else
        {
            var info = new EncounterInfo
            {
                SpeciesName = speciesName,
                SpeciesIndex = encounter.Species,
                Form = encounter.Form,
                LocationName = locationName,
                LocationId = locationId,
                EncounterType = encounterType,
                Gender = genderRatio,
                IsAlpha = isAlpha,
                MinLevel = 0,
                MaxLevel = 0,
                MetLevel = metLevel,
                FlawlessIVCount = 0,
                IsShinyLocked = false,
                FixedBall = string.Empty,
                FatefulEncounter = false
            };

            switch (encounter)
            {
                case EncounterSlot8a slotEncounter:
                    info.MinLevel = slotEncounter.LevelMin;
                    info.MaxLevel = slotEncounter.LevelMax;
                    info.FlawlessIVCount = slotEncounter.FlawlessIVCount;
                    info.IsShinyLocked = slotEncounter.Shiny == Shiny.Never;
                    break;

                case EncounterStatic8a staticEncounter:
                    info.MinLevel = staticEncounter.LevelMin;
                    info.MaxLevel = staticEncounter.LevelMax;
                    info.FlawlessIVCount = staticEncounter.FlawlessIVCount;
                    info.IsShinyLocked = staticEncounter.Shiny == Shiny.Never;
                    info.FixedBall = staticEncounter.FixedBall.ToString();
                    info.FatefulEncounter = staticEncounter.FatefulEncounter;
                    break;
            }

            encounterList.Add(info);
            errorLogger.WriteLine($"[{DateTime.Now}] Processed new encounter: {info.SpeciesName} " +
                $"(Dex: {dexNumber}) at {locationName} (ID: {locationId}), Levels {info.MinLevel}-{info.MaxLevel}, " +
                $"Met Level: {info.MetLevel}, Type: {encounterType}, Gender: {info.Gender}, IsShinyLocked: {info.IsShinyLocked}, Form: {info.Form}");
        }
    }

    private static string DetermineGenderRatio(IPersonalInfo personalInfo)
    {
        ArgumentNullException.ThrowIfNull(personalInfo);

        return personalInfo switch
        {
            { Genderless: true } => "Genderless",
            { OnlyFemale: true } => "Female",
            { OnlyMale: true } => "Male",
            { Gender: 0 } => "Male",
            { Gender: 254 } => "Female",
            { Gender: 255 } => "Genderless",
            _ => "Male, Female"
        };
    }

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
        public required bool IsAlpha { get; set; }
        public required string Gender { get; set; }
        public required int FlawlessIVCount { get; set; }
        public required bool IsShinyLocked { get; set; }
        public required string FixedBall { get; set; }
        public required bool FatefulEncounter { get; set; }
    }
}


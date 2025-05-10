using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace PKHeX.Core.Legality.Encounters.Data.MetLocations;

/// <summary>
/// Generates encounter location data for Pokémon Legends: Arceus as JSON.
/// </summary>
public static class EncounterLocationsLA
{
    /// <summary>
    /// Generates JSON file with encounter data for Pokémon Legends: Arceus.
    /// </summary>
    /// <param name="outputPath">Path to save the generated JSON file</param>
    /// <param name="errorLogPath">Path to save error logs</param>
    /// <exception cref="ArgumentNullException">Thrown when parameters are null</exception>
    /// <exception cref="Exception">Thrown when an error occurs during generation</exception>
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

    /// <summary>
    /// Processes encounter slots and adds them to the encounter data dictionary.
    /// </summary>
    /// <param name="areas">Areas containing encounter slots</param>
    /// <param name="encounterData">Dictionary to store encounter information</param>
    /// <param name="gameStrings">Game strings for localization</param>
    /// <param name="errorLogger">Stream writer for logging errors</param>
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

    /// <summary>
    /// Processes static encounters and adds them to the encounter data dictionary.
    /// </summary>
    /// <param name="encounters">Static encounters to process</param>
    /// <param name="encounterData">Dictionary to store encounter information</param>
    /// <param name="gameStrings">Game strings for localization</param>
    /// <param name="errorLogger">Stream writer for logging errors</param>
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

    /// <summary>
    /// Adds alternate form encounters for species with multiple forms.
    /// </summary>
    /// <param name="baseEncounter">Base encounter that might have alternate forms</param>
    /// <param name="locationId">Location ID of the encounter</param>
    /// <param name="encounterType">Type of the encounter (e.g., "Static", "Grass")</param>
    /// <param name="encounterData">Dictionary to store encounter information</param>
    /// <param name="gameStrings">Game strings for localization</param>
    /// <param name="errorLogger">Stream writer for logging errors</param>
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

    /// <summary>
    /// Creates a form variant encounter based on the base encounter with a new form.
    /// </summary>
    /// <param name="baseEncounter">Base encounter to derive from</param>
    /// <param name="newForm">New form number to apply</param>
    /// <returns>New ISpeciesForm with the alternate form</returns>
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

    /// <summary>
    /// Adds encounter information and follows the evolution line to add evolved forms.
    /// </summary>
    /// <param name="encounter">Encounter to process</param>
    /// <param name="locationId">Location ID of the encounter</param>
    /// <param name="encounterType">Type of the encounter (e.g., "Static", "Grass")</param>
    /// <param name="encounterData">Dictionary to store encounter information</param>
    /// <param name="gameStrings">Game strings for localization</param>
    /// <param name="errorLogger">Stream writer for logging errors</param>
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

    /// <summary>
    /// Processes the evolution line of a species to add evolved forms.
    /// </summary>
    /// <param name="baseEncounter">Base encounter to derive evolutions from</param>
    /// <param name="locationId">Location ID of the encounter</param>
    /// <param name="locationName">Location name of the encounter</param>
    /// <param name="encounterType">Type of the encounter (e.g., "Static", "Grass")</param>
    /// <param name="encounterData">Dictionary to store encounter information</param>
    /// <param name="gameStrings">Game strings for localization</param>
    /// <param name="errorLogger">Stream writer for logging errors</param>
    /// <param name="species">Species ID of the Pokémon</param>
    /// <param name="form">Form number of the Pokémon</param>
    /// <param name="pt">Personal table for Legends Arceus</param>
    /// <param name="processedForms">Set of already processed species/form pairs</param>
    /// <param name="metLevel">Original met level for the encounter</param>
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

            // Get the minimum level required for evolution with correct form parameters
            var evolutionMinLevel = GetMinEvolutionLevel(species, form, evoSpecies, evoForm);
            // The minimum level for the evolved form is the maximum of the base level and the evolution level
            var minLevel = Math.Max(baseLevel, evolutionMinLevel);

            var evoEncounter = CreateEvolvedEncounter(baseEncounter, evoSpecies, evoForm, minLevel);

            string evolvedEncounterType = $"{encounterType} (Evolved)";
            AddSingleEncounterInfo(evoEncounter, locationId, locationName, evolvedEncounterType, encounterData, gameStrings, errorLogger, metLevel);

            ProcessEvolutionLine(evoEncounter, locationId, locationName, encounterType, encounterData, gameStrings, errorLogger,
                evoSpecies, evoForm, pt, processedForms, metLevel);
        }
    }

    /// <summary>
    /// Gets immediate evolutions for a species and form.
    /// </summary>
    /// <param name="species">Species ID</param>
    /// <param name="form">Form number</param>
    /// <param name="pt">Personal table for Legends Arceus</param>
    /// <param name="processedForms">Set of already processed species/form pairs</param>
    /// <returns>List of species and form pairs for immediate evolutions</returns>
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

    /// <summary>
    /// Gets the minimum level required for evolution.
    /// </summary>
    /// <param name="baseSpecies">Base species ID</param>
    /// <param name="baseForm">Base form number</param>
    /// <param name="evolvedSpecies">Evolved species ID</param>
    /// <param name="evolvedForm">Evolved form number</param>
    /// <returns>Minimum level required for evolution</returns>
    private static int GetMinEvolutionLevel(ushort baseSpecies, byte baseForm, ushort evolvedSpecies, byte evolvedForm)
    {
        var tree = EvolutionTree.GetEvolutionTree(EntityContext.Gen8a);
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
    /// Gets the evolution level from an evolution method.
    /// </summary>
    /// <param name="evo">Evolution method</param>
    /// <returns>Level required for evolution</returns>
    private static int GetEvolutionLevel(EvolutionMethod evo)
    {
        if (evo.Level > 0)
            return evo.Level;
        if (evo.Method == EvolutionType.LevelUp && evo.Argument > 0)
            return evo.Argument;
        return 0;
    }

    /// <summary>
    /// Creates an evolved encounter from a base encounter.
    /// </summary>
    /// <param name="baseEncounter">Base encounter to derive from</param>
    /// <param name="evoSpecies">Evolved species ID</param>
    /// <param name="evoForm">Evolved form number</param>
    /// <param name="minLevel">Minimum level for the evolved form</param>
    /// <returns>New ISpeciesForm with the evolved species</returns>
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

    // Legends Arceus Ribbon and Mark definitions
    private static readonly string[] LAValidRibbons = ["Hisui"];

    // Previous generation ribbons that can be present
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
        "ChampionAlola", "BattleRoyale", "BattleTreeGreat", "BattleTreeMaster",
        "ChampionGalar", "TowerMaster", "MasterRank"
    ];

    /// <summary>
    /// Adds a single encounter to the encounter data dictionary.
    /// </summary>
    /// <param name="encounter">Encounter to add</param>
    /// <param name="locationId">Location ID of the encounter</param>
    /// <param name="locationName">Location name of the encounter</param>
    /// <param name="encounterType">Type of the encounter (e.g., "Static", "Grass")</param>
    /// <param name="encounterData">Dictionary to store encounter information</param>
    /// <param name="gameStrings">Game strings for localization</param>
    /// <param name="errorLogger">Stream writer for logging errors</param>
    /// <param name="metLevel">Original met level for the encounter</param>
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

                    // Update FlawlessIVCount if the new value is higher
                    if (slotUpdate.FlawlessIVCount > existingEncounter.FlawlessIVCount)
                    {
                        existingEncounter.FlawlessIVCount = slotUpdate.FlawlessIVCount;
                    }
                    break;

                case EncounterStatic8a staticUpdate:
                    existingEncounter.MinLevel = Math.Min(existingEncounter.MinLevel, staticUpdate.LevelMin);
                    existingEncounter.MaxLevel = Math.Max(existingEncounter.MaxLevel, staticUpdate.LevelMax);
                    existingEncounter.MetLevel = Math.Min(existingEncounter.MetLevel, metLevel);

                    // Update FlawlessIVCount if the new value is higher
                    if (staticUpdate.FlawlessIVCount > existingEncounter.FlawlessIVCount)
                    {
                        existingEncounter.FlawlessIVCount = staticUpdate.FlawlessIVCount;
                    }
                    break;
            }

            errorLogger.WriteLine($"[{DateTime.Now}] Updated existing encounter: {existingEncounter.SpeciesName} " +
                $"(Dex: {dexNumber}) at {locationName} (ID: {locationId}), Levels {existingEncounter.MinLevel}-{existingEncounter.MaxLevel}, " +
                $"Met Level: {existingEncounter.MetLevel}, FlawlessIVCount: {existingEncounter.FlawlessIVCount}");
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

            // Set marks and ribbons for this encounter
            SetEncounterMarksAndRibbons(info, errorLogger);

            encounterList.Add(info);
            errorLogger.WriteLine($"[{DateTime.Now}] Processed new encounter: {info.SpeciesName} " +
                $"(Dex: {dexNumber}) at {locationName} (ID: {locationId}), Levels {info.MinLevel}-{info.MaxLevel}, " +
                $"Met Level: {info.MetLevel}, Type: {encounterType}, Gender: {info.Gender}, IsShinyLocked: {info.IsShinyLocked}, " +
                $"Form: {info.Form}, FlawlessIVCount: {info.FlawlessIVCount}, IsAlpha: {info.IsAlpha}, " +
                $"Required Marks: {string.Join(", ", info.RequiredMarks)}, " +
                $"Possible Marks: {string.Join(", ", info.PossibleMarks)}");
        }
    }

    /// <summary>
    /// Sets marks and ribbons for a specific encounter.
    /// </summary>
    /// <param name="encounter">The encounter to set marks and ribbons for</param>
    /// <param name="errorLogger">Logger for recording processing information</param>
    private static void SetEncounterMarksAndRibbons(EncounterInfo encounter, StreamWriter errorLogger)
    {
        var requiredMarks = new List<string>();
        var possibleMarks = new List<string>();
        var validRibbons = new List<string>();

        // Add Alpha mark for Alpha encounters
        if (encounter.IsAlpha)
        {
            requiredMarks.Add("MarkAlpha");
        }

        // Add valid ribbons for LA
        validRibbons.AddRange(LAValidRibbons);
        validRibbons.AddRange(PreviousGenRibbons);

        encounter.RequiredMarks = [.. requiredMarks];
        encounter.PossibleMarks = [.. possibleMarks.Except(requiredMarks).ToArray()];
        encounter.ValidRibbons = [.. validRibbons];

        errorLogger.WriteLine($"[{DateTime.Now}] Mark/Ribbon analysis for {encounter.SpeciesName}: " +
            $"Required Marks: {string.Join(", ", requiredMarks)}, " +
            $"Possible Marks: {string.Join(", ", possibleMarks)}");
    }

    /// <summary>
    /// Determines the gender ratio description for a Pokémon.
    /// </summary>
    /// <param name="personalInfo">Personal info of the Pokémon</param>
    /// <returns>String describing the gender ratio</returns>
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

    /// <summary>
    /// Contains information about a Pokémon encounter for JSON output.
    /// </summary>
    private sealed class EncounterInfo
    {
        /// <summary>
        /// Name of the Pokémon species.
        /// </summary>
        public required string SpeciesName { get; set; }

        /// <summary>
        /// Pokédex index of the species.
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
        /// ID of the location where the Pokémon can be encountered.
        /// </summary>
        public required int LocationId { get; set; }

        /// <summary>
        /// Minimum level of the encounter.
        /// </summary>
        public required int MinLevel { get; set; }

        /// <summary>
        /// Maximum level of the encounter.
        /// </summary>
        public required int MaxLevel { get; set; }

        /// <summary>
        /// Level that will be recorded as the met level.
        /// </summary>
        public required int MetLevel { get; set; }

        /// <summary>
        /// Type of encounter (e.g., "Static", "Grass").
        /// </summary>
        public required string EncounterType { get; set; }

        /// <summary>
        /// Whether the encountered Pokémon is an Alpha.
        /// </summary>
        public required bool IsAlpha { get; set; }

        /// <summary>
        /// Gender ratio description for the Pokémon.
        /// </summary>
        public required string Gender { get; set; }

        /// <summary>
        /// Number of guaranteed perfect IVs (31) for the encounter.
        /// </summary>
        public required int FlawlessIVCount { get; set; }

        /// <summary>
        /// Whether the encounter is shiny-locked.
        /// </summary>
        public required bool IsShinyLocked { get; set; }

        /// <summary>
        /// If the encounter requires a specific ball type, this will contain its name.
        /// </summary>
        public required string FixedBall { get; set; }

        /// <summary>
        /// Whether the encounter has the fateful encounter flag.
        /// </summary>
        public required bool FatefulEncounter { get; set; }

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
    }
}

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

            foreach (var slot in area.Slots)
            {
                bool canGigantamax = Gigantamax.CanToggle(slot.Species, slot.Form);

                AddEncounterInfoWithEvolutions(encounterData, gameStrings, errorLogger, slot.Species, slot.Form,
                    locationName, area.Location, slot.LevelMin, slot.LevelMax, $"Wild {slotType}",
                    false, false, string.Empty, "Both", canGigantamax, 0, string.Empty);
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
                    canGigantamax,
                    0,
                    string.Empty
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
            if (encounter.IVs != null && IsIVsSpecified(encounter.IVs))
            {
                setIVs = FormatIVs(encounter.IVs);
            }

            AddEncounterInfoWithEvolutions(
                encounterData, gameStrings, errorLogger, encounter.Species, encounter.Form,
                locationName, encounter.Location, encounter.Level, encounter.Level, "Static",
                encounter.Shiny == Shiny.Never, encounter.Gift, fixedBall, versionName, canGigantamax,
                flawlessIVCount, setIVs);
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
                string.Empty
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
                string.Empty
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
                string.Empty
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

        foreach (var encounter in Encounters8Nest.DynAdv_SWSH)
        {
            bool canGigantamax = Gigantamax.CanToggle(encounter.Species, encounter.Form) || encounter.CanGigantamax;
            int flawlessIVCount = encounter.FlawlessIVCount;

            AddEncounterInfoWithEvolutions(
                encounterData, gameStrings, errorLogger, encounter.Species, encounter.Form,
                locationName, MaxLair, encounter.Level, encounter.Level, "Max Lair",
                encounter.Shiny == Shiny.Never, false, string.Empty, "Both", canGigantamax,
                flawlessIVCount, string.Empty);
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
        string setIVs = "")
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
            canGigantamax, flawlessIVCount, setIVs);

        var processedForms = new HashSet<(ushort Species, byte Form)> { (speciesIndex, form) };

        ProcessEvolutionLine(encounterData, gameStrings, pt, errorLogger, speciesIndex, form, locationName, locationId,
            minLevel, maxLevel, minLevel, encounterType, isShinyLocked, isGift, fixedBall, encounterVersion,
            canGigantamax, flawlessIVCount, setIVs, processedForms);
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

            // Get the minimum level required for evolution with correct form parameters
            var evolutionMinLevel = GetMinEvolutionLevel(species, form, evoSpecies, evoForm);
            // The minimum level for the evolved form is the maximum of the base level and the evolution level
            var minLevel = Math.Max(baseLevel, evolutionMinLevel);

            bool evoCanGigantamax = baseCanGigantamax || Gigantamax.CanToggle(evoSpecies, evoForm);

            AddSingleEncounterInfo(
                encounterData, gameStrings, errorLogger, evoSpecies, evoForm, locationName, locationId,
                minLevel, Math.Max(minLevel, maxLevel), metLevel, $"{encounterType} (Evolved)",
                isShinyLocked, isGift, fixedBall, encounterVersion, evoCanGigantamax, flawlessIVCount, setIVs);

            ProcessEvolutionLine(
                encounterData, gameStrings, pt, errorLogger, evoSpecies, evoForm, locationName, locationId,
                minLevel, Math.Max(minLevel, maxLevel), metLevel, encounterType, isShinyLocked, isGift, fixedBall,
                encounterVersion, evoCanGigantamax, flawlessIVCount, setIVs, processedForms);
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
        string setIVs)
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
                Gender = genderRatio,
                FlawlessIVCount = flawlessIVCount,
                SetIVs = setIVs
            });

            errorLogger.WriteLine($"[{DateTime.Now}] Processed new encounter: {speciesName} " +
                $"(Dex: {dexNumber}) at {locationName} (ID: {locationId}), Levels {minLevel}-{maxLevel}, " +
                $"Met Level: {metLevel}, Type: {encounterType}, Version: {encounterVersion}, " +
                $"Can Gigantamax: {canGigantamax}, Gender: {genderRatio}, " +
                $"IVs: {(flawlessIVCount > 0 ? $"{flawlessIVCount} perfect IVs" : setIVs)}");
        }
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
    }
}

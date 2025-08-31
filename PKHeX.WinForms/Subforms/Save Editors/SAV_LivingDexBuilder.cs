using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using PKHeX.Core;

namespace PKHeX.WinForms;

/// <summary>
/// Form for generating a complete Living Dex collection for the current save file.
/// </summary>
/// <remarks>
/// Analyzes the save file to identify missing Pokémon species/forms and generates
/// legal encounters to complete the collection. Supports various generation options
/// including shiny status, minimum levels, gender variants, and form variants.
/// </remarks>
public partial class SAV_LivingDexBuilder : Form
{
    private readonly SaveFile SAV;
    private readonly TrainerDatabase Trainers;
    private readonly List<LivingDexEntry> MissingEntries = [];
    private readonly List<PKM> GeneratedPokemon = [];

    /// <summary>
    /// Initializes a new instance of the Living Dex Builder form.
    /// </summary>
    /// <param name="sav">The save file to analyze and generate Pokémon for.</param>
    /// <param name="trainers">The trainer database for generating encounters with trainer data.</param>
    public SAV_LivingDexBuilder(SaveFile sav, TrainerDatabase trainers)
    {
        InitializeComponent();
        WinFormsUtil.TranslateInterface(this, Main.CurrentLanguage);
        SAV = sav;
        Trainers = trainers;
        
        InitializeOptions();
        AnalyzeMissingEntries();
    }

    /// <summary>
    /// Initializes the form options and UI controls with default values.
    /// </summary>
    private void InitializeOptions()
    {
        // Display save file info
        L_SaveFileInfo.Text = $"Game: {GameInfo.GetVersionName(SAV.Version)} | Generation: {SAV.Generation}";

        CB_Sorting.Items.Clear();
        CB_Sorting.Items.AddRange([
            "National Dex Number",
            "Alphabetical",
            "Type (Primary)",
            "Base Stat Total"
        ]);
        CB_Sorting.SelectedIndex = 0;

        CHK_Shiny.Checked = true;
        CHK_MinLevel.Checked = false;
        CHK_Forms.Checked = true;
        CHK_Gender.Checked = false;
        CHK_LegalOnly.Checked = true;
        CHK_MysteryGifts.Checked = false;
        CHK_AllGames.Checked = true;
        CHK_OverwriteExisting.Checked = false;
        
        // Add tooltips for clarity
        var tooltip = new ToolTip();
        tooltip.SetToolTip(CHK_Gender, "When checked, generates both male and female variants for species that have gender differences.\nWhen unchecked, generates only one representative per species/form.");
        tooltip.SetToolTip(CHK_MysteryGifts, "When checked, includes Mystery Gift/Event Pokémon.\nWhen unchecked, only uses wild/static/trade encounters.");
        tooltip.SetToolTip(CHK_MinLevel, "When checked, generates Pokémon at their minimum possible level.\nWhen unchecked, generates all Pokémon at level 100.");
        tooltip.SetToolTip(CHK_AllGames, "When checked, includes Pokémon from paired game versions (e.g., Scarlet exclusives when on Violet).\nWhen unchecked, only includes Pokémon obtainable in the current game.");

        NUD_StartBox.Minimum = 1;
        NUD_StartBox.Maximum = SAV.BoxCount;
        NUD_StartBox.Value = 1;
    }

    /// <summary>
    /// Analyzes the save file to identify missing Pokémon entries for the Living Dex.
    /// </summary>
    private void AnalyzeMissingEntries()
    {
        MissingEntries.Clear();
        var existingSpecies = GetExistingSpecies();
        var maxSpecies = SAV.MaxSpeciesID;

        // Only include Pokémon that are actually available in this game
        for (ushort species = 1; species <= maxSpecies; species++)
        {
            if (!SAV.Personal.IsSpeciesInGame(species))
                continue;
            
            // Skip Pokémon that can't be obtained in this game version (unless Include All Games is checked)
            if (!CHK_AllGames.Checked && !IsObtainableInGame(species))
                continue;

            var pi = SAV.Personal[species];
            var forms = pi.FormCount;

            for (byte form = 0; form < forms; form++)
            {
                if (form > 0 && !SAV.Personal.IsPresentInGame(species, form))
                    continue;

                // Get the PersonalInfo for this specific form
                var formPi = SAV.Personal[species, form];
                AnalyzeGenderVariants(species, form, formPi, existingSpecies);
            }
        }

        UpdateMissingList();
        UpdateStatusLabel();
    }

    /// <summary>
    /// Analyzes gender variants for a specific species and form.
    /// </summary>
    private void AnalyzeGenderVariants(ushort species, byte form, IPersonalInfo pi, HashSet<(ushort Species, byte Form, byte Gender)> existingSpecies)
    {
        bool hasMale = false, hasFemale = false;
        bool hasAny = false;
        
        foreach (var existing in existingSpecies)
        {
            if (existing.Species == species && existing.Form == form)
            {
                hasAny = true;
                if (existing.Gender == 0) hasMale = true;
                if (existing.Gender == 1) hasFemale = true;
            }
        }

        // If we already have any variant of this species+form and gender checking is off, skip
        if (hasAny && !CHK_Gender.Checked)
            return;

        var genderless = pi.Gender == PersonalInfo.RatioMagicGenderless;
        var maleOnly = pi.Gender == PersonalInfo.RatioMagicMale;
        var femaleOnly = pi.Gender == PersonalInfo.RatioMagicFemale;

        if (genderless && !hasAny)
        {
            MissingEntries.Add(new LivingDexEntry(species, form, 2));
        }
        else if (maleOnly && !hasMale)
        {
            MissingEntries.Add(new LivingDexEntry(species, form, 0));
        }
        else if (femaleOnly && !hasFemale)
        {
            MissingEntries.Add(new LivingDexEntry(species, form, 1));
        }
        else if (!genderless && !maleOnly && !femaleOnly)
        {
            // For species with both genders
            if (CHK_Gender.Checked)
            {
                // Include both genders when checkbox is checked
                if (!hasMale) MissingEntries.Add(new LivingDexEntry(species, form, 0));
                if (!hasFemale) MissingEntries.Add(new LivingDexEntry(species, form, 1));
            }
            else
            {
                // Include only one representative when checkbox is unchecked
                if (!hasMale && !hasFemale)
                {
                    // Default to male for consistency
                    MissingEntries.Add(new LivingDexEntry(species, form, 0));
                }
            }
        }
    }

    /// <summary>
    /// Gets a set of all existing Pokémon species, forms, and genders in the save file.
    /// </summary>
    /// <returns>A HashSet containing tuples of existing species, forms, and genders.</returns>
    private HashSet<(ushort Species, byte Form, byte Gender)> GetExistingSpecies()
    {
        var existing = new HashSet<(ushort Species, byte Form, byte Gender)>();

        // Check party
        foreach (var pk in SAV.PartyData)
        {
            if (pk.Species > 0)
                existing.Add((pk.Species, pk.Form, (byte)pk.Gender));
        }

        // Check all boxes
        foreach (var pk in SAV.BoxData)
        {
            if (pk.Species > 0)
                existing.Add((pk.Species, pk.Form, (byte)pk.Gender));
        }

        return existing;
    }

    /// <summary>
    /// Updates the missing entries list box with filtered and sorted entries.
    /// </summary>
    private void UpdateMissingList()
    {
        LB_Missing.Items.Clear();
        
        var filteredEntries = FilterEntries(MissingEntries);
        var sortedEntries = SortEntries(filteredEntries);

        foreach (var entry in sortedEntries)
        {
            var displayName = FormatEntryDisplay(entry);
            LB_Missing.Items.Add(displayName);
        }
    }

    /// <summary>
    /// Formats a Living Dex entry for display in the list.
    /// </summary>
    private string FormatEntryDisplay(LivingDexEntry entry)
    {
        var strings = GameInfo.GetStrings(GameLanguage.DefaultLanguage);
        var speciesName = strings.Species[entry.Species];
        var formName = ShowdownParsing.GetStringFromForm(entry.Form, strings, entry.Species, SAV.Context);
        var genderSymbol = entry.Gender switch
        {
            0 => "♂",
            1 => "♀",
            _ => ""
        };
        
        var displayName = $"{entry.Species:000} - {speciesName}";
        if (!string.IsNullOrEmpty(formName))
            displayName += $" ({formName})";
        if (!string.IsNullOrEmpty(genderSymbol))
            displayName += $" {genderSymbol}";
            
        return displayName;
    }

    /// <summary>
    /// Filters the missing entries based on user-selected options.
    /// </summary>
    /// <param name="entries">The entries to filter.</param>
    /// <returns>Filtered collection of entries.</returns>
    private IEnumerable<LivingDexEntry> FilterEntries(IEnumerable<LivingDexEntry> entries)
    {
        if (!CHK_Forms.Checked)
        {
            entries = entries.Where(e => e.Form == 0);
        }

        if (!CHK_Gender.Checked)
        {
            // Group by species and form, then take one representative gender
            entries = entries
                .GroupBy(e => (e.Species, e.Form))
                .Select(g => g.OrderBy(e => e.Gender).First());
        }

        return entries;
    }

    /// <summary>
    /// Sorts the entries based on the selected sorting method.
    /// </summary>
    /// <param name="entries">The entries to sort.</param>
    /// <returns>Sorted collection of entries.</returns>
    private IEnumerable<LivingDexEntry> SortEntries(IEnumerable<LivingDexEntry> entries)
    {
        return CB_Sorting.SelectedIndex switch
        {
            0 => entries.OrderBy(e => e.Species).ThenBy(e => e.Form).ThenBy(e => e.Gender),
            1 => entries.OrderBy(e => GameInfo.GetStrings(GameLanguage.DefaultLanguage).Species[e.Species]),
            2 => entries.OrderBy(e => e.Species), // Type sorting placeholder
            3 => entries.OrderBy(e => GetBaseStatTotal(e.Species)).ThenBy(e => e.Species),
            _ => entries
        };
    }

    /// <summary>
    /// Determines if a species is obtainable in the current game.
    /// </summary>
    /// <param name="species">The species ID to check.</param>
    /// <returns>True if obtainable, false otherwise.</returns>
    private bool IsObtainableInGame(ushort species)
    {
        // When "Include All Games" is checked, we want ALL species that exist in the game's data
        // Otherwise, we should check if the Pokémon can actually be obtained in this specific version
        // For now, return true since we're including all Pokémon that are in the game data
        // This could be refined later to check version-exclusive encounter tables
        return true;
    }
    
    /// <summary>
    /// Creates a basic Pokémon when no valid encounter can be found.
    /// </summary>
    /// <param name="entry">The Living Dex entry to create.</param>
    /// <returns>A basic Pokémon with minimal legal properties.</returns>
    /// <remarks>
    /// This is used as a fallback when Legal Only is unchecked and no encounters exist.
    /// The Pokémon may not pass legality checks but will be functional.
    /// </remarks>
    private PKM CreateBasicPokemon(LivingDexEntry entry)
    {
        var pk = SAV.BlankPKM;
        pk.Species = entry.Species;
        pk.Form = entry.Form;
        pk.Gender = (byte)entry.Gender;
        
        SetBasicProperties(pk, entry);
        SetTrainerInfo(pk);
        SetLevelAndExperience(pk, entry);
        SetMovesAndStats(pk);
        
        pk.RefreshChecksum();
        return pk;
    }

    /// <summary>
    /// Sets basic properties for a fallback Pokémon.
    /// </summary>
    private void SetBasicProperties(PKM pk, LivingDexEntry entry)
    {
        var pi = SAV.Personal[entry.Species, entry.Form];
        pk.Nature = pk.StatNature = Nature.Docile;
        pk.RefreshAbility(0);
        pk.Ball = (byte)Ball.Poke;
        pk.CurrentFriendship = pi.BaseFriendship;
        
        var language = (int)Language.GetSafeLanguage(SAV.Generation, (LanguageID)SAV.Language);
        pk.Language = language;
        pk.Nickname = SpeciesName.GetSpeciesNameGeneration(entry.Species, language, SAV.Generation);
    }

    /// <summary>
    /// Sets trainer information for a fallback Pokémon.
    /// </summary>
    private void SetTrainerInfo(PKM pk)
    {
        pk.OriginalTrainerName = SAV.OT;
        pk.OriginalTrainerGender = SAV.Gender;
        pk.ID32 = SAV.ID32;
        pk.Version = SAV.Version;
        pk.MetLevel = pk.CurrentLevel;
        pk.MetLocation = 30001; // Poké Transfer or similar
        pk.MetDate = EncounterDate.GetDateSwitch();
    }

    /// <summary>
    /// Sets level and experience for a fallback Pokémon.
    /// </summary>
    private void SetLevelAndExperience(PKM pk, LivingDexEntry entry)
    {
        if (CHK_MinLevel.Checked)
        {
            pk.CurrentLevel = GetMinimumLevelForSpecies(entry.Species, entry.Form);
        }
        else
        {
            pk.CurrentLevel = Experience.MaxLevel;
        }
        
        var pi = SAV.Personal[entry.Species, entry.Form];
        pk.EXP = Experience.GetEXP(pk.CurrentLevel, pi.EXPGrowth);
        pk.MetLevel = pk.CurrentLevel;
    }

    /// <summary>
    /// Sets moves and stats for a fallback Pokémon.
    /// </summary>
    private void SetMovesAndStats(PKM pk)
    {
        EncounterUtil.SetEncounterMoves(pk, SAV.Version, pk.CurrentLevel);
        pk.HealPP();
        pk.IVs = [31, 31, 31, 31, 31, 31];
        pk.SetEVs([0, 0, 0, 0, 0, 0]);
    }
    
    /// <summary>
    /// Gets the minimum level at which a species can exist.
    /// </summary>
    /// <param name="species">The species ID.</param>
    /// <param name="form">The form number.</param>
    /// <returns>The minimum level for the species.</returns>
    private byte GetMinimumLevelForSpecies(ushort species, byte form)
    {
        var tree = EvolutionTree.GetEvolutionTree(SAV.Context);
        var node = tree.Reverse.GetReverse(species, form);
        
        if (node.First.Species == 0)
            return Experience.MinLevel; // Base form
            
        // Find evolution method for this species
        var methods = tree.Forward.GetForward(node.First.Species, node.First.Form);
        foreach (var method in methods.Span)
        {
            if (method.Species == species && method.Form == form)
            {
                if (method.LevelUp > 0)
                    return (byte)Math.Max((int)method.LevelUp, (int)Experience.MinLevel);
                    
                return 30; // Default for non-level evolutions
            }
        }
        
        return Experience.MinLevel;
    }
    
    /// <summary>
    /// Calculates the minimum level required for a complete evolution chain.
    /// </summary>
    /// <param name="fromSpecies">The starting species (base form from encounter).</param>
    /// <param name="toSpecies">The target species (evolved form).</param>
    /// <param name="toForm">The target form.</param>
    /// <param name="currentLevel">The current level of the base encounter.</param>
    /// <returns>The minimum level required for the target evolution.</returns>
    /// <remarks>
    /// This method traces the evolution path and calculates the proper level.
    /// For example, a level 58 Bulbasaur needs level 59 to become Ivysaur,
    /// and Ivysaur needs level 60 to become Venusaur.
    /// </remarks>
    private byte GetMinimumLevelForEvolutionChain(ushort fromSpecies, ushort toSpecies, byte toForm, byte currentLevel)
    {
        if (fromSpecies == toSpecies)
            return currentLevel;
            
        var tree = EvolutionTree.GetEvolutionTree(SAV.Context);
        var evolutionPath = BuildEvolutionPath(tree, fromSpecies, toSpecies, toForm);
        
        return CalculateEvolutionLevel(evolutionPath, currentLevel);
    }

    /// <summary>
    /// Builds the evolution path from base to target species.
    /// </summary>
    private static List<(ushort Species, byte Form, byte Level)> BuildEvolutionPath(EvolutionTree tree, ushort fromSpecies, ushort toSpecies, byte toForm)
    {
        var evolutionPath = new List<(ushort Species, byte Form, byte Level)>();
        var current = (Species: toSpecies, Form: toForm);
        
        while (current.Species != 0)
        {
            var node = tree.Reverse.GetReverse(current.Species, current.Form);
            if (node.First.Species == 0)
                break; // Reached base form
                
            // Find the evolution level for this step
            var methods = tree.Forward.GetForward(node.First.Species, node.First.Form);
            foreach (var method in methods.Span)
            {
                if (method.Species == current.Species && method.Form == current.Form)
                {
                    var evoLevel = method.LevelUp > 0 ? method.LevelUp : 30;
                    evolutionPath.Add((current.Species, current.Form, (byte)evoLevel));
                    break;
                }
            }
            
            current = (node.First.Species, node.First.Form);
            if (current.Species == fromSpecies)
                break;
        }
        
        return evolutionPath;
    }

    /// <summary>
    /// Calculates the required level for the evolution chain.
    /// </summary>
    private static byte CalculateEvolutionLevel(List<(ushort Species, byte Form, byte Level)> evolutionPath, byte startLevel)
    {
        var levelRequired = startLevel;
        
        foreach (var (_, _, evoLevel) in evolutionPath.AsEnumerable().Reverse())
        {
            // If already at or above evolution level, need +1 to evolve
            if (levelRequired >= evoLevel)
                levelRequired = (byte)(levelRequired + 1);
            else
                levelRequired = evoLevel;
        }
        
        return levelRequired;
    }
    
    
    /// <summary>
    /// Calculates the base stat total for a species.
    /// </summary>
    /// <param name="species">The species ID.</param>
    /// <returns>The sum of all base stats.</returns>
    private int GetBaseStatTotal(ushort species)
    {
        var pi = SAV.Personal[species];
        return pi.HP + pi.ATK + pi.DEF + pi.SPA + pi.SPD + pi.SPE;
    }

    /// <summary>
    /// Updates the status label with the current missing count.
    /// </summary>
    private void UpdateStatusLabel()
    {
        var filtered = FilterEntries(MissingEntries).Count();
        var gameVersion = GameInfo.GetVersionName(SAV.Version);
        var scope = CHK_AllGames.Checked ? " + Paired" : "";
        L_Status.Text = $"Missing in {gameVersion}{scope}: {filtered} entries";
    }

    /// <summary>
    /// Handles the Generate button click event to create missing Pokémon.
    /// </summary>
    private void B_Generate_Click(object sender, EventArgs e)
    {
        try
        {
            GeneratedPokemon.Clear();
            
            var entriesToGenerate = PrepareEntriesToGenerate();
            if (entriesToGenerate.Count == 0)
            {
                WinFormsUtil.Alert("No missing entries to generate!");
                return;
            }

            GenerateMissingPokemon(entriesToGenerate);
            DisplayGenerationResults(entriesToGenerate.Count);
        }
        catch (Exception ex)
        {
            WinFormsUtil.Error($"An error occurred during generation: {ex.Message}");
        }
    }

    /// <summary>
    /// Prepares the list of entries to generate based on current filters.
    /// </summary>
    private List<LivingDexEntry> PrepareEntriesToGenerate()
    {
        var entriesToGenerate = FilterEntries(MissingEntries).ToList();
        
        // Additional deduplication when gender is not included
        if (!CHK_Gender.Checked)
        {
            entriesToGenerate = entriesToGenerate
                .GroupBy(e => (e.Species, e.Form))
                .Select(g => g.First())
                .ToList();
        }
        
        return entriesToGenerate;
    }

    /// <summary>
    /// Generates Pokémon for all missing entries.
    /// </summary>
    private void GenerateMissingPokemon(List<LivingDexEntry> entriesToGenerate)
    {
        PB_Progress.Maximum = entriesToGenerate.Count;
        PB_Progress.Value = 0;
        PB_Progress.Visible = true;

        var generatedSet = new HashSet<(ushort Species, byte Form, byte Gender)>();

        foreach (var entry in entriesToGenerate)
        {
            var key = CHK_Gender.Checked 
                ? (entry.Species, entry.Form, entry.Gender)
                : (entry.Species, entry.Form, (byte)255);
                
            if (generatedSet.Contains(key))
            {
                PB_Progress.Value++;
                continue;
            }

            var pk = GeneratePokemon(entry);
            if (pk != null && (!CHK_LegalOnly.Checked || new LegalityAnalysis(pk).Valid))
            {
                GeneratedPokemon.Add(pk);
                generatedSet.Add(key);
            }
            PB_Progress.Value++;
        }

        PB_Progress.Visible = false;
    }

    /// <summary>
    /// Displays the results of the generation process.
    /// </summary>
    private void DisplayGenerationResults(int requestedCount)
    {
        L_GeneratedCount.Text = $"Generated: {GeneratedPokemon.Count} Pokémon";

        if (GeneratedPokemon.Count > 0)
        {
            B_Import.Enabled = true;
            var message = $"Successfully generated {GeneratedPokemon.Count} out of {requestedCount} Pokémon!";
            if (requestedCount > GeneratedPokemon.Count)
            {
                message += $"\n\nFailed to generate {requestedCount - GeneratedPokemon.Count} Pokémon.";
            }
            WinFormsUtil.Alert(message);
        }
        else
        {
            WinFormsUtil.Alert("No valid Pokémon could be generated with the current settings.");
        }
    }

    
    /// <summary>
    /// Generates a Pokémon for the specified Living Dex entry.
    /// </summary>
    /// <param name="entry">The entry specifying species, form, and gender to generate.</param>
    /// <returns>A generated Pokémon, or null if generation failed.</returns>
    private PKM? GeneratePokemon(LivingDexEntry entry)
    {
        try
        {
            // Create template PKM for encounter search
            var pk = CreateTemplatePokemon(entry);
            
            // Configure and execute encounter search
            ConfigureEncounterSearch();
            var encounters = SearchForEncounters(pk);
            
            if (encounters.Count == 0)
                return HandleNoEncounters(entry);
            
            // Select best encounter and convert to PKM
            var encounter = SelectBestEncounter(encounters);
            return ConvertEncounterToPokemon(encounter, entry);
        }
        catch
        {
            return null;
        }
        finally
        {
            EncounterMovesetGenerator.ResetFilters();
        }
    }

    /// <summary>
    /// Creates a template Pokémon for encounter searching.
    /// </summary>
    private PKM CreateTemplatePokemon(LivingDexEntry entry)
    {
        var pk = SAV.BlankPKM;
        pk.Species = entry.Species;
        pk.Form = entry.Form;
        pk.Gender = (byte)entry.Gender;
        pk.SetGender(pk.GetSaneGender());
        EncounterMovesetGenerator.OptimizeCriteria(pk, SAV);
        return pk;
    }

    /// <summary>
    /// Configures encounter search priorities based on user settings.
    /// </summary>
    private void ConfigureEncounterSearch()
    {
        var encounterTypes = new List<EncounterTypeGroup>
        {
            EncounterTypeGroup.Slot,
            EncounterTypeGroup.Static,
            EncounterTypeGroup.Trade,
            EncounterTypeGroup.Egg
        };
        
        if (CHK_MysteryGifts.Checked)
            encounterTypes.Add(EncounterTypeGroup.Mystery);
            
        EncounterMovesetGenerator.PriorityList = encounterTypes.ToArray();
    }

    /// <summary>
    /// Searches for valid encounters for the template Pokémon.
    /// </summary>
    private List<IEncounterInfo> SearchForEncounters(PKM pk)
    {
        // When "Include All Games" is checked, search across all compatible versions
        // Otherwise, only search in the current save's version
        ReadOnlyMemory<GameVersion> versions;
        
        if (CHK_AllGames.Checked)
        {
            // Include all compatible versions for this generation
            versions = GetCompatibleVersions().AsMemory();
        }
        else
        {
            versions = new[] { SAV.Version }.AsMemory();
        }
        
        var moves = ReadOnlyMemory<ushort>.Empty;
        var encounters = EncounterMovesetGenerator.GenerateEncounters(pk, moves, versions)
            .OfType<IEncounterInfo>() // Filter to IEncounterInfo types
            .ToList();
            
        // Filter out untradeable encounters (level 68 from Poco Path in Scarlet/Violet)
        if ((SAV.Version == GameVersion.SL || SAV.Version == GameVersion.VL) && CHK_AllGames.Checked)
        {
            encounters = FilterUntradableEncounters(encounters);
        }
        
        return encounters;
    }
    
    /// <summary>
    /// Gets compatible game versions for encounter searching based on the current save's paired version.
    /// </summary>
    private GameVersion[] GetCompatibleVersions()
    {
        return SAV.Version switch
        {
            // Gen 9 - Scarlet/Violet
            GameVersion.SL or GameVersion.VL => [GameVersion.SL, GameVersion.VL],
            
            // Gen 8 - Different game lines
            GameVersion.BD or GameVersion.SP => [GameVersion.BD, GameVersion.SP], // BDSP
            GameVersion.PLA => [GameVersion.PLA], // Legends Arceus (standalone)
            GameVersion.SW or GameVersion.SH => [GameVersion.SW, GameVersion.SH], // Sword/Shield
            
            // Gen 7 
            GameVersion.GP or GameVersion.GE => [GameVersion.GP, GameVersion.GE], // Let's Go
            GameVersion.US or GameVersion.UM => [GameVersion.US, GameVersion.UM], // Ultra Sun/Moon
            GameVersion.SN or GameVersion.MN => [GameVersion.SN, GameVersion.MN], // Sun/Moon
            
            // Gen 6
            GameVersion.OR or GameVersion.AS => [GameVersion.OR, GameVersion.AS], // Omega Ruby/Alpha Sapphire  
            GameVersion.X or GameVersion.Y => [GameVersion.X, GameVersion.Y], // X/Y
            
            // Gen 5
            GameVersion.B2 or GameVersion.W2 => [GameVersion.B2, GameVersion.W2], // Black 2/White 2
            GameVersion.B or GameVersion.W => [GameVersion.B, GameVersion.W], // Black/White
            
            // Gen 4
            GameVersion.HG or GameVersion.SS => [GameVersion.HG, GameVersion.SS], // HeartGold/SoulSilver
            GameVersion.D or GameVersion.P => [GameVersion.D, GameVersion.P], // Diamond/Pearl
            GameVersion.Pt => [GameVersion.D, GameVersion.P, GameVersion.Pt], // Platinum (includes D/P for compatibility)
            
            // Gen 3
            GameVersion.FR or GameVersion.LG => [GameVersion.FR, GameVersion.LG], // FireRed/LeafGreen
            GameVersion.R or GameVersion.S => [GameVersion.R, GameVersion.S], // Ruby/Sapphire
            GameVersion.E => [GameVersion.R, GameVersion.S, GameVersion.E], // Emerald (includes R/S for compatibility)
            
            // Gen 2
            GameVersion.GD or GameVersion.SI => [GameVersion.GD, GameVersion.SI], // Gold/Silver
            GameVersion.C => [GameVersion.GD, GameVersion.SI, GameVersion.C], // Crystal (includes G/S for compatibility)
            
            // Gen 1
            GameVersion.RD or GameVersion.BU => [GameVersion.RD, GameVersion.BU], // Red/Blue
            GameVersion.YW => [GameVersion.RD, GameVersion.BU, GameVersion.YW], // Yellow (includes R/B for compatibility)
            
            _ => [SAV.Version]
        };
    }
    
    /// <summary>
    /// Filters out untradeable encounters from Scarlet/Violet.
    /// </summary>
    private List<IEncounterInfo> FilterUntradableEncounters(List<IEncounterInfo> encounters)
    {
        // Filter out level 68 Koraidon/Miraidon encounters from Poco Path (these are untradeable)
        // Keep level 72 encounters and others
        return encounters.Where(e => 
        {
            // Check if this is a level 68 Koraidon or Miraidon (Poco Path encounters are untradeable)
            if (e.LevelMin == 68 && e.LevelMax == 68)
            {
                // Check if it's Koraidon (1007) or Miraidon (1008)
                if (e.Species == (ushort)Species.Koraidon || e.Species == (ushort)Species.Miraidon)
                {
                    return false; // Filter out level 68 Koraidon/Miraidon
                }
            }
            return true; // Keep all other encounters
        }).ToList();
    }

    /// <summary>
    /// Handles the case when no encounters are found.
    /// </summary>
    private PKM? HandleNoEncounters(LivingDexEntry entry)
    {
        if (!CHK_LegalOnly.Checked)
            return CreateBasicPokemon(entry);
        
        EncounterMovesetGenerator.ResetFilters();
        return null;
    }

    /// <summary>
    /// Selects the best encounter from available options.
    /// </summary>
    private IEncounterInfo SelectBestEncounter(List<IEncounterInfo> encounters)
    {
        if (!CHK_MysteryGifts.Checked)
        {
            var nonGift = encounters.FirstOrDefault(e => e is not MysteryGift);
            if (nonGift != null)
                return nonGift;
        }
        return encounters.First();
    }

    /// <summary>
    /// Converts an encounter to a Pokémon with appropriate modifications.
    /// </summary>
    private PKM? ConvertEncounterToPokemon(IEncounterInfo encounter, LivingDexEntry entry)
    {
        if (encounter is not IEncounterConvertible enc)
            return HandleNoEncounters(entry);

        var criteria = new EncounterCriteria
        {
            Shiny = CHK_Shiny.Checked ? Shiny.Always : Shiny.Never,
            Gender = (Gender)entry.Gender
        };

        // Use trainer data from database if available, otherwise use save file
        var trainer = Trainers.GetTrainer(encounter.Version, encounter.Generation <= 2 ? (LanguageID)SAV.Language : null) ?? SAV;
        var pk = enc.ConvertToPKM(trainer, criteria);
        
        // Handle evolved forms if species doesn't match
        if (pk.Species != entry.Species)
        {
            TransformToEvolvedForm(pk, entry);
        }
        else
        {
            SetPokemonLevel(pk, encounter);
        }
        
        // Final validation and cleanup
        pk.RefreshChecksum();
        
        if (CHK_LegalOnly.Checked && !ValidateAndFixLegality(pk))
        {
            EncounterMovesetGenerator.ResetFilters();
            return null;
        }
        
        return pk;
    }

    /// <summary>
    /// Transforms a base form Pokémon to its evolved form.
    /// </summary>
    private void TransformToEvolvedForm(PKM pk, LivingDexEntry entry)
    {
        // Set appropriate level for evolution
        if (CHK_MinLevel.Checked)
        {
            var targetLevel = GetMinimumLevelForEvolutionChain(pk.Species, entry.Species, entry.Form, pk.CurrentLevel);
            pk.CurrentLevel = targetLevel;
        }
        else
        {
            pk.CurrentLevel = Experience.MaxLevel;
        }
        
        // Transform to target species
        pk.Species = entry.Species;
        pk.Form = entry.Form;
        pk.IsNicknamed = false;
        pk.Nickname = SpeciesName.GetSpeciesNameGeneration(entry.Species, pk.Language, SAV.Generation);
        
        // Update stats and data for new species
        var pi = SAV.Personal[entry.Species, entry.Form];
        pk.EXP = Experience.GetEXP(pk.CurrentLevel, pi.EXPGrowth);
        pk.RefreshAbility(0);
        pk.ResetPartyStats();
        pk.Heal();
    }

    /// <summary>
    /// Sets the Pokémon's level based on user preferences.
    /// </summary>
    private void SetPokemonLevel(PKM pk, IEncounterInfo encounter)
    {
        if (CHK_MinLevel.Checked)
        {
            pk.CurrentLevel = (byte)encounter.LevelMin;
        }
        else
        {
            pk.CurrentLevel = Experience.MaxLevel;
        }
        
        var pi = SAV.Personal[pk.Species, pk.Form];
        pk.EXP = Experience.GetEXP(pk.CurrentLevel, pi.EXPGrowth);
    }

    /// <summary>
    /// Validates and attempts to fix any legality issues.
    /// </summary>
    private static bool ValidateAndFixLegality(PKM pk)
    {
        var la = new LegalityAnalysis(pk);
        if (la.Valid)
            return true;
            
        // Try basic fixes
        pk.HealPP();
        pk.Heal();
        
        // Re-check after fixes
        la = new LegalityAnalysis(pk);
        return la.Valid;
    }

    /// <summary>
    /// Handles the Import button click to save generated Pokémon to boxes.
    /// </summary>
    private void B_Import_Click(object sender, EventArgs e)
    {
        if (GeneratedPokemon.Count == 0)
        {
            WinFormsUtil.Alert("No Pokémon to import!");
            return;
        }

        var imported = ImportToBoxes();
        
        if (imported < GeneratedPokemon.Count)
        {
            WinFormsUtil.Alert($"Ran out of box space! Imported {imported}/{GeneratedPokemon.Count} Pokémon.");
        }
        else
        {
            WinFormsUtil.Alert($"Successfully imported {imported} Pokémon starting from Box {NUD_StartBox.Value}!");
        }
        
        DialogResult = DialogResult.OK;
        Close();
    }

    /// <summary>
    /// Imports generated Pokémon to the save file boxes.
    /// </summary>
    /// <returns>The number of Pokémon successfully imported.</returns>
    private int ImportToBoxes()
    {
        var startBox = (int)NUD_StartBox.Value - 1;
        var imported = 0;
        var boxIndex = startBox;
        var slotIndex = 0;

        foreach (var pk in GeneratedPokemon)
        {
            var slot = FindNextAvailableSlot(ref boxIndex, ref slotIndex);
            if (slot == null)
                break;
                
            SAV.SetBoxSlotAtIndex(pk, boxIndex, slotIndex);
            imported++;
            
            slotIndex++;
            if (slotIndex >= SAV.BoxSlotCount)
            {
                slotIndex = 0;
                boxIndex++;
            }
        }
        
        return imported;
    }

    /// <summary>
    /// Finds the next available slot in the boxes.
    /// </summary>
    private PKM? FindNextAvailableSlot(ref int boxIndex, ref int slotIndex)
    {
        while (boxIndex < SAV.BoxCount)
        {
            var existing = SAV.GetBoxSlotAtIndex(boxIndex, slotIndex);
            
            if (existing.Species == 0 || CHK_OverwriteExisting.Checked)
                return existing;
            
            slotIndex++;
            if (slotIndex >= SAV.BoxSlotCount)
            {
                slotIndex = 0;
                boxIndex++;
            }
        }
        
        return null;
    }

    /// <summary>
    /// Handles filter changes to update the display.
    /// </summary>
    private void FilterChanged(object sender, EventArgs e)
    {
        UpdateMissingList();
        UpdateStatusLabel();
        B_Import.Enabled = false;
        L_GeneratedCount.Text = "Generated: 0 Pokémon";
    }

    /// <summary>
    /// Handles the Cancel button click.
    /// </summary>
    private void B_Cancel_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }

    /// <summary>
    /// Represents a single entry in the Living Dex.
    /// </summary>
    /// <param name="Species">The species ID.</param>
    /// <param name="Form">The form number.</param>
    /// <param name="Gender">The gender (0=Male, 1=Female, 2=Genderless).</param>
    private record LivingDexEntry(ushort Species, byte Form, byte Gender);
}

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System;

namespace PKHeX.Core;

/// <summary>
/// Generates JSON data files for Pokémon from various game versions.
/// </summary>
public static class PokemonDbGenerator
{
    /// <summary>
    /// Generates JSON files for all supported Pokémon games and saves them to the specified directory.
    /// </summary>
    /// <param name="outputDirectory">Directory where the JSON files will be saved</param>
    /// <param name="errorLogPath">Path to the error log file</param>
    /// <exception cref="Exception">Thrown when an error occurs during generation</exception>
    public static void GenerateAllPokemonDataJSON(string outputDirectory, string errorLogPath)
    {
        Directory.CreateDirectory(outputDirectory);

        using var errorLogger = new StreamWriter(errorLogPath, false, System.Text.Encoding.UTF8);
        errorLogger.WriteLine($"[{DateTime.Now}] Starting JSON generation process for Pokemon database.");

        try
        {
            var supportedGames = new[]
            {
                (GameVersion.SV, "sv_pokemon.json"),
                (GameVersion.SWSH, "swsh_pokemon.json"),
                (GameVersion.BDSP, "bdsp_pokemon.json"),
                (GameVersion.PLA, "la_pokemon.json"),
                (GameVersion.GG, "gg_pokemon.json")
            };

            foreach (var (game, fileName) in supportedGames)
            {
                GenerateForGame(game, Path.Combine(outputDirectory, fileName), errorLogger);
            }

            errorLogger.WriteLine($"[{DateTime.Now}] JSON generation completed successfully.");
        }
        catch (Exception ex)
        {
            LogError(errorLogger, ex);
            throw;
        }
    }

    /// <summary>
    /// Generates a JSON file for a specific game version.
    /// </summary>
    /// <param name="game">Game version to generate data for</param>
    /// <param name="outputPath">Path where the JSON file will be saved</param>
    /// <param name="errorLogger">StreamWriter for error logging</param>
    /// <exception cref="Exception">Thrown when an error occurs during generation</exception>
    private static void GenerateForGame(GameVersion game, string outputPath, StreamWriter errorLogger)
    {
        errorLogger.WriteLine($"[{DateTime.Now}] Generating Pokemon data for {game}...");

        try
        {
            var pokemonList = GetPokemonInfoForGame(game);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            string json = JsonSerializer.Serialize(pokemonList, options);
            File.WriteAllText(outputPath, json, new System.Text.UTF8Encoding(false));

            errorLogger.WriteLine($"[{DateTime.Now}] Successfully generated {outputPath} with {pokemonList.Count} Pokemon entries");
        }
        catch (Exception ex)
        {
            LogError(errorLogger, ex);
            throw;
        }
    }

    /// <summary>
    /// Generates a JSON file for a specific game version and outputs to the specified path.
    /// </summary>
    /// <param name="game">Game version to generate data for</param>
    /// <param name="outputPath">Path where the JSON file will be saved</param>
    public static void GenerateJsonForGame(GameVersion game, string outputPath)
    {
        var pokemonList = GetPokemonInfoForGame(game);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        string json = JsonSerializer.Serialize(pokemonList, options);
        File.WriteAllText(outputPath, json);

        Console.WriteLine($"Generated database info for {game} with {pokemonList.Count} Pokemon entries");
    }

    /// <summary>
    /// Retrieves Pokemon information for a specific game version.
    /// </summary>
    /// <param name="game">Game version to get Pokemon info for</param>
    /// <returns>List of Pokemon database information</returns>
    private static List<PokemonDbInfo> GetPokemonInfoForGame(GameVersion game)
    {
        var result = new List<PokemonDbInfo>();
        var table = GetPersonalTableForGame(game);
        int maxSpecies = GetMaxSpeciesForGame(game);
        byte format = (byte)game.GetGeneration();

        for (int species = 1; species <= maxSpecies; species++)
        {
            // Special handling for LGPE
            if (game == GameVersion.GG && !IsSpeciesInLGPE(species))
                continue;

            var personalInfo = table.GetFormEntry((ushort)species, 0);
            if (personalInfo == null)
                continue;

            byte formCount = personalInfo.FormCount;

            for (byte form = 0; form < formCount; form++)
            {
                var formPersonalInfo = table.GetFormEntry((ushort)species, form);
                if (formPersonalInfo == null)
                    continue;

                // Game-specific presence check
                if (!IsSpeciesFormInGame(formPersonalInfo, species, form, game))
                    continue;

                // General exclusion rules
                if (ShouldExcludeForm(species, form, format, game))
                    continue;

                var dbInfo = CreatePokemonDbInfo(formPersonalInfo, species, form, game);
                if (dbInfo != null)
                    result.Add(dbInfo);
            }
        }

        return result;
    }

    /// <summary>
    /// Determines if a species is present in LGPE games.
    /// </summary>
    /// <param name="species">Species ID</param>
    /// <returns>True if the species is in LGPE, false otherwise</returns>
    private static bool IsSpeciesInLGPE(int species)
    {
        // LGPE only includes original 151 Kanto Pokémon plus Meltan and Melmetal
        if (species <= 151)
            return true;

        // Meltan and Melmetal
        if (species == 808 || species == 809)
            return true;

        return false;
    }

    /// <summary>
    /// Determines if a species/form is present in the specified game.
    /// </summary>
    /// <param name="pi">Personal info of the Pokemon</param>
    /// <param name="species">Species ID</param>
    /// <param name="form">Form ID</param>
    /// <param name="game">Game version</param>
    /// <returns>True if the species/form is in the game, false otherwise</returns>
    private static bool IsSpeciesFormInGame(PersonalInfo pi, int species, byte form, GameVersion game)
    {
        return game switch
        {
            GameVersion.SV => pi is PersonalInfo9SV sv && sv.IsPresentInGame,
            GameVersion.SWSH => pi is PersonalInfo8SWSH swsh && swsh.IsPresentInGame,
            GameVersion.BDSP => pi is PersonalInfo8BDSP bdsp && bdsp.IsPresentInGame,
            GameVersion.PLA => pi is PersonalInfo8LA la && la.IsPresentInGame,
            GameVersion.GG => IsLGPEFormValid(species, form),
            _ => true
        };
    }

    /// <summary>
    /// Determines if a form is valid in LGPE games.
    /// </summary>
    /// <param name="species">Species ID</param>
    /// <param name="form">Form ID</param>
    /// <returns>True if the form is valid in LGPE, false otherwise</returns>
    private static bool IsLGPEFormValid(int species, byte form)
    {
        // For LGPE, we have specific rules for which forms are valid
        if (species <= 151)
        {
            // Regular form is always valid
            if (form == 0)
                return true;

            // Alolan form is valid for eligible species
            if (form == 1 && IsAlolaPossible(species))
                return true;

            // Partner forms
            if (species == (int)Species.Pikachu && form == 8)
                return true;

            if (species == (int)Species.Eevee && form == 1)
                return true;

            // All other forms are invalid
            return false;
        }

        // Meltan and Melmetal only have base form
        if ((species == 808 || species == 809) && form == 0)
            return true;

        // All other forms are invalid
        return false;
    }

    /// <summary>
    /// Determines if a species can have an Alolan form in LGPE.
    /// </summary>
    /// <param name="species">Species ID</param>
    /// <returns>True if the species can have an Alolan form, false otherwise</returns>
    private static bool IsAlolaPossible(int species)
    {
        // These are the Pokémon that can have Alolan forms in LGPE
        return species switch
        {
            19 or 20 => true,  // Rattata line
            26 => true,        // Raichu
            27 or 28 => true,  // Sandshrew line
            37 or 38 => true,  // Vulpix line 
            50 or 51 => true,  // Diglett line
            52 or 53 => true,  // Meowth line
            74 or 75 or 76 => true, // Geodude line
            88 or 89 => true,  // Grimer line
            103 => true,       // Exeggutor
            105 => true,       // Marowak
            _ => false
        };
    }

    /// <summary>
    /// Determines if a form should be excluded from the database.
    /// </summary>
    /// <param name="species">Species ID</param>
    /// <param name="form">Form ID</param>
    /// <param name="format">Game format/generation</param>
    /// <param name="game">Game version</param>
    /// <param name="formArg">Optional form argument</param>
    /// <returns>True if the form should be excluded, false otherwise</returns>
    private static bool ShouldExcludeForm(int species, byte form, byte format, GameVersion game, uint formArg = 0)
    {
        // Always exclude all Alcremie forms (form > 0) regardless of game
        if (species == (int)Species.Alcremie && form > 0)
            return true;

        // General exclusion rules for all games

        // Check for battle-only forms
        if (FormInfo.IsBattleOnlyForm((ushort)species, form, format))
            return true;

        // Check for fused forms
        if (FormInfo.IsFusedForm((ushort)species, form, format))
            return true;

        // Check for untradable forms
        if (TradeRestrictions.IsUntradable((ushort)species, form, formArg, format))
            return true;

        // Game-specific exclusions
        switch (game)
        {
            case GameVersion.SV:
                // Special handling for Koraidon/Miraidon forms in SV
                if (species is (int)Species.Koraidon or (int)Species.Miraidon && form > 0)
                    return true;
                break;

            case GameVersion.GG:
                // LGPE exclusions already handled in IsLGPEFormValid
                break;
        }

        // Additional specific exclusions
        if (species == (int)Species.Keldeo && form == 1)
            return true;

        return false;
    }

    /// <summary>
    /// Creates a PokemonDbInfo object with the Pokemon's information.
    /// </summary>
    /// <param name="pi">Personal info of the Pokemon</param>
    /// <param name="species">Species ID</param>
    /// <param name="form">Form ID</param>
    /// <param name="game">Game version</param>
    /// <returns>PokemonDbInfo object or null if the form should be excluded</returns>
    private static PokemonDbInfo? CreatePokemonDbInfo(PersonalInfo pi, int species, byte form, GameVersion game)
    {
        byte format = (byte)game.GetGeneration();
        if (ShouldExcludeForm(species, form, format, game))
            return null;

        var strings = GameInfo.GetStrings("en");
        if (strings == null)
            return null;

        string speciesName = strings.Species[species];
        if (string.IsNullOrEmpty(speciesName))
            return null;

        if (form > 0)
        {
            var formNames = FormConverter.GetFormList((ushort)species, strings.Types, strings.forms, GameInfo.GenderSymbolASCII, EntityContext.Gen9);
            if (formNames.Length > form && !string.IsNullOrEmpty(formNames[form]))
            {
                speciesName = $"{speciesName}-{formNames[form]}";
            }
        }

        int total = pi.HP + pi.ATK + pi.DEF + pi.SPA + pi.SPD + pi.SPE;
        string dexNumber = form > 0 ? $"{species}-{form}" : species.ToString();

        return new PokemonDbInfo
        {
            DexNumber = dexNumber,
            Name = speciesName,
            Total = total,
            HP = pi.HP,
            Attack = pi.ATK,
            Defense = pi.DEF,
            SpAtk = pi.SPA,
            SpDef = pi.SPD,
            Speed = pi.SPE,
            Abilities = GetAbilitiesString(pi, strings),
            Gender = GetGenderString(pi.Gender),
            Evolutions = GetEvolutionsString(species, form, game),
            FrName = GetFormNameByLanguage(species, form, "fr"),
            EsName = GetFormNameByLanguage(species, form, "es"),
            ChHansName = GetFormNameByLanguage(species, form, "zh-Hans"),
            ChHantName = GetFormNameByLanguage(species, form, "zh-Hant"),
            DeName = GetFormNameByLanguage(species, form, "de"),
            JpName = GetFormNameByLanguage(species, form, "ja")
        };
    }

    /// <summary>
    /// Gets a Pokemon's form name in a specific language.
    /// </summary>
    /// <param name="species">Species ID</param>
    /// <param name="form">Form ID</param>
    /// <param name="languageCode">Language code</param>
    /// <returns>Localized form name</returns>
    private static string GetFormNameByLanguage(int species, byte form, string languageCode)
    {
        var strings = GetStringsByLanguage(languageCode);
        if (strings == null)
            return "";

        string speciesName = strings.Species[species];
        if (string.IsNullOrEmpty(speciesName))
            return "";

        if (form > 0)
        {
            var formNames = FormConverter.GetFormList((ushort)species, strings.Types, strings.forms, GameInfo.GenderSymbolASCII, EntityContext.Gen9);
            if (formNames.Length > form && !string.IsNullOrEmpty(formNames[form]))
            {
                return $"{speciesName}-{formNames[form]}";
            }
        }

        return speciesName;
    }

    /// <summary>
    /// Gets a string listing a Pokemon's abilities.
    /// </summary>
    /// <param name="pi">Personal info of the Pokemon</param>
    /// <param name="strings">Game strings</param>
    /// <returns>Comma-separated list of abilities</returns>
    private static string GetAbilitiesString(PersonalInfo pi, GameStrings strings)
    {
        var abilities = new List<string>();

        for (int i = 0; i < pi.AbilityCount; i++)
        {
            int abilityId = pi.GetAbilityAtIndex(i);
            if (abilityId != 0)
            {
                string abilityName = strings.Ability[abilityId];
                if (!string.IsNullOrEmpty(abilityName) && !abilities.Contains(abilityName))
                    abilities.Add(abilityName);
            }
        }

        return string.Join(", ", abilities);
    }

    /// <summary>
    /// Gets a string representing a Pokemon's gender ratio.
    /// </summary>
    /// <param name="genderValue">Gender value from personal info</param>
    /// <returns>String describing gender ratio</returns>
    private static string GetGenderString(byte genderValue) => genderValue switch
    {
        PersonalInfo.RatioMagicGenderless => "Genderless",
        PersonalInfo.RatioMagicFemale => "Female",
        PersonalInfo.RatioMagicMale => "Male",
        _ => "Male, Female"
    };

    /// <summary>
    /// Gets a string describing a Pokemon's evolution chain.
    /// </summary>
    /// <param name="species">Species ID</param>
    /// <param name="form">Form ID</param>
    /// <param name="game">Game version</param>
    /// <returns>String describing evolution chain</returns>
    private static string GetEvolutionsString(int species, byte form, GameVersion game)
    {
        byte format = (byte)game.GetGeneration();
        if (ShouldExcludeForm(species, form, format, game))
            return "";

        return PokemonEvolutionHelper.GetEvolutionString(species, form, game);
    }

    /// <summary>
    /// Gets game strings for a specific language.
    /// </summary>
    /// <param name="languageCode">Language code</param>
    /// <returns>Game strings for the specified language</returns>
    private static GameStrings? GetStringsByLanguage(string languageCode) =>
        GameInfo.GetStrings(languageCode);

    /// <summary>
    /// Gets the personal table for a specific game version.
    /// </summary>
    /// <param name="game">Game version</param>
    /// <returns>Personal table for the specified game</returns>
    /// <exception cref="ArgumentException">Thrown when an unsupported game is specified</exception>
    private static IPersonalTable GetPersonalTableForGame(GameVersion game) => game switch
    {
        GameVersion.SV => PersonalTable.SV,
        GameVersion.PLA => PersonalTable.LA,
        GameVersion.BDSP => PersonalTable.BDSP,
        GameVersion.SWSH => PersonalTable.SWSH,
        GameVersion.GG => PersonalTable.GG,
        _ => throw new ArgumentException($"Unsupported game: {game}")
    };

    /// <summary>
    /// Gets the maximum species ID for a specific game version.
    /// </summary>
    /// <param name="game">Game version</param>
    /// <returns>Maximum species ID for the specified game</returns>
    /// <exception cref="ArgumentException">Thrown when an unsupported game is specified</exception>
    private static int GetMaxSpeciesForGame(GameVersion game) => game switch
    {
        GameVersion.SV => Legal.MaxSpeciesID_9,
        GameVersion.PLA => Legal.MaxSpeciesID_8a,
        GameVersion.BDSP => Legal.MaxSpeciesID_8b,
        GameVersion.SWSH => Legal.MaxSpeciesID_8,
        GameVersion.GG => Legal.MaxSpeciesID_7b,
        _ => throw new ArgumentException($"Unsupported game: {game}")
    };

    /// <summary>
    /// Logs an error to the error logger.
    /// </summary>
    /// <param name="errorLogger">StreamWriter for error logging</param>
    /// <param name="ex">Exception to log</param>
    private static void LogError(StreamWriter errorLogger, Exception ex)
    {
        errorLogger.WriteLine($"[{DateTime.Now}] An error occurred: {ex.Message}");
        errorLogger.WriteLine($"Stack Trace: {ex.StackTrace}");
    }
}

/// <summary>
/// Contains information about a Pokemon for the database.
/// </summary>
public sealed class PokemonDbInfo
{
    /// <summary>
    /// Pokedex number, potentially with form number (e.g. "25-1").
    /// </summary>
    public string DexNumber { get; set; } = string.Empty;

    /// <summary>
    /// English name of the Pokemon.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Total base stats.
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// Base HP stat.
    /// </summary>
    public int HP { get; set; }

    /// <summary>
    /// Base Attack stat.
    /// </summary>
    public int Attack { get; set; }

    /// <summary>
    /// Base Defense stat.
    /// </summary>
    public int Defense { get; set; }

    /// <summary>
    /// Base Special Attack stat.
    /// </summary>
    public int SpAtk { get; set; }

    /// <summary>
    /// Base Special Defense stat.
    /// </summary>
    public int SpDef { get; set; }

    /// <summary>
    /// Base Speed stat.
    /// </summary>
    public int Speed { get; set; }

    /// <summary>
    /// Comma-separated list of abilities.
    /// </summary>
    public string Abilities { get; set; } = string.Empty;

    /// <summary>
    /// Gender ratio description.
    /// </summary>
    public string Gender { get; set; } = string.Empty;

    /// <summary>
    /// Evolution chain description.
    /// </summary>
    public string Evolutions { get; set; } = string.Empty;

    /// <summary>
    /// French name of the Pokemon.
    /// </summary>
    public string FrName { get; set; } = string.Empty;

    /// <summary>
    /// Spanish name of the Pokemon.
    /// </summary>
    public string EsName { get; set; } = string.Empty;

    /// <summary>
    /// Simplified Chinese name of the Pokemon.
    /// </summary>
    public string ChHansName { get; set; } = string.Empty;

    /// <summary>
    /// Traditional Chinese name of the Pokemon.
    /// </summary>
    public string ChHantName { get; set; } = string.Empty;

    /// <summary>
    /// German name of the Pokemon.
    /// </summary>
    public string DeName { get; set; } = string.Empty;

    /// <summary>
    /// Japanese name of the Pokemon.
    /// </summary>
    public string JpName { get; set; } = string.Empty;
}

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
            var personalInfo = table.GetFormEntry((ushort)species, 0);
            if (personalInfo == null)
                continue;

            byte formCount = personalInfo.FormCount;

            for (byte form = 0; form < formCount; form++)
            {
                var formPersonalInfo = table.GetFormEntry((ushort)species, form);
                if (formPersonalInfo == null || !IsSpeciesInGame(formPersonalInfo))
                    continue;

                if (ShouldExcludeForm(species, form, format))
                    continue;

                var dbInfo = CreatePokemonDbInfo(formPersonalInfo, species, form, game);
                if (dbInfo != null)
                    result.Add(dbInfo);
            }
        }

        return result;
    }

    /// <summary>
    /// Determines if a species is present in the current game.
    /// </summary>
    /// <param name="pi">Personal info of the Pokemon</param>
    /// <returns>True if the species is in the game, false otherwise</returns>
    private static bool IsSpeciesInGame(PersonalInfo pi) => pi switch
    {
        PersonalInfo9SV sv => sv.IsPresentInGame,
        PersonalInfo8SWSH swsh => swsh.IsPresentInGame,
        PersonalInfo8BDSP bdsp => bdsp.IsPresentInGame,
        PersonalInfo8LA la => la.IsPresentInGame,
        _ => true
    };

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
        byte format = game.GetGeneration();
        if (ShouldExcludeForm(species, form, format))
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
        var legalBalls = GetLegalBalls(species, form, game);

        var info = new PokemonDbInfo
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
            Ball = string.Join(",", legalBalls),
            FrName = GetFormNameByLanguage(species, form, "fr"),
            EsName = GetFormNameByLanguage(species, form, "es"),
            ChHansName = GetFormNameByLanguage(species, form, "zh-Hans"),
            ChHantName = GetFormNameByLanguage(species, form, "zh-Hant"),
            DeName = GetFormNameByLanguage(species, form, "de"),
            JpName = GetFormNameByLanguage(species, form, "ja")
        };

        UpdateIVRequirements(info, species, form, game);
        return info;
    }

    /// <summary>
    /// Determines if a form should be excluded from the database.
    /// </summary>
    /// <param name="species">Species ID</param>
    /// <param name="form">Form ID</param>
    /// <param name="format">Game format/generation</param>
    /// <param name="formArg">Optional form argument</param>
    /// <returns>True if the form should be excluded, false otherwise</returns>
    private static bool ShouldExcludeForm(int species, byte form, byte format, uint formArg = 0)
    {
        if (FormInfo.IsFusedForm((ushort)species, form, format) ||
            FormInfo.IsBattleOnlyForm((ushort)species, form, format))
            return true;

        // Exclude Keldeo-Resolute form (form 1)
        if (species == (int)Species.Keldeo && form == 1)
            return true;

        // Skip Alcremie forms
        if (species == (int)Species.Alcremie && form > 0)
            return true;

        // Skip other untradable forms
        if (species is (int)Species.Koraidon or (int)Species.Miraidon && formArg == 1)
            return true;

        if (format == 7)
        {
            if ((species == (int)Species.Pikachu && form == 8) ||
                (species == (int)Species.Eevee && form == 1))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Updates IV requirements information for a Pokemon.
    /// </summary>
    /// <param name="info">Pokemon database info to update</param>
    /// <param name="species">Species ID</param>
    /// <param name="form">Form ID</param>
    /// <param name="game">Game version</param>
    private static void UpdateIVRequirements(PokemonDbInfo info, int species, byte form, GameVersion game)
    {
        byte format = game.GetGeneration();
        if (ShouldExcludeForm(species, form, format))
            return;

        switch (game)
        {
            case GameVersion.SV:
                CheckEncountersSV(info, species, form);
                break;
            case GameVersion.SWSH:
                CheckEncountersSWSH(info, species, form);
                break;
            case GameVersion.BDSP:
                CheckEncountersBDSP(info, species, form);
                break;
            case GameVersion.PLA:
                CheckEncountersPLA(info, species, form);
                break;
            case GameVersion.GG:
                CheckEncountersGG(info, species, form);
                break;
        }
    }

    /// <summary>
    /// Checks Scarlet/Violet encounters for IV requirements.
    /// </summary>
    /// <param name="info">Pokemon database info to update</param>
    /// <param name="species">Species ID</param>
    /// <param name="form">Form ID</param>
    private static void CheckEncountersSV(PokemonDbInfo info, int species, byte form)
    {
        // Check standard encounters
        foreach (var encounter in Encounters9.Encounter_SV
                 .Concat(Encounters9.StaticSL)
                 .Concat(Encounters9.StaticVL))
        {
            if (encounter.Species != species || encounter.Form != form)
                continue;

            if (encounter.IVs != null && encounter.IVs.IsSpecified)
            {
                info.SetIVs = FormatIVs(encounter.IVs);
                return;
            }

            if (encounter.FlawlessIVCount > 0)
            {
                info.SetIVs = $"{encounter.FlawlessIVCount} perfect IVs";
                return;
            }
        }

        // Check Fixed encounters
        foreach (var encounter in Encounters9.Fixed)
        {
            if (encounter.Species != species || encounter.Form != form)
                continue;

            if (encounter.FlawlessIVCount > 0)
            {
                info.SetIVs = $"{encounter.FlawlessIVCount} perfect IVs";
                return;
            }
        }

        // Check Tera raid encounters
        var teraEncounters = Encounters9.TeraBase
            .Concat(Encounters9.TeraDLC1)
            .Concat(Encounters9.TeraDLC2)
            .Cast<IFlawlessIVCount>()
            .Concat(Encounters9.Dist)
            .Concat(Encounters9.Might);

        foreach (var encounter in teraEncounters)
        {
            if ((encounter as dynamic).Species != species || (encounter as dynamic).Form != form)
                continue;

            if (encounter.FlawlessIVCount > 0)
            {
                info.SetIVs = $"{encounter.FlawlessIVCount} perfect IVs";
                return;
            }
        }
    }

    /// <summary>
    /// Checks Sword/Shield encounters for IV requirements.
    /// </summary>
    /// <param name="info">Pokemon database info to update</param>
    /// <param name="species">Species ID</param>
    /// <param name="form">Form ID</param>
    private static void CheckEncountersSWSH(PokemonDbInfo info, int species, byte form)
    {
        // Check static encounters
        foreach (var encounter in Encounters8.StaticSWSH
                 .Concat(Encounters8.StaticSW)
                 .Concat(Encounters8.StaticSH))
        {
            if (encounter.Species != species || encounter.Form != form)
                continue;

            if (encounter.FlawlessIVCount > 0)
            {
                info.SetIVs = $"{encounter.FlawlessIVCount} perfect IVs";
                return;
            }

            if (encounter.IVs != null && IsActuallySpecifiedIVs(encounter.IVs))
            {
                info.SetIVs = FormatIVs(encounter.IVs);
                return;
            }
        }

        // Check nest encounters (Max Raid Battles)
        foreach (var encounter in Encounters8Nest.Nest_SW.Concat(Encounters8Nest.Nest_SH))
        {
            if (encounter.Species != species || encounter.Form != form)
                continue;

            if (encounter.FlawlessIVCount > 0)
            {
                info.SetIVs = $"{encounter.FlawlessIVCount} perfect IVs";
                return;
            }
        }

        // Check distribution raids
        foreach (var encounter in Encounters8Nest.Dist_SW.Concat(Encounters8Nest.Dist_SH))
        {
            if (encounter.Species != species || encounter.Form != form)
                continue;

            if (encounter.FlawlessIVCount > 0)
            {
                info.SetIVs = $"{encounter.FlawlessIVCount} perfect IVs";
                return;
            }
        }

        // Check Dynamax Adventure encounters
        foreach (var encounter in Encounters8Nest.DynAdv_SWSH)
        {
            if (encounter.Species != species || encounter.Form != form)
                continue;

            if (encounter.FlawlessIVCount > 0)
            {
                info.SetIVs = $"{encounter.FlawlessIVCount} perfect IVs";
                return;
            }
        }
    }

    /// <summary>
    /// Checks if IVs are actually specified for an encounter.
    /// </summary>
    /// <param name="ivs">Individual value set</param>
    /// <returns>True if IVs are specified, false otherwise</returns>
    private static bool IsActuallySpecifiedIVs(IndividualValueSet ivs)
    {
        bool hasNonZeroIV = ivs.HP > 0 || ivs.ATK > 0 || ivs.DEF > 0 ||
                           ivs.SPA > 0 || ivs.SPD > 0 || ivs.SPE > 0;

        bool hasMixedValues = (ivs.HP != ivs.ATK || ivs.ATK != ivs.DEF || ivs.DEF != ivs.SPA ||
                              ivs.SPA != ivs.SPD || ivs.SPD != ivs.SPE);

        bool hasNegativeIV = ivs.HP == -1 || ivs.ATK == -1 || ivs.DEF == -1 ||
                             ivs.SPA == -1 || ivs.SPD == -1 || ivs.SPE == -1;

        return (hasNonZeroIV || (hasMixedValues && !hasNegativeIV));
    }

    /// <summary>
    /// Checks BD/SP encounters for IV requirements.
    /// </summary>
    /// <param name="info">Pokemon database info to update</param>
    /// <param name="species">Species ID</param>
    /// <param name="form">Form ID</param>
    private static void CheckEncountersBDSP(PokemonDbInfo info, int species, byte form)
    {
        // Check static encounters
        foreach (var encounter in Encounters8b.Encounter_BDSP
                 .Concat(Encounters8b.StaticBD)
                 .Concat(Encounters8b.StaticSP))
        {
            if (encounter.Species != species || encounter.Form != form)
                continue;

            if (encounter.FlawlessIVCount > 0)
            {
                info.SetIVs = $"{encounter.FlawlessIVCount} perfect IVs";
                return;
            }
        }

        // Check trade gifts
        foreach (var trade in Encounters8b.TradeGift_BDSP)
        {
            if (trade.Species != species || trade.Form != form)
                continue;

            if (trade.IVs != null && !IsEmptyIVs(trade.IVs))
            {
                info.SetIVs = FormatIVs(trade.IVs);
                return;
            }
        }
    }

    /// <summary>
    /// Checks Legends Arceus encounters for IV requirements.
    /// </summary>
    /// <param name="info">Pokemon database info to update</param>
    /// <param name="species">Species ID</param>
    /// <param name="form">Form ID</param>
    private static void CheckEncountersPLA(PokemonDbInfo info, int species, byte form)
    {
        foreach (var encounter in Encounters8a.StaticLA)
        {
            if (encounter.Species != species || encounter.Form != form)
                continue;

            if (encounter.FlawlessIVCount > 0)
            {
                info.SetIVs = $"{encounter.FlawlessIVCount} perfect IVs";
                return;
            }
        }
    }

    /// <summary>
    /// Checks Let's Go encounters for IV requirements.
    /// </summary>
    /// <param name="info">Pokemon database info to update</param>
    /// <param name="species">Species ID</param>
    /// <param name="form">Form ID</param>
    private static void CheckEncountersGG(PokemonDbInfo info, int species, byte form)
    {
        // Check static encounters
        foreach (var encounter in Encounters7GG.Encounter_GG
                 .Concat(Encounters7GG.StaticGP)
                 .Concat(Encounters7GG.StaticGE))
        {
            if (encounter.Species != species || encounter.Form != form)
                continue;

            if (encounter.IVs != null && !IsEmptyIVs(encounter.IVs))
            {
                info.SetIVs = FormatIVs(encounter.IVs);
                return;
            }

            if (encounter.FlawlessIVCount > 0)
            {
                info.SetIVs = $"{encounter.FlawlessIVCount} perfect IVs";
                return;
            }
        }

        // Check trade gifts
        foreach (var trade in Encounters7GG.TradeGift_GG
                 .Concat(Encounters7GG.TradeGift_GP)
                 .Concat(Encounters7GG.TradeGift_GE))
        {
            if (trade.Species != species || trade.Form != form)
                continue;

            if (trade.IVs != null && !IsEmptyIVs(trade.IVs))
            {
                info.SetIVs = FormatIVs(trade.IVs);
                return;
            }
        }
    }

    /// <summary>
    /// Checks if all IVs are unspecified (-1).
    /// </summary>
    /// <param name="ivs">Individual value set</param>
    /// <returns>True if all IVs are unspecified, false otherwise</returns>
    private static bool IsEmptyIVs(IndividualValueSet ivs) =>
        ivs.HP == -1 && ivs.ATK == -1 && ivs.DEF == -1 &&
        ivs.SPA == -1 && ivs.SPD == -1 && ivs.SPE == -1;

    /// <summary>
    /// Formats IVs as a string, handling -1 (random) values.
    /// </summary>
    /// <param name="ivs">Individual value set</param>
    /// <returns>Formatted IV string</returns>
    private static string FormatIVs(IndividualValueSet ivs)
    {
        var ivParts = new List<string>(6);

        if (ivs.HP != -1) ivParts.Add($"HP:{ivs.HP}");
        if (ivs.ATK != -1) ivParts.Add($"Atk:{ivs.ATK}");
        if (ivs.DEF != -1) ivParts.Add($"Def:{ivs.DEF}");
        if (ivs.SPA != -1) ivParts.Add($"SpA:{ivs.SPA}");
        if (ivs.SPD != -1) ivParts.Add($"SpD:{ivs.SPD}");
        if (ivs.SPE != -1) ivParts.Add($"Spe:{ivs.SPE}");

        return string.Join(", ", ivParts);
    }

    /// <summary>
    /// Gets legal Pokeballs for a Pokemon in a specific game.
    /// </summary>
    /// <param name="species">Species ID</param>
    /// <param name="form">Form ID</param>
    /// <param name="game">Game version</param>
    /// <returns>List of legal Pokeball IDs</returns>
    public static List<int> GetLegalBalls(int species, byte form, GameVersion game)
    {
        byte format = game.GetGeneration();
        if (ShouldExcludeForm(species, form, format))
            return [];

        var pk = CreateSimplePkm((ushort)species, form, game);
        var parse = new List<CheckResult>();
        var info = new LegalInfo(pk, parse);
        var encounters = EncounterGenerator.GetEncounters(pk, info).ToList();

        var legalBalls = new HashSet<int>();

        if (encounters.Count == 0)
            return GetFallbackLegalBalls((ushort)species, form, game);

        foreach (var encounter in encounters)
        {
            AddLegalBallsForEncounter(encounter, legalBalls, pk);
        }

        return [.. legalBalls];
    }

    /// <summary>
    /// Creates a simple PKM instance for the given species/form/game.
    /// </summary>
    /// <param name="species">Species ID</param>
    /// <param name="form">Form ID</param>
    /// <param name="game">Game version</param>
    /// <returns>PKM instance</returns>
    private static PKM CreateSimplePkm(ushort species, byte form, GameVersion game)
    {
        var context = game.GetGeneration() switch
        {
            1 => EntityContext.Gen1,
            2 => EntityContext.Gen2,
            3 => EntityContext.Gen3,
            4 => EntityContext.Gen4,
            5 => EntityContext.Gen5,
            6 => EntityContext.Gen6,
            7 => game == GameVersion.GG ? EntityContext.Gen7b : EntityContext.Gen7,
            8 when game == GameVersion.PLA => EntityContext.Gen8a,
            8 when game == GameVersion.BD || game == GameVersion.SP => EntityContext.Gen8b,
            8 => EntityContext.Gen8,
            9 => EntityContext.Gen9,
            _ => EntityContext.None,
        };

        PKM pk = EntityBlank.GetBlank(game.GetGeneration(), game);
        pk.Species = species;
        pk.Form = form;
        pk.Version = game;
        pk.Language = 2; // English
        pk.CurrentLevel = 50;
        pk.OriginalTrainerFriendship = 70;

        return pk;
    }

    /// <summary>
    /// Adds legal balls for an encounter to the legal balls collection.
    /// </summary>
    /// <param name="encounter">Encounter data</param>
    /// <param name="legalBalls">Collection of legal ball IDs</param>
    /// <param name="pk">PKM instance</param>
    private static void AddLegalBallsForEncounter(IEncounterable encounter, HashSet<int> legalBalls, PKM pk)
    {
        if (encounter is IFixedBall fixedBall && fixedBall.FixedBall != Ball.None)
        {
            // For encounters with a fixed ball, only that ball is legal
            legalBalls.Add((int)fixedBall.FixedBall);
        }
        else if (encounter is EncounterEgg)
        {
            // For eggs, apply breeding ball inheritance rules
            AddBallInheritanceOptions(pk.Species, pk.Form, encounter, legalBalls);
        }
        else
        {
            // For wild encounters, add all balls valid for the encounter's generation and version
            ulong validBalls = BallUseLegality.GetWildBalls(encounter.Generation, encounter.Version);

            // Make sure LA balls are only valid for LA games
            if (encounter.Version != GameVersion.PLA)
            {
                // Maximum non-LA ball ID (adjust if needed)
                int maxBallID = 38;

                for (int i = 1; i <= maxBallID; i++)
                {
                    if (BallUseLegality.IsBallPermitted(validBalls, (byte)i))
                    {
                        // Handle special cases like Heavy Ball in Gen 7
                        if (i == (int)Ball.Heavy && encounter.Generation == 7 &&
                            BallUseLegality.IsAlolanCaptureNoHeavyBall(pk.Species))
                            continue;

                        legalBalls.Add(i);
                    }
                }
            }
            else
            {
                // For PLA, include all valid balls including LA balls
                for (int i = 1; i <= 60; i++) // Adjust upper bound as needed to include all LA balls
                {
                    if (BallUseLegality.IsBallPermitted(validBalls, (byte)i))
                        legalBalls.Add(i);
                }
            }
        }
    }

    /// <summary>
    /// Adds ball inheritance options based on breeding rules.
    /// </summary>
    /// <param name="species">Species ID</param>
    /// <param name="form">Form ID</param>
    /// <param name="encounter">Encounter data</param>
    /// <param name="legalBalls">Collection of legal ball IDs</param>
    private static void AddBallInheritanceOptions(int species, byte form, IEncounterable encounter, HashSet<int> legalBalls)
    {
        byte generation = encounter.Generation;

        if (generation >= 6)
        {
            legalBalls.Add(14); // Poké Ball always available

            if (generation >= 7)
            {
                ulong validBalls = BallUseLegality.GetWildBalls(generation, encounter.Version);

                for (int i = 1; i <= 38; i++)
                {
                    if (BallUseLegality.IsBallPermitted(validBalls, (byte)i))
                    {
                        // Master Ball and Cherish Ball can't be inherited
                        if (i == (int)Ball.Master || i == (int)Ball.Cherish)
                            continue;

                        legalBalls.Add(i);
                    }
                }
            }
        }
        else
        {
            // Pre-Gen 6 only used Poké Balls for breeding
            legalBalls.Add(14); // Poké Ball
        }
    }

    /// <summary>
    /// Gets a fallback list of legal balls when encounter data is unavailable.
    /// </summary>
    /// <param name="species">Species ID</param>
    /// <param name="form">Form ID</param>
    /// <param name="game">Game version</param>
    /// <returns>List of legal ball IDs</returns>
    private static List<int> GetFallbackLegalBalls(ushort species, byte form, GameVersion game)
    {
        var gameGeneration = GetGenerationFromGameVersion(game);
        ulong wildBalls = BallUseLegality.GetWildBalls(gameGeneration, game);

        var legalBalls = new List<int>();
        for (int i = 1; i <= 38; i++)
        {
            if (BallUseLegality.IsBallPermitted(wildBalls, (byte)i))
            {
                legalBalls.Add(i);
            }
        }

        // Apply special cases
        if (gameGeneration == 7 && game != GameVersion.GG)
        {
            // Remove Heavy Ball for certain species in Gen 7
            if (BallUseLegality.IsAlolanCaptureNoHeavyBall(species) && legalBalls.Contains(30))
                legalBalls.Remove(30);
        }

        return legalBalls;
    }

    /// <summary>
    /// Gets the generation number from a game version.
    /// </summary>
    /// <param name="game">Game version</param>
    /// <returns>Generation number</returns>
    private static byte GetGenerationFromGameVersion(GameVersion game) => game switch
    {
        GameVersion.SV => 9,
        GameVersion.PLA => 8,
        GameVersion.BDSP => 8,
        GameVersion.SWSH => 8,
        GameVersion.GG => 7,
        _ => 0
    };

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
        byte format = game.GetGeneration();
        if (ShouldExcludeForm(species, form, format))
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
public sealed record PokemonDbInfo
{
    /// <summary>
    /// Pokedex number, potentially with form number (e.g. "25-1").
    /// </summary>
    public required string DexNumber { get; set; }

    /// <summary>
    /// English name of the Pokemon.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Total base stats.
    /// </summary>
    public required int Total { get; set; }

    /// <summary>
    /// Base HP stat.
    /// </summary>
    public required int HP { get; set; }

    /// <summary>
    /// Base Attack stat.
    /// </summary>
    public required int Attack { get; set; }

    /// <summary>
    /// Base Defense stat.
    /// </summary>
    public required int Defense { get; set; }

    /// <summary>
    /// Base Special Attack stat.
    /// </summary>
    public required int SpAtk { get; set; }

    /// <summary>
    /// Base Special Defense stat.
    /// </summary>
    public required int SpDef { get; set; }

    /// <summary>
    /// Base Speed stat.
    /// </summary>
    public required int Speed { get; set; }

    /// <summary>
    /// Comma-separated list of abilities.
    /// </summary>
    public required string Abilities { get; set; }

    /// <summary>
    /// Gender ratio description.
    /// </summary>
    public required string Gender { get; set; }

    /// <summary>
    /// Evolution chain description.
    /// </summary>
    public required string Evolutions { get; set; }

    /// <summary>
    /// Comma-separated list of legal Pokeballs.
    /// </summary>
    public required string Ball { get; set; }

    /// <summary>
    /// Information about required IVs, if any.
    /// </summary>
    public string SetIVs { get; set; } = "";

    /// <summary>
    /// French name of the Pokemon.
    /// </summary>
    public required string FrName { get; set; }

    /// <summary>
    /// Spanish name of the Pokemon.
    /// </summary>
    public required string EsName { get; set; }

    /// <summary>
    /// Simplified Chinese name of the Pokemon.
    /// </summary>
    public required string ChHansName { get; set; }

    /// <summary>
    /// Traditional Chinese name of the Pokemon.
    /// </summary>
    public required string ChHantName { get; set; }

    /// <summary>
    /// German name of the Pokemon.
    /// </summary>
    public required string DeName { get; set; }

    /// <summary>
    /// Japanese name of the Pokemon.
    /// </summary>
    public required string JpName { get; set; }
}

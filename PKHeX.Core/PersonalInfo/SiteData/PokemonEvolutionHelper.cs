using System.Collections.Generic;
using System.Text;

namespace PKHeX.Core
{
    /// <summary>
    /// Simple helper class for getting evolution chains with minimal information.
    /// </summary>
    public static class PokemonEvolutionHelper
    {
        /// <summary>
        /// Gets formatted evolution string for a Pokémon species and form.
        /// </summary>
        /// <param name="species">Species ID</param>
        /// <param name="form">Form number</param>
        /// <param name="game">Game version</param>
        /// <returns>Evolution string in format "Ivysaur (16), Venusaur (32)"</returns>
        public static string GetEvolutionString(int species, byte form, GameVersion game)
        {
            // Get the appropriate evolution tree based on game version
            var context = GetEntityContext(game);
            var evoTree = EvolutionTree.GetEvolutionTree(context);

            // Get evolutions (what this species evolves into)
            var evolutions = new List<(int Species, byte Form, int Level)>();
            GetDirectEvolutions((ushort)species, form, evoTree, evolutions);

            // If this is an evolved form, get what it evolves from
            var preEvolutions = new List<(int Species, byte Form, int Level)>();
            if (evolutions.Count == 0) // If it doesn't evolve further, check what it evolves from
            {
                var node = evoTree.Reverse.GetReverse((ushort)species, form);
                if (node.First.Species != 0) // If it has a pre-evolution
                {
                    // Get the level required for this species to evolve from its pre-evolution
                    int level = GetEvolutionLevelFromMethod(node.First.Method);

                    // Add the pre-evolution to the list
                    preEvolutions.Add((node.First.Species, node.First.Form, level));
                }
            }

            // Format the results based on whether it's a base form or evolved form
            if (evolutions.Count > 0)
            {
                return FormatEvolutions(evolutions);
            }
            else if (preEvolutions.Count > 0)
            {
                return FormatPreEvolution(preEvolutions[0]);
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets the entity context for a game version.
        /// </summary>
        private static EntityContext GetEntityContext(GameVersion game)
        {
            return game switch
            {
                GameVersion.SV => EntityContext.Gen9,
                GameVersion.PLA => EntityContext.Gen8a,
                GameVersion.BDSP => EntityContext.Gen8b,
                GameVersion.SWSH => EntityContext.Gen8,
                GameVersion.GG => EntityContext.Gen7b,
                _ => EntityContext.Gen9 // Default to latest gen if unknown
            };
        }

        /// <summary>
        /// Gets direct evolutions for a species and form.
        /// </summary>
        private static void GetDirectEvolutions(ushort species, byte form, EvolutionTree evoTree, List<(int Species, byte Form, int Level)> evolutions)
        {
            // Get immediate evolutions for the specific form
            var evos = evoTree.Forward.GetForward(species, form);
            foreach (var evo in evos.Span)
            {
                int level = GetEvolutionLevel(evo);
                byte evoForm = (byte)evo.Form;
                evolutions.Add((evo.Species, evoForm, level));

                // Recursively check for further evolutions
                GetDirectEvolutions((ushort)evo.Species, evoForm, evoTree, evolutions);
            }
        }

        /// <summary>
        /// Gets pre-evolutions for a species and form.
        /// </summary>
        private static void GetPreEvolutions(ushort species, byte form, EvolutionTree evoTree, List<(int Species, byte Form, int Level)> preEvolutions)
        {
            // Get the reverse evolution data
            var node = evoTree.Reverse.GetReverse(species, form);
            var first = node.First;
            if (first.Species == 0)
                return; // No pre-evolutions

            // Get the level at which this species evolves from its pre-evolution
            int level = GetEvolutionLevelFromMethod(first.Method);
            preEvolutions.Add((first.Species, first.Form, level));

            // Recursively check for earlier pre-evolutions
            GetPreEvolutions(first.Species, first.Form, evoTree, preEvolutions);
        }

        /// <summary>
        /// Gets the evolution level from an evolution method.
        /// </summary>
        private static int GetEvolutionLevelFromMethod(EvolutionMethod method)
        {
            if (method.Level > 0)
                return method.Level;
            if (method.Method == EvolutionType.LevelUp && method.Argument > 0)
                return method.Argument;
            return 0;
        }

        /// <summary>
        /// Gets the evolution level from an evolution entry.
        /// </summary>
        private static int GetEvolutionLevel(EvolutionMethod evo)
        {
            // For level-up evolutions with a specific level requirement
            if (evo.Level > 0)
                return evo.Level;
            // For some type of level-up evolutions, the argument field has the level
            if (evo.Method == EvolutionType.LevelUp && evo.Argument > 0)
                return evo.Argument;
            // Non-level evolution
            return 0;
        }

        /// <summary>
        /// Formats pre-evolution into a readable string.
        /// </summary>
        private static string FormatPreEvolution((int Species, byte Form, int Level) preEvolution)
        {
            var strings = GameInfo.GetStrings("en");
            if (strings == null)
                return string.Empty;

            var (species, form, level) = preEvolution;
            string speciesName = strings.Species[species];

            if (form > 0)
            {
                var formNames = FormConverter.GetFormList((ushort)species, strings.Types, strings.forms, GameInfo.GenderSymbolASCII, EntityContext.Gen9);
                if (formNames.Length > form && !string.IsNullOrEmpty(formNames[form]))
                {
                    speciesName = $"{speciesName}-{formNames[form]}";
                }
            }

            // Add level only if it's a level-up evolution
            if (level > 0)
            {
                return $"{speciesName} ({level})";
            }

            return speciesName;
        }

        /// <summary>
        /// Formats evolutions into a readable string.
        /// </summary>
        private static string FormatEvolutions(List<(int Species, byte Form, int Level)> evolutions)
        {
            var sb = new StringBuilder();
            var strings = GameInfo.GetStrings("en");
            if (strings == null)
                return string.Empty;

            for (int i = 0; i < evolutions.Count; i++)
            {
                var (species, form, level) = evolutions[i];

                if (i > 0)
                    sb.Append(", ");

                string speciesName = strings.Species[species];
                if (form > 0)
                {
                    var formNames = FormConverter.GetFormList((ushort)species, strings.Types, strings.forms, GameInfo.GenderSymbolASCII, EntityContext.Gen9);
                    if (formNames.Length > form && !string.IsNullOrEmpty(formNames[form]))
                    {
                        speciesName = $"{speciesName}-{formNames[form]}";
                    }
                }

                sb.Append(speciesName);

                // Add level only if it's a level-up evolution
                if (level > 0)
                {
                    sb.Append($" ({level})");
                }
            }

            return sb.ToString();
        }
    }
}

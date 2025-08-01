using System;

namespace PKHeX.Core;

/// <summary>
/// RNG logic for Generation 5 (Black/White/Black2/White2) games.
/// </summary>
/// <remarks><see cref="LCRNG64"/></remarks>
public static class MonochromeRNG
{
    /// <summary>
    /// Calculates the XOR result of the Trainer ID and Secret ID, masked to the least significant bit.
    /// </summary>
    public static uint GetBitXor<T>(T tr) where T : ITrainerID32ReadOnly => ((uint)tr.TID16 ^ tr.SID16) & 1;

    /// <inheritdoc cref="GetBitXor{T}(T)"/>
    public static uint GetBitXor(ushort tid, ushort sid) => ((uint)tid ^ sid) & 1;

    /// <summary>
    /// Calculates the XOR result of the PID, Trainer ID, and Secret ID.
    /// </summary>
    public static uint GetBitXor<T>(T tr, uint pid) where T : ITrainerID32ReadOnly => GetBitXor(tr) ^ (pid & 1) ^ (pid >> 31);

    /// <inheritdoc cref="GetBitXor{T}(T,uint)"/>
    public static uint GetBitXor(uint pid, ushort tid, ushort sid) => GetBitXor(tid, sid) ^ (pid & 1) ^ (pid >> 31);

    /// <summary>
    /// Assigns a valid PID and gender based on the specified criteria.
    /// </summary>
    /// <param name="pk">The Pokémon object to be modified with the generated attributes.</param>
    /// <param name="criteria">The encounter criteria that define the desired attributes for the Pokémon.</param>
    /// <param name="gr">The gender ratio used to determine the Pokémon's gender.</param>
    /// <param name="seed">The initial random seed used for generating the Pokémon's PID and other attributes.</param>
    /// <param name="wildXor">Wild XOR is applied to the PID, which increases the chance of a shiny Pokémon (but actually doesn't).</param>
    /// <param name="type">Shiny type to apply to the generated PID.</param>
    /// <param name="ability">The ability permission that determines which ability can be assigned to the Pokémon.</param>
    /// <summary>
    /// Generates and assigns a valid PID and gender to a PK5 Pokémon, ensuring compliance with specified encounter criteria, gender ratio, RNG seed, shiny state, ability permissions, and optional forced gender.
    /// </summary>
    /// <param name="pk">The PK5 Pokémon object to update.</param>
    /// <param name="criteria">Encounter criteria specifying gender, ability, and shiny requirements.</param>
    /// <param name="gr">The gender ratio value for the species.</param>
    /// <param name="seed">The 64-bit RNG seed used for PID generation.</param>
    /// <param name="wildXor">Whether to apply the wild XOR adjustment to the PID.</param>
    /// <param name="type">The desired shiny state for the generated Pokémon.</param>
    /// <param name="ability">Ability slot constraints for the generated Pokémon.</param>
    /// <param name="forceGender">If specified, forces the generated Pokémon to this gender regardless of criteria.</param>
    public static void Generate(PK5 pk, in EncounterCriteria criteria, byte gr, ulong seed, bool wildXor, Shiny type, AbilityPermission ability, byte forceGender = FixedGenderUtil.GenderRandom)
    {
        // The RNG is random enough (64-bit) that our results won't be traceable to a specific seed (sufficiently random).
        // Therefore, we can skip the whole advancement sequence and "create" a PID directly from our random seed.

        // Nature: Separate from PID.
        // Gender: still follows the Gen3/4 correlation based on low-8 bits of PID.
        // Ability: bit 16
        // Correlation: Trainer ID low-bits with PID high-bit and low-bit xor must be 0.
        var bitXor = GetBitXor(pk);
        var id32 = pk.ID32;
        var isSingleAbility = ability.IsSingleValue(out _);
        var isForcedGender = forceGender is not FixedGenderUtil.GenderRandom && gr is not (0 or 254 or 255); // random gender species-forms only

        // Logic: get a PID that matches Gender, then force the other correlations since they won't invalidate the gender.
        // We could probably do this in a single step (shiny, gender), but it's not worth the complexity.
        while (true)
        {
            var pid = LCRNG64.Next32(ref seed);

            // Force correlations
            if (isForcedGender) // assume gender in criteria is the specified one, and the species can be either gender.
            {
                pid = (pid & 0xFFFFFF00) | forceGender switch
                {
                    0 => gr + LCRNG64.NextRange(ref seed, 253u - gr + 1), // male
                    _ => 1 + LCRNG64.NextRange(ref seed, gr - 1u), // female
                };
            }

            if (type is Shiny.Never)
            {
                if (ShinyUtil.GetIsShiny3(id32, pid))
                    pid ^= 0x1000_0000; // force not shiny. the wild xor is never true.
            }
            else if (type is Shiny.Always)
            {
                // inlined GetShinyPID without ability force (done later)
                var gval = (byte)pid;
                var trainerXor = (id32 >> 16) ^ (ushort)id32;
                pid = ((gval ^ trainerXor) << 16) | gval;
            }

            if (isSingleAbility)
            {
                // force ability bit
                var bit = (ability == AbilityPermission.OnlySecond ? 1 : 0);
                var abilityBit = bit << 16;
                if ((pid & 0x1_0000) != abilityBit)
                    pid ^= 0x1_0000;
            }

            if (wildXor)
            {
                // great job checking the wrong bits to give 2x shiny chance for wild stuff.
                var corr = (pid >> 31) ^ (pid & 1) ^ bitXor;
                if (corr != 0)
                    pid ^= 0x8000_0000;
            }

            // Verify the result is what we want.
            var gender = EntityGender.GetFromPIDAndRatio(pid, gr);
            if (!isForcedGender && criteria.IsSpecifiedGender() && !criteria.IsSatisfiedGender(gender))
                continue;
            if (!isSingleAbility && criteria.IsSpecifiedAbility() && !criteria.IsSatisfiedAbility((int)(pid >> 16) & 1))
                continue;

            var xor = ShinyUtil.GetShinyXor(pid, id32);
            if (!IsShinyStateOK(criteria.Shiny, xor, type))
                continue;

            pk.PID = pid;
            pk.Gender = gender;
            break;
        }
    }

    /// <summary>
    /// Determines if the RNG can produce the maximum possible PID value under the specified conditions.
    /// </summary>
    /// <param name="wildXor">Whether the wild XOR modification is applied to the PID.</param>
    /// <param name="type">The desired shiny state.</param>
    /// <param name="ability">The required ability slot.</param>
    /// <param name="forceGender">The forced gender value, or GenderRandom if not forced.</param>
    /// <returns>True if maximum PID values are possible with the given parameters; false if gender is forced.</returns>
    public static bool CanBeMax(bool wildXor, Shiny type, AbilityPermission ability, byte forceGender = FixedGenderUtil.GenderRandom)
    {
        if (forceGender is not FixedGenderUtil.GenderRandom)
            return false;
        // need a lot more permutation thinking to actually verify this.
        return true;
    }

    /// <summary>
    /// Determines whether the given PID is valid for a forced gender assignment.
    /// </summary>
    /// <param name="pid">The Pokémon's personality value.</param>
    /// <param name="gender">
    /// The forced gender: 0 for male, 1 for female.
    /// </param>
    /// <returns>
    /// True if the PID's low byte is valid for the specified forced gender; otherwise, false.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the gender value is not 0 (male) or 1 (female).
    /// </exception>
    public static bool IsValidForcedRandomGender(uint pid, byte gender) => gender switch
    {
        0 => (pid & 0xFF) is not (254 or 255),
        1 => (pid & 0xFF) is not 0,
        _ => throw new ArgumentOutOfRangeException(nameof(gender), gender, null),
    };

    /// <summary>
    /// Determines if the given shiny XOR value satisfies the desired shiny state and encounter type.
    /// </summary>
    /// <param name="desired">The desired shiny state.</param>
    /// <param name="xor">The shiny XOR value to evaluate.</param>
    /// <param name="encType">The shiny state of the encounter.</param>
    /// <returns>True if the shiny state is valid for the given criteria; otherwise, false.</returns>
    private static bool IsShinyStateOK(Shiny desired, uint xor, Shiny encType) => desired switch
    {
        Shiny.AlwaysSquare when encType is not Shiny.Never && xor != 0 => false,
        Shiny.AlwaysStar when encType is not Shiny.Never && xor is >= 8 or 0 => false,
        Shiny.Never when encType is not Shiny.Always && xor < 8 => false,
        _ => true
    };

    /// <summary>
    /// Calculates a shiny PID based on the provided parameters.
    /// </summary>
    /// <param name="gval">The random gender value that gives the desired gender.</param>
    /// <param name="abilityBit">A bit indicating the desired ability slot. Must be 0 or 1. This affects the resulting PID's ability-related bit.</param>
    /// <param name="TID16">The 16-bit Trainer ID (TID) of the Pokémon's trainer.</param>
    /// <param name="SID16">The 16-bit Secret ID (SID) of the Pokémon's trainer.</param>
    public static uint GetShinyPID(uint gval, uint abilityBit, ushort TID16, ushort SID16)
    {
        uint pid = ((gval ^ TID16 ^ SID16) << 16) | gval;
        if ((pid & 0x10000) != (abilityBit & 1) << 16)
            pid ^= 0x10000;
        return pid;
    }
}

using System.Runtime.CompilerServices;

namespace PKHeX.Core;

/// <summary>
/// 64 Bit Linear Congruential Random Number Generator
/// </summary>
public static class LCRNG64
{
    public const ulong Mult  = 0x5D588B656C078965;
    public const ulong Add   = 0x0000000000269EC3;
    public const ulong rMult = 0xDEDCEDAE9638806D;
    public const ulong rAdd  = 0x9B1AE6E9A384E6F9;

    /// <summary>
/// Computes the next seed value in the 64-bit linear congruential random number sequence.
/// </summary>
/// <param name="seed">The current RNG seed.</param>
/// <returns>The next seed value.</returns>
[MethodImpl(MethodImplOptions.AggressiveInlining)] public static ulong Next(ulong seed) => (seed * Mult) + Add;
    /// <summary>
/// Computes the previous seed value in the 64-bit linear congruential RNG sequence.
/// </summary>
/// <param name="seed">The current RNG seed.</param>
/// <returns>The previous seed in the sequence.</returns>
[MethodImpl(MethodImplOptions.AggressiveInlining)] public static ulong Prev(ulong seed) => (seed * rMult) + rAdd;

    /// <summary>
/// Advances the referenced seed to the next state in the 64-bit LCRNG sequence and returns the updated seed.
/// </summary>
/// <param name="seed">The current RNG seed, passed by reference and updated in place.</param>
/// <returns>The next seed value in the sequence.</returns>
[MethodImpl(MethodImplOptions.AggressiveInlining)] public static ulong Next(ref ulong seed) => seed = Next(seed);
    /// <summary>
/// Updates the referenced seed to the previous value in the 64-bit LCRNG sequence and returns it.
/// </summary>
/// <param name="seed">The seed to update to its previous value.</param>
/// <returns>The previous seed value in the sequence.</returns>
[MethodImpl(MethodImplOptions.AggressiveInlining)] public static ulong Prev(ref ulong seed) => seed = Prev(seed);

    /// <summary>
/// Advances the seed to the next state and returns the upper 32 bits of the new seed as a 32-bit unsigned integer.
/// </summary>
/// <param name="seed">The current RNG seed, which will be updated to the next state.</param>
/// <returns>The upper 32 bits of the updated seed.</returns>
public static uint Next32(ref ulong seed) => (uint)(seed = Next(seed) >> 32);
    /// <summary>
/// Returns a pseudo-random unsigned integer in the range [0, exclusiveMax) using the next state of the given seed.
/// </summary>
/// <param name="seed">The current RNG seed value.</param>
/// <param name="exclusiveMax">The exclusive upper bound for the generated random number.</param>
/// <returns>A pseudo-random value greater than or equal to 0 and less than exclusiveMax.</returns>
public static uint NextRange(ulong seed, uint exclusiveMax) => (uint)(((Next(seed) >> 32) * exclusiveMax) >> 32);
    /// <summary>
/// Advances the seed to the next value and returns a uniformly distributed random number in the range [0, exclusiveMax).
/// </summary>
/// <param name="seed">The current RNG seed, which will be updated to the next state.</param>
/// <param name="exclusiveMax">The exclusive upper bound for the random number.</param>
/// <returns>A random unsigned integer greater than or equal to 0 and less than exclusiveMax.</returns>
public static uint NextRange(ref ulong seed, uint exclusiveMax) => (uint)((((seed = Next(seed)) >> 32) * exclusiveMax) >> 32);
}

# Why Your Raid Pokémon Needs a Valid Seed: A Beginner's Guide

## Introduction: What's the Big Deal About Seeds?

Imagine you're baking cookies. If you want chocolate chip cookies, you need the right recipe and ingredients. You can't just throw random stuff in the oven and expect perfect cookies to come out! 

Raid Pokémon work the same way. Every raid Pokémon in Sword/Shield and Scarlet/Violet is created from a special "recipe" called a **seed**. This seed is like a secret code that determines EVERYTHING about the Pokémon - its stats, nature, ability, whether it's shiny, and more.

## The Cookie Recipe Analogy

Let's stick with our cookie analogy to understand why you can't just pick any stats you want:

🍪 **The Recipe (Seed)**: A specific set of instructions
- 2 cups flour → determines IVs
- 1 cup sugar → determines nature  
- 1 tsp vanilla → determines ability
- Chocolate chips → determines if shiny

If you change even ONE ingredient amount, you get different cookies! Similarly, each seed produces ONE specific combination of Pokémon traits. You can't mix and match traits from different seeds.

## How the Game Creates Raid Pokémon

When you encounter a raid den in your game:

1. **The game picks a seed** (like picking recipe #7,239,451 from a cookbook with 4 billion recipes)
2. **The seed goes through a math formula** (like following the recipe steps)
3. **Out pops your Pokémon** with specific stats determined by that seed

Here's the important part: The game ALWAYS follows these steps in the EXACT SAME ORDER:
- First: Encryption Constant (EC) 
- Second: Process ID (PID) which affects shininess
- Third: IVs (Individual Values - the Pokémon's stats)
- Fourth: Ability
- Fifth: Gender
- Sixth: Nature
- Seventh: Height/Weight

It's like a factory assembly line - you can't skip steps or do them out of order!

## Why Showdown Sets Often Don't Work

When people create Pokémon in Showdown (the battle simulator), they pick whatever stats they want:
- "I want 31/0/31/31/31/31 IVs"
- "I want it to be shiny"
- "I want Adamant nature"
- "I want Hidden Ability"

But here's the problem: **There might not be ANY seed that produces this exact combination!**

It's like saying "I want cookies that are chocolate chip, sugar cookie, AND oatmeal raisin all at once" - that recipe doesn't exist!

## Real Example: The Impossible Shiny

Let's say you want a shiny Charizard from a raid with these exact IVs: 31/31/31/31/31/31 (perfect).

The game would need to:
1. Find a seed that makes it shiny (only about 1 in 4,096 seeds do this)
2. That SAME seed must also generate perfect IVs
3. That SAME seed must also give the nature you want
4. That SAME seed must also give the ability you want

The chances of one seed doing ALL of this? Almost impossible! It's like winning the lottery while getting struck by lightning!

## How PKHeX Checks Your Pokémon

PKHeX is like a detective that can reverse-engineer the recipe. When you import a Pokémon, it:

1. **Looks at the Encryption Constant** - "Okay, this tells me which seeds COULD have made this"
2. **Checks the PID** - "Do any of those seeds also produce this PID? No? FAKE!"
3. **Verifies the IVs** - "Would this seed generate these exact IVs? No? FAKE!"
4. **Confirms everything else** - Nature, ability, gender, size values

If even ONE thing doesn't match what the seed would produce, PKHeX knows it's illegal!

## Generation Differences

### Sword/Shield (Gen 8) Raids
- Uses 64-bit seeds (huge number: 18,446,744,073,709,551,616 possibilities!)
- Different star ratings = different guaranteed perfect IVs
  - 1-star raid = 1 perfect IV guaranteed
  - 5-star raid = 4-5 perfect IVs guaranteed
- Can be shiny or not based on your Trainer ID

### Scarlet/Violet (Gen 9) Tera Raids
- Uses 32-bit seeds (smaller: 4,294,967,296 possibilities)
- Has additional Tera Type to validate
- 6-star and 7-star raids ALWAYS have perfect IVs (31 in all stats)
- Some raids are shiny-locked (can NEVER be shiny)

## How to Generate Legal Raid Pokémon

Instead of trying to create impossible combinations, use the seed finder plugins:

### 🎉 GOOD NEWS: If You Have PKHeX-All-In-One

**If you're using PKHeX-All-In-One (this repository), the plugins are ALREADY INCLUDED!** You can skip installation and jump straight to using them:
- For Scarlet/Violet: Go to Tools → SV Seed Finder
- For Sword/Shield: Go to Tools → Gen 8 Raid Seed Finder

### If You're Using Regular PKHeX

You'll need to install the plugins separately:

#### For Scarlet/Violet:
1. **Download the SV Seed Finder Plugin**: [https://github.com/hexbyt3/SVSeedFinderPlugin](https://github.com/hexbyt3/SVSeedFinderPlugin)
2. **Install it**:
   - Download the .dll file from the releases page
   - Put it in PKHeX's `plugins` folder
   - Restart PKHeX
3. **Use it**:
   - Go to Tools → SV Seed Finder
   - Pick your Pokémon species
   - Set REASONABLE criteria (don't ask for perfect everything!)
   - Let it search for valid seeds
   - Double-click a result to generate that exact Pokémon

#### For Sword/Shield:
1. **Download the SWSH Seed Finder Plugin**: [https://github.com/hexbyt3/SWSHSeedFinderPlugin](https://github.com/hexbyt3/SWSHSeedFinderPlugin)
2. **Install it**:
   - Download the .dll file from the releases page
   - Put it in PKHeX's `plugins` folder
   - Restart PKHeX
3. **Use it**:
   - Go to Tools → Gen 8 Raid Seed Finder
   - Pick your Pokémon species
   - Choose den type (Normal, Crystal, Event, Max Lair)
   - Set your criteria
   - Search and generate!

## Step-by-Step: Finding Your Perfect (Legal) Pokémon

Let's find a shiny Pikachu in Scarlet/Violet:

1. **Open PKHeX** (or PKHeX-All-In-One) and load your save file
2. **Open the SV Seed Finder** (Tools menu)
3. **Type "Pikachu"** in the species box
4. **Set your wishes** (but be reasonable!):
   - Shiny: Yes
   - Nature: Timid
   - IVs: Leave some flexibility (don't demand all perfect)
5. **Click Search** and wait
6. **Pick a result** that looks good
7. **Double-click it** to load into PKHeX
8. **Done!** You have a 100% legal raid Pikachu

## Common Mistakes to Avoid

❌ **DON'T**: Try to create a Pokémon with any random stats you want
✅ **DO**: Use seed finders to find legal combinations

❌ **DON'T**: Assume all IV spreads are possible for all Pokémon
✅ **DO**: Understand that each seed limits what's possible

❌ **DON'T**: Mix traits from different seeds
✅ **DO**: Accept that each seed produces ONE specific Pokémon

❌ **DON'T**: Ignore shiny locks (some raids can NEVER be shiny)
✅ **DO**: Check if the raid can actually be shiny first

## Understanding the Technical Stuff (Simple Version)

Think of it like a combination lock:
- The **seed** is the combination (like 12-34-56)
- Each number must be turned in order
- Each turn produces a specific result
- You can't get different results from the same combination
- You can't work backwards and pick your results first

The game uses something called **Xoroshiro128+** (for Gen 8) or **Xoroshiro128Plus** (for Gen 9). Don't worry about the name - just know it's a special math formula that:
- Takes a seed number
- Does math to it
- Spits out your Pokémon's traits IN ORDER
- Always gets the same result from the same seed

## Why This Matters

Nintendo and Game Freak designed this system to:
1. **Prevent cheating** - You can't just make up impossible Pokémon
2. **Keep raids fair** - Everyone has the same chances
3. **Make shinies special** - They're rare because few seeds produce them
4. **Ensure authenticity** - Legal Pokémon can be verified

## Quick Reference Guide

### Want a Legal Raid Pokémon?
1. Use the seed finder plugins
2. Search with reasonable criteria
3. Pick from actual results
4. Generate in PKHeX

### Red Flags Your Pokémon is Illegal:
- PKHeX shows a red ❌ symbol
- "Invalid: Unable to match encounter conditions"
- "Invalid: PID-IV correlation"
- "Invalid: Encryption Constant mismatch"

### Making it Legal:
- You CAN'T just edit values to fix it
- You MUST find a seed that produces what you want
- Use the plugins - they do the hard work for you!

## Final Words: Work WITH the System, Not Against It

Remember our cookie analogy? You can't force the universe to create a cookie recipe that doesn't exist. Similarly, you can't force the game to accept a Pokémon combination that no seed can produce.

The seed finder plugins are like having a cookbook with 4 billion recipes - surely you can find one you like! Don't try to invent impossible recipes; instead, search through the real ones until you find something close to what you want.

The plugins make this easy:
- They search millions of seeds per minute
- They show you only legal combinations
- They let you generate perfect, legal Pokémon
- They save you from the frustration of illegal Pokémon

## Remember This!

**Every raid Pokémon is like a lottery ticket** - the seed is the ticket number, and it determines ALL the prizes you get. You can't mix and match prizes from different tickets!

Use the seed finders, be patient, and you'll get legal Pokémon that work everywhere - in battles, in Pokémon HOME, in online play, everywhere!

---

*Happy hunting, trainers! Remember: Legal Pokémon = Happy Pokémon (and happy trainers who don't get banned)!* 🎮✨
PKHeX-All-In-One
=====
![License](https://img.shields.io/badge/License-GPLv3-blue.svg)
![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/hexbyt3/PKHeX-ALL-IN-ONE/total?color=violet)
<div>
  <span>English</span> / <a href=".github/README-es.md">Español</a> / <a href=".github/README-fr.md">Français</a> / <a href=".github/README-de.md">Deutsch</a> / <a href=".github/README-it.md">Italiano</a> / <a href=".github/README-zh-Hant.md">繁體中文</a> / <a href=".github/README-zh-Hans.md">简体中文</a>
</div>

# About this Fork
This is a fork of [PKHeX](https://github.com/kwsch/PKHeX) that is suited to my needs and my community/friends needs.  It has the most popular PKHeX plugins automatically integrated and loaded so that inexperienced users can quickly start using PKHeX without the hassle of figuring out how to clone/build plugins, etc. 

**Please do NOT ask for support on the official PKHeX Discord Server (PDP) or their GitHub.  If you have an issue, please join the appropriate Discord server found below.**

Current Loaded Plugins:
* [AutoLegalityMod](https://github.com/hexbyt3/ALM4SysBot) - for showdown imports and legalizing.  It also includes LiveHex which is a great way to instantly connect to your modded switch and change various things on your save file.
* [SWSHSeedFinderPlugin](https://github.com/hexbyt3/SWSHSeedFinderPlugin) - A custom plugin I built to quickly and effectively search for specific seeds from Raids and generate legal pokemon with a few clicks for Sword and Shield.
* [SVSeedFinderPlugin](https://github.com/hexbyt3/SVSeedFinderPlugin) - Same as above, just for Scarlet/Violet.
* [TeraFinder](https://github.com/Manu098vm/Tera-Finder) - A Scarlet/Violet plugin for PKHeX specializing in various Tera Raid functions.
* [PluginPile](https://github.com/foohyfooh/PKHeXPluginPile) - Tons of various plugins for PKHeX for various different games.

# Added Features
* Dark Theme - Integrates into all menus/submenus, including custom plugins.
* Live Dex Generator - A new feature that is still being worked on that allows users to complete their dex quicker.  Tools > Data > Live Dex Builder
* Other misc. features I can't remember off the top of my head right now.

# Discord Support Server
Join SYNC (SysBot Network Collective) for support regarding this Repository.

[<img src="https://canary.discordapp.com/api/guilds/1369342739581505536/widget.png?style=banner2">](https://discord.gg/WRs22V6DgE)

# Special Thanks
Thanks to Kswch and all the contributors for their constant updates and maintaining this program and the plugins mentioned above.

# Information
Pokémon core series save editor, programmed in [C#](https://en.wikipedia.org/wiki/C_Sharp_%28programming_language%29).

Supports the following files:
* Save files ("main", \*.sav, \*.dsv, \*.dat, \*.gci, \*.bin)
* GameCube Memory Card files (\*.raw, \*.bin) containing GC Pokémon savegames.
* Individual Pokémon entity files (.pk\*, \*.ck3, \*.xk3, \*.pb7, \*.sk2, \*.bk4, \*.rk4)
* Mystery Gift files (\*.pgt, \*.pcd, \*.pgf, .wc\*) including conversion to .pk\*
* Importing GO Park entities (\*.gp1) including conversion to .pb7
* Importing teams from Decrypted 3DS Battle Videos
* Transferring from one generation to another, converting formats along the way.

Data is displayed in a view which can be edited and saved.
The interface can be translated with resource/external text files so that different languages can be supported.

Pokémon Showdown sets and QR codes can be imported/exported to assist in sharing.

PKHeX expects save files that are not encrypted with console-specific keys. Use a savedata manager to import and export savedata from the console ([Checkpoint](https://github.com/FlagBrew/Checkpoint), save_manager, [JKSM](https://github.com/J-D-K/JKSM), or SaveDataFiler).

**We do not support or condone cheating at the expense of others. Do not use significantly hacked Pokémon in battle or in trades with those who are unaware hacked Pokémon are in use.**

## Screenshots
![image](https://github.com/user-attachments/assets/af8137e4-998c-4425-98d0-5404e06d045d)


## Building

PKHeX-All-In-One is a Windows Forms application which requires [.NET 9.0](https://dotnet.microsoft.com/download/dotnet/9.0).

The executable can be built with any compiler that supports C# 13.

### Build Configurations

Use the Debug or Release build configurations when building. There isn't any platform specific code to worry about!

## Dependencies

PKHeX's QR code generation code is taken from [QRCoder](https://github.com/codebude/QRCoder), which is licensed under [the MIT license](https://github.com/codebude/QRCoder/blob/master/LICENSE.txt).

PKHeX's shiny sprite collection is taken from [pokesprite](https://github.com/msikma/pokesprite), which is licensed under [the MIT license](https://github.com/msikma/pokesprite/blob/master/LICENSE).

PKHeX's Pokémon Legends: Arceus sprite collection is taken from the [National Pokédex - Icon Dex](https://www.deviantart.com/pikafan2000/art/National-Pokedex-Version-Delta-Icon-Dex-824897934) project and its abundance of collaborators and contributors.

### IDE

PKHeX can be opened with IDEs such as [Visual Studio](https://visualstudio.microsoft.com/downloads/) by opening the .sln or .csproj file.

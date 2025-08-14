# Dragon Industries - ANEXCAL Relations



## Description

This is the core function library used by my other mods. You can also build your own mods on top of it. It offers the following functionality:
* A simpler much-more-automated process for adding custom items, techs, recipes, and machines, including some specialized templates
* New game lifecycle callbacks
* Automatic cleanup and synchronization of game and save data when mod data changes
* A very large library of helper methods

DI also has a couple features in its own right, including an optional boost to ore smelting efficiency and making the "X100" colorless cube distinct: ![](https://i.imgur.com/TrXlBqB.png)

It also has functions accessible via REPL for setting the range of effect of the MassScan mod at runtime, as well as toggling protection zones on or off.


## Installation

If you have not already done so for another mod, follow these instructions:
1. Find your game install folder.
2. Open BepInEx/config/BepInEx.cfg and enable "HideGameManagerObject"
3. Unzip the contents of this zip into Bepinex/plugins or a subfolder thereof (subfolders recommended for organization)
4. (Optional) Change config options in the XML file in the Config folder of the unpacked files.

## Notes

* New games must be loaded, saved, and reloaded for mods to take effect.
* This mod, as always, is "use at your own risk". Even under the best of circumstances, let alone cases like incorrect installs or unintentional mod errors, save files become dependent on and can be corrupted by mods used within.
* While the author of this mod owns all copyright of its custom code, Fire Hose games owns Techtonica and all code and assets therein, and assets derived from Techtonica assets are not solely the creation or property of the mod author.


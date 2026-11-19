Example mod: https://github.com/SierraFrey/NineSolsAPI-CustomFlagManager-ExampleMod

There was an idea for a mod I had that would use custom data in a save file, but I realized there's some flaws with it. There's no conventional way of distinguishing which save files have modded data and what mod data they contain.
This is my attempt at fixing these problems. Current features include:

Methods that help with creating new save flags. They must be called before a file is loaded/created.
  
- CreateScriptableDataBool()

- CreateGameFlagInt()

- CreateScriptableDataFloat()

- CreateGameFlagString()


The methods will need to be called from a CustomFlagManager instance. The methods require a unique name for the flag and optionally a default value. The API does the rest for you.


A list that shows which mods have data in a save file when selecting it. Clicking on a mod in a list can take you to a download page if it has one.

Warnings for when you attempt to load a save when you don't have a mod in the list, you have a different version of a mod in the list, or you have a mod not in the list.

The list can be disabled in the config settings.

CustomFlagManager instances can be toggle on or off with the "enabled" variable. It has some strict rules:

Toggling is only allowed outside of gameplay. (at startup or title screen)

Instance disables itself when playing without the instance adding any flags.

Instance enables itself when loading a save that contains data of that instance.

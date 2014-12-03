KeySAV2
=======

KeySAV 2.0

3DS X/Y/OR/AS SaveFile Box / Battle Video PKX cracking program.

KeySAV is a tool that can decrypt portions of the X/Y title/extdata from a Digital Game / PowerSaves/CyberGadget rip. 
With this tool you can view IVs/Nature/ESV/TSV of Hatched/Unhatched Pokémon that are in your boxes or battle videos, so long as you set everything up properly.

SUPPORTS BOTH RETAIL CARTRIDGES AND DIGITAL COPIES

SD Card Reader or PowerSaves/CyberGadget game reader required.

Thread:
http://projectpokemon.org/forums/showthread.php?41752

Changelog
=========
KeySAV2 1.3.2
- Added "Egg Received" and "Met/Hatched" columns
- Fixed legacy break help box

KeySAV2 1.3.1
- Added option to use legacy save-twice file-breaking behavior (click "What's this?" in the app for more info)

KeySAV2 1.3
* Added available columns: Level, Region, Country, Held Item, Language, Game, Number, PID, Mark, Dex Number, Form
* Fixed "mark" - pentagon mark is now present for all Pokemon created in generation 6
* Fixed "Number" column to be 1-indexed
* Fixed help box to be resizable and able to be kept open while working in the main window
* Added ORAS game support for "Game" and "Region" values
* Fixed superfluous spaces in CSV data
* Added perfect IV marker columns
* Added perfect IV counter column
* Added IV and EV sum columns
* Changed pk6 file output to be in box order rather than species order (for better PkHex support)
* Code refactoring (combined dumpPKX_BV and dumpPKX_SAV)
* Added option to show ESV for hatched Pokemon
* Added option to enclose trainer and Pokemon nicknames in quotes for CSV exports (to prevent problems with commas in either)
# Instructions:

[Click Here](https://github.com/ArsonAssassin/Archipelago.Core/wiki/How-to-start-playing-a-game-using-this-library) for
general instructions.

## Playing a Game with Crash 2

### Required Software

Important: As the mandatory client runs only on Windows, no other systems are supported.

- [Duckstation](https://www.duckstation.org) - Detailed installation instructions for Duckstation can be found at the above link.
- Archipelago version 0.6.5 or later.
- The [Crash 2 Archipelago Client and .apworld](https://github.com/Sim10XXX/C2AP/releases)
- A legal Crash Bandicoot 2 Europe (PAL) ROM.  We cannot help with this step.
### Create a Config (.yaml) File

#### What is a config file and why do I need one?

See the guide on setting up a basic YAML at the Archipelago setup guide: [Basic Multiworld Setup Guide](https://archipelago.gg/tutorial/Archipelago/setup_en)

This also includes instructions on generating and hosting the file.  The "On your local installation" instructions
are particularly important.

#### Where do I get a config file?

Run `ArchipelagoLauncher.exe` and generate template files.  Copy `Crash 2.yaml`, fill it out, and place
it in the `players` folder.

### Generate and host your world

Run `ArchipelagoGenerate.exe` to build a world from the YAML files in your `players` folder.  This places
a `.zip` file in the `output` folder.

You may upload this to [the Archipelago website](https://archipelago.gg/uploads) or host the game locally with
`ArchipelagoHost.exe`.

### Setting Up Crash 2 for Archipelago

1. Download the C2AP.Desktop.exe and crash2.apworld from the GitHub page linked above.
2. Double click the apworld to install to your Archipelago installation.
3. Open Duckstation and load into Crash 2.
4. In Duckstation, navigate to Settings > Game Properties > Console and select "Interpreter" under "Execution Mode".
5. Make sure that Runahead (inside of Emulation Settings) is disabled
6. Start a new game (the client will restore your progress if you are continuing an existing seed).
7. Open C2AP.Desktop.exe, the Crash 2 client.  You will likely want to do so as an administrator.
8. In the top left of the Crash 2 client, click the "burger" menu to open the settings page.
9. Enter your host, slot, and optionally your password.
10. Click Connect. The first time you connect, a few error messages may appear - these are okay.
11. Start playing!

## What does randomization do to this game?

When the player completes a task (such as collecting a crystal or gem), an item is sent.
Collecting one of these may not increment the player's crystal/gem counter,
while a check received from another game may do so.

This does not randomize the location of crystals or gems.

Unlocking warp rooms requires collecting enough crystal items through Archipelago, and unlocking colored gem platforms requires collecting the respective colored gem through Archipelago.  Unlike the vanilla game, you may not need to complete
the crystal check for every level to advance. The in-game pause menu keeps track what items you have received and the warp room crytals/gems keep track of what locations you have checked

## What items and locations get shuffled?
Crystals and gems are always shuffled
lives and wumpa fruit will be added as "filler" based on the player's options into the item pool

Depending on what randomization options are selected, traps and other unlocks can also be put into the item pool

## Which items can be in another player's world?

Any of the items which can be shuffled may also be placed into another player's world.

## What does another world's item look like in Crash 2?

The visuals of the game are unchanged by the Archipelago randomization.  The Crash 2 Archipelago Client
will display the obtained item and to whom it belongs.

## When the player receives an item, what happens?

The player's game, HUD, and/or pause menu will update accordingly

## Unique Local Commands

The following command (without a slash or exclamation point) is available when using the C2AP client to play with Archipelago.

- `useQuietHints` Suppresses hints for found locations to make the client easier to read. On by default.
- `useVerboseHints` Include found locations in hint lists. Due to Archipelago Server limitations, only applies to hints requested after this change.

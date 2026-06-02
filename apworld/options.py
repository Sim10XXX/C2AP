from dataclasses import dataclass

from Options import Toggle, DefaultOnToggle, Option, Range, Choice, ItemDict, OptionList, DeathLink, PerGameCommonOptions
from Options import OptionGroup, OptionSet


# In this file, we define the options the player can pick.
# The most common types of options are Toggle, Range and Choice.

# Options will be in the game's template yaml.
# They will be represented by checkboxes, sliders etc. on the game's options page on the website.
# (Note: Options can also be made invisible from either of these places by overriding Option.visibility.
#  APQuest doesn't have an example of this, but this can be used for secret / hidden / advanced options.)

# For further reading on options, you can also read the Options API Document:
# https://github.com/ArchipelagoMW/Archipelago/blob/main/docs/options%20api.md


# The first type of Option we'll discuss is the Toggle.
# A toggle is an option that can either be on or off. This will be represented by a checkbox on the website.
# The default for a toggle is "off".
# If you want a toggle to be on by default, you can use the "DefaultOnToggle" class instead of the "Toggle" class.

class LevelExitLocations(Toggle):
    """
    Add all 27 level exits as checks into the pool
    These are distinct from the secret exit checks
    """
    display_name = "Level Exit Locations"

class AddExtraCrystals(Range):
    """
    Add extra crystals to the pool
    Note: adding crystals can cause there to be more items than locations, and some clear gems will be unplaced
    """
    display_name = "Add Extra Crystals"

    range_start = 0
    range_end = 64-25
    default = 0

class SpeedrunLogic(OptionSet):
    """
    This option lets you choose to enable any of a few implemented speedrun skips.
    For any that you enable, you may be required to do it in your seed, depending on how
    the randomization plays out. List them using the names provided below.

    Supported tricks:
    red_gem_early: get the red gem in Snow Go without using the secret entrance.
    road_to_ruin_gem: get the secret entrance gem in Road to Ruin without using the secret entrance.
    ruination_skip_green: access the green gem path in Ruination without needing the green gem.
    """
    display_name = "Speedrun Logic"
    valid_keys = ["red_gem_early", "road_to_ruin_gem", "ruination_skip_green"]
    default = []


class WumpaFruitChance(Range):

    """
    Chance that a filler item is a single wumpa fruit instead of a life
    """
    display_name = "Wumpa Fruit Chance"

    range_start = 0
    range_end = 100
    default = 80

class FruitSanity(Choice):
    """
    Adds wumpa fruit checks
    This applies only to free-standing wumpa fruit (not fruit spawned from boxes/enemies)
    Full sanity adds a total of 2395 individual wumpa checks to the game
    Fruit bundles condenses this into 404 grouped wumpa checks
    """
    display_name = "Fruit-Sanity"

    option_disabled = 0
    option_fruit_bundles = 1
    option_full_sanity = 2

    default = option_disabled
class ExcludeDifficultWumpas(DefaultOnToggle):
    """
    Excludes particularly difficult/annoying wumpa fruit locations, which makes it so they won't contain useful/progression items
    The specific location locations affected by this option are marked with a "*" in their name
    """
    display_name = "Exclude Difficult Wumpas"
class FillWumpaChecksLocally(Range):
    """
    % chance to pre-emptively fill wumpa checks with a Crash 2 filler item
    The purpose of this is to prevent games with fewer checks from being clogged with Crash 2 filler
    This runs before the multiworld item placement algorithm and serves as a better alternative to using the default local_items option
    You could also set this option to 100% and have traps enabled if you want an "avoid the wumpa fruit" gamemode
    """
    display_name = "Fill Wumpa Checks Locally Chance"

    range_start = 0
    range_end = 100
    default = 70

class RandomizeWarpDestinations(Toggle):
    """
    Randomize the destination of the 25 basic warp portals in the Warp Room.
    This currently does not include the secret warp room.
    Bosses are not randomized.
    """
    display_name = "Randomize Warp Destinations"

class NonRandomizedWarpDestinations(OptionList):
    """
    If "randomize_warp_destinations" is enabled, any levels placed in this list will retain their original position in the Warp Room.
    You must specify the secret entrances separately.
    For example, "Snow Go (Secret Entrance)" will keep the Snow Go secret entrance portal as the second portal in the secret warp room.
    """
    # todo: decouple secret entrances
    display_name = "Non-Randomized Warp Destinations"
    valid_keys = [
        "Turtle Woods",
        "Snow Go",
        "Snow Go (Secret Entrance)",
        "Hang Eight",
        "The Pits",
        "Crash Dash",
        "Snow Biz",
        "Air Crash",
        "Air Crash (Secret Entrance)",
        "Bear It",
        "Crash Crush",
        "The Eel Deal",
        "Plant Food",
        "Sewer or Later",
        "Bear Down",
        "Road to Ruin",
        "Road to Ruin (Secret Entrance)",
        "Un-Bearable",
        "Hangin' Out",
        "Diggin' It",
        "Cold Hard Crash",
        "Ruination",
        "Bee-Having",
        "Piston it Away",
        "Rock It",
        "Night Fight",
        "Pack Attack",
        "Spaced Out",
        "Totally Fly",
        "Totally Bear"
    ]

class TrapChance(Range):

    """
    Chance to replace a filler life/fruit with a trap
    If playing with fruit sanity you should lower this chance because there will be a LOT of traps
    """
    display_name = "Trap Chance"

    range_start = 0
    range_end = 100
    default = 0

class TrapDuration(Range):
    """
    Amount of seconds a trap will be active for
    Receiving a trap when it was already active will reset the duration
    """
    display_name = "Trap Duration"

    range_start = 0
    range_end = 600
    default = 30

class SmallCrashTrapWeight(Range):

    """
    Relative chance for a trap to be a Small Crash Trap
    This makes Crash smol
    (Due to an issue with Polar + small Crash, this trap will be inactive in bear levels)
    Chance for a specific trap to be picked is weight / totalWeight
    """
    display_name = "Small Crash Trap Weight"

    range_start = 0
    range_end = 100
    default = 10

# class SmallCrashSize(Range):
#
#     """
#     For testing purposes, this option will be removed after pre-release
#     Extreme values will likely be unplayable
#     This value is read as a percent, so normal crash is size 100
#     """
#     display_name = "Small Crash Size"
#
#     range_start = 0
#     range_end = 10000
#     default = 33

class BigCrashTrapWeight(Range):

    """
    Relative chance for a trap to be a Big Crash Trap
    This makes Crash beeg
    """
    display_name = "Big Crash Trap Weight"

    range_start = 0
    range_end = 100
    default = 10

# class BigCrashSize(Range):
#
#     """
#     For testing purposes, this option will be removed after pre-release
#     Extreme values will likely be unplayable
#     This value is read as a percent, so normal crash is size 100
#     """
#     display_name = "Big Crash Size"
#
#     range_start = 0
#     range_end = 10000
#     default = 150

class NoLivesTrapWeight(Range):
    """
    Relative chance for a trap to be a No Lives Trap
    This temporarily removes all your lives
    """
    display_name = "No Lives Weight"

    range_start = 0
    range_end = 100
    default = 10

class JetpackControlsTrapWeight(Range):
    """
    Relative chance for a trap to be a Jetpack Controls Trap
    Just imagine you are in a jetpack level
    """
    display_name = "Jetpack Controls Weight"

    range_start = 0
    range_end = 100
    default = 10

# # We must now define a dataclass inheriting from PerGameCommonOptions that we put all our options in.
# # This is in the format "option_name_in_snake_case: OptionClassName".
@dataclass
class Crash2Options(PerGameCommonOptions):
    level_exit_locations: LevelExitLocations
    extra_crystals: AddExtraCrystals
    speedrun_logic: SpeedrunLogic
    death_link: DeathLink
    wumpa_chance: WumpaFruitChance
    fruit_sanity: FruitSanity
    exclude_difficult_wumpas: ExcludeDifficultWumpas
    fill_wumpa_checks_locally_chance: FillWumpaChecksLocally
    randomize_warp_destinations: RandomizeWarpDestinations
    non_randomized_warp_destinations: NonRandomizedWarpDestinations
    trap_chance: TrapChance
    trap_duration: TrapDuration
    small_crash_weight: SmallCrashTrapWeight
    # small_crash_size: SmallCrashSize
    big_crash_weight: BigCrashTrapWeight
    # big_crash_size: BigCrashSize
    no_lives_weight: NoLivesTrapWeight
    jetpack_controls_weight: JetpackControlsTrapWeight

#
# # If we want to group our options by similar type, we can do so as well. This looks nice on the website.
option_groups = [
    OptionGroup(
        "Fruitsanity",
        [FruitSanity, ExcludeDifficultWumpas, FillWumpaChecksLocally],
    ),
    OptionGroup(
        "Warp Randomizer",
        [RandomizeWarpDestinations, NonRandomizedWarpDestinations],
    ),
    OptionGroup(
        "Trap Options",
        [TrapChance, TrapDuration, SmallCrashTrapWeight, BigCrashTrapWeight, NoLivesTrapWeight, JetpackControlsTrapWeight],
    ),
]

# Finally, we can define some option presets if we want the player to be able to quickly choose a specific "mode".
# option_presets = {
#     "boring": {
#         "hard_mode": False,
#         "hammer": False,
#         "extra_starting_chest": False,
#         "start_with_one_confetti_cannon": False,
#         "trap_chance": 0,
#         "confetti_explosiveness": ConfettiExplosiveness.range_start,
#         "player_sprite": PlayerSprite.option_human,
#     },
#     "the true way to play": {
#         "hard_mode": True,
#         "hammer": True,
#         "extra_starting_chest": True,
#         "start_with_one_confetti_cannon": True,
#         "trap_chance": 50,
#         "confetti_explosiveness": ConfettiExplosiveness.range_end,
#         "player_sprite": PlayerSprite.option_duck,
#     },
# }

from collections.abc import Mapping
from collections import defaultdict
from typing import Any

import worlds.tunic.ut_stuff
# Imports of base Archipelago modules must be absolute.
from worlds.AutoWorld import World, WebWorld

# Imports of your world's files must be relative.
from . import items, locations, regions, rules, randomize_warp#, web_world
from . import options as crash2_options  # rename due to a name conflict with World.options

# APQuest will go through all the parts of the world api one step at a time,
# with many examples and comments across multiple files.
# If you'd rather read one continuous document, or just like reading multiple sources,
# we also have this document specifying the entire world api:
# https://github.com/ArchipelagoMW/Archipelago/blob/main/docs/world%20api.md


# The world class is the heart and soul of an apworld implementation.
# It holds all the data and functions required to build the world and submit it to the multiworld generator.
# You could have all your world code in just this one class, but for readability and better structure,
# it is common to split up world functionality into multiple files.
# This implementation in particular has the following additional files, each covering one topic:
# regions.py, locations.py, rules.py, items.py, options.py and web_world.py.
# It is recommended that you read these in that specific order, then come back to the world class.
class Crash2WebWorld(WebWorld):

    # We need to override the "game" field of the WebWorld superclass.
    # This must be the same string as the regular World class.
    game = "Crash2"

    theme = "grass"

    option_groups = crash2_options.option_groups

def setup_groups():
    # item groups hardcoded since there's not many
    item_name_groups = {
        "Colored Gems": {"Blue Gem", "Yellow Gem", "Red Gem", "Purple Gem", "Green Gem"},
        "Filler": {"Life", "Wumpa Fruit"},
        "Traps": {"Big Crash Trap", "Small Crash Trap", "No Lives Trap", "Jetpack Controls Trap"}
    }

    # the below creates the following:
    # 1) individual level groups (e.g. Turtle Woods, Totally Bear) which contain the crystal, all gems, and all exits for each level
    # 2) per-warp room groups (e.g. Warp 1 Crystals, Warp 4 Gems, Warp 5 Regular Exits). Didn't do per-warp room secret exits since there's only 5 across the whole game
    # 3) full warp room groups (e.g. Warp 1 (All), Warp 2 (All)) which contain all crystals, gems, and exits for each
    # 3) miscellaneous: all regular exits, all secret exits, all bosses, all colored gems
    location_name_groups = defaultdict(set)
    location_name_groups["All Bosses"] = {"Ripper Roo Defeated", "Komodo Brothers Defeated", "Tiny Tiger Defeated", "Dr. N. Gin Defeated"}

    for loc in locations.LOCATION_NAME_TO_ID.keys():
        ind = loc.find(":")
        if ind == -1:  # locations without a : are either bosses (dealt with above) or the polar secret which is ungrouped
            continue

        level_name = loc[:ind]  # level names are before the colon
        warp = locations.level_lookup[level_name]  # level_lookup is pre-set up with level names -> warp
        location_name_groups[level_name].add(loc)  # per-level groups
        location_name_groups[f"{warp} (All)"].add(loc)  # per-warp room groups

        if "Gem" in loc and "Clear" not in loc:  # didn't feel particularly justified to rename these locations to make this easier
            location_name_groups["All Colored Gems"].add(loc)

        # All <type> groups
        for group in ["Crystal", "Clear Gem", "Secret Exit", "Regular Exit"]:
            if group in loc:
                location_name_groups[f"All {group}s"].add(loc)

        # crystals, gems, and regular exits have same logic. Just check if those words are in the location, ezpz
        for group in ["Crystal", "Gem", "Regular Exit"]:
            if group in loc:
                location_name_groups[f"{warp} {group}s"].add(loc)

    return item_name_groups, location_name_groups

class Crash2World(World):
    """
    Crash Bandicoot 2: Cortex Strikes Back
    """

    # The docstring should contain a description of the game, to be displayed on the WebHost.

    # You must override the "game" field to say the name of the game.
    game = "Crash2"
    web = Crash2WebWorld()
    # The WebWorld is a definition class that governs how this world will be displayed on the website.
    #web = web_world.APQuestWebWorld()

    # This is how we associate the options defined in our options.py with our world.
    # (Note: options.py has been imported as "apquest_options" at the top of this file to avoid a name conflict)
    options_dataclass = crash2_options.Crash2Options
    options: crash2_options.Crash2Options  # Common mistake: This has to be a colon (:), not an equals sign (=).

    # Our world class must have a static location_name_to_id and item_name_to_id defined.
    # We define these in regions.py and items.py respectively, so we just set them here.
    locations.prepare_fruit_sanity()
    location_name_to_id = locations.LOCATION_NAME_TO_ID
    item_name_to_id = items.ITEM_NAME_TO_ID
    item_name_groups, location_name_groups = setup_groups()

    # warp room level layout
    warp_room: list[int] = randomize_warp.warpRoomLevelIds
    secret_warp_room_levels = []
    secret_warp_room_entrance_ids = []

    # There is always one region that the generator starts from & assumes you can always go back to.
    # This defaults to "Menu", but you can change it by overriding origin_region_name.
    origin_region_name = "Warp Room 1"

    # Our world class must have certain functions ("steps") that get called during generation.
    # The main ones are: create_regions, set_rules, create_items.
    # For better structure and readability, we put each of these in their own file.
    def generate_early(self) -> None:

        if self.options.randomize_warp_destinations.value:
            if hasattr(self.multiworld, "re_gen_passthrough"):
                if "Crash2" in self.multiworld.re_gen_passthrough:
                    passthrough = self.multiworld.re_gen_passthrough["Crash2"]
                    self.warp_room = passthrough["warp_room_destinations"]
            else:
                secret_warps = ["Road to Ruin (Secret Entrance)", "Air Crash (Secret Entrance)", "Snow Go (Secret Entrance)", "Totally Bear", "Totally Fly"]
                self.warp_room = randomize_warp.shuffle_warp_room_destinations(self, self.options.non_randomized_warp_destinations.value + secret_warps)
    def create_regions(self) -> None:
        regions.create_and_connect_regions(self)
        locations.create_all_locations(self)

    def set_rules(self) -> None:
        rules.set_all_rules(self)

    def create_items(self) -> None:
        items.create_all_items(self)

    # Our world class must also have a create_item function that can create any one of our items by name at any time.
    # We also put this in a different file, the same one that create_items is in.
    def create_item(self, name: str) -> items.Crash2Item:
        return items.create_item_with_correct_classification(self, name)

    # For features such as item links and panic-method start inventory, AP may ask your world to create extra filler.
    # The way it does this is by calling get_filler_item_name.
    # For this purpose, your world *must* have at least one infinitely repeatable item (usually filler).
    # You must override this function and return this infinitely repeatable item's name.
    # In our case, we defined a function called get_random_filler_item_name for this purpose in our items.py.
    def get_filler_item_name(self) -> str:
        return items.get_random_filler_item_name(self)

    # There may be data that the game client will need to modify the behavior of the game.
    # This is what slot_data exists for. Upon every client connection, the slot's slot_data is sent to the client.
    # slot_data is just a dictionary using basic types, that will be converted to json when sent to the client.
    def fill_slot_data(self) -> Mapping[str, Any]:
        # If you need access to the player's chosen options on the client side, there is a helper for that.
        return {
            "options": self.options.as_dict(
                "fruit_sanity", "randomize_warp_destinations", "non_randomized_warp_destinations",
                "trap_duration", "death_link"
            ),
            "warp_room_destinations": self.warp_room,
            "secret_warp_room_entrances": self.secret_warp_room_entrance_ids,
            "seed": self.multiworld.seed_name,  # to verify the server's multiworld
            "slot": self.multiworld.player_name[self.player],  # to connect to server
            #"entrances": self.get_entrances()
        }

    def interpret_slot_data(self, slot_data: dict[str, Any]) -> dict[str, Any]:
        return slot_data

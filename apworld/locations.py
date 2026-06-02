from __future__ import annotations

from typing import TYPE_CHECKING

from BaseClasses import ItemClassification, Location, LocationProgressType

from . import items

from . import data

if TYPE_CHECKING:
    from .world import Crash2World

levelNameToId = {
    "Turtle Woods": 0x1E,
    "Snow Go": 0x0E,
    "Hang Eight": 0x19,
    "The Pits": 0x1F,
    "Crash Dash": 0x18,
    "Ripper Roo": 0x06,
    "Snow Biz": 0x11,
    "Air Crash": 0x20,
    "Bear It": 0x1D,
    "Crash Crush": 0x1B,
    "The Eel Deal": 0x23,
    "Komodo Brothers": 0x08,
    "Plant Food": 0x21,
    "Sewer or Later": 0x0A,
    "Bear Down": 0x22,
    "Road to Ruin": 0x16,
    "Un-Bearable": 0x17,
    "Tiny Tiger": 0x03,
    "Hangin' Out": 0x0D,
    "Diggin' It": 0x15,
    "Cold Hard Crash": 0x13,
    "Ruination": 0x0F,
    "Bee-Having": 0x24,
    "Dr. N. Gin": 0x09,
    "Piston it Away": 0x10,
    "Rock It": 0x12,
    "Night Fight": 0x0C,
    "Pack Attack": 0x1A,
    "Spaced Out": 0x26,
    "Dr. Neo Cortex": 0x07,
    "Totally Bear": 0x25,
    "Totally Fly": 0x27,
}

# gets the name of the level with that level ID. array where the index is the level ID you want. level IDs cannot go above 0x3F (63).
levelIdToName = [None] * 64
# fills array based on map defined right above this!
for name in levelNameToId:
    levelIdToName[levelNameToId[name]] = name


# Every location must have a unique integer ID associated with it.
# We will have a lookup from location name to ID here that, in world.py, we will import and bind to the world class.
# Even if a location doesn't exist on specific options, it must be present in this lookup.
# LOCATION_NAME_TO_ID = {
#     "Top Left Room Chest": 1,
#     "Top Middle Chest": 2,
#     "Bottom Left Chest": 3,
#     "Bottom Left Extra Chest": 4,
#     "Bottom Right Room Left Chest": 5,
#     "Bottom Right Room Right Chest": 6,
#     # Location IDs don't need to be sequential, as long as they're unique and greater than 0.
#     "Right Room Enemy Drop": 10,
# }
LOCATION_NAME_TO_ID = {
    "Turtle Woods: Crystal": 1,
    "Turtle Woods: Clear Gem (Box Gem)": 2,
    "Turtle Woods: Blue Gem": 3,

    "Snow Go: Crystal": 4,
    "Snow Go: Clear Gem (Box Gem)": 5,
    "Snow Go: Red Gem": 6,

    "Hang Eight: Crystal": 7,
    "Hang Eight: Clear Gem (Box Gem)": 8,
    "Hang Eight: Clear Gem (Timer)": 9,

    "The Pits: Crystal": 10,
    "The Pits: Clear Gem (Box Gem)": 11,

    "Crash Dash: Crystal": 12,
    "Crash Dash: Clear Gem (Box Gem)": 13,

    "Ripper Roo Defeated": 14,

    "Snow Biz: Crystal": 15,
    "Snow Biz: Clear Gem (Box Gem)": 16,

    "Air Crash: Crystal": 17,
    "Air Crash: Clear Gem (Box Gem)": 18,
    "Air Crash: Clear Gem (Death Route)": 19,

    "Bear It: Crystal": 20,
    "Bear It: Clear Gem (Box Gem)": 21,

    "Crash Crush: Crystal": 22,
    "Crash Crush: Clear Gem (Box Gem)": 23,

    "The Eel Deal: Crystal": 24,
    "The Eel Deal: Clear Gem (Box Gem)": 25,
    "The Eel Deal: Green Gem": 26,

    "Komodo Brothers Defeated": 27,

    "Plant Food: Crystal": 28,
    "Plant Food: Clear Gem (Box Gem)": 29,
    "Plant Food: Yellow Gem": 30,

    "Sewer or Later: Crystal": 31,
    "Sewer or Later: Clear Gem (Box Gem)": 32,
    "Sewer or Later: Clear Gem (Yellow Gem Path)": 33,

    "Bear Down: Crystal": 34,
    "Bear Down: Clear Gem (Box Gem)": 35,

    "Road to Ruin: Crystal": 36,
    "Road to Ruin: Clear Gem (Box Gem)": 37,
    "Road to Ruin: Clear Gem (Death Route)": 38,

    "Un-Bearable: Crystal": 39,
    "Un-Bearable: Clear Gem (Box Gem)": 40,

    "Tiny Tiger Defeated": 41,

    "Hangin' Out: Crystal": 42,
    "Hangin' Out: Clear Gem (Box Gem)": 43,

    "Diggin' It: Crystal": 44,
    "Diggin' It: Clear Gem (Box Gem)": 45,
    "Diggin' It: Clear Gem (Death Route)": 46,

    "Cold Hard Crash: Crystal": 47,
    "Cold Hard Crash: Clear Gem (Box Gem)": 48,
    "Cold Hard Crash: Clear Gem (Death Route)": 49,

    "Ruination: Crystal": 50,
    "Ruination: Clear Gem (Box Gem)": 51,
    "Ruination: Clear Gem (Green Gem Path)": 52,

    "Bee-Having: Crystal": 53,
    "Bee-Having: Clear Gem (Box Gem)": 54,
    "Bee-Having: Purple Gem": 55,

    "Dr. N. Gin Defeated": 56,

    "Piston it Away: Crystal": 57,
    "Piston it Away: Clear Gem (Box Gem)": 58,
    "Piston it Away: Clear Gem (Death Route)": 59,

    "Rock It: Crystal": 60,
    "Rock It: Clear Gem (Box Gem)": 61,

    "Night Fight: Crystal": 62,
    "Night Fight: Clear Gem (Box Gem)": 63,
    "Night Fight: Clear Gem (Death Route)": 64,

    "Pack Attack: Crystal": 65,
    "Pack Attack: Clear Gem (Box Gem)": 66,

    "Spaced Out: Crystal": 67,
    "Spaced Out: Clear Gem (Box Gem)": 68,
    "Spaced Out: Clear Gem (All Colored Gems Path)": 69,

    #"Dr. Neo Cortex Defeated": 70,

    "Totally Bear: Clear Gem (Box Gem)": 71,
    "Totally Fly: Clear Gem (Box Gem)": 72,

    "Turtle Woods: Regular Exit": 73,
    "Snow Go: Regular Exit": 74,
    "Hang Eight: Regular Exit": 75,
    "The Pits: Regular Exit": 76,
    "Crash Dash: Regular Exit": 77,
    "Snow Biz: Regular Exit": 79,
    "Air Crash: Regular Exit": 80,
    "Bear It: Regular Exit": 81,
    "Crash Crush: Regular Exit": 82,
    "The Eel Deal: Regular Exit": 83,
    "Plant Food: Regular Exit": 85,
    "Sewer or Later: Regular Exit": 86,
    "Bear Down: Regular Exit": 87,
    "Road to Ruin: Regular Exit": 88,
    "Un-Bearable: Regular Exit": 89,
    "Hangin' Out: Regular Exit": 91,
    "Diggin' It: Regular Exit": 92,
    "Cold Hard Crash: Regular Exit": 93,
    "Ruination: Regular Exit": 94,
    "Bee-Having: Regular Exit": 95,
    "Piston it Away: Regular Exit": 97,
    "Rock It: Regular Exit": 98,
    "Night Fight: Regular Exit": 99,
    "Pack Attack: Regular Exit": 100,
    "Spaced Out: Regular Exit": 101,
    "Totally Bear: Regular Exit": 102,
    "Totally Fly: Regular Exit": 103,

    # Secret exits
    "Air Crash: Secret Exit": 104,
    "Hangin' Out: Secret Exit": 105,
    "Diggin' It: Secret Exit": 106,
    "Un-Bearable: Secret Exit": 107,
    "Bear Down: Secret Exit": 108,

    # Extra

    "Polar Lives Secret": 109,

    # "Test Location 0": 10000,
    # "Test Location 1": 20000,
    # "Test Location 2": 10002,
}

# for easier lookup during location group setup
warp_1 = dict.fromkeys(["Turtle Woods", "Snow Go", "Hang Eight", "The Pits", "Crash Dash"], "Warp 1")
warp_2 = dict.fromkeys(["Snow Biz", "Air Crash", "Bear It", "Crash Crush", "The Eel Deal"], "Warp 2")
warp_3 = dict.fromkeys(["Plant Food", "Sewer or Later", "Bear Down", "Road to Ruin", "Un-Bearable"], "Warp 3")
warp_4 = dict.fromkeys(["Hangin' Out", "Diggin' It", "Cold Hard Crash", "Ruination", "Bee-Having"], "Warp 4")
warp_5 = dict.fromkeys(["Piston it Away", "Rock It", "Night Fight", "Pack Attack", "Spaced Out"], "Warp 5")
warp_6 = dict.fromkeys(["Totally Bear", "Totally Fly"], "Warp 6")
level_lookup = {**warp_1, **warp_2, **warp_3, **warp_4, **warp_5, **warp_6}

# Fruit_Sanity_Data = {}


# Each Location instance must correctly report the "game" it belongs to.
# To make this simple, it is common practice to subclass the basic Location class and override the "game" field.
class Crash2Location(Location):
    game = "Crash2"


def prepare_fruit_sanity():

    level_name = ""
    new_level_name = ""
    bundle_name = ""
    bundle_location_name = ""
    wumpa_location_name = ""
    location_name = ""
    bundle_id = 10000
    wumpa_id = 20000
    wumpa_count = 0

    #with open("worlds/crash2/fruitbundles.txt", "r") as file:
        #file.readlines()

    for line in data.fruitbundlestxt.splitlines():
        if line[0] == "#":

            if "level: " in line:
                new_level_name = line.replace("#level: ", "")
                # Fruit_Sanity_Data[level_name] = {}
            else:
                if bundle_name != "":
                    # add the location
                    if level_name == "":
                        level_name = new_level_name
                    bundle_location_name = level_name + " " + bundle_name + " Bundle (" + str(wumpa_count) + " Wumpas)"
                    LOCATION_NAME_TO_ID[bundle_location_name] = bundle_id
                    bundle_id += 1
                    wumpa_count = 0
                bundle_name = line.replace("#", "")
                # Fruit_Sanity_Data[level_name][bundle_name] = ([], bundle_id)
                level_name = new_level_name
                location_name = level_name + " " + bundle_name
        elif len(line.split("-")) == 2:
            wumpa_count += 1
            # if world.options.fruit_sanity == 2:  # if full sanity, add location for each wumpa
            wumpa_location_name = location_name + " Wumpa #" + str(wumpa_count)
                # world.location_name_to_id[wumpa_location_name] = id
            LOCATION_NAME_TO_ID[wumpa_location_name] = wumpa_id

            # f.write(hex(levelNameToId[level_name]).replace("0x", "").upper() + "-" + line.split("-")[1] + ":" + str(wumpa_id) +"\n")
            # Fruit_Sanity_Data[level_name][bundle_name][0].append(wumpa_id)
            wumpa_id += 1
                # region = world.get_region(level_name)
                # region.locations.append(
                #     Crash2Location(world.player, wumpa_location_name,
                #                    world.location_name_to_id[wumpa_location_name], region))
    # print("bundle id :" + str(bundle_id))
    # print("wumpa id :" + str(wumpa_id))
    # f.close()

# Let's make one more helper method before we begin actually creating locations.
# Later on in the code, we'll want specific subsections of LOCATION_NAME_TO_ID.
# To reduce the chance of copy-paste errors writing something like {"Chest": LOCATION_NAME_TO_ID["Chest"]},
# let's make a helper method that takes a list of location names and returns them as a dict with their IDs.
# Note: There is a minor typing quirk here. Some functions want location addresses to be an "int | None",
# so while our function here only ever returns dict[str, int], we annotate it as dict[str, int | None].
def get_location_names_with_ids(location_names: list[str]) -> dict[str, int | None]:
    return {location_name: LOCATION_NAME_TO_ID[location_name] for location_name in location_names}


def create_all_locations(world: Crash2World) -> None:
    create_regular_locations(world)
    create_events(world)


def create_regular_locations(world: Crash2World) -> None:
    # Finally, we need to put the Locations ("checks") into their regions.
    # Once again, before we do anything, we can grab our regions we created by using world.get_region()
    # overworld = world.get_region("Overworld")
    # top_left_room = world.get_region("Top Left Room")
    # bottom_right_room = world.get_region("Bottom Right Room")
    # right_room = world.get_region("Right Room")

    #level_exits_option = False # this will be an option in the future maybe

    locations = LOCATION_NAME_TO_ID.keys()
    for name in levelNameToId:
        region = world.get_region(name)
        for location in locations:
            if name in location:
                if not world.options.level_exit_locations and "Regular Exit" in location:
                    continue
                if "Bundle" in location:
                    if world.options.fruit_sanity != 1:
                        continue
                if "Wumpa #" in location:
                    if world.options.fruit_sanity != 2:
                        continue
                new_location = Crash2Location(world.player, location, world.location_name_to_id[location], region)
                if "*" in location:
                    if world.options.exclude_difficult_wumpas:
                        new_location.progress_type = LocationProgressType.EXCLUDED
                if "Wumpa" in location:
                    if world.random.randint(0, 99) < world.options.fill_wumpa_checks_locally_chance:
                        new_location.place_locked_item(world.create_filler())
                region.locations.append( new_location)
    location = "Polar Lives Secret"
    region = world.get_region("Warp Room 2")
    region.locations.append(
        Crash2Location(world.player, location, world.location_name_to_id[location], region))


    # if world.options.fruit_sanity != 0:
    #     level_name = ""
    #     bundle_name = ""
    #     location_name = ""
    #     # id = 10000
    #     # if world.options.fruit_sanity == 2:
    #     #     id = 20000
    #     wumpa_count = 0
    #     #with open("fruitbundles.txt", "r") as file:
    #
    #     for line in fruitbundlestxt.splitlines():
    #         if line[0] == "#":
    #             if "level: " in line:
    #                 level_name = line.replace("#level: ", "")
    #             else:
    #                 if bundle_name != "":
    #                     # add the location
    #                     if world.options.fruit_sanity == 1: # if bundles then add a bundle location
    #                         location_name = level_name + " " + bundle_name + " Bundle (" + str(wumpa_count) + " Wumpas)"
    #                         #world.location_name_to_id[location_name] = id
    #                         #LOCATION_NAME_TO_ID[location_name] = id
    #                         # id += 1
    #                         print("---" + location_name + str(world.location_name_to_id[location_name]))
    #                         region = world.get_region(level_name)
    #                         region.locations.append(
    #                             Crash2Location(world.player, location_name, world.location_name_to_id[location_name], region))
    #                     wumpa_count = 0
    #                 bundle_name = line.replace("#", "")
    #                 if world.options.fruit_sanity == 2:
    #                     location_name = level_name + " " + bundle_name
    #         if len(line.split("-")) == 2:
    #             wumpa_count += 1
    #             if world.options.fruit_sanity == 2: # if full sanity, add location for each wumpa
    #                 wumpa_location_name = location_name + " Wumpa #" + str(wumpa_count)
    #                 #world.location_name_to_id[wumpa_location_name] = id
    #                 #LOCATION_NAME_TO_ID[wumpa_location_name] = id
    #                 # id += 1
    #                 region = world.get_region(level_name)
    #                 region.locations.append(
    #                     Crash2Location(world.player, wumpa_location_name, world.location_name_to_id[wumpa_location_name], region))


    # region = world.get_region("Dr. Neo Cortex")
    # location = ""
    # region.locations.append(Crash2Location(world.player, location, world.location_name_to_id[location], region))

    # One way to create locations is by just creating them directly via their constructor.
    # bottom_left_chest = APQuestLocation(
    #     world.player, "Bottom Left Chest", world.location_name_to_id["Bottom Left Chest"], overworld
    # )
    #
    # # You can then add them to the region.
    # overworld.locations.append(bottom_left_chest)
    #
    # # A simpler way to do this is by using the region.add_locations helper.
    # # For this, you need to have a dict of location names to their IDs (i.e. a subset of location_name_to_id)
    # # Aha! So that's why we made that "get_location_names_with_ids" helper method earlier.
    # # You also need to pass your overridden Location class.
    # bottom_right_room_locations = get_location_names_with_ids(
    #     ["Bottom Right Room Left Chest", "Bottom Right Room Right Chest"]
    # )
    # bottom_right_room.add_locations(bottom_right_room_locations, APQuestLocation)
    #
    # top_left_room_locations = get_location_names_with_ids(["Top Left Room Chest"])
    # top_left_room.add_locations(top_left_room_locations, APQuestLocation)
    #
    # right_room_locations = get_location_names_with_ids(["Right Room Enemy Drop"])
    # right_room.add_locations(right_room_locations, APQuestLocation)

    # Locations may be in different regions depending on the player's options.
    # In our case, the hammer option puts the Top Middle Chest into its own room called Top Middle Room.
    # top_middle_room_locations = get_location_names_with_ids(["Top Middle Chest"])
    # if world.options.hammer:
    #     top_middle_room = world.get_region("Top Middle Room")
    #     top_middle_room.add_locations(top_middle_room_locations, APQuestLocation)
    # else:
    #     overworld.add_locations(top_middle_room_locations, APQuestLocation)
    #
    # # Locations may exist only if the player enables certain options.
    # # In our case, the extra_starting_chest option adds the Bottom Left Extra Chest location.
    # if world.options.extra_starting_chest:
    #     # Once again, it is important to stress that even though the Bottom Left Extra Chest location doesn't always
    #     # exist, it must still always be present in the world's location_name_to_id.
    #     # Whether the location actually exists in the seed is purely determined by whether we create and add it here.
    #     bottom_left_extra_chest = get_location_names_with_ids(["Bottom Left Extra Chest"])
    #     overworld.add_locations(bottom_left_extra_chest, APQuestLocation)


def create_events(world: Crash2World) -> None:

    world.get_region("Dr. Neo Cortex").add_event(
        "Dr. Neo Cortex Defeated", "Victory", location_type=Crash2Location, item_type=items.Crash2Item
    )

    world.get_region("Air Crash").add_event(
        "Air Crash Secret Exit Event", "Snow Go Secret Entrance", location_type=Crash2Location, item_type=items.Crash2Item
    )
    world.get_region("Bear Down").add_event(
        "Bear Down Secret Exit Event", "Air Crash Secret Entrance", location_type=Crash2Location,
        item_type=items.Crash2Item
    )
    world.get_region("Un-Bearable").add_event(
        "Un-Bearable Secret Exit Event", "Totally Bear Secret Entrance", location_type=Crash2Location,
        item_type=items.Crash2Item
    )
    world.get_region("Hangin' Out").add_event(
        "Hangin' Out Secret Exit Event", "Totally Fly Secret Entrance", location_type=Crash2Location,
        item_type=items.Crash2Item
    )
    world.get_region("Diggin' It").add_event(
        "Diggin' It Secret Exit Event", "Road to Ruin Secret Entrance", location_type=Crash2Location,
        item_type=items.Crash2Item
    )
    # # Sometimes, the player may perform in-game actions that allow them to progress which are not related to Items.
    # # In our case, the player must press a button in the top left room to open the final boss door.
    # # AP has something for this purpose: "Event locations" and "Event items".
    # # An event location is no different than a regular location, except it has the address "None".
    # # It is treated during generation like any other location, but then it is discarded.
    # # This location cannot be "sent" and its item cannot be "received", but the item can be used in logic rules.
    # # Since we are creating more locations and adding them to regions, we need to grab those regions again first.
    # top_left_room = world.get_region("Top Left Room")
    # final_boss_room = world.get_region("Final Boss Room")
    #
    # # One way to create an event is simply to use one of the normal methods of creating a location.
    # button_in_top_left_room = APQuestLocation(world.player, "Top Left Room Button", None, top_left_room)
    # top_left_room.locations.append(button_in_top_left_room)
    #
    # # We then need to put an event item onto the location.
    # # An event item is an item whose code is "None" (same as the event location's address),
    # # and whose classification is "progression". Item creation will be discussed more in items.py.
    # # Note: Usually, items are created in world.create_items(), which for us happens in items.py.
    # # However, when the location of an item is known ahead of time (as is the case with an event location/item pair),
    # # it is common practice to create the item when creating the location.
    # # Since locations also have to be finalized after world.create_regions(), which runs before world.create_items(),
    # # we'll create both the event location and the event item in our locations.py code.
    # button_item = items.APQuestItem("Top Left Room Button Pressed", ItemClassification.progression, None, world.player)
    # button_in_top_left_room.place_locked_item(button_item)
    #
    # # A way simpler way to do create an event location/item pair is by using the region.create_event helper.
    # # Luckily, we have another event we want to create: The Victory event.
    # # We will use this event to track whether the player can win the game.
    # # The Victory event is a completely optional abstraction - This will be discussed more in set_rules().
    # final_boss_room.add_event(
    #     "Final Boss Defeated", "Victory", location_type=APQuestLocation, item_type=items.APQuestItem
    # )
    #
    # # If you create all your regions and locations line-by-line like this,
    # # the length of your create_regions might get out of hand.
    # # Many worlds use more data-driven approaches using dataclasses or NamedTuples.
    # # However, it is worth understanding how the actual creation of regions and locations works,
    # # That way, we're not just mindlessly copy-pasting! :)


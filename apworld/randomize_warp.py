from __future__ import annotations

from typing import TYPE_CHECKING

from BaseClasses import ItemClassification, Location

from . import items

from . import data

if TYPE_CHECKING:
    from .world import Crash2World

# five levels per warp room, starting at the fifth level of each warp room
# this matches the way the level IDs are ordered in the monty hall (warp room) object and keeps things simple.
# the 0x100 bit denotes secret entrance. this also makes it simpler to shuffle things around.

# locations.py has the level IDs mapped to level names. they are also available at https://wiki.cbhacks.com/w/Level_ID#Crash_2
warpRoomLevelIds = [
    0x18, 0x1F, 0x19, 0x0E, 0x1E,
    0x23, 0x1B, 0x1D, 0x20, 0x11,
    0x17, 0x16, 0x22, 0x0A, 0x21,
    0x24, 0x0F, 0x13, 0x15, 0x0D,
    0x26, 0x1A, 0x0C, 0x12, 0x10,
    0x27, 0x25, 0x116, 0x10E, 0x120
]

levelIdsWithSecretEntrance = [
    0x0E, # snow go
    0x16, # road to ruin
    0x20, # air crash
]

levelIdsWithSecretExit = [
    0x0D, # hangin' out
    0x17, # un-bearable
    0x15, # diggin' it
    0x20, # air crash
    0x22, # bear down
]

# Not actually randomized (yet??), determines the level ID for the boss of each warp room.
# warp room 6, the secret one, has no boss, so it is excluded.
warpRoomBossLevelIds = [
    0x06,
    0x08,
    0x03,
    0x09,
    0x07
]

# This is for the excluded locations list.
levelNameToId = {
    "Turtle Woods": 0x1E,
    "Snow Go": 0x0E,
    "Snow Go (Secret Entrance)": 0x10E,
    "Hang Eight": 0x19,
    "The Pits": 0x1F,
    "Crash Dash": 0x18,
    "Ripper Roo": 0x06,
    "Snow Biz": 0x11,
    "Air Crash": 0x20,
    "Air Crash (Secret Entrance)": 0x120,
    "Bear It": 0x1D,
    "Crash Crush": 0x1B,
    "The Eel Deal": 0x23,
    "Komodo Brothers": 0x08,
    "Plant Food": 0x21,
    "Sewer or Later": 0x0A,
    "Bear Down": 0x22,
    "Road to Ruin": 0x16,
    "Road to Ruin (Secret Entrance)": 0x116,
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

def get_nth_element_index(lst, element, occurrence):
    index = 0
    occ = 0
    for elt in lst:
        if elt == element:
            if occ == occurrence:
                return index
            occ += 1
        index += 1
    return None

def shuffle_warp_room_destinations(world, excluded_level_names):
    # convert level names here to level IDs
    excluded = []
    for level_name in excluded_level_names:
        excluded.append(levelNameToId[level_name])

    # FINAL list of levels for each warp room, including both excluded and randomized entrances. Includes secret warp room.
    # this is going to be used to build the final list.
    # fill with 'empty slots' (-1). they are either replaced by the excluded entrances, or randomized entrances.
    total_dest_count = len(warpRoomLevelIds)
    shuffled_warp_dests = [-1] * total_dest_count
    
    # Check if level a is in the same warp room as level b, or in any warp rooms before. If it can't be determined, return on_unplaced
    def check_level_order(level_a, level_b, on_unplaced):
        if level_a in shuffled_warp_dests and level_b in shuffled_warp_dests:
            if shuffled_warp_dests.index(level_a) >= 25 or shuffled_warp_dests.index(level_b) >= 25:
                return on_unplaced
            return shuffled_warp_dests.index(level_a) // 5 <= shuffled_warp_dests.index(level_b) // 5
        return on_unplaced

    level_order_rules = [] # each element in this array is a two-element array. the level in the first element must come before or be in the same warp room as the second.

    # Step 1: Randomize the regular warp rooms, except for excluded destinations.
    temp_warp_dests:list = warpRoomLevelIds[0:25]
    available_secret_entrances = warpRoomLevelIds[25:30]
    for excluded_level in excluded:
        if excluded_level in temp_warp_dests:
            temp_warp_dests.remove(excluded_level)
        else:
            available_secret_entrances.remove(excluded_level)
    world.random.shuffle(temp_warp_dests)

    # Step 2: Assign secret exits.
    secret_exits = [-1] * 5
    secret_entrances = [-1] * 5
    world.secret_warp_room_entrance_ids = []
    for i in range(5):
        # NOTE: totally bear and totally fly don't need logic. they just go back to the level that unlocked them.
        original_secret_entrance = warpRoomLevelIds[25 + i]
        if original_secret_entrance in excluded:
            # secret entrance is excluded. e.g. snow go secret entrance
            # this means that e.g. air crash will unlock it. air crash or snow go can themselves still be anywhere, so they need to be checked for logical positioning later.
            secret_exits[i] = levelIdsWithSecretExit[i]
            secret_entrances[i] = original_secret_entrance
        else:
            # secret entrance is randomized. e.g. snow go secret entrance
            # this means that e.g. hangin' out could unlock it. (we actually do this backwards, i.e. air crash will randomly pick what to unlock. doesn't matter.)
            # we try to avoid invalid pairings to be picked here when e.g. both the exit and target level are not randomized but the entrance itself is.
            # for example, air crash picks road to ruin secret entrance but road to ruin comes after air crash. road to ruin secret entrance was part of the random pool along with snow go secret entrance.
            secret_exits[i] = levelIdsWithSecretExit[i] # e.g. diggin' it
            valid_secret_entrances = [entrance for entrance in available_secret_entrances if check_level_order(entrance & 0x3F, secret_exits[i], True)] # filter down to logically possible pairings
            assert(len(valid_secret_entrances) > 0)
            secret_entrances[i] = world.random.choice(valid_secret_entrances) # e.g. road to ruin secret entrance
            available_secret_entrances.remove(secret_entrances[i])
        if secret_entrances[i] & 0x100 and not check_level_order(secret_entrances[i] & 0x3F, secret_exits[i], False):
            level_order_rules.append([secret_entrances[i] & 0x3F, secret_exits[i]])
        world.secret_warp_room_entrance_ids.append(secret_exits[i])

    # Step 3: Fill in the 25 warp room slots with the excluded locations.
    for i in range(25):
        level = warpRoomLevelIds[i]
        if level in excluded:
            shuffled_warp_dests[i] = level

    # Step 4: Randomly place levels that need to be in a specific order.
    for level_order in level_order_rules:
        level_a = level_order[0]
        level_b = level_order[1]

        # remove the levels from the shuffled list because this routine will already have placed them in some random spot.
        if level_a in temp_warp_dests:
            temp_warp_dests.remove(level_a)
        if level_b in temp_warp_dests:
            temp_warp_dests.remove(level_b)

        if level_a in shuffled_warp_dests and level_b in shuffled_warp_dests:
            assert(check_level_order(level_a, level_b, False))
        elif level_a in shuffled_warp_dests and level_b not in shuffled_warp_dests:
            # level a already placed. level b goes in random spot after level a.
            level_a_dest_index = shuffled_warp_dests.index(level_a)
            level_a_warp_room = level_a_dest_index//5
            available_dests = shuffled_warp_dests[level_a_warp_room*5:25]
            unavailable_dests = shuffled_warp_dests[0:level_a_warp_room*5]
            total_available_slots = available_dests.count(-1)
            assert(total_available_slots >= 1) # need 1 empty slot for this to work
            level_b_chosen_slot = world.random.randint(0, total_available_slots - 1) + unavailable_dests.count(-1)
            level_b_dest_index = get_nth_element_index(shuffled_warp_dests, -1, level_b_chosen_slot)
            shuffled_warp_dests[level_b_dest_index] = level_b
        elif level_a not in shuffled_warp_dests and level_b in shuffled_warp_dests:
            # level b already placed. level a goes in random spot before level b.
            level_b_dest_index = shuffled_warp_dests.index(level_b)
            level_b_warp_room = level_b_dest_index//5 + 1
            available_dests = shuffled_warp_dests[0:level_b_warp_room*5]
            total_available_slots = available_dests.count(-1)
            assert(total_available_slots >= 1) # need 1 empty slot for this to work
            level_a_chosen_slot = world.random.randint(0, total_available_slots - 1)
            level_a_dest_index = get_nth_element_index(shuffled_warp_dests, -1, level_a_chosen_slot)
            shuffled_warp_dests[level_a_dest_index] = level_a
        else:
            # level a and level b go in random spots.
            total_available_slots = shuffled_warp_dests[0:25].count(-1)
            assert(total_available_slots >= 2) # need 2 empty slots for this to work
            while True:
                level_a_chosen_slot = world.random.randint(0, total_available_slots - 1)
                level_a_dest_index = get_nth_element_index(shuffled_warp_dests, -1, level_a_chosen_slot)
                level_a_warp_room = level_a_dest_index//5
                available_dests = shuffled_warp_dests[level_a_warp_room*5:25]
                unavailable_dests = shuffled_warp_dests[0:level_a_warp_room*5]
                if available_dests.count(-1) >= 2:
                    # subtracting 1 here because one of the slots has just been taken up by level a
                    level_b_chosen_slot = world.random.randint(0, available_dests.count(-1) - 1 - 1) + unavailable_dests.count(-1)
                    if level_b_chosen_slot >= level_a_chosen_slot:
                        level_b_chosen_slot += 1
                    level_b_dest_index = get_nth_element_index(shuffled_warp_dests, -1, level_b_chosen_slot)
                    shuffled_warp_dests[level_a_dest_index] = level_a
                    shuffled_warp_dests[level_b_dest_index] = level_b
                    break

    # Step 5: Place remaining randomized levels in the warp room.
    for i in range(25):
        if shuffled_warp_dests[i] == -1: # in case it hasn't been set already by the previous step
            shuffled_warp_dests[i] = temp_warp_dests.pop()

    # Step 6: Place secret entrances in the warp room.
    for i in range(5):
        shuffled_warp_dests[25 + i] = secret_entrances[i]

    return shuffled_warp_dests

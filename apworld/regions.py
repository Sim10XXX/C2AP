from __future__ import annotations

from typing import TYPE_CHECKING

from BaseClasses import Entrance, Region

from .locations import levelIdToName, levelNameToId
from .randomize_warp import warpRoomBossLevelIds

if TYPE_CHECKING:
    from .world import Crash2World

# A region is a container for locations ("checks"), which connects to other regions via "Entrance" objects.
# Many games will model their Regions after physical in-game places, but you can also have more abstract regions.
# For a location to be in logic, its containing region must be reachable.
# The Entrances connecting regions can have rules - more on that in rules.py.
# This makes regions especially useful for traversal logic ("Can the player reach this part of the map?")

# Every location must be inside a region, and you must have at least one region.
# This is why we create regions first, and then later we create the locations (in locations.py).

def create_and_connect_regions(world: Crash2World) -> None:
    create_all_regions(world)
    connect_regions(world)


def create_all_regions(world: Crash2World) -> None:
    # Creating a region is as simple as calling the constructor of the Region class.
    # overworld = Region("Overworld", world.player, world.multiworld)
    # top_left_room = Region("Top Left Room", world.player, world.multiworld)
    # bottom_right_room = Region("Bottom Right Room", world.player, world.multiworld)
    # right_room = Region("Right Room", world.player, world.multiworld)
    # final_boss_room = Region("Final Boss Room", world.player, world.multiworld)

    regions = []
    for i in range(6): # Warp room 6 is be the secret warp room
        regions.append(Region("Warp Room "+str(i+1), world.player, world.multiworld))


    for name in levelNameToId:
        regions.append(Region(name, world.player, world.multiworld))

    # Let's put all these regions in a list.
    # regions = [overworld, top_left_room, bottom_right_room, right_room, final_boss_room]

    # Some regions may only exist if the player enables certain options.
    # In our case, the Hammer locks the top middle chest in its own room if the hammer option is enabled.
    # if world.options.hammer:
    #     top_middle_room = Region("Top Middle Room", world.player, world.multiworld)
    #     regions.append(top_middle_room)

    # We now need to add these regions to multiworld.regions so that AP knows about their existence.
    world.multiworld.regions += regions

def connect_regions(world: Crash2World) -> None:
    # We have regions now, but still need to connect them to each other.
    # But wait, we no longer have access to the region variables we created in create_all_regions()!
    # Luckily, once you've submitted your regions to multiworld.regions,
    # you can get them at any time using world.get_region(...).
    # overworld = world.get_region("Overworld")
    # top_left_room = world.get_region("Top Left Room")
    # bottom_right_room = world.get_region("Bottom Right Room")
    # right_room = world.get_region("Right Room")
    # final_boss_room = world.get_region("Final Boss Room")

    # Okay, now we can get connecting. For this, we need to create Entrances.
    # Entrances are inherently one-way, but crucially, AP assumes you can always return to the origin region.
    # One way to create an Entrance is by calling the Entrance constructor.
    # overworld_to_bottom_right_room = Entrance(world.player, "Overworld to Bottom Right Room", parent=overworld)
    # overworld.exits.append(overworld_to_bottom_right_room)

    # Connect every warp room to the next warp room, except 5 doesn't connect to 6 (secret warp room)
    for i in range(1, 5):
        world.get_region("Warp Room " + str(i)).connect(
            world.get_region("Warp Room " + str(i+1)),
            "Warp Room " + str(i) + " to Warp Room " + str(i+1))
    
    # Connect every warp room to its levels
    for i in range(6):
        warp_room = world.get_region("Warp Room "+str(i+1))
        # Clear (and then fill) this so that we can connect any level using the secret entrance rules.
        if i == 5:
            world.secret_warp_room_levels = []
        # the five levels.
        for j in range(5):
            # the warp room list has 'special' IDs to differentiate from secret entrances. strip that out.
            level_id = world.warp_room[i * 5 + j] & 0x3F
            is_secret_entrance = world.warp_room[i * 5 + j] & 0x100
            level_name = levelIdToName[level_id]
            level_name_full = level_name + " Secret Entrance" if is_secret_entrance else level_name
            level = world.get_region(level_name)
            warp_room.connect(level, "Warp Room "+str(i+1) + " to " + level_name_full)
            # special case: save exact destination of each level in warp room 6 to set up rules correctly.
            if i == 5:
                world.secret_warp_room_levels.append(level_name_full)
        # and the boss! warp room 6 has no boss.
        if i < 5:
            level_name = levelIdToName[warpRoomBossLevelIds[i]]
            level = world.get_region(level_name)
            warp_room.connect(level, "Warp Room "+str(i+1) + " to " + level_name)
    
    # Connect levels with secret exits to warp room 6
    warp_room = world.get_region("Warp Room 6")
    for level in ["Air Crash", "Bear Down", "Un-Bearable", "Diggin' It", "Hangin' Out"]: # Secret Exits. These do not get randomized ever.
        world.get_region(level).connect(warp_room, level + " to Warp Room 6")

    # You can then connect the Entrance to the target region.
    # overworld_to_bottom_right_room.connect(bottom_right_room)

    # An even easier way is to use the region.connect helper.
    # overworld.connect(right_room, "Overworld to Right Room")
    # right_room.connect(final_boss_room, "Right Room to Final Boss Room")

    # The region.connect helper even allows adding a rule immediately.
    # We'll talk more about rule creation in the set_all_rules() function in rules.py.
    # overworld.connect(top_left_room, "Overworld to Top Left Room", lambda state: state.has("Key", world.player))

    # Some Entrances may only exist if the player enables certain options.
    # In our case, the Hammer locks the top middle chest in its own room if the hammer option is enabled.
    # In this case, we previously created an extra "Top Middle Room" region that we now need to connect to Overworld.
    # if world.options.hammer:
    #     top_middle_room = world.get_region("Top Middle Room")
    #     overworld.connect(top_middle_room, "Overworld to Top Middle Room")

from __future__ import annotations

from typing import TYPE_CHECKING

from BaseClasses import CollectionState
from worlds.generic.Rules import add_rule, set_rule

if TYPE_CHECKING:
    from .world import Crash2World


def set_all_rules(world: Crash2World) -> None:
    # In order for AP to generate an item layout that is actually possible for the player to complete,
    # we need to define rules for our Entrances and Locations.
    # Note: Regions do not have rules, the Entrances connecting them do!
    # We'll do entrances first, then locations, and then finally we set our victory condition.

    set_all_entrance_rules(world)
    set_all_location_rules(world)
    set_completion_condition(world)


def set_all_entrance_rules(world: Crash2World) -> None:
    # First, we need to actually grab our entrances. Luckily, there is a helper method for this.
    # overworld_to_bottom_right_room = world.get_entrance("Overworld to Bottom Right Room")
    # overworld_to_top_left_room = world.get_entrance("Overworld to Top Left Room")
    # right_room_to_final_boss_room = world.get_entrance("Right Room to Final Boss Room")

    # An access rule is a function. We can define this function like any other function.
    # This function must accept exactly one parameter: A "CollectionState".
    # A CollectionState describes the current progress of the players in the multiworld, i.e. what items they have,
    # which regions they've reached, etc.
    # In an access rule, we can ask whether the player has a collected a certain item.
    # We can do this via the state.has(...) function.
    # This function takes an item name, a player number, and an optional count parameter (more on that below)
    # Since a rule only takes a CollectionState parameter, but we also need the player number in the state.has call,
    # our function needs to be locally defined so that it has access to the player number from the outer scope.
    # In our case, we are inside a function that has access to the "world" parameter, so we can use world.player.
    # def can_destroy_bush(state: CollectionState) -> bool:
    #     return state.has("Sword", world.player)

    # Now we can set our "can_destroy_bush" rule to our entrance which requires slashing a bush to clear the path.
    # One way to set rules is via the set_rule() function, which works on both Entrances and Locations.
    # set_rule(overworld_to_bottom_right_room, can_destroy_bush)

    # Because the function has to be defined locally, most worlds prefer the lambda syntax.
    # set_rule(overworld_to_top_left_room, lambda state: state.has("Key", world.player))

    set_rule(world.get_entrance("Warp Room 1 to Warp Room 2"), lambda state: state.has("Crystal", world.player, 5))
    set_rule(world.get_entrance("Warp Room 1 to Ripper Roo"), lambda state: state.has("Crystal", world.player, 5))

    set_rule(world.get_entrance("Warp Room 2 to Warp Room 3"), lambda state: state.has("Crystal", world.player, 10))
    set_rule(world.get_entrance("Warp Room 2 to Komodo Brothers"), lambda state: state.has("Crystal", world.player, 10))

    set_rule(world.get_entrance("Warp Room 3 to Warp Room 4"), lambda state: state.has("Crystal", world.player, 15))
    set_rule(world.get_entrance("Warp Room 3 to Tiny Tiger"), lambda state: state.has("Crystal", world.player, 15))

    set_rule(world.get_entrance("Warp Room 4 to Warp Room 5"), lambda state: state.has("Crystal", world.player, 20))
    set_rule(world.get_entrance("Warp Room 4 to Dr. N. Gin"), lambda state: state.has("Crystal", world.player, 20))

    set_rule(world.get_entrance("Warp Room 5 to Dr. Neo Cortex"), lambda state: state.has("Crystal", world.player, 25))


    set_rule(world.get_entrance("Warp Room 6 to " + world.secret_warp_room_levels[0]),
             lambda state: state.has("Air Crash Secret Entrance", world.player))
    set_rule(world.get_entrance("Warp Room 6 to " + world.secret_warp_room_levels[1]),
             lambda state: state.has("Snow Go Secret Entrance", world.player))
    set_rule(world.get_entrance("Warp Room 6 to " + world.secret_warp_room_levels[2]),
             lambda state: state.has("Road to Ruin Secret Entrance", world.player))
    set_rule(world.get_entrance("Warp Room 6 to " + world.secret_warp_room_levels[3]),
             lambda state: state.has("Totally Bear Secret Entrance", world.player))
    set_rule(world.get_entrance("Warp Room 6 to " + world.secret_warp_room_levels[4]),
             lambda state: state.has("Totally Fly Secret Entrance", world.player))
    # Conditions can depend on event items.
    # set_rule(right_room_to_final_boss_room, lambda state: state.has("Top Left Room Button Pressed", world.player))

    # Some entrance rules may only apply if the player enabled certain options.
    # In our case, if the hammer option is enabled, we need to add the Hammer requirement to the Entrance from
    # Overworld to the Top Middle Room.
    # if world.options.hammer:
    #     overworld_to_top_middle_room = world.get_entrance("Overworld to Top Middle Room")
    #     set_rule(overworld_to_top_middle_room, lambda state: state.has("Hammer", world.player))


def set_all_location_rules(world: Crash2World) -> None:
    # # Location rules work no differently from Entrance rules.
    # # Most of our locations are chests that can simply be opened by walking up to them.
    # # Thus, their logical requirements are covered by the Entrance rules of the Entrances that were required to
    # # reach the region that the chest sits in.
    # # However, our two enemies work differently.
    # # Entering the room with the enemy is not enough, you also need to have enough combat items to be able to defeat it.
    # # So, we need to set requirements on the Locations themselves.
    # # Since combat is a bit more complicated, we'll use this chance to cover some advanced access rule concepts.
    #
    # # Sometimes, you may want to have different rules depending on the player's chosen options.
    # # There is a wrong way to do this, and a right way to do this. Let's do the wrong way first.
    # right_room_enemy = world.get_location("Right Room Enemy Drop")
    #
    # # DON'T DO THIS!!!!
    # set_rule(
    #     right_room_enemy,
    #     lambda state: (
    #         state.has("Sword", world.player)
    #         and (not world.options.hard_mode or state.has_any(("Shield", "Health Upgrade"), world.player))
    #     ),
    # )
    # # DON'T DO THIS!!!!
    #
    # # Now, what's actually wrong with this? It works perfectly fine, right?
    # # If hard mode disabled, Sword is enough. If hard mode is enabled, we also need a Shield or a Health Upgrade.
    # # The access rule we just wrote does this correctly, so what's the problem?
    # # The problem is performance.
    # # Most of your world code doesn't need to be perfectly performant, since it just runs once per slot.
    # # However, access rules in particular are by far the hottest code path in Archipelago.
    # # An access rule will potentially be called thousands or even millions of times over the course of one generation.
    # # As a result, access rules are the one place where it's really worth putting in some effort to optimize.
    # # What's the performance problem here?
    # # Every time our access rule is called, it has to evaluate whether world.options.hard_mode is True or False.
    # # Wouldn't it be better if in easy mode, the access rule only checked for Sword to begin with?
    # # Wouldn't it also be better if in hard mode, it already knew it had to check Shield and Health Upgrade as well?
    # # Well, we can achieve this by doing the "if world.options.hard_mode" check outside the set_rule call,
    # # and instead having two *different* set_rule calls depending on which case we're in.


    # handle some general rules for wumpa checks
    if world.options.fruit_sanity != 0:
        for location in world.get_locations():
            if "Wumpa" not in location.name:
                continue
            if "Secret Entrance" in location.name:
                level_name = location.name[:location.name.find(" Secret Entrance")]
                if level_name == "Road to Ruin" and world.options.speedrun_logic:
                    continue
                access_item = level_name + " Secret Entrance"
                set_rule(location,
                         lambda state, access_item=access_item: state.has(access_item, world.player))
                # print("rule: " + location.name + ", needs: " + level_name + " Secret Entrance")
            elif "Gem Path" in location.name:
                split_location = location.name.split(" ")
                gem_color = split_location[split_location.index("Gem") - 1]
                if gem_color == "Green" and world.options.speedrun_logic:
                    continue
                access_item = gem_color + " Gem"
                set_rule(location,
                         lambda state, access_item=access_item: state.has(access_item, world.player))
                # print("rule: " + location.name + ", needs: " + gem_color + " Gem")


    set_rule(world.get_location("Hang Eight: Clear Gem (Box Gem)"),
             lambda state: state.has("Blue Gem", world.player))

    set_rule(world.get_location("Snow Biz: Clear Gem (Box Gem)"),
             lambda state: state.has("Red Gem", world.player))

    set_rule(world.get_location("Sewer or Later: Clear Gem (Yellow Gem Path)"),
             lambda state: state.has("Yellow Gem", world.player))

    set_rule(world.get_location("Spaced Out: Clear Gem (All Colored Gems Path)"),
             lambda state: state.has_all(("Blue Gem", "Red Gem", "Green Gem", "Yellow Gem", "Purple Gem"), world.player))

    set_rule(world.get_location("Air Crash: Clear Gem (Box Gem)"),
             lambda state: state.has("Air Crash Secret Entrance", world.player))

    red_gem = True if "red_gem_early" in world.options.speedrun_logic.value else False
    road_to_ruin = True if "road_to_ruin_gem" in world.options.speedrun_logic.value else False
    ruination_skip_green = True if "ruination_skip_green" in world.options.speedrun_logic.value else False

    set_rule(world.get_location("Snow Go: Red Gem"),
             lambda state: state.has("Snow Go Secret Entrance", world.player) or red_gem)
    set_rule(world.get_location("Road to Ruin: Clear Gem (Box Gem)"),
             lambda state: state.has("Road to Ruin Secret Entrance", world.player) or road_to_ruin)
    set_rule(world.get_location("Ruination: Clear Gem (Green Gem Path)"),
             lambda state: state.has("Green Gem", world.player) or ruination_skip_green)

    # if world.options.hard_mode:
    #     # If you have multiple conditions, you can obviously chain them via "or" or "and".
    #     # However, there are also the nice helper functions "state.has_any" and "state.has_all".
    #     set_rule(
    #         right_room_enemy,
    #         lambda state: (
    #             state.has("Sword", world.player) and state.has_any(("Shield", "Health Upgrade"), world.player)
    #         ),
    #     )
    # else:
    #     set_rule(right_room_enemy, lambda state: state.has("Sword", world.player))
    #
    # # Another way to chain multiple conditions is via the add_rule function.
    # # This makes the access rules a bit slower though, so it should only be used if your structure justifies it.
    # # In our case, it's pretty useful because hard mode and easy mode have different requirements.
    # final_boss = world.get_location("Final Boss Defeated")
    #
    # # For the "known" requirements, it's still better to chain them using a normal "and" condition.
    # add_rule(final_boss, lambda state: state.has_all(("Sword", "Shield"), world.player))
    #
    # if world.options.hard_mode:
    #     # You can check for multiple copies of an item by using the optional count parameter of state.has().
    #     add_rule(final_boss, lambda state: state.has("Health Upgrade", world.player, 2))


def set_completion_condition(world: Crash2World) -> None:
    # # Finally, we need to set a completion condition for our world, defining what the player needs to win the game.
    # # You can just set a completion condition directly like any other condition, referencing items the player receives:
    # world.multiworld.completion_condition[world.player] = lambda state: state.has_all(("Sword", "Shield"), world.player)
    #
    # # In our case, we went for the Victory event design pattern (see create_events() in locations.py).
    # # So lets undo what we just did, and instead set the completion condition to:
    # world.multiworld.completion_condition[world.player] = lambda state: state.has("Victory", world.player)
    world.multiworld.completion_condition[world.player] = lambda state: state.has("Victory", world.player)

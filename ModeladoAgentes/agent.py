from mesa import Agent
import numpy as np
import heapq
import math
from util import decimal_to_binary

class FireRescueAgent(Agent):
    def __init__(self, model, is_rescuer=False):
        super().__init__(model)
        self.is_rescuer = is_rescuer
        self.target_fire = None
        self.target_smoke = None
        self.hasVictim = False
        self.AP_PER_TURN = 4     # Action points gained per turn
        self.MAX_AP = 8          # Maximum action points that can be stored
        self.storedAP = 0  # Stored action points
        self.COST_MOVE = 1
        self.COST_MOVE_WITH_VICTIM = 2
        self.COST_EXTINGUISH_SMOKE = 1
        self.COST_EXTINGUISH_FIRE = 2
        self.COST_DAMAGE_WALL = 2  # Cost to damage a wall
        self.COST_OPEN_DOOR = 1    # Cost to open a door

    def step(self):
        self.check_stun()
        self.validate_target_fire()

        # 1. Gain action points at the beginning of the turn
        self.storedAP += self.AP_PER_TURN
        if self.storedAP > self.MAX_AP:
            self.storedAP = self.MAX_AP

        print(f"[Agent {self.unique_id}] Starting step with {self.storedAP} AP at position {self.pos}.")

        if self.target_fire:
            print(f"[Agent {self.unique_id}] Targeting fire at {self.target_fire}.")
        else:
            print(f"[Agent {self.unique_id}] No current fire target.")

        # Main loop: Perform actions until AP falls to the threshold (4 or less)
        while self.storedAP >= 4:
            action_performed = False

            # 1. Extinguish smoke at current position if present
            if self.model.fires.data[self.pos] == 0.5 and self.storedAP >= self.COST_EXTINGUISH_SMOKE:
                self.extinguish_smoke(self.pos)
                action_performed = True
                continue

            # 2. Attempt to extinguish fires and smokes in adjacent cells
            neighbors = self.model.grid.get_neighborhood(
                self.pos,
                moore=False,  # Only cardinal directions
                include_center=False
            )

            for neighbor_pos in neighbors:
                if self.has_wall_between_with_closed_door(self.pos, neighbor_pos):
                    continue

                fire_value = self.model.fires.data[neighbor_pos]
                if fire_value == 1 and self.storedAP >= self.COST_EXTINGUISH_FIRE:
                    self.extinguish_fire(neighbor_pos)
                    action_performed = True
                    break
                elif fire_value == 0.5 and self.storedAP >= self.COST_EXTINGUISH_SMOKE:
                    self.extinguish_smoke(neighbor_pos)
                    action_performed = True
                    break

            if action_performed:
                continue

            # 3. Handle rescuing logic
            if self.is_rescuer:
                action_performed = self.perform_rescuer_actions()

            # 4. Handle non-rescuer logic
            else:
                action_performed = self.perform_non_rescuer_actions()

            if not action_performed:
                print(f"[Agent {self.unique_id}] No immediate actions available. Stopping turn with {self.storedAP} AP.")
                break

        print(f"[Agent {self.unique_id}] Ended turn with {self.storedAP} AP.")

    def perform_rescuer_actions(self):
        """Handles actions specific to rescuer agents when AP > threshold."""
        if self.hasVictim:
            # Check if at exit
            if self.model.is_exit(self.pos):
                self.drop_victim()
                return True
            else:
                # Move towards nearest exit
                target_pos = self.find_nearest_exit()
                if target_pos:
                    path, _ = self.a_star(self.pos, target_pos)
                    if len(path) > 1:
                        next_step = path[1]
                        move_cost = self.get_movement_cost(self.pos, next_step)
                        if self.storedAP >= move_cost:
                            self.move_to(next_step, with_victim=True)
                            return True
        else:
            # Check for victim or POI at current cell
            if self.model.is_victim_at(self.pos):
                self.pick_up_victim()
                return True
            elif self.model.is_poi_at(self.pos):
                self.reveal_poi()
                return True
            else:
                # Move towards nearest victim or POI
                target_pos = self.find_nearest_poi()
                if target_pos:
                    path, _ = self.a_star(self.pos, target_pos)
                    if len(path) > 1:
                        next_step = path[1]
                        move_cost = self.get_movement_cost(self.pos, next_step)
                        if self.storedAP >= move_cost:
                            self.move_to(next_step)
                            return True
                else:
                    print(f"[Agent {self.unique_id}] No victims or POIs left to rescue.")
        return False  # No actions performed

    def perform_non_rescuer_actions(self):
        """Handles actions specific to non-rescuer agents when AP > threshold."""
        fireAssigned = False
        if not self.target_fire:
            fireAssigned = self.assign_fire_target()
        
        if not self.target_fire and not fireAssigned and not self.target_smoke:
            self.assign_smoke_target()

        if self.target_fire and self.storedAP >= self.COST_MOVE:
            path, total_cost = self.a_star(self.pos, self.target_fire)
            if len(path) > 1:
                next_step = path[1]
                move_cost = self.get_movement_cost(self.pos, next_step)
                # Estimate total cost to reach and extinguish fire
                total_action_cost = total_cost + self.COST_EXTINGUISH_FIRE
                if self.storedAP >= total_action_cost:
                    self.move_to(next_step)
                    return True
                else:
                    # Decide whether to wait and accumulate AP or move closer
                    remaining_AP_after_move = self.storedAP - move_cost
                    if remaining_AP_after_move >= 4:
                        # Move closer to avoid wasting AP
                        self.move_to(next_step)
                        return True
                    else:
                        print(f"[Agent {self.unique_id}] Not enough AP to reach fire. Waiting to accumulate AP.")
                        return False  # Wait to accumulate more AP
        elif self.target_smoke and self.storedAP >= self.COST_MOVE:
            path, total_cost = self.a_star(self.pos, self.target_smoke)
            if len(path) > 1:
                next_step = path[1]
                move_cost = self.get_movement_cost(self.pos, next_step)
                # Estimate total cost to reach and extinguish smoke
                total_action_cost = total_cost + self.COST_EXTINGUISH_SMOKE
                if self.storedAP >= total_action_cost:
                    self.move_to(next_step)
                    return True
                else:
                    # Decide whether to wait and accumulate AP or move closer
                    remaining_AP_after_move = self.storedAP - move_cost
                    if remaining_AP_after_move >= 4:
                        # Move closer to avoid wasting AP
                        self.move_to(next_step)
                        return True
                    else:
                        print(f"[Agent {self.unique_id}] Not enough AP to reach smoke. Waiting to accumulate AP.")
                        return False  # Wait to accumulate more AP
        return False  # No actions performed

    def pick_up_victim(self):
        if self.model.is_victim_at(self.pos) and not self.hasVictim:
            self.hasVictim = True
            self.model.remove_victim(self.pos)
            print(f"[Agent {self.unique_id}] Picked up a victim at {self.pos}.")

    def drop_victim(self):
        if self.hasVictim and self.model.is_exit(self.pos):
            self.hasVictim = False
            self.model.people_rescued += 1

            print(f"[Agent {self.unique_id}] Dropped off a victim at exit {self.pos}.")

    def extinguish_fire(self, pos):
        if self.storedAP >= self.COST_EXTINGUISH_FIRE:
            if not self.has_wall_between_with_closed_door(self.pos, pos):
                self.model.set_fire_changes_cell(pos, 0) # Remove fire
                self.storedAP -= self.COST_EXTINGUISH_FIRE
                print(f"[Agent {self.unique_id}] Extinguished fire at {pos}. Remaining AP: {self.storedAP}")

                # Reset target if extinguished fire was the target
                if self.target_fire == pos:
                    print(f"[Agent {self.unique_id}] Resetting target as fire at {pos} was extinguished.")
                    self.target_fire = None

    def extinguish_smoke(self, pos):
        if self.storedAP >= self.COST_EXTINGUISH_SMOKE:
            if not self.has_wall_between_with_closed_door(self.pos, pos):
                self.model.set_fire_changes_cell(pos, 0)  # Remove smoke
                self.storedAP -= self.COST_EXTINGUISH_SMOKE
                print(f"[Agent {self.unique_id}] Extinguished smoke at {pos}. Remaining AP: {self.storedAP}")

    def check_and_extinguish(self, current_pos):
        # Check the current cell
        if self.model.fires.data[current_pos] == 1 and self.storedAP >= self.COST_EXTINGUISH_FIRE:
            self.extinguish_fire(current_pos)
        elif self.model.fires.data[current_pos] == 0.5 and self.storedAP >= self.COST_EXTINGUISH_SMOKE:
            self.extinguish_smoke(current_pos)

        # Check neighboring cells
        neighbors = self.model.grid.get_neighborhood(
            current_pos,
            moore=False,  # Only cardinal directions
            include_center=False
        )
        for neighbor_pos in neighbors:
            fire_value = self.model.fires.data[neighbor_pos]
            if fire_value == 1 and self.storedAP >= self.COST_EXTINGUISH_FIRE:
                self.extinguish_fire(neighbor_pos)
            elif fire_value == 0.5 and self.storedAP >= self.COST_EXTINGUISH_SMOKE:
                self.extinguish_smoke(neighbor_pos)

    def move_to(self, pos, with_victim=False):
        move_cost = self.COST_MOVE_WITH_VICTIM if with_victim else self.COST_MOVE

        fire_value = self.model.fires.data[pos]
        if fire_value == 1:  # Fire detected
            if self.storedAP >= self.COST_EXTINGUISH_FIRE:
                print(f"[Agent {self.unique_id}] Fire detected at {pos}. Extinguishing it before moving.")
                self.extinguish_fire(pos)

        total_cost = move_cost
        door_state = self.model.check_door(self.pos, pos)
        if door_state == 'closed':
            total_cost += self.COST_OPEN_DOOR

        if self.storedAP >= total_cost:
            if door_state == 'closed':
                print(f"[Agent {self.unique_id}] Opening door between {self.pos} and {pos}.")
                self.model.open_door(self.pos, pos)
                self.storedAP -= self.COST_OPEN_DOOR

            self.model.grid.move_agent(self, pos)
            self.storedAP -= move_cost
            print(f"[Agent {self.unique_id}] Moved to {pos}. Remaining AP: {self.storedAP}.")

            # Check current cell and adjacent cells for fire or smoke
            self.check_and_extinguish(pos)
        else:
            print(f"[Agent {self.unique_id}] Not enough AP to move to {pos}. Needed {total_cost}, had {self.storedAP}.")

    def check_actions_after_move(self, pos, remaining_ap):
        actions_available = False
        neighbors = self.model.grid.get_neighborhood(
            pos,
            moore=False,  # Only cardinal directions
            include_center=False
        )

        for neighbor_pos in neighbors:
            if self.has_wall_between_with_closed_door(pos, neighbor_pos):
                continue

            fire_value = self.model.fires.data[neighbor_pos]
            if fire_value == 1 and remaining_ap >= self.COST_EXTINGUISH_FIRE:
                actions_available = True
                break  # Can extinguish fire
            elif fire_value == 0.5 and remaining_ap >= self.COST_EXTINGUISH_SMOKE:
                actions_available = True
                break  # Can extinguish smoke
            else:
                door_state = self.model.check_door(pos, neighbor_pos)
                if door_state == 'closed' and remaining_ap >= self.COST_OPEN_DOOR:
                    actions_available = True
                    break  # Can open door
        return actions_available

    def damage_wall(self, pos1, pos2):
        walls = decimal_to_binary(int(self.model.walls[pos1]))

        (x1, y1) = pos1
        (x2, y2) = pos2

        difference_x = x2 - x1
        difference_y = y2 - y1

        direction = (difference_x, difference_y)
        if self.storedAP >= self.COST_DAMAGE_WALL:
            self.model.set_wall_explosions(walls, direction, pos1, pos2)
            # No need to deduct AP here; it's done in move_to

    def has_wall_between_without_closed_door(self, pos1, pos2):
        return self.model.has_wall_between_without_closed_door(pos1, pos2)

    def has_wall_between_with_closed_door(self, pos1, pos2):
        return self.model.has_wall_between_with_closed_door(pos1, pos2)

    def can_move_between(self, pos1, pos2):
        if self.has_wall_between_with_closed_door(pos1, pos2):
            return False
        # For pathfinding, consider closed doors as passable with extra cost
        return True

    def is_targeting_fire(self, fire_pos):
        # Check if this agent is targeting the given fire
        return hasattr(self, "target_fire") and self.target_fire == fire_pos
    
    def is_targeting_smoke(self, smoke_pos):
        # Check if this agent is targeting the given smoke
        return hasattr(self, "target_smoke") and self.target_smoke == smoke_pos
            
    def find_nearest_exit(self):
        # Get positions of all exits
        exit_positions = list(self.model.entry_points)
        
        if not exit_positions:
            return None  # No exits available

        min_total_cost = float('inf')
        closest_exit = None

        # Iterate over each exit to find the one with the minimal total cost
        for exit_pos in exit_positions:
            path, total_cost = self.a_star(self.pos, exit_pos)
            if path and total_cost < min_total_cost:
                min_total_cost = total_cost
                closest_exit = exit_pos

        return closest_exit
    
    def find_nearest_poi(self):
        # Get positions of all POIs
        poi_positions = self.model.get_poi_positions()
        
        if not poi_positions:
            return None  

        min_total_cost = float('inf')
        closest_poi = None

        # Iterate over each POI to find the one with the minimal total cost
        for poi_pos in poi_positions:
            path, total_cost = self.a_star(self.pos, poi_pos)
            if path and total_cost < min_total_cost:
                min_total_cost = total_cost
                closest_poi = poi_pos

        return closest_poi
    
    def check_stun(self):
        # Check if the agent is in the same cell as a fire
        if self.model.fires.data[self.pos] == 1:
            print(f"[Agent {self.unique_id}] stunned at {self.pos}. Escaping to nearest entry point.")

            # Find the nearest entry point
            nearest_entry = self.find_nearest_exit()
            if nearest_entry:
                # Move instantly to the entry point
                self.model.grid.move_agent(self, nearest_entry)
                print(f"[Agent {self.unique_id}] Escaped to entry point at {nearest_entry}.")

                # Check for fire at the entry point
                if self.model.fires.data[nearest_entry] == 1:
                    # Convert fire to smoke
                    self.model.fires.set_cell(nearest_entry, 0.5)
                    print(f"[Agent {self.unique_id}] Converted fire to smoke at entry point {nearest_entry}.")
    
    def reveal_poi(self):
        poi_type = self.model.reveal_poi_at(self.pos)
        if poi_type == 'v':
            print(f"Victim revealed at {self.pos}")
            # Optionally pick up the victim immediately
            self.pick_up_victim()
        elif poi_type == 'f':
            print(f"False alarm revealed at {self.pos}")

    def is_wall_break_necessary(self, from_pos, to_pos):
        """
        Check if breaking the wall is the only way to reach the target position.
        """
        neighbors = self.model.grid.get_neighborhood(
            from_pos,
            moore=False,
            include_center=False
        )

        for neighbor in neighbors:
            if neighbor == to_pos:
                continue
            if not self.has_wall_between_without_closed_door(from_pos, neighbor):
                return False  # Found an alternative path
    
    def count_walls_between(self, a, b):
        # Simple implementation; improve as needed
        count = 0
        current = a
        while current != b:
            next_step = (current[0] + np.sign(b[0] - current[0]),
                        current[1] + np.sign(b[1] - current[1]))
            if self.has_wall_between_with_closed_door(current, next_step):
                count += 1
            current = next_step
        return count

    def find_highest_priority_fire(self):
        fires = self.model.get_all_fires()
        if not fires:
            return None
        # Filter out fires already targeted by other agents
        untargeted_fires = [fire for fire in fires if not self.model.is_fire_targeted(fire)]
        if not untargeted_fires:
            return None
        
        # Use A* to find the closest fire
        closest_fire = min(
            untargeted_fires,
            key=lambda fire: self.a_star(self.pos, fire)[1]
        )

        return closest_fire
    
    def find_highest_priority_smoke(self):
        smokes = self.model.get_all_smokes()
        if not smokes:
            return None

        untargeted_smokes = [smoke for smoke in smokes if not self.model.is_smoke_targeted(smoke)]
        if not untargeted_smokes:
            return None

        # Use A* to find the closest smoke
        closest_smoke = min(
            untargeted_smokes,
            key=lambda smoke: self.a_star(self.pos, smoke)[1]
        )

        return closest_smoke
    
    def assign_fire_target(self):
        fire_pos = self.find_highest_priority_fire()
        if fire_pos:
            self.target_fire = fire_pos
            print(f"[Agent {self.unique_id}] Assigned new fire target at {fire_pos}.")
            # Register the target in the model to prevent other agents from targeting it
            self.model.fire_targets[self.unique_id] = fire_pos
            return True
        else:
            print(f"[Agent {self.unique_id}] No fires left to target.")
            return False
        
    def assign_smoke_target(self):
        smoke_pos = self.find_highest_priority_smoke()
        if smoke_pos:
            self.target_fire = smoke_pos
            print(f"[Agent {self.unique_id}] Assigned new smoke target at {smoke_pos}.")
            # Register the target in the model to prevent other agents from targeting it
            self.model.smoke_targets[self.unique_id] = smoke_pos
        else:
            print(f"[Agent {self.unique_id}] No smoke left to target.")
    
    def a_star(self, start, goal):
        open_set = []
        heapq.heappush(open_set, (0, start))
        
        came_from = {}
        g_score = {start: 0}
        
        while open_set:
            current = heapq.heappop(open_set)[1]
            
            if current == goal:
                # Reconstruct the path and return both path and cost
                path = self.reconstruct_path(came_from, current)
                return path, g_score[current]
            
            for neighbor in self.get_neighbors(current):
                movement_cost = self.get_movement_cost(current, neighbor)
                tentative_g_score = g_score[current] + movement_cost
                if neighbor not in g_score or tentative_g_score < g_score[neighbor]:
                    came_from[neighbor] = current
                    g_score[neighbor] = tentative_g_score
                    f_score = tentative_g_score + self.a_star_heuristic(neighbor, goal)
                    heapq.heappush(open_set, (f_score, neighbor))
        
        return [], float('inf')  # No path found
    
    def reconstruct_path(self, came_from, current):
        total_path = [current]
        while current in came_from:
            current = came_from[current]
            total_path.insert(0, current)
        return total_path
    
    def get_movement_cost(self, current, neighbor):
        door_state = self.model.check_door(current, neighbor)
        if door_state == 'closed':
            return 1 + self.COST_OPEN_DOOR  # Additional cost for opening a closed door
        else:
            return 1  

    def get_neighbors(self, pos):
        neighbors = []
        directions = [(0, -1), (-1, 0), (0, 1), (1, 0)]  # N, W, S, E
        
        for dx, dy in directions:
            neighbor = (pos[0] + dx, pos[1] + dy)
            
            # Check grid bounds
            if not (0 <= neighbor[0] < self.model.width and 0 <= neighbor[1] < self.model.height):
                continue
            
            # Check for walls
            if not self.has_wall_between_without_closed_door(pos, neighbor):
                neighbors.append(neighbor)
            
        return neighbors

    def a_star_heuristic(self, a, b):
        return abs(a[0] - b[0]) + abs(a[1] - b[1])
    
    def validate_target_fire(self):
        # Check if the current target is still a fire
        if self.target_fire and self.model.fires.data[self.target_fire] != 1:
            print(f"[Agent {self.unique_id}] Target fire at {self.target_fire} is no longer valid.")
            self.target_fire = None
            # Remove the target from model.fire_targets
            if self.unique_id in self.model.fire_targets:
                del self.model.fire_targets[self.unique_id]
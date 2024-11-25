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
        self.hasVictim = False
        self.AP_PER_TURN = 4     # Action points gained per turn
        self.MAX_AP = 8          # Maximum action points that can be stored
        self.storedAP = self.AP_PER_TURN  # Stored action points
        self.COST_MOVE = 1
        self.COST_MOVE_WITH_VICTIM = 2
        self.COST_EXTINGUISH_SMOKE = 1
        self.COST_EXTINGUISH_FIRE = 2
        self.COST_DAMAGE_WALL = 2  # Cost to damage a wall
        self.COST_OPEN_DOOR = 1    # Cost to open a door

    def step(self):
        print(f"[Agent {self.unique_id}] Starting step with {self.storedAP} AP at position {self.pos}.")

        if self.target_fire:
            print(f"[Agent {self.unique_id}] Targeting fire at {self.target_fire}.")
        else:
            print(f"[Agent {self.unique_id}] No current fire target.")
        # 1. Gain action points at the beginning of the turn
        self.storedAP += self.AP_PER_TURN
        if self.storedAP > self.MAX_AP:
            self.storedAP = self.MAX_AP

        while self.storedAP > 0:
            action_performed = False

            # 2. Attempt to extinguish fires and smokes in adjacent cells
            neighbors = self.model.grid.get_neighborhood(
                self.pos,
                moore=False,  # Only cardinal directions
                include_center=False
            )

            if self.storedAP >= self.COST_MOVE:
                target_pos = self.find_highest_priority_fire()
                if target_pos is None:
                    break  # No fires left
                # Register target fire
                self.model.fire_targets[self.unique_id] = target_pos
                path = self.find_path_to(target_pos)

            for neighbor_pos in neighbors:
                if self.has_wall_between(self.pos, neighbor_pos):
                    continue

                

                fire_value = self.model.fires.data[neighbor_pos]
                if fire_value == 1 and self.storedAP >= self.COST_EXTINGUISH_FIRE:
                    self.extinguish_fire(neighbor_pos)
                    action_performed = True
                    break  # Extinguished a fire; check again
                elif fire_value == 0.5 and self.storedAP >= self.COST_EXTINGUISH_SMOKE:
                    self.extinguish_smoke(neighbor_pos)
                    action_performed = True
                    break  # Extinguished smoke; check again

            if action_performed:
                continue  # Go back to while loop to use storedAP

            # Rescuer agent behavior
            if self.is_rescuer:
                if self.hasVictim:
                    # Check if at exit
                    if self.model.is_exit(self.pos):
                        self.drop_victim()
                        action_performed = True
                        continue
                    else:
                        # Move towards nearest exit
                        if self.storedAP >= self.COST_MOVE_WITH_VICTIM:
                            target_pos = self.find_nearest_exit()
                            if target_pos is None:
                                break  # No exit found (should not happen)
                            path = self.find_path_to(target_pos, with_victim=True)
                            if len(path) > 1:
                                next_step = path[1]
                                move_cost = self.calculate_move_cost(self.pos, next_step, with_victim=True)
                                if self.storedAP >= move_cost:
                                    self.move_to(next_step, with_victim=True)
                                    action_performed = True
                                    continue
                                else:
                                    break  # Not enough AP to move
                            else:
                                break  # Cannot move further
                        else:
                            break  # Not enough AP to move
                else:
                    # Check if there is a victim or POI at current cell
                    if self.model.is_victim_at(self.pos):
                        self.pick_up_victim()
                        action_performed = True
                        continue
                    elif self.model.is_poi_at(self.pos):
                        self.reveal_poi()
                        action_performed = True
                        continue
                    else:
                        # Move towards nearest victim or POI
                        if self.storedAP >= self.COST_MOVE:
                            target_pos = self.find_nearest_victim_or_poi()
                            if target_pos is None:
                                break  # No victims or POIs left
                            path = self.find_path_to(target_pos)
                            if len(path) > 1:
                                next_step = path[1]
                                move_cost = self.calculate_move_cost(self.pos, next_step)
                                if self.storedAP >= move_cost:
                                    self.move_to(next_step)
                                    action_performed = True
                                    continue
                if not self.hasVictim and not action_performed:
                    self.assign_fire_target()
            else:
                # Non-rescuer agents' behavior remains the same
                if not self.target_fire:
                    self.assign_fire_target()
                if self.target_fire and self.storedAP >= self.COST_MOVE:
                    path = self.find_path_to(self.target_fire)
                    if len(path) > 1:
                        next_step = path[1]
                        move_cost = self.calculate_move_cost(self.pos, next_step)
                        # Estimate total cost to reach and extinguish fire
                        total_cost = move_cost + (len(path) - 2) * self.COST_MOVE + self.COST_EXTINGUISH_FIRE
                        if self.storedAP >= total_cost:
                            self.move_to(next_step)
                            action_performed = True
                            continue
                        else:
                            # Decide whether to wait and accumulate AP
                            if self.storedAP < self.MAX_AP:
                                break  # Wait to accumulate more AP
                            else:
                                # Move closer if possible
                                if self.storedAP >= move_cost:
                                    self.move_to(next_step)
                                    action_performed = True
                                    continue
                                else:
                                    break  # Not enough AP to move
                    else:
                        break  # Cannot move further
                else:
                    break  # Not enough AP to move

            if not action_performed:
                break  # No action can be performed; end turn

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
            if not self.has_wall_between(self.pos, pos):
                self.model.fires.set_cell(pos, 0)  # Remove fire
                self.storedAP -= self.COST_EXTINGUISH_FIRE
                print(f"[Agent {self.unique_id}] Extinguished fire at {pos}.")


    def extinguish_smoke(self, pos):
        if self.storedAP >= self.COST_EXTINGUISH_SMOKE:
            if not self.has_wall_between(self.pos, pos):
                self.model.fires.set_cell(pos, 0)  # Remove smoke
                self.storedAP -= self.COST_EXTINGUISH_SMOKE
                print(f"[Agent {self.unique_id}] Extinguished smoke at {pos}.")

    def score_fires(self):
        fires = self.model.get_all_fires()
        fire_scores = {}
        for fire_pos in fires:
            cluster_size = self.model.get_fire_cluster_size(fire_pos)
            distance_to_agent = self.heuristic(self.pos, fire_pos)
            proximity_score = max(1, 10 - distance_to_agent)
            total_score = cluster_size * proximity_score
            fire_scores[fire_pos] = total_score
        return fire_scores


    def move_to(self, pos, with_victim=False):
        move_cost = self.COST_MOVE_WITH_VICTIM if with_victim else self.COST_MOVE

        if self.storedAP >= move_cost:
            if self.has_wall_between(self.pos, pos):
                # Check if breaking the wall is the only way
                if self.is_wall_break_necessary(self.pos, pos):
                    if self.storedAP >= self.COST_DAMAGE_WALL + move_cost:
                        # Damage the wall
                        self.damage_wall(self.pos, pos)
                        self.storedAP -= self.COST_DAMAGE_WALL
                        # Move after damaging the wall
                        self.model.grid.move_agent(self, pos)
                        self.storedAP -= move_cost
                else:
                    pass  # Do not break the wall if unnecessary
            else:
                # No wall
                door_state = self.model.check_door(self.pos, pos)
                if door_state == 'closed':
                    if self.storedAP >= self.COST_OPEN_DOOR + move_cost:
                        # Open the door
                        self.model.open_door(self.pos, pos)
                        self.storedAP -= self.COST_OPEN_DOOR
                        # Move after opening the door
                        self.model.grid.move_agent(self, pos)
                        self.storedAP -= move_cost
                else:
                    # Door is open or no door
                    self.model.grid.move_agent(self, pos)
                    self.storedAP -= move_cost

    def calculate_move_cost(self, from_pos, to_pos, with_victim=False):
        move_cost = self.COST_MOVE_WITH_VICTIM if with_victim else self.COST_MOVE
        cost = 0
        if self.has_wall_between(from_pos, to_pos):
            cost += self.COST_DAMAGE_WALL  # Cost to damage wall
            cost += move_cost
        else:
            door_state = self.model.check_door(from_pos, to_pos)
            if door_state == 'closed':
                cost += self.COST_OPEN_DOOR
                cost += move_cost
            else:
                cost += move_cost
        return cost

    def check_actions_after_move(self, pos, remaining_ap):
        actions_available = False
        neighbors = self.model.grid.get_neighborhood(
            pos,
            moore=False,  # Only cardinal directions
            include_center=False
        )

        for neighbor_pos in neighbors:
            if self.has_wall_between(pos, neighbor_pos):
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

    def has_wall_between(self, pos1, pos2):
        return self.model.has_wall_between(pos1, pos2)

    def can_move_between(self, pos1, pos2):
        if self.has_wall_between(pos1, pos2):
            return False
        # For pathfinding, consider closed doors as passable with extra cost
        return True

    def find_highest_priority_fire(self):
        fire_scores = self.score_fires()
        if not fire_scores:
            return None
        # Sort fires based on score, descending
        sorted_fires = sorted(fire_scores.items(), key=lambda x: x[1], reverse=True)
        # Exclude fires targeted by other agents
        for fire_pos, score in sorted_fires:
            if not self.model.is_fire_targeted(fire_pos):
                return fire_pos
        return None
    
    def assign_fire_target(self):
        fire_pos = self.find_highest_priority_fire()
        if fire_pos:
            self.target_fire = fire_pos
    
    def is_targeting_fire(self, fire_pos):
        # Check if this agent is targeting the given fire
        return hasattr(self, "target_fire") and self.target_fire == fire_pos

            
    def find_nearest_exit(self):
        # Find the nearest exit cell
        queue = [(0, self.pos)]
        visited = set()
        while queue:
            cost, current_pos = heapq.heappop(queue)
            if current_pos in visited:
                continue
            visited.add(current_pos)

            if self.model.is_exit(current_pos):
                return current_pos

            neighbors = self.model.grid.get_neighborhood(
                current_pos,
                moore=False,
                include_center=False
            )
            for neighbor in neighbors:
                if neighbor not in visited and self.can_move_between(current_pos, neighbor):
                    heapq.heappush(queue, (cost + 1, neighbor))
        return None
    
    def find_nearest_victim(self):
        # Find the nearest cell with a victim
        queue = [(0, self.pos)]
        visited = set()
        while queue:
            cost, current_pos = heapq.heappop(queue)
            if current_pos in visited:
                continue
            visited.add(current_pos)

            if self.model.is_victim_at(current_pos):
                return current_pos

            neighbors = self.model.grid.get_neighborhood(
                current_pos,
                moore=False,
                include_center=False
            )
            for neighbor in neighbors:
                if neighbor not in visited and self.can_move_between(current_pos, neighbor):
                    heapq.heappush(queue, (cost + 1, neighbor))
        return None
    
    def find_nearest_victim_or_poi(self):
    # Find the nearest cell with a victim or POI
        queue = [(0, self.pos)]
        visited = set()
        while queue:
            cost, current_pos = heapq.heappop(queue)
            if current_pos in visited:
                continue
            visited.add(current_pos)

            if self.model.is_victim_at(current_pos) or self.model.is_poi_at(current_pos):
                return current_pos

            neighbors = self.model.grid.get_neighborhood(
                current_pos,
                moore=False,
                include_center=False
            )
            for neighbor in neighbors:
                if neighbor not in visited and self.can_move_between(current_pos, neighbor):
                    heapq.heappush(queue, (cost + 1, neighbor))
        return None
    
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
            if not self.has_wall_between(from_pos, neighbor):
                return False  # Found an alternative path

        
    def find_path_to(self, target_pos, with_victim=False):
        """
        A* algorithm considering walls and doors, avoiding unnecessary wall-breaking.
        """
        open_set = []
        heapq.heappush(open_set, (0, self.pos))
        came_from = {}
        g_score = {self.pos: 0}
        while open_set:
            _, current = heapq.heappop(open_set)
            if current == target_pos:
                return self.reconstruct_path(came_from, current)

            neighbors = self.model.grid.get_neighborhood(
                current,
                moore=False,
                include_center=False
            )
            for neighbor in neighbors:
                move_cost = self.calculate_move_cost(current, neighbor, with_victim)

                # Skip paths requiring unnecessary wall-breaking
                if self.has_wall_between(current, neighbor) and not self.is_wall_break_necessary(current, neighbor):
                    continue

                tentative_g_score = g_score[current] + move_cost
                if neighbor not in g_score or tentative_g_score < g_score[neighbor]:
                    came_from[neighbor] = current
                    g_score[neighbor] = tentative_g_score
                    f_score = tentative_g_score + self.heuristic(neighbor, target_pos)
                    heapq.heappush(open_set, (f_score, neighbor))
        return []  # Return empty path if no valid path found


    def reconstruct_path(self, came_from, current):
        total_path = [current]
        while current in came_from:
            current = came_from[current]
            total_path.insert(0, current)
        return total_path
    
    def count_walls_between(self, a, b):
    # Simple implementation; improve as needed
        count = 0
        current = a
        while current != b:
            next_step = (current[0] + np.sign(b[0] - current[0]),
                        current[1] + np.sign(b[1] - current[1]))
            if self.has_wall_between(current, next_step):
                count += 1
            current = next_step
        return count


    def heuristic(self, a, b):
        # Manhattan distance
        distance = abs(a[0] - b[0]) + abs(a[1] - b[1])
        # Add wall penalty
        walls_between = self.count_walls_between(a, b)
        wall_penalty = walls_between * 5  # Adjust weight as needed
        return distance + wall_penalty

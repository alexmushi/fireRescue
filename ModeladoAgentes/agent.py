from mesa import Agent
import numpy as np
import heapq
import math
from util import decimal_to_binary

class FireRescueAgent(Agent):
    def __init__(self, model, is_rescuer=False):
        super().__init__(model)
        self.is_rescuer = is_rescuer
        self.hasVictim = False
        self.AP_PER_TURN = 4     # Action points gained per turn
        self.MAX_AP = 8          # Maximum action points that can be stored
        self.storedAP = self.AP_PER_TURN  # Stored action points
        self.COST_MOVE = 1
        self.COST_EXTINGUISH_SMOKE = 1
        self.COST_EXTINGUISH_FIRE = 2
        self.COST_DAMAGE_WALL = 2  # Cost to damage a wall
        self.COST_OPEN_DOOR = 1    # Cost to open a door

    def step(self):
        if self.is_rescuer:
            self.storedAP += self.AP_PER_TURN
            if self.storedAP > self.MAX_AP:
                self.storedAP = self.MAX_AP

        # Agentes normales
        else:
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

                # 3. Attempt to open doors in adjacent cells if needed
                for neighbor_pos in neighbors:
                    if self.storedAP >= self.COST_OPEN_DOOR:
                        door_state = self.model.check_door(self.pos, neighbor_pos)
                        if door_state == 'closed':
                            self.model.open_door(self.pos, neighbor_pos)
                            self.storedAP -= self.COST_OPEN_DOOR
                            action_performed = True
                            break  # Opened a door; check again

                if action_performed:
                    continue  # Go back to while loop to use storedAP

                # 4. Decide whether to move
                # Agent only moves if it can perform an action after moving or has more than 4 AP
                if self.storedAP >= self.COST_MOVE:
                    target_pos = self.find_nearest_fire()
                    if target_pos is None:
                        break  # No fires left
                    path = self.find_path_to(target_pos)
                    if len(path) > 1:
                        next_step = path[1]
                        # Calculate the cost to move to next_step
                        move_cost = self.calculate_move_cost(self.pos, next_step)
                        if self.storedAP >= move_cost:
                            # Calculate remaining AP after moving
                            ap_after_move = self.storedAP - move_cost
                            # From next_step, check if there is any action the agent can perform
                            actions_available = self.check_actions_after_move(next_step, ap_after_move)
                            if actions_available:
                                # Proceed to move
                                self.move_to(next_step)
                                action_performed = True
                                continue  # After moving, check again
                            else:
                                if self.storedAP > 4:
                                    # Proceed to move anyway to reach 8 AP next turn
                                    self.move_to(next_step)
                                    action_performed = True
                                    continue  # After moving, check again
                                else:
                                    # Do not move; save AP
                                    break
                        else:
                            break  # Not enough AP to move
                    else:
                        break  # Cannot move further
                else:
                    break  # Not enough AP to move

        # End of turn; any cleanup can be done here

    def extinguish_fire(self, pos):
        if self.storedAP >= self.COST_EXTINGUISH_FIRE:
            if not self.has_wall_between(self.pos, pos):
                self.model.fires.set_cell(pos, 0)  # Remove fire
                self.storedAP -= self.COST_EXTINGUISH_FIRE

    def extinguish_smoke(self, pos):
        if self.storedAP >= self.COST_EXTINGUISH_SMOKE:
            if not self.has_wall_between(self.pos, pos):
                self.model.fires.set_cell(pos, 0)  # Remove smoke
                self.storedAP -= self.COST_EXTINGUISH_SMOKE

    def move_to(self, pos):
        if self.storedAP >= self.COST_MOVE:
            if self.has_wall_between(self.pos, pos):
                # Wall between current position and target position
                if self.storedAP >= self.COST_DAMAGE_WALL + self.COST_MOVE:
                    # Damage the wall
                    self.damage_wall(self.pos, pos)
                    self.storedAP -= self.COST_DAMAGE_WALL
                    # Move after damaging the wall
                    self.model.grid.move_agent(self, pos)
                    self.storedAP -= self.COST_MOVE
                else:
                    pass  # Cannot move due to insufficient AP
            else:
                # No wall
                door_state = self.model.check_door(self.pos, pos)
                if door_state == 'closed':
                    if self.storedAP >= self.COST_OPEN_DOOR + self.COST_MOVE:
                        # Open the door
                        self.model.open_door(self.pos, pos)
                        self.storedAP -= self.COST_OPEN_DOOR
                        # Move after opening the door
                        self.model.grid.move_agent(self, pos)
                        self.storedAP -= self.COST_MOVE
                    else:
                        pass  # Cannot move due to insufficient AP
                else:
                    # Door is open or no door
                    self.model.grid.move_agent(self, pos)
                    self.storedAP -= self.COST_MOVE

    def calculate_move_cost(self, from_pos, to_pos):
        cost = 0
        if self.has_wall_between(from_pos, to_pos):
            cost += self.COST_DAMAGE_WALL  # Cost to damage wall
            cost += self.COST_MOVE
        else:
            door_state = self.model.check_door(from_pos, to_pos)
            if door_state == 'closed':
                cost += self.COST_OPEN_DOOR
                cost += self.COST_MOVE
            else:
                cost += self.COST_MOVE
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

    def find_nearest_fire(self):
        # Find the nearest cell with fire
        queue = [(0, self.pos)]
        visited = set()
        while queue:
            cost, current_pos = heapq.heappop(queue)
            if current_pos in visited:
                continue
            visited.add(current_pos)

            fire_value = self.model.fires.data[current_pos]
            if fire_value == 1:
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

    def find_path_to(self, target_pos):
        # A* algorithm considering walls and doors
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
                if self.has_wall_between(current, neighbor):
                    continue  # Cannot pass through walls
                door_state = self.model.check_door(current, neighbor)
                if door_state == 'closed':
                    move_cost = self.COST_MOVE + self.COST_OPEN_DOOR
                else:
                    move_cost = self.COST_MOVE
                tentative_g_score = g_score[current] + move_cost
                if neighbor not in g_score or tentative_g_score < g_score[neighbor]:
                    came_from[neighbor] = current
                    g_score[neighbor] = tentative_g_score
                    f_score = tentative_g_score + self.heuristic(neighbor, target_pos)
                    heapq.heappush(open_set, (f_score, neighbor))
        return []

    def reconstruct_path(self, came_from, current):
        total_path = [current]
        while current in came_from:
            current = came_from[current]
            total_path.insert(0, current)
        return total_path

    def heuristic(self, a, b):
        # Manhattan distance
        return abs(a[0] - b[0]) + abs(a[1] - b[1])
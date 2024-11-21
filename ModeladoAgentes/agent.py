# Agent.py

from mesa import Agent
import numpy as np
import heapq
import math
from util import decimal_to_binary

class FireRescueAgent(Agent):
    def __init__(self, model):
        super().__init__(model)
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
        # 1. Gain action points at the beginning of the turn
        self.storedAP += self.AP_PER_TURN
        if self.storedAP > self.MAX_AP:
            self.storedAP = self.MAX_AP

        action_performed = False

        # 2. Attempt to open doors in adjacent cells
        neighbors = self.model.grid.get_neighborhood(
            self.pos,
            moore=False,  # Only cardinal directions
            include_center=False
        )

        for neighbor_pos in neighbors:
            if self.storedAP >= self.COST_OPEN_DOOR:
                door_state = self.model.check_door(self.pos, neighbor_pos)
                if door_state == 'closed':
                    self.model.open_door(self.pos, neighbor_pos)
                    self.storedAP -= self.COST_OPEN_DOOR
                    action_performed = True
                    break  # Perform one action per step

        # 3. Extinguish fires and smokes in adjacent cells
        for neighbor_pos in neighbors:
            if self.has_wall_between(self.pos, neighbor_pos):
                continue

            fire_value = self.model.fires.data[neighbor_pos]
            if fire_value == 1 and self.storedAP >= self.COST_EXTINGUISH_FIRE:
                self.extinguish_fire(neighbor_pos)
                action_performed = True
            elif fire_value == 0.5 and self.storedAP >= self.COST_EXTINGUISH_SMOKE:
                self.extinguish_smoke(neighbor_pos)
                action_performed = True

        # 4. Attempt to rescue POI in the same cell
        current_poi_value = self.model.points_of_interest.data[self.pos]
        if current_poi_value != '' and self.storedAP >= 1:
            self.rescue_poi(self.pos)
            action_performed = True

        # 5. Decide whether to move or save AP
        if not action_performed:
            if self.storedAP < self.MAX_AP:
                pass  # Save AP if not full
            else:
                while self.storedAP >= self.COST_MOVE:
                    target_pos = self.find_nearest_fire()
                    if target_pos is None:
                        break
                    path = self.find_path_to(target_pos)
                    if len(path) > 1:
                        next_step = path[1]
                        self.move_to(next_step)
                    else:
                        break

                    # Attempt an action in the new position
                    fire_value = self.model.fires.data[self.pos]
                    if fire_value == 1 and self.storedAP >= self.COST_EXTINGUISH_FIRE:
                        self.extinguish_fire(self.pos)
                        break
                    elif fire_value == 0.5 and self.storedAP >= self.COST_EXTINGUISH_SMOKE:
                        self.extinguish_smoke(self.pos)
                        break
                    elif current_poi_value != '' and self.storedAP >= 1:
                        self.rescue_poi(self.pos)
                        break

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

    def rescue_poi(self, pos):
        self.model.points_of_interest.set_cell(pos, '')  # Rescue POI
        self.hasVictim = True  # Update state if necessary

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
                    pass  # Cannot move
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
                        pass  # Cannot move
                else:
                    # Door is open or no door
                    self.model.grid.move_agent(self, pos)
                    self.storedAP -= self.COST_MOVE

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

# Mesa imports
from mesa import Model
from mesa.space import MultiGrid, PropertyLayer
from mesa.time import RandomActivation
from mesa.datacollection import DataCollector

# NumPy y Pandas imports
import numpy as np
import pandas as pd
import random

from util import get_game_variables, decimal_to_binary, binary_to_decimal, get_walls

# Import the FireRescueAgent class from the agent.py file
from agent import FireRescueAgent

class FireRescueModel(Model):
    def __init__(self, width = 10, height = 8, agents = 6, seed=None):
        super().__init__(seed=seed)
        self.width = width
        self.height = height

        self.points_of_interest = PropertyLayer(
            name="Points of Interest", width=width, height=height, default_value='', dtype=str)

        self.fires = PropertyLayer(
            name="Fires", width=width, height=height, default_value=0.0, dtype=float)

        self.grid = MultiGrid(width, height, torus=False,
            property_layers=[self.points_of_interest, self.fires])

        self.damage_points = 0
        self.false_alarms = 4
        self.victims = 8

        self.set_game_data("BeachHouse.txt")

        for _ in range(agents):
            agent = FireRescueAgent(self)
            entry_point = random.choice(self.entry_points)
            (x, y) = entry_point
            self.grid.place_agent(agent, (x, y))

    def set_game_data(self, archivo):
        walls, damage, points_of_interest, fires, doors, entry_points = get_game_variables(archivo)
        for poi in points_of_interest:
            x = poi['x']
            y = poi['y']
            pos = (x, y)
            self.points_of_interest.set_cell(pos, poi['type'])

        for fire in fires:
            x = fire['x']
            y = fire['y']
            pos = (x, y)
            self.fires.set_cell(pos, 1)
        
        self.walls = walls
        self.damage = damage
        self.doors = doors
        self.entry_points = entry_points
    
    def check_walls(self, pos, complete = False):
        (x, y) = pos
        wall_value = int(self.walls[x, y])

        binary_wall = decimal_to_binary(wall_value)

        up = binary_wall[0]
        left = binary_wall[1]
        down = binary_wall[2]
        right = binary_wall[3]

        possible_positions = []
        complete_positions = []

        if complete == True:
            if y - 1 >= 0:
                complete_positions.append((x, y - 1))
            if x - 1 >= 0:
                complete_positions.append((x - 1, y))
            if y + 1 < self.height:
                complete_positions.append((x, y + 1))
            if x + 1 < self.width:
                complete_positions.append((x + 1, y))

        # If no wall is found then it is a possible position
        if up == '0' and y - 1 >= 0:
            possible_positions.append((x, y - 1))
        if left == '0' and x - 1 >= 0:
            possible_positions.append((x - 1, y))
        if down == '0' and y + 1 < self.height:
            possible_positions.append((x, y + 1))
        if right == '0' and x + 1 < self.width:
            possible_positions.append((x + 1, y))

        if complete == True:
            return possible_positions, complete_positions
        else:
            return possible_positions
    
    def has_wall_between(self, pos1, pos2):
        (x1, y1) = pos1
        (x2, y2) = pos2

        difference_x = x2 - x1
        difference_y = y2 - y1

        direction = (difference_x, difference_y)

        walls = decimal_to_binary(int(self.walls[pos1]))

        if direction == (0, -1):
            return walls[0] == '1'
        elif direction == (-1, 0):
            return walls[1] == '1'
        elif direction == (0, 1):
            return walls[2] == '1'
        elif direction == (1, 0):
            return walls[3] == '1'

    def check_door(self, cell1, cell2):
        door_key = frozenset([cell1, cell2])
        if door_key in self.doors:
            return self.doors[door_key]
        return None
    
    def open_door(self, cell1, cell2):
        door_key = frozenset([cell1, cell2])
        if door_key in self.doors:
            if self.doors[door_key] != 'destroyed':
                self.doors[door_key] = 'open'
    
    def destroy_door(self, cell1, cell2):
        door_key = frozenset([cell1, cell2])
        if door_key in self.doors:
            self.doors[door_key] = 'destroyed'
    
    def close_door(self, cell1, cell2):
        door_key = frozenset([cell1, cell2])
        if door_key in self.doors:
            if self.doors[door_key] != 'destroyed':
                self.doors[door_key] = 'closed'
    
    def select_random_internal_cell(self):
        MIN_X, MAX_X = 1, 8
        MIN_Y, MAX_Y = 1, 6

        x = random.randint(MIN_X, MAX_X)
        y = random.randint(MIN_Y, MAX_Y)

        return (x, y)
    
    def assign_new_points_of_interest(self):

        possible_poi = []
        if self.false_alarms > 0:
            possible_poi.append('f')
        if self.victims > 0:
            possible_poi.append('v')
        
        chosen_poi = random.choice(possible_poi)

        (x, y) = self.select_random_internal_cell()

        self.points_of_interest.data[x, y] = chosen_poi
    
        if chosen_poi == 'f':
            self.false_alarms -= 1
        elif chosen_poi == 'v':
            self.victims -= 1
    
    def check_missing_points_of_interest(self):
        non_empty_count = np.count_nonzero(self.points_of_interest.data != '')
        if non_empty_count < 3:
            self.assign_new_points_of_interest()
    
    def destroy_wall(self, pos, wall_index_to_destroy):
        current_wall_value = decimal_to_binary(int(self.walls[pos]))
        current_wall_list = list(current_wall_value)

        current_wall_list[wall_index_to_destroy] = '0'

        new_wall_value = ''.join(current_wall_list)

        self.walls[pos] = binary_to_decimal(new_wall_value)

    def damage_wall(self, pos, wall_index_to_damage, apply_damage = True):
        current_wall_damage = self.damage[pos]

        wall_damage_list = list(current_wall_damage)
        wall_damage_list[wall_index_to_damage] += 1
        self.damage[pos] = tuple(wall_damage_list)

        if apply_damage == True:
            self.damage_points += 1
    
    def explosion_wall(self, pos, wall_index_to_explode, apply_damage = True):
        current_wall_damage = self.damage[pos]

        if current_wall_damage[wall_index_to_explode] < 2:
            self.damage_wall(pos, wall_index_to_explode, apply_damage)

        new_wall_damage = self.damage[pos]

        if new_wall_damage[wall_index_to_explode] == 2:
            self.destroy_wall(pos, wall_index_to_explode)
    
    def set_wall_explosions(self, walls, direction, current_pos, new_pos):
        if direction == (0, -1):
            if walls[0] == '1':
                self.explosion_wall(current_pos, 0)
                self.explosion_wall(new_pos, 2, False)
        elif direction == (-1, 0):
            if walls[1] == '1':
                self.explosion_wall(current_pos, 1)
                self.explosion_wall(new_pos, 3, False)
        elif direction == (0, 1):
            if walls[2] == '1':
                self.explosion_wall(current_pos, 2)
                self.explosion_wall(new_pos, 0, False)
        elif direction == (1, 0):
            if walls[3] == '1':
                self.explosion_wall(current_pos, 3)
                self.explosion_wall(new_pos, 1, False)
    
    def continue_explosion(self, explosion_base_pos, current_pos):

        (x, y) = current_pos
        (x_base, y_base) = explosion_base_pos

        difference_x = x - x_base
        difference_y = y - y_base

        direction = (difference_x, difference_y)
        new_pos = (x + direction[0], y + direction[1])

        if not (0 <= new_pos[0] < self.width and 0 <= new_pos[1] < self.height):
            return
        
        adjacent_with_no_walls = self.check_walls(current_pos)

        if new_pos in adjacent_with_no_walls:
            if self.fires.data[new_pos] == 0:
                self.fires.set_cell(new_pos, 1)
            elif self.fires.data[new_pos] == 0.5:
                self.fires.set_cell(new_pos, 1)
            elif self.fires.data[new_pos] == 1:
                self.continue_explosion(current_pos, new_pos)
        elif self.check_door(current_pos, new_pos) is not None:
            door_state = self.check_door(current_pos, new_pos)
            if door_state == 'closed':
                self.destroy_door(current_pos, new_pos)
            elif door_state == 'open' or door_state == 'destroyed':
                if self.fires.data[new_pos] == 0:
                    self.fires.set_cell(new_pos, 1)
                elif self.fires.data[new_pos] == 0.5:
                    self.fires.set_cell(new_pos, 1)
                elif self.fires.data[new_pos] == 1:
                    self.continue_explosion(current_pos, new_pos)
        else:
            # Has a wall or door so different rules apply
            walls = decimal_to_binary(int(self.walls[current_pos]))

            self.set_wall_explosions(walls, direction, current_pos, new_pos)

    def explosion(self, pos):
        adjacent_with_no_walls, all_adjacent_cells = self.check_walls(pos, True)

        for adjacent in adjacent_with_no_walls:
            if self.fires.data[adjacent] == 0:
                self.fires.set_cell(adjacent, 1)
            elif self.fires.data[adjacent] == 0.5:
                self.fires.set_cell(adjacent, 1)
            elif self.fires.data[adjacent] == 1:
                self.continue_explosion(pos, adjacent)

        cells_with_walls = []
        for cell in all_adjacent_cells:
            if cell not in adjacent_with_no_walls:
                cells_with_walls.append(cell)

        for cell in cells_with_walls:
            if self.check_door(pos, cell) is not None:
                door_state = self.check_door(pos, cell)
                if door_state == 'closed':
                    self.destroy_door(pos, cell)
                elif door_state == 'open' or door_state == 'destroyed':
                    if self.fires.data[cell] == 0:
                        self.fires.set_cell(cell, 1)
                    elif self.fires.data[cell] == 0.5:
                        self.fires.set_cell(cell, 1)
                    elif self.fires.data[cell] == 1:
                        self.continue_explosion(pos, cell)
            else: 
                walls = decimal_to_binary(int(self.walls[pos]))

                difference_x = cell[0] - pos[0]
                difference_y = cell[1] - pos[1]

                direction = (difference_x, difference_y)

                self.set_wall_explosions(walls, direction, pos, cell)

    def assign_fire(self):
        (x, y) = self.select_random_internal_cell()

        pos = (x, y)

        if self.fires.data[x, y] == 1:
            self.explosion(pos)
        elif self.fires.data[x, y] == 0.5:
            self.fires.set_cell(pos, 1)
        elif self.fires.data[x, y] == 0:
            self.fires.set_cell(pos, 0.5)
    
    def convert_smoke_to_fire(self, pos):
        possible_positions, all_adjacent_cells = self.check_walls(pos, True)

        for position in range(len(possible_positions)):
            current_position = possible_positions[position]
            fire_position = self.fires.data[current_position]
            if fire_position == 1:
                self.fires.set_cell(pos, 1)
                break
        
        cells_with_walls = []
        for cell in all_adjacent_cells:
            if cell not in possible_positions:
                cells_with_walls.append(cell)
        
        for cell in cells_with_walls:
            if self.check_door(pos, cell) is not None:
                door_state = self.check_door(pos, cell)
                fire_position = self.fires.data[cell]
                if door_state == 'open':
                    if fire_position == 1:
                        self.fires.set_cell(pos, 1)
                        break
    
    def check_smoke(self):
        cells_with_smoke = self.fires.select_cells(lambda x: x == 0.5)

        for cell in cells_with_smoke:
            self.convert_smoke_to_fire(cell)

    def print_map(self, walls_array, fires_array):
        height, width = walls_array.shape
        for y in range(height):
            # Print the top walls of the current row
            top_line = ''
            for x in range(width):
                wall_value = walls_array[y, x]
                walls = get_walls(wall_value)
                top_line += '+'
                if walls['top']:
                    if y > 0:
                        door_state = self.check_door((x, y), (x, y - 1))
                        if door_state == 'open':
                            top_line += ' O '
                        elif door_state == 'closed':
                            top_line += ' D '
                        elif door_state == 'destroyed':
                            top_line += ' X '
                        else:
                            top_line += '---'
                    else:
                        top_line += '---'
                else:
                    top_line += '   '
            top_line += '+'
            print(top_line)
            
            # Print the left walls and cell contents
            middle_line = ''
            for x in range(width):
                wall_value = walls_array[y, x]
                walls = get_walls(wall_value)
                if walls['left']:
                    if x > 0:
                        door_state = self.check_door((x, y), (x - 1, y))
                        if door_state == 'open':
                            middle_line += 'O'
                        elif door_state == 'closed':
                            middle_line += 'D'
                        elif door_state == 'destroyed':
                            middle_line += 'X'
                        else:
                            middle_line += '|'
                    else:
                        middle_line += '|'
                else:
                    middle_line += ' '
                fire_value = fires_array[y, x]
                if fire_value == 1:
                    cell_content = ' F '
                elif fire_value == 0.5:
                    cell_content = ' S '
                else:
                    cell_content = '   '  
                middle_line += cell_content
            # Handle the right wall of the last cell in the row
            last_cell_walls = get_walls(walls_array[y, width - 1])
            if last_cell_walls['right']:
                # Assuming no doors on the right edge
                middle_line += '|'
            else:
                middle_line += ' '
            print(middle_line)
        
        # Print the bottom walls of the last row
        bottom_line = ''
        for x in range(width):
            wall_value = walls_array[height - 1, x]
            walls = get_walls(wall_value)
            bottom_line += '+'
            if walls['bottom']:
                if y < height - 1:
                    door_state = self.check_door((x, height - 1), (x, height))
                    if door_state == 'open':
                        bottom_line += ' O '
                    elif door_state == 'closed':
                        bottom_line += ' D '
                    elif door_state == 'destroyed':
                        bottom_line += ' X '
                    else:
                        bottom_line += '---'
                else:
                    bottom_line += '---'
            else:
                bottom_line += '   '
        bottom_line += '+'
        print(bottom_line)
    
    def step(self):
        self.agents.shuffle_do("step")

        self.assign_fire()
        
        self.check_smoke()

        self.check_missing_points_of_interest()

model = FireRescueModel()

model.print_map(model.walls.T, model.fires.data.T)

for i in range(50):
    model.step()

print("After 50 steps" + "\n")

model.print_map(model.walls.T, model.fires.data.T)

print("Damage Points: ", model.damage_points)
print(model.steps)

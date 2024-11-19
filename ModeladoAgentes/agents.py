# Mesa imports
from mesa import Agent, Model
from mesa.space import MultiGrid, PropertyLayer
from mesa.time import RandomActivation
from mesa.datacollection import DataCollector

# Matplotlib imports
import matplotlib
import matplotlib.pyplot as plt
import matplotlib.animation as animation

# Configuración de Matplotlib
plt.rcParams["animation.html"] = "jshtml"
matplotlib.rcParams['animation.embed_limit'] = 2**128

# NumPy y Pandas imports
import numpy as np
import pandas as pd
import random

from util import get_game_variables, decimal_to_binary

class FireRescueAgent(Agent):
    def __init__(self, id, model):
        super().__init__(id, model)

        self.hasVictim = False
        self.actionPoints = 3

class FireRescueModel(Model):
    def __init__(self, width = 10, height = 8, agents = 6):
        super().__init__()
        self.width = width
        self.height = height
        self.schedule = RandomActivation(self)

        self.points_of_interest = PropertyLayer(
            name="Points of Interest", width=width, height=height, default_value='', dtype=str)

        self.fires = PropertyLayer(
            name="Fires", width=width, height=height, default_value=0, dtype=float)

        self.grid = MultiGrid(width, height, torus=False,
            property_layers=[self.points_of_interest, self.fires])

        self.damage = 0
        self.false_alarms = 4
        self.victims = 8

        self.set_game_data("inputs.txt")
    

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
    
    def check_walls(self, pos):
        (x, y) = pos
        wall_value = int(self.walls[x, y])

        binary_wall = decimal_to_binary(wall_value)

        up = binary_wall[0]
        left = binary_wall[1]
        down = binary_wall[2]
        right = binary_wall[3]

        possible_positions = []

        # If no wall is found then it is a possible position
        if up == '0':
            possible_positions.append((x, y - 1))
        if left == '0':
            possible_positions.append((x - 1, y))
        if down == '0':
            possible_positions.append((x, y + 1))
        if right == '0':
            possible_positions.append((x + 1, y))
        
        return possible_positions

    def check_door(self, cell1, cell2):
        door_key = frozenset([cell1, cell2])
        return self.doors[door_key]
    
    def open_door(self, cell1, cell2):
        door_key = frozenset([cell1, cell2])
        if door_key in self.doors:
            self.doors[door_key] = 'open'
    
    def close_door(self, cell1, cell2):
        door_key = frozenset([cell1, cell2])
        if door_key in self.doors:
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
    
    def assign_fire(self):
        (x, y) = self.select_random_internal_cell()

        pos = (x, y)

        if self.fires.data[x, y] == 1:
            # TODO: Agregar exparsión de fuego (explosiones y todo)
            print("Fire where it exists")
        elif self.fires.data[x, y] == 0.5:
            self.fires.set_cell(pos, 1)
        elif self.fires.data[x, y] == 0:
            self.fires.set_cell(pos, 0.5)
    
    def convert_smoke_to_fire(self, pos):
        possible_positions = self.check_walls(pos)

        for position in range(len(possible_positions)):
            current_position = possible_positions[position]
            fire_position = self.fires.data[current_position]
            if fire_position == 1:
                print("Smoke to fire at", pos)
                print("Walls: ", decimal_to_binary(int(self.walls[pos])))
                self.fires.set_cell(pos, 1)
                break
    
    def check_smoke(self):
        cells_with_smoke = self.fires.select_cells(lambda x: x == 0.5)

        for cell in cells_with_smoke:
            self.convert_smoke_to_fire(cell)

    def step(self):
        self.schedule.step()

        self.check_smoke()

        self.assign_fire()

        self.check_missing_points_of_interest()

        for y in range(self.height):
            row_values = []
            for x in range(self.width):
                fire_value = self.fires.data[x, y]
                row_values.append('🔥' if fire_value == 1 else '💨' if fire_value == 0.5 else '.')
            print(' '.join(row_values))

        print()

model = FireRescueModel()

for i in range(5): 
    model.step()
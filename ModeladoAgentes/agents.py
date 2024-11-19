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

from util import get_game_variables

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
        (x, y) = pos

        positions_to_check = []
        positions_to_check.append(self.fires.data[x, y + 1])
        positions_to_check.append(self.fires.data[x, y - 1])
        positions_to_check.append(self.fires.data[x - 1, y])
        positions_to_check.append(self.fires.data[x + 1, y])

        for position in range(len(positions_to_check)):
            current_position = positions_to_check[position]
            if current_position == 1:
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

model = FireRescueModel()

for i in range(5): 
    model.step()
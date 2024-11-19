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
            name="Fires", width=width, height=height, default_value=0, dtype=int)

        self.grid = MultiGrid(width, height, torus=False,
            property_layers=[self.points_of_interest, self.fires])

        self.damage = 0

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

    def step(self):
        self.schedule.step()

model = FireRescueModel()

model.step()
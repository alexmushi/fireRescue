# Mesa imports
from mesa import Agent

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

class FireRescueAgent(Agent):
    def __init__(self, id, model):
        super().__init__(id, model)

        self.hasVictim = False
        self.actionPoints = 3
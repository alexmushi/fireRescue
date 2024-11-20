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

import heapq  # Para el algoritmo A*
import math

from util import decimal_to_binary

class FireRescueAgent(Agent):
    def __init__(self, model):
        super().__init__(model)
        self.hasVictim = False
        self.AP_PER_TURN = 4     # Puntos de acción ganados por turno
        self.MAX_AP = 8          # Máximo de puntos de acción que se pueden almacenar
        self.storedAP = self.AP_PER_TURN  # AP almacenados
        self.COST_MOVE = 1
        self.COST_EXTINGUISH_SMOKE = 1
        self.COST_EXTINGUISH_FIRE = 2
        self.COST_DAMAGE_WALL = 2  # Costo de dañar una pared

    def step(self):
        # 1. Ganar puntos de acción al inicio del turno
        self.storedAP += self.AP_PER_TURN
        if self.storedAP > self.MAX_AP:
            self.storedAP = self.MAX_AP

        action_performed = False

        # 2. Apagar fuegos y humos adyacentes
        neighbors = self.model.grid.get_neighborhood(
            self.pos,
            moore=False,  # Solo direcciones cardinales
            include_center=True
        )

        for neighbor_pos in neighbors:
            # Verificar si hay una pared entre la posición actual y la posición vecina
            if self.has_wall_between(self.pos, neighbor_pos):
                continue  # No se puede interactuar a través de paredes

            # Obtener valor de fuego en la posición vecina
            fire_value = self.model.fires.data[neighbor_pos]

            if fire_value == 1 and self.storedAP >= self.COST_EXTINGUISH_FIRE:
                # Apagar fuego
                self.extinguish_fire(neighbor_pos)
                action_performed = True
            elif fire_value == 0.5 and self.storedAP >= self.COST_EXTINGUISH_SMOKE:
                # Apagar humo
                self.extinguish_smoke(neighbor_pos)
                action_performed = True

        # 3. Intentar rescatar POI en la misma casilla
        current_poi_value = self.model.points_of_interest.data[self.pos]
        if current_poi_value != '' and self.storedAP >= 1:
            # Rescatar POI
            self.rescue_poi(self.pos)
            action_performed = True

        # 4. Decidir si mover o guardar AP
        if not action_performed:
            if self.storedAP < self.MAX_AP:
                # Guardar AP, no hacer nada
                pass
            else:
                # AP almacenados al máximo, moverse hacia el objetivo hasta tener 4 AP
                while self.storedAP > 4:
                    target_pos = self.find_nearest_objective()
                    if target_pos is None:
                        # No hay más objetivos, finalizar turno
                        break
                    path = self.find_path_to(target_pos)
                    if len(path) > 1:
                        next_step = path[1]  # Próxima casilla en el camino
                        self.move_to(next_step)
                    else:
                        # Ya estamos en la posición objetivo
                        break

                    # Intentar realizar una acción en la nueva posición
                    current_fire_value = self.model.fires.data[self.pos]
                    current_poi_value = self.model.points_of_interest.data[self.pos]
                    if current_fire_value == 1 and self.storedAP >= self.COST_EXTINGUISH_FIRE:
                        self.extinguish_fire(self.pos)
                        break
                    elif current_fire_value == 0.5 and self.storedAP >= self.COST_EXTINGUISH_SMOKE:
                        self.extinguish_smoke(self.pos)
                        break
                    elif current_poi_value != '' and self.storedAP >= 1:
                        self.rescue_poi(self.pos)
                        break

    def extinguish_fire(self, pos):
        if self.storedAP >= self.COST_EXTINGUISH_FIRE:
            if not self.has_wall_between(self.pos, pos):
                self.model.fires.set_cell(pos, 0)  # Eliminar fuego
                self.storedAP -= self.COST_EXTINGUISH_FIRE

    def extinguish_smoke(self, pos):
        if self.storedAP >= self.COST_EXTINGUISH_SMOKE:
            if not self.has_wall_between(self.pos, pos):
                self.model.fires.set_cell(pos, 0)  # Eliminar humo
                self.storedAP -= self.COST_EXTINGUISH_SMOKE

    def rescue_poi(self, pos):
        if self.storedAP >= 1:
            self.model.points_of_interest.set_cell(pos, '')  # Rescatar POI
            self.storedAP -= 1
            self.hasVictim = True  # Actualizar estado si es necesario

    def move_to(self, pos):
        if self.storedAP >= self.COST_MOVE:
            if self.has_wall_between(self.pos, pos):
                # Hay una pared entre la posición actual y la posición destino
                if self.storedAP >= self.COST_DAMAGE_WALL + self.COST_MOVE:
                    # Dañar la pared
                    # TODO: CHECAR LAS VECES QUE SE APLICA EL DAÑO A LA PARED
                    self.damage_wall(self.pos, pos)
                    # Moverse después de dañar la pared
                    self.model.grid.move_agent(self, pos)
                    self.storedAP -= self.COST_MOVE
                else:
                    # No hay suficientes AP para dañar la pared y moverse
                    pass  # No se puede mover
            else:
                # No hay pared, puede moverse
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
            self.storedAP -= self.COST_DAMAGE_WALL

    def has_wall_between(self, pos1, pos2):
        return self.model.has_wall_between(pos1, pos2)

    def find_nearest_objective(self):
        # Implementación simplificada para encontrar el objetivo más cercano
        queue = [(0, self.pos)]
        visited = set()
        while queue:
            cost, current_pos = heapq.heappop(queue)
            if current_pos in visited:
                continue
            visited.add(current_pos)

            # Verificar si la posición actual es un objetivo
            fire_value = self.model.fires.data[current_pos]
            poi_value = self.model.points_of_interest.data[current_pos]
            if fire_value == 1 or fire_value == 0.5 or poi_value != '':
                return current_pos

            neighbors = self.model.grid.get_neighborhood(
                current_pos,
                moore=False,
                include_center=False
            )
            for neighbor in neighbors:
                if neighbor not in visited and not self.has_wall_between(current_pos, neighbor):
                    heapq.heappush(queue, (cost + 1, neighbor))
        return None

    def find_path_to(self, target_pos):
        # Implementación simplificada del algoritmo A* considerando paredes
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
                    continue  # No se puede pasar a través de paredes
                tentative_g_score = g_score[current] + 1
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
        # Distancia de Manhattan
        return abs(a[0] - b[0]) + abs(a[1] - b[1])
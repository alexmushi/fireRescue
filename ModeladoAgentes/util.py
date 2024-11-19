import numpy as np

def leer_archivo(archivo):
    with open(archivo, 'r') as file:
        return file.read()

def binary_to_decimal(binary_input):
    binary_str = str(binary_input)
    decimal_number = 0
    exponent = len(binary_str) - 1

    for digit in binary_str:
        if digit not in ('0', '1'):
            raise ValueError(f"Invalid binary digit '{digit}' found.")
        decimal_number += int(digit) * (2 ** exponent)
        exponent -= 1

    return decimal_number

def get_game_variables(archivo):
    contenido = leer_archivo(archivo).strip().split("\n")
    index = 0

    walls = np.zeros((10, 8))
    for row in range(6):
        line = contenido[index].strip()
        for part in range(len(line.split())):
            wall = binary_to_decimal(line.split()[part])
            walls[part + 1, row + 1] = wall
        index += 1

    damage = np.empty_like(walls, dtype=object)
    damage.fill((0, 0, 0, 0))

    points_of_interest = []
    for _ in range(3):
        line = contenido[index].strip()
        parts = line.split()
        y = int(parts[0])
        x = int(parts[1])
        poi_type = parts[2]
        points_of_interest.append({'x': x, 'y': y, 'type': poi_type})
        index += 1
    
    fires = []
    for _ in range(10):
        line = contenido[index].strip()
        parts = line.split()
        y = int(parts[0])
        x = int(parts[1])
        fires.append({'x': x, 'y': y})
        index += 1

    doors = {}
    for _ in range(8):
        line = contenido[index].strip()
        parts = line.split()
        y1 = int(parts[0])
        x1 = int(parts[1])
        y2 = int(parts[2])
        x2 = int(parts[3])
        cell1 = (x1, y1)
        cell2 = (x2, y2)
        door_key = frozenset([cell1, cell2])
        doors[door_key] = 'closed'  
        index += 1
    
    entry_points = []
    for _ in range(4):
        line = contenido[index].strip()
        parts = line.split()
        y = int(parts[0])
        x = int(parts[1])
        entry_points.append((x, y))
        index += 1

    return walls, damage, points_of_interest, fires, doors, entry_points
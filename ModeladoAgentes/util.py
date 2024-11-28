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

def decimal_to_binary(decimal_input):
    binary_str = ''
    number = decimal_input
    
    if number == 0:
        binary_str = '0'
    else:
        while number > 0:
            binary_digit = number % 2
            binary_str = str(binary_digit) + binary_str
            number = number // 2
    
    # Rellenar con ceros a la izquierda para tener 4 dígitos
    binary_str = binary_str.zfill(4)
    
    return binary_str

def get_walls(value):
     # Walls are represented as (top, left, bottom, right) in binary
    # Bits correspond to (8, 4, 2, 1)
    bits = f"{int(value):04b}"
    walls = {
        'top': bits[0] == '1',
        'left': bits[1] == '1',
        'bottom': bits[2] == '1',
        'right': bits[3] == '1'
    }
    return walls

def serialize_doors(doors):
    doors_serialized = [
        {
            "coord1": list(coord1),
            "coord2": list(coord2),
            "status": status
        }
        for door, status in doors.items()
        for coord1, coord2 in [sorted(door)]
    ]

    return doors_serialized

def _serialize_door_position(door_key):
        sorted_positions = sorted(door_key)

        return [list(cell) for cell in sorted_positions]

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
    
    for x in range(10):
        walls[x, 0] = 2
    
    for x in range(10):
        walls[x, 7] = 8
    
    for y in range(8):
        walls[0, y] = 1

    for y in range(8):
        walls[9, y] = 4
    
    # Casos de esquinas
    walls[0, 0] = 0
    walls[9, 0] = 0
    walls[0, 7] = 0
    walls[9, 7] = 0

    damage = np.empty_like(walls, dtype=object)
    damage.fill((0, 0, 0, 0))

    points_of_interest = []
    total_victims = 0
    total_false_alarms = 0
    for _ in range(3):
        line = contenido[index].strip()
        parts = line.split()
        y = int(parts[0])
        x = int(parts[1])
        poi_type = parts[2]
        points_of_interest.append({'x': x, 'y': y, 'type': poi_type})

        if poi_type == 'v':
            total_victims += 1
        elif poi_type == 'f':
            total_false_alarms += 1

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

    return walls, damage, points_of_interest, fires, doors, entry_points, total_victims, total_false_alarms
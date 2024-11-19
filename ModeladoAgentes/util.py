def leer_archivo(archivo):
    with open(archivo, 'r') as file:
        return file.read()

def get_game_variables(archivo):
    contenido = leer_archivo(archivo).strip().split("\n")
    index = 0

    index += 6

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
        y1 = int(parts[0]) - 1
        x1 = int(parts[1]) - 1
        y2 = int(parts[2]) - 1
        x2 = int(parts[3]) - 1
        cell1 = (x1, y1)
        cell2 = (x2, y2)
        door_key = frozenset([cell1, cell2])
        doors[door_key] = 'closed'  
        index += 1
    
    entry_points = []
    for _ in range(4):
        line = contenido[index].strip()
        parts = line.split()
        y = int(parts[0]) - 1
        x = int(parts[1]) - 1
        entry_points.append((x, y))
        index += 1

    return points_of_interest, fires, doors, entry_points
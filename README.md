# Flash Point: Fire Rescue – Multi‑Agent Simulation

This repository contains a research project that models *Flash Point: Fire Rescue* as a cooperative multi‑agent system.  
Python agents coordinate to extinguish fires and rescue victims inside a virtual house while a Unity front‑end renders the evolving board using a Star Wars theme.

The current strategy achieves a **>95 % win rate** on the classic board using six autonomous firefighters.

## Project structure

- **ModeladoAgentes/** – Python implementation built with [Mesa](https://mesa.readthedocs.io/) and NumPy. It exposes an HTTP server that steps the simulation and sends state updates.
- **FireRescue/** – Unity 3D project (Editor `6000.0.24f1`) that visualises walls, doors, smoke, fire, and agent actions in real time.

## Features

- A* path‑finding for movement and target selection.
- Dynamic fire & smoke propagation with explosions, walls and doors.
- Dedicated rescuer agent that prioritises victims and exits.
- JSON HTTP interface between Mesa model and Unity viewer.
- Demonstrated win rate above 95 % across hundreds of random seeds.

## Getting started

1. **Python server**  
   ```bash
   cd ModeladoAgentes
   pip install mesa numpy
   python server.py  # starts on http://localhost:8585
   ```
2. **Unity visualisation**  
   Open the `FireRescue/` folder in Unity (`6000.0.24f1` or later) and press **Play**.  
   The scene requests updates from the Python server and animates agent decisions.

using UnityEngine;
using System;
using System.Collections.Generic;

public static class AddWalls 
{

    public static int CheckDirection(int current_x, int current_y, int next_x, int next_y)
    {
        int direction_x = next_x - current_x;
        int direction_y = next_y - current_y;

        if (direction_y == -1)
        {
            return 0; // Up
        }
        else if (direction_x == -1)
        {
            return 1; // Left
        }
        else if (direction_y == 1)
        {
            return 2; // Down
        }
        else if (direction_x == 1)
        {
            return 3; // Right
        }
        else
        {
            return -1;
        }
    } 


    public static void AddWallsToCells(List<List<double>> walls, GameObject wallPrefab, GameObject doorPrefab, Transform gridParent, List<Door> doors)
    {

        for (int col = 0; col < walls.Count; col++)
        {
            for (int row = 0; row < walls[col].Count; row++)
            {
                string cellName = $"Cell({col},{row})";
                GameObject cell = gridParent.Find(cellName)?.gameObject;

                List<int> doorDirections = new List<int>();
                foreach (var door in doors)
                {
                    if (door.coord1[0] == col && door.coord1[1] == row)
                    {
                        int direction = CheckDirection(door.coord1[0], door.coord1[1], door.coord2[0], door.coord2[1]);
                        doorDirections.Add(direction);
                    }
                    else if (door.coord2[0] == col && door.coord2[1] == row)
                    {
                        int direction = CheckDirection(door.coord2[0], door.coord2[1], door.coord1[0], door.coord1[1]);
                        doorDirections.Add(direction);
                    }
                }

                if (cell != null)
                {
                    double wallData = walls[col][row];
                    AddWallsToCell(cell, wallData, wallPrefab, doorPrefab, doorDirections);
                }
            }
        }
    } 

   public static void AddWallsToCell(GameObject cell, double wallData, GameObject wallPrefab, GameObject doorPrefab, List<int> doorDirections)
    {
        Vector3 cellPosition = cell.transform.position;
        int walls = (int)wallData;
        float halfSize = 0.5f;

        string binaryWalls = Convert.ToString(walls, 2).PadLeft(4, '0');
        if (binaryWalls[0] == '1')
        {
            if (!doorDirections.Contains(0))
            {
                Vector3 position = cellPosition + new Vector3(0.168f, 0, halfSize);
                Quaternion rotation = Quaternion.Euler(0, 90, 0);
                GameObject newWall = UnityEngine.Object.Instantiate(wallPrefab, position, rotation, cell.transform);
                newWall.name = $"Up Wall at {cell.name}";
            }
            else {
                Vector3 position = cellPosition + new Vector3(0, 0.4f, halfSize);
                Quaternion rotation = Quaternion.Euler(90, 0, 0);
                GameObject newDoor = UnityEngine.Object.Instantiate(doorPrefab, position, rotation, cell.transform);
                newDoor.name = $"Up Door at {cell.name}";
            }
        }

        if (binaryWalls[1] == '1')
        {
            if (!doorDirections.Contains(1))
            {
                Vector3 position = cellPosition + new Vector3(-halfSize, 0, 0.168f);
                Quaternion rotation = Quaternion.identity;
                GameObject newWall = UnityEngine.Object.Instantiate(wallPrefab, position, rotation, cell.transform);
                newWall.name = $"Left Wall at {cell.name}";
            }
            else {
                Vector3 position = cellPosition + new Vector3(-halfSize, 0.4f, 0);
                Quaternion rotation = Quaternion.Euler(90, 90, 0);
                GameObject newDoor = UnityEngine.Object.Instantiate(doorPrefab, position, rotation, cell.transform);
                newDoor.name = $"Left Door at {cell.name}";
            }
        }

        if (binaryWalls[2] == '1')
        {
            if (!doorDirections.Contains(2))
            {
                Vector3 position = cellPosition + new Vector3(0.168f, 0, -halfSize);
                Quaternion rotation = Quaternion.Euler(0, 90, 0);
                GameObject newWall = UnityEngine.Object.Instantiate(wallPrefab, position, rotation, cell.transform);
                newWall.name = $"Down Wall at {cell.name}";
            }
            else {
                Vector3 position = cellPosition + new Vector3(0, 0.4f, -halfSize);
                Quaternion rotation = Quaternion.Euler(90, 0, 0);
                GameObject newDoor = UnityEngine.Object.Instantiate(doorPrefab, position, rotation, cell.transform);
                newDoor.name = $"Down Door at {cell.name}";
            }
        }

        if (binaryWalls[3] == '1')
        {
            if (!doorDirections.Contains(3))
            {
                Vector3 position = cellPosition + new Vector3(halfSize, 0, 0.168f);
                Quaternion rotation = Quaternion.identity;
                GameObject newWall = UnityEngine.Object.Instantiate(wallPrefab, position, rotation, cell.transform);
                newWall.name = $"Right Wall at {cell.name}";
            }
            else {
                Vector3 position = cellPosition + new Vector3(halfSize, 0.4f, 0);
                Quaternion rotation = Quaternion.Euler(90, 90, 0);
                GameObject newDoor = UnityEngine.Object.Instantiate(doorPrefab, position, rotation, cell.transform);
                newDoor.name = $"Right Door at {cell.name}";
            }
        }
    }
}

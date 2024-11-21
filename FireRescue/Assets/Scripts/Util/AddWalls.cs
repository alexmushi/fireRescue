using UnityEngine;
using System;
using System.Collections.Generic;

public static class AddWalls 
{
    public static void AddWallsToCells(List<List<double>> walls, GameObject wallPrefab, Transform gridParent)
    {
        for (int col = 0; col < walls.Count; col++)
        {
            for (int row = 0; row < walls[col].Count; row++)
            {
                string cellName = $"Cell({col},{row})";
                GameObject cell = gridParent.Find(cellName)?.gameObject;

                if (cell != null)
                {
                    double wallData = walls[col][row];
                    AddWallsToCell(cell, wallData, wallPrefab);
                }
            }
        }
    } 

   public static void AddWallsToCell(GameObject cell, double wallData, GameObject wallPrefab)
    {
        Vector3 cellPosition = cell.transform.position;
        int walls = (int)wallData;
        float halfSize = 0.5f;

        string binaryWalls = Convert.ToString(walls, 2).PadLeft(4, '0');

        if (binaryWalls[0] == '1')
        {
            Vector3 position = cellPosition + new Vector3(0.168f, 0, halfSize);
            Quaternion rotation = Quaternion.Euler(0, 90, 0);
            GameObject newWall = UnityEngine.Object.Instantiate(wallPrefab, position, rotation, cell.transform);
            newWall.name = $"Up Wall at {cell.name}";
        }

        if (binaryWalls[1] == '1')
        {
            Vector3 position = cellPosition + new Vector3(-halfSize, 0, 0.168f);
            Quaternion rotation = Quaternion.identity;
            GameObject newWall = UnityEngine.Object.Instantiate(wallPrefab, position, rotation, cell.transform);
            newWall.name = $"Left Wall at {cell.name}";
        }

        if (binaryWalls[2] == '1')
        {
            Vector3 position = cellPosition + new Vector3(0.168f, 0, -halfSize);
            Quaternion rotation = Quaternion.Euler(0, 90, 0);
            GameObject newWall = UnityEngine.Object.Instantiate(wallPrefab, position, rotation, cell.transform);
            newWall.name = $"Down Wall at {cell.name}";
        }

        if (binaryWalls[3] == '1')
        {
            Vector3 position = cellPosition + new Vector3(halfSize, 0, 0.168f);
            Quaternion rotation = Quaternion.identity;
            GameObject newWall = UnityEngine.Object.Instantiate(wallPrefab, position, rotation, cell.transform);
            newWall.name = $"Right Wall at {cell.name}";
        }
    }
}

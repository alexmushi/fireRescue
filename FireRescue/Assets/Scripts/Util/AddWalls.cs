using UnityEngine;
using System.Collections.Generic;

public static class AddWalls 
{
    public static void AddWallsToCells(List<List<double>> walls, GameObject wallPrefab, Transform gridParent)
    {
        for (int row = 0; row < walls.Count; row++)
        {
            for (int col = 0; col < walls[row].Count; col++)
            {
                string cellName = $"Cell({row},{col})";
                GameObject cell = gridParent.Find(cellName)?.gameObject;

                if (cell != null)
                {
                    double wallData = walls[row][col];
                    AddWallsToCell(cell, wallData, wallPrefab);
                }
            }
        }
    } 

   public static void AddWallsToCell(GameObject cell, double wallData, GameObject wallPrefab)
    {
        Vector3 cellPosition = cell.transform.position;

        int walls = (int)wallData;

        if ((walls & 0b1000) > 0)
            Object.Instantiate(
                wallPrefab, 
                cellPosition + new Vector3(0, 1f, 0), 
                Quaternion.identity, 
                cell.transform);

        if ((walls & 0b0100) > 0) 
            Object.Instantiate(
                wallPrefab, 
                cellPosition + new Vector3(-1f, 0, 0), 
                Quaternion.Euler(0, 90, 0), 
                cell.transform);

        if ((walls & 0b0010) > 0) 
            Object.Instantiate(
                wallPrefab, 
                cellPosition + new Vector3(0, -1f, 0),
                Quaternion.identity, 
                cell.transform);

        if ((walls & 0b0001) > 0) 
            Object.Instantiate(
                wallPrefab, 
                cellPosition + new Vector3(1f, 0, 0), 
                Quaternion.Euler(0, 90, 0), 
                cell.transform);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class AddWallsManager : MonoBehaviour
{
    public static AddWallsManager Instance { get; private set; }

    // References to prefabs and other dependencies
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject doorPrefab;
    [SerializeField] private GameObject entryPointPrefab;

    private void Awake()
    {
        // Ensure only one instance exists
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: Persist across scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddWallsToCells(
        List<List<double>> walls, 
        Transform gridParent, 
        List<Door> doors, 
        List<int[]> entryPoints)
    {
        int maxCol = walls.Count;
        int maxRow = walls[0].Count; 

        for (int col = 0; col < maxCol; col++)
        {
            for (int row = 0; row < maxRow; row++)
            {
                string cellName = $"Cell({col},{row})";
                GameObject cell = gridParent.Find(cellName)?.gameObject;

                bool isEntryPoint = HelperFunctions.Instance.IsEntryPoint(entryPoints, col, row);

                List<int> doorDirections = new List<int>();
                foreach (var door in doors)
                {
                    if (door.coord1[0] == col && door.coord1[1] == row)
                    {
                        int direction = HelperFunctions.Instance.CheckDirection(door.coord1[0], door.coord1[1], door.coord2[0], door.coord2[1]);
                        doorDirections.Add(direction);
                    }
                    else if (door.coord2[0] == col && door.coord2[1] == row)
                    {
                        int direction = HelperFunctions.Instance.CheckDirection(door.coord2[0], door.coord2[1], door.coord1[0], door.coord1[1]);
                        doorDirections.Add(direction);
                    }
                }

                if (cell != null)
                {
                    double wallData = walls[col][row];
                    AddWallsToCell(cell, wallData, doorDirections, entryPoints, isEntryPoint, col, row, maxCol, maxRow);
                }
            }
        }
    } 

    public void AddWallsToCell(
        GameObject cell,
        double wallData, 
        List<int> doorDirections, 
        List<int[]> entryPoints,
        bool isEntryPoint, 
        int col, 
        int row, 
        int maxCol, 
        int maxRow)
    {
        Vector3 cellPosition = cell.transform.position;
        int walls = (int)wallData;
        float halfSize = 0.5f;

        string binaryWalls = Convert.ToString(walls, 2).PadLeft(4, '0');

        List<int> entryPointDirections = new List<int>();

        bool isTopEdge = row == 0;
        bool isBottomEdge = row + 1 == maxRow - 1;
        bool isLeftEdge = col == 0;
        bool isRightEdge = col + 1 == maxCol - 1;

        if (isEntryPoint)
        {
            if (isBottomEdge)
            {
                binaryWalls = HelperFunctions.Instance.ReplaceCharAt(binaryWalls, 2, '0'); 
                entryPointDirections.Add(2);
            }
            if (isRightEdge)
            {
                binaryWalls = HelperFunctions.Instance.ReplaceCharAt(binaryWalls, 3, '0');
                entryPointDirections.Add(3);
            }
        }

        bool isAdjacentCellEntryPoint = false;

        if (isTopEdge)
        {
            // Check to see if the adjacent cell is an entry point
            float x_position = col;
            float y_position = row + 1;
            isAdjacentCellEntryPoint = HelperFunctions.Instance.IsEntryPoint(entryPoints, (int)x_position, (int)y_position);

            if (isAdjacentCellEntryPoint)
            {
                entryPointDirections.Add(2);
            }
        }

        if (isLeftEdge)
        {
            // Check to see if the adjacent cell is an entry point
            float x_position = col + 1;
            float y_position = row;
            isAdjacentCellEntryPoint = HelperFunctions.Instance.IsEntryPoint(entryPoints, (int)x_position, (int)y_position);
            
            if (isAdjacentCellEntryPoint)
            {
                entryPointDirections.Add(3);
            }
        }

        if (binaryWalls[2] == '1' && isAdjacentCellEntryPoint == false)
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
        else if (entryPointDirections.Contains(2))
        {
            Vector3 position = cellPosition + new Vector3(0, 0.08f, -halfSize);
            Quaternion rotation = Quaternion.Euler(-90, 0, 0);
            GameObject newDoorFrame = UnityEngine.Object.Instantiate(entryPointPrefab, position, rotation, cell.transform);
            newDoorFrame.name = $"Down Entry Door at {cell.name}";
        }

        if (binaryWalls[3] == '1' && isAdjacentCellEntryPoint == false)
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
         else if (entryPointDirections.Contains(3))
        {
            Vector3 position = cellPosition + new Vector3(halfSize, 0.08f, 0);
            Quaternion rotation = Quaternion.Euler(-90, 0, 90);
            GameObject newDoorFrame = UnityEngine.Object.Instantiate(entryPointPrefab, position, rotation, cell.transform);
            newDoorFrame.name = $"Right Entry Door at {cell.name}";
        }
    }

    
    public IEnumerator OpenDoor(List<List<int>> positions, Transform gridParent)
    {
        // positions[0] and positions[1] represent the two cells between which the door is located
        List<int> pos1 = positions[0];
        List<int> pos2 = positions[1];

        int direction = HelperFunctions.Instance.CheckDirection(pos1[0], pos1[1], pos2[0], pos2[1]);

        string cellName = "";
        string doorName = "";

        if (direction == 0) // North
        {
            cellName = $"Cell({pos1[0]},{pos1[1] - 1})";
            doorName = HelperFunctions.Instance.GetDirectionName(2, "Door", pos1[0], pos1[1] - 1);
        }
        else if (direction == 1) // West
        {
            cellName = $"Cell({pos1[0] - 1},{pos1[1]})";
            doorName = HelperFunctions.Instance.GetDirectionName(3, "Door", pos1[0] - 1, pos1[1]);
        }
        else if (direction == 2) // South
        {
            cellName = $"Cell({pos1[0]},{pos1[1]})";
            doorName = HelperFunctions.Instance.GetDirectionName(2, "Door", pos1[0], pos1[1]);
        }
        else if (direction == 3) // East
        {
            cellName = $"Cell({pos1[0]},{pos1[1]})";
            doorName = HelperFunctions.Instance.GetDirectionName(3, "Door", pos1[0], pos1[1]);
        }

        GameObject cell = gridParent.Find(cellName)?.gameObject;
        if (cell != null)
        {
            Transform doorTransform = cell.transform.Find(doorName);
            if (doorTransform != null)
            {
                Transform emptyMesh1Transform = FindChildWithTag(doorTransform, "EmptyMesh1Tag");
                if (emptyMesh1Transform != null)
                {
                    float moveAmount = 1.2f; // Adjust this value as needed
                    emptyMesh1Transform.position += Vector3.down * moveAmount;
                }

                // Optionally, wait for a frame if needed
                yield return null;
            }
        }
    }

    private Transform FindChildWithTag(Transform parent, string tag)
    {
        foreach (Transform child in parent)
        {
            if (child.CompareTag(tag))
            {
                return child;
            }

            // Recursively search in the children's children
            Transform result = FindChildWithTag(child, tag);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }
}

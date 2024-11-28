using UnityEngine;
using System;
using System.Collections.Generic;

public class HelperFunctions : MonoBehaviour
{
    public static HelperFunctions Instance { get; private set; }

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

    // Utility Methods

    public int CheckDirection(int current_x, int current_y, int next_x, int next_y)
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

    public bool IsEntryPoint(List<int[]> entryPoints, int col, int row)
    {
        foreach (var point in entryPoints)
        {
            if (point[0] == col && point[1] == row)
            {
                return true;
            }
        }
        return false;
    }

    public string ReplaceCharAt(string str, int index, char newChar)
    {
        if (index < 0 || index >= str.Length)
            throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
        
        char[] chars = str.ToCharArray();
        chars[index] = newChar;
        return new string(chars);
    }

    public string GetDirectionName(int direction, string type, int col, int row)
    {
        switch (direction)
        {
            case 0:
                return $"Up {type} at Cell({col},{row})";
            case 1:
                return $"Left {type} at Cell({col},{row})";
            case 2:
                return $"Down {type} at Cell({col},{row})";
            case 3:
                return $"Right {type} at Cell({col},{row})";
            default:
                return "unknown";
        }
    }

    public Vector3 GetForceDirection(int direction)
    {
        switch (direction)
        {
            case 0:
                return new Vector3(0, 0, -1);
            case 1:
                return new Vector3(-1, 0, 0);
            case 2:
                return new Vector3(0, 0, 1);
            case 3:
                return new Vector3(1, 0, 0);
            default:
                return Vector3.zero;
        }
    }
}

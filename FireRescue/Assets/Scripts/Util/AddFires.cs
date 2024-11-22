using UnityEngine;
using System.Collections.Generic;

public class AddFires : MonoBehaviour
{
    public static AddFires Instance { get; private set; }

    [SerializeField] private GameObject firePrefab;

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

    public void AddFiresToCells(List<List<double>> fires, Transform gridParent)
    {
        int maxCol = fires.Count;
        int maxRow = fires[0].Count;

        for (int col = 0; col < maxCol; col++)
        {
            for (int row = 0; row < maxRow; row++)
            {
                string cellName = $"Cell({col},{row})";
                GameObject cell = gridParent.Find(cellName)?.gameObject;

                if (fires[col][row] == 1)
                {
                    GameObject fire = Instantiate(firePrefab, cell.transform.position, Quaternion.identity);
                    fire.transform.SetParent(cell.transform);
                    fire.name = "Fire at " + cellName;
                }
            }
        }
    }
}

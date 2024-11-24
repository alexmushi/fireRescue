using UnityEngine;
using System.Collections.Generic;

public class AddFiresAndPOI : MonoBehaviour
{
    public static AddFiresAndPOI Instance { get; private set; }

    [SerializeField] private GameObject firePrefab;
    [SerializeField] private GameObject poiPrefab;
    [SerializeField] private GameObject smokePrefab;

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

    public void AddPOIToCells(List<List<string>> points_of_interest, Transform gridParent)
    {
        int maxCol = points_of_interest.Count;
        int maxRow = points_of_interest[0].Count;

        for (int col = 0; col < maxCol; col++)
        {
            for (int row = 0; row < maxRow; row++)
            {
                string cellName = $"Cell({col},{row})";
                GameObject cell = gridParent.Find(cellName)?.gameObject;

                string poiType = points_of_interest[col][row];
                if (poiType == "v" || poiType == "f")
                {
                    Vector3 position = cell.transform.position + new Vector3(0, 0.5f, 0);
                    Quaternion rotation = Quaternion.Euler(-90, 0, 0);
                    GameObject poi = Instantiate(poiPrefab, position, rotation);
                    poi.transform.SetParent(cell.transform);
                    poi.name = "POI at " + cellName;
                }
            }
        }
    }

    public void AddNewFiresAndSmokes(List<NewStatusDouble> fires, int width, int height, Transform gridParent) {

        foreach (NewStatusDouble fire in fires)
        {
            int col = fire.position[0];
            int row = fire.position[1];

            string cellName = $"Cell({col},{row})";
            GameObject cell = gridParent.Find(cellName)?.gameObject;

            if (fire.new_value == 1)
            {
                GameObject newFire = Instantiate(firePrefab, cell.transform.position, Quaternion.identity);
                newFire.transform.SetParent(cell.transform);
                newFire.name = "Fire at " + cellName;
            }
            else if (fire.new_value == 0.5)
            {
                GameObject smoke = Instantiate(smokePrefab, cell.transform.position + new Vector3(0, 0.5f, 0), Quaternion.Euler(-90, 0, 0));
                smoke.transform.SetParent(cell.transform);
                smoke.name = "Smoke at " + cellName;
            }
        }
    }
}

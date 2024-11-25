using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

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
                    GameObject fire = UnityEngine.Object.Instantiate(firePrefab, cell.transform.position, Quaternion.identity);
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
                    GameObject poi = UnityEngine.Object.Instantiate(poiPrefab, position, rotation);
                    poi.transform.SetParent(cell.transform);
                    poi.name = "POI at " + cellName;
                }
            }
        }
    }

    public IEnumerator AddNewFiresAndSmokes(List<NewStatusDouble> fires, int width, int height, Transform gridParent, float delay = 0.5f) {

        foreach (NewStatusDouble fire in fires)
        {
            int col = fire.position[0];
            int row = fire.position[1];

            string cellName = $"Cell({col},{row})";
            GameObject cell = gridParent.Find(cellName)?.gameObject;

            if (fire.new_value == 1)
            {
                GameObject newFire = UnityEngine.Object.Instantiate(firePrefab, cell.transform.position, Quaternion.identity);
                newFire.transform.SetParent(cell.transform);
                newFire.name = "Fire at " + cellName;
            }
            else if (fire.new_value == 0.5)
            {
                GameObject smoke = UnityEngine.Object.Instantiate(smokePrefab, cell.transform.position + new Vector3(0, 0.1f, 0), Quaternion.Euler(-90, 0, 0));
                smoke.transform.SetParent(cell.transform);
                smoke.name = "Smoke at " + cellName;
            }

            yield return new WaitForSeconds(delay);
        }
    }

    public IEnumerator Explosion(
        List<NewExplosion> explosions, 
        List<NewStatusDouble> fires,
        List<NewStatusInt> walls,
        List<NewStatusIntList> damage,
        List<NewStatusDoors> doors,
        int width, int height,
        Transform gridParent) 
        {

        if (explosions.Count > 0)
        {
            foreach (NewExplosion explosion in explosions)
            {
                int expCol = explosion.position[0];
                int expRow = explosion.position[1];

                yield return StartCoroutine(ExplosionDoor(doors, expCol, expRow, gridParent));

                yield return StartCoroutine(ExplosionWall(walls, expCol, expRow, gridParent));

                yield return StartCoroutine(ExplosionPlaceFire(fires, expCol, expRow, gridParent));
            }
        }

        yield return null;
    }

    private IEnumerator ExplosionDoor(List<NewStatusDoors> doors, int expCol, int expRow, Transform gridParent)
    {
        foreach (NewStatusDoors door in doors)
        {
            bool isDoorHit = 
                (door.position[0][0] == expCol && door.position[0][1] == expRow) ||
                (door.position[1][0] == expCol && door.position[1][1] == expRow);
                    
            int direction = HelperFunctions.Instance.CheckDirection(door.position[0][0], door.position[0][1], door.position[1][0], door.position[1][1]);
            string cellName = "";
            string doorName = "";
            if (isDoorHit && door.new_value == "destroyed") 
            {
                if (direction == 0) {
                    cellName = $"Cell({expCol},{expRow})";
                    doorName = GetDirectionName(direction, "Door", expCol, expRow);
                }
                else if (direction == 1) {
                    cellName = $"Cell({expCol - 1},{expRow})";
                    doorName = GetDirectionName(direction, "Door", expCol - 1, expRow);
                }
                else if (direction == 2) {
                    cellName = $"Cell({expCol},{expRow - 1})";
                    doorName = GetDirectionName(direction, "Door", expCol, expRow - 1);
                }
                else if (direction == 3) {
                    cellName = $"Cell({expCol},{expRow})";
                    doorName = GetDirectionName(direction, "Door", expCol, expRow);
                }

                GameObject cell = gridParent.Find(cellName)?.gameObject;
                GameObject doorObject = cell.transform.Find(doorName)?.gameObject;

                if (doorObject == null) continue;

                Rigidbody rb = doorObject.GetComponent<Rigidbody>();

                Vector3 forceDirection = GetForceDirection(direction);

                float forceMagnitude = 30f; 
                rb.AddForce(forceDirection * forceMagnitude);

                rb.AddTorque(new Vector3(100f, 100f, 100f));

                Destroy(doorObject, 1.5f); 

                yield return new WaitForSeconds(1.5f);
            }
        }
    }

    private IEnumerator ExplosionWall(List<NewStatusInt> walls, int expCol, int expRow, Transform gridParent)
    {
        foreach (NewStatusInt wall in walls)
        {
            if (wall.position[0] == expCol && wall.position[1] == expRow)
            {
                string binaryWalls = Convert.ToString(wall.new_value, 2).PadLeft(4, '0');

                string cellName = "";
                string wallName = "";
                int direction = 0;

                for (int i = 0; i < binaryWalls.Length; i++)
                {
                    if (binaryWalls[i] == '0')
                    {
                        direction = i;

                        int newDirection = 0;
                        if (direction == 0) {
                            cellName = $"Cell({expCol},{expRow - 1})";
                            wallName = GetDirectionName(2, "Wall", expCol, expRow - 1);
                            newDirection = 2;
                        }
                        else if (direction == 1) {
                            cellName = $"Cell({expCol - 1},{expRow})";
                            wallName = GetDirectionName(3, "Wall", expCol - 1, expRow);
                            newDirection = 1;
                        }
                        else if (direction == 2) {
                            cellName = $"Cell({expCol},{expRow})";
                            wallName = GetDirectionName(direction, "Wall", expCol, expRow);
                            newDirection = 0;
                        }
                        else if (direction == 3) {
                            cellName = $"Cell({expCol},{expRow})";
                            wallName = GetDirectionName(direction, "Wall", expCol, expRow);
                            newDirection = 3;
                        }

                        GameObject cell = gridParent.Find(cellName)?.gameObject;
                        GameObject wallObject = cell.transform.Find(wallName)?.gameObject;

                        if (wallObject == null) continue;

                        Rigidbody rb = wallObject.GetComponent<Rigidbody>();

                        Vector3 forceDirection = GetForceDirection(newDirection);

                        float forceMagnitude = 30f; 
                        wallObject.transform.position = wallObject.transform.position + new Vector3(0, 0.5f, 0);
                        rb.AddForce(forceDirection * forceMagnitude);

                        rb.AddTorque(new Vector3(100f, 100f, 100f));

                        Destroy(wallObject, 1.5f); 

                        yield return new WaitForSeconds(1.5f);
                    }
                }

            }
        }
        yield return null;
    }

    private IEnumerator ExplosionPlaceFire(List<NewStatusDouble> fires, int expCol, int expRow, Transform gridParent) {

        for (int i = fires.Count - 1; i >= 0; i--)
        {
            NewStatusDouble fire = fires[i];
            int fireCol = fire.position[0];
            int fireRow = fire.position[1];
            
            // Up
            if (fireCol == expCol && fireRow == expRow - 1) {
                placeFireCoordinate(fire, expCol, expRow - 1, gridParent);
                fires.RemoveAt(i);
                yield return new WaitForSeconds(0.5f);
            }
            // Left
            else if (fireCol == expCol - 1 && fireRow == expRow) {
                placeFireCoordinate(fire, expCol - 1, expRow, gridParent);
                fires.RemoveAt(i);
                yield return new WaitForSeconds(0.5f);
            }
            // Down
            else if (fireCol == expCol && fireRow == expRow + 1) {
                placeFireCoordinate(fire, expCol, expRow + 1, gridParent);
                fires.RemoveAt(i);
                yield return new WaitForSeconds(0.5f);
            }
            // Right
            else if (fireCol == expCol + 1 && fireRow == expRow) {
                placeFireCoordinate(fire, expCol + 1, expRow, gridParent);
                fires.RemoveAt(i);
                yield return new WaitForSeconds(0.5f);
            }
        }

        yield return null;
    }

    private void placeFireCoordinate(NewStatusDouble fire, int col, int row, Transform gridParent) {
        string cellName = $"Cell({col},{row})";
        GameObject cell = gridParent.Find(cellName)?.gameObject;

        if (fire.new_value == 1)
        {
            GameObject newFire = UnityEngine.Object.Instantiate(firePrefab, cell.transform.position, Quaternion.identity);
            newFire.transform.SetParent(cell.transform);
            newFire.name = "Fire at " + cellName;
        }
        else if (fire.new_value == 0.5)
        {
            GameObject smoke = UnityEngine.Object.Instantiate(smokePrefab, cell.transform.position + new Vector3(0, 0.1f, 0), Quaternion.Euler(-90, 0, 0));
            smoke.transform.SetParent(cell.transform);
            smoke.name = "Smoke at " + cellName;
        }
    }

    private string GetDirectionName(int direction, string type, int col, int row)
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

    private Vector3 GetForceDirection(int direction)
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

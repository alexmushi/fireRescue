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
    [SerializeField] private GameObject mouseDroidPrefab;
    [SerializeField] private GameObject clonePrefab;
    [SerializeField] private GameObject droidPrefab;

    public AudioSource audioSource;
    public AudioSource mouseDroidAudioSource;

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
                    string poiTypeName = "";
                    if (poiType == "v") {
                        poiTypeName = "Victim";
                    } else if (poiType == "f") {
                        poiTypeName = "Fake";
                    }

                    Vector3 position = cell.transform.position + new Vector3(0, 0.5f, 0);
                    Quaternion rotation = Quaternion.Euler(-90, 0, 0);
                    GameObject poi = UnityEngine.Object.Instantiate(poiPrefab, position, rotation);
                    poi.transform.SetParent(cell.transform);
                    poi.name = $"{poiTypeName} POI at " + cellName;
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
                GameObject previousSmoke = cell.transform.Find("Smoke at " + cellName)?.gameObject;

                if (previousSmoke != null) {
                    Destroy(previousSmoke);
                }
                
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
                Shakes.Instance.TriggerShake();

                int expCol = explosion.position[0];
                int expRow = explosion.position[1];

                yield return StartCoroutine(ExplosionDoor(doors, expCol, expRow, gridParent));

                yield return StartCoroutine(ExplosionWall(walls, expCol, expRow, gridParent));

                yield return StartCoroutine(ExplosionDamageWall(damage, expCol, expRow, gridParent));

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
                    doorName = HelperFunctions.Instance.GetDirectionName(direction, "Door", expCol, expRow);
                }
                else if (direction == 1) {
                    cellName = $"Cell({expCol - 1},{expRow})";
                    doorName = HelperFunctions.Instance.GetDirectionName(direction, "Door", expCol - 1, expRow);
                }
                else if (direction == 2) {
                    cellName = $"Cell({expCol},{expRow - 1})";
                    doorName = HelperFunctions.Instance.GetDirectionName(direction, "Door", expCol, expRow - 1);
                }
                else if (direction == 3) {
                    cellName = $"Cell({expCol},{expRow})";
                    doorName = HelperFunctions.Instance.GetDirectionName(direction, "Door", expCol, expRow);
                }

                GameObject cell = gridParent.Find(cellName)?.gameObject;
                GameObject doorObject = cell.transform.Find(doorName)?.gameObject;

                if (doorObject == null) continue;

                Rigidbody rb = doorObject.GetComponent<Rigidbody>();

                Vector3 forceDirection = HelperFunctions.Instance.GetForceDirection(direction);

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
                            wallName = HelperFunctions.Instance.GetDirectionName(2, "Wall", expCol, expRow - 1);
                            newDirection = 2;
                        }
                        else if (direction == 1) {
                            cellName = $"Cell({expCol - 1},{expRow})";
                            wallName = HelperFunctions.Instance.GetDirectionName(3, "Wall", expCol - 1, expRow);
                            newDirection = 1;
                        }
                        else if (direction == 2) {
                            cellName = $"Cell({expCol},{expRow})";
                            wallName = HelperFunctions.Instance.GetDirectionName(direction, "Wall", expCol, expRow);
                            newDirection = 0;
                        }
                        else if (direction == 3) {
                            cellName = $"Cell({expCol},{expRow})";
                            wallName = HelperFunctions.Instance.GetDirectionName(direction, "Wall", expCol, expRow);
                            newDirection = 3;
                        }

                        GameObject cell = gridParent.Find(cellName)?.gameObject;
                        GameObject wallObject = cell.transform.Find(wallName)?.gameObject;

                        if (wallObject == null) continue;

                        Rigidbody rb = wallObject.GetComponent<Rigidbody>();

                        Vector3 forceDirection = HelperFunctions.Instance.GetForceDirection(newDirection);

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

    private IEnumerator ExplosionDamageWall(List<NewStatusIntList> damage, int expCol, int expRow, Transform gridParent) {
        foreach (NewStatusIntList wall in damage)
        {
            if (wall.position[0] == expCol && wall.position[1] == expRow)
            {
                for (int i = 0; i < wall.new_value.Count; i++)
                {
                    int direction = wall.new_value[i];

                    if (direction == 1) {
                        string cellName = "";
                        string wallName = "";

                        if (i == 0) {
                            cellName = $"Cell({expCol},{expRow - 1})";
                            wallName = HelperFunctions.Instance.GetDirectionName(2, "Wall", expCol, expRow - 1);
                        }
                        else if (i == 1) {
                            cellName = $"Cell({expCol - 1},{expRow})";
                            wallName = HelperFunctions.Instance.GetDirectionName(3, "Wall", expCol - 1, expRow);
                        }
                        else if (i == 2) {
                            cellName = $"Cell({expCol},{expRow})";
                            wallName = HelperFunctions.Instance.GetDirectionName(2, "Wall", expCol, expRow);
                        }
                        else if (i == 3) {
                            cellName = $"Cell({expCol},{expRow})";
                            wallName = HelperFunctions.Instance.GetDirectionName(3, "Wall", expCol, expRow);
                        }


                        GameObject cell = gridParent.Find(cellName)?.gameObject;
                        GameObject wallObject = cell.transform.Find(wallName)?.gameObject;

                        if (wallObject == null) continue;

                        Shakes.Instance.TriggerWallShake(wallObject.transform, 0.5f, 0.1f);

                        yield return new WaitForSeconds(0.1f);
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
            GameObject previousSmoke = cell.transform.Find("Smoke at " + cellName)?.gameObject;

            if (previousSmoke != null) {
                Destroy(previousSmoke);
            }

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

    public IEnumerator extinguishFires(List<NewStatusDouble> fires, Transform gridParent) {
        for (int i = fires.Count - 1; i >= 0; i--)
        {
            NewStatusDouble fire = fires[i];
            int fireCol = fire.position[0];
            int fireRow = fire.position[1];

            if (fire.new_value == 0)
            {
                string cellName = $"Cell({fireCol},{fireRow})";
                string fireName = "Fire at " + cellName;

                GameObject cell = gridParent.Find(cellName)?.gameObject;
                GameObject fireObject = cell.transform.Find(fireName)?.gameObject;

                if (fireObject != null) {

                    FireAnimation fireAnim = fireObject.GetComponent<FireAnimation>();
                    yield return StartCoroutine(fireAnim.PutOutFire());
                    fires.RemoveAt(i);
                    yield return new WaitForSeconds(0.5f);

                } else {

                    string smokeName = "Smoke at " + cellName;
                    GameObject smokeObject = cell.transform.Find(smokeName)?.gameObject;

                    if (smokeObject != null) {
                        SmokeMovement smokeAnim = smokeObject.GetComponent<SmokeMovement>();
                        yield return StartCoroutine(smokeAnim.PutOutSmoke());
                        fires.RemoveAt(i);
                        yield return new WaitForSeconds(0.5f);
                    }
                }
            }
        }
        yield return null;
    }

    public IEnumerator ExtinguishFireAtPosition(List<int> position, Transform gridParent)
    {
        string cellName = $"Cell({position[0]},{position[1]})";
        GameObject cell = gridParent.Find(cellName)?.gameObject;

        if (cell != null)
        {
            Transform fireTransform = cell.transform.Find("Fire at " + cellName);
            if (fireTransform != null)
            {
                // Optionally, play extinguishing animation
                
                Destroy(fireTransform.gameObject);
                yield return null;
            }
        }
    }

    // NEW METHOD: Extinguish smoke at a specific position
    public IEnumerator ExtinguishSmokeAtPosition(List<int> position, Transform gridParent)
    {
        string cellName = $"Cell({position[0]},{position[1]})";
        GameObject cell = gridParent.Find(cellName)?.gameObject;

        if (cell != null)
        {
            Transform smokeTransform = cell.transform.Find("Smoke at " + cellName);
            if (smokeTransform != null)
            {
                // Optionally, play extinguishing animation
                Destroy(smokeTransform.gameObject);
                yield return null;
            }
        }
    }

    public IEnumerator RevealVictimAtPosition(List<int> position, List<NewStatusString> points_of_interest, Transform gridParent)
    {
        string cellName = $"Cell({position[0]},{position[1]})";
        GameObject cell = gridParent.Find(cellName)?.gameObject;

        if (cell != null)
        {
            for (int i = points_of_interest.Count - 1; i >= 0; i--)
            {
                NewStatusString poi = points_of_interest[i];
                int poiCol = poi.position[0];
                int poiRow = poi.position[1];

                if (poiCol == position[0] && poiRow == position[1])
                {
                    string poiType = poi.new_value;
                    if (poiType == "show_victim") {
                        string victimPoiTypeName = "Victim POI at " + cellName;

                        GameObject victimPoi = cell.transform.Find(victimPoiTypeName)?.gameObject;

                        int randomIndex = UnityEngine.Random.Range(0, 2);
                        GameObject selectedPrefab = randomIndex == 0 ? droidPrefab : clonePrefab;

                        if (victimPoi != null) {
                            Destroy(victimPoi);
                            points_of_interest.RemoveAt(i);

                            Quaternion rotation = Quaternion.Euler(0, 0, 0);
                            Vector3 positionObject = cell.transform.position;
                            if (selectedPrefab.name == "Clone Trooper") {
                                rotation = Quaternion.Euler(0, 180, 0);
                            }
                            else if (selectedPrefab.name == "R2D2") {
                                rotation = Quaternion.Euler(-90, 0, 180);
                                positionObject = cell.transform.position + new Vector3(0, 0.21f, 0);
                            }

                            GameObject poiObject = UnityEngine.Object.Instantiate(selectedPrefab, positionObject, rotation);
                            poiObject.transform.SetParent(gridParent);
                            poiObject.name = $"Victim {selectedPrefab.name}";
                            yield return new WaitForSeconds(0.5f);
                        }
                    }
                }
            }
        }
    }

    public IEnumerator RevealFalseAlarmAtPosition(List<int> position, List<NewStatusString> points_of_interest, Transform gridParent)
    {
        string cellName = $"Cell({position[0]},{position[1]})";
        GameObject cell = gridParent.Find(cellName)?.gameObject;

        if (cell != null)
        {
            for (int i = points_of_interest.Count - 1; i >= 0; i--) {
                NewStatusString poi = points_of_interest[i];
                int poiCol = poi.position[0];
                int poiRow = poi.position[1];

                if (poiCol == position[0] && poiRow == position[1]) {
                    string poiType = poi.new_value;
                    if (poiType == "reveal")
                    {
                        string fakePoiTypeName = "Fake POI at " + cellName;

                        GameObject fakePoi = cell.transform.Find(fakePoiTypeName)?.gameObject;

                        if (fakePoi != null) {
                            Destroy(fakePoi);
                            points_of_interest.RemoveAt(i);
                            GameObject poiObject = UnityEngine.Object.Instantiate(mouseDroidPrefab, cell.transform.position, Quaternion.Euler(-90, 0, 0));
                            poiObject.transform.SetParent(gridParent);
                            poiObject.name = $"Fake {mouseDroidPrefab.name}";

                            StartCoroutine(MoveFakePOI(poiObject));

                            if (!mouseDroidAudioSource.isPlaying)
                            {
                                mouseDroidAudioSource.Play();
                            }

                            yield return new WaitForSeconds(0.5f);
                        }
                    }

                    yield return null;
                }
            }
        }
    }

    private IEnumerator MoveFakePOI(GameObject poiObject)
    {
        // Move in circles for 2 seconds
        float duration = 3f;
        float time = 0f;
        Vector3 center = poiObject.transform.position;
        float radius = 0.5f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float angle = time * Mathf.PI * 2f; // Full circle every second
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            poiObject.transform.position = center + new Vector3(x, 0, z);
            yield return null;
        }

        // Move off to the side for 1 second
        Vector3 direction = new Vector3(1, 0, 0); // Move along the x-axis
        duration = 1f;
        time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            poiObject.transform.position += direction * Time.deltaTime * 2f; // Speed = 2 units/sec
            yield return null;
        }

        // Destroy the object
        Destroy(poiObject);
    }

    public IEnumerator getRidOfPOI(List<NewStatusString> points_of_interest, Transform gridParent) {
        for (int i = points_of_interest.Count - 1; i >= 0; i--)
        {
            NewStatusString poi = points_of_interest[i];
            int poiCol = poi.position[0];
            int poiRow = poi.position[1];

            string cellName = $"Cell({poiCol},{poiRow})";
            GameObject cell = gridParent.Find(cellName)?.gameObject;

            string poiType = poi.new_value;
            if (poiType == "death") {
                string victimPoiTypeName = "Victim POI at " + cellName;

                GameObject victimPoi = cell.transform.Find(victimPoiTypeName)?.gameObject;

                if (!audioSource.isPlaying)
                    {
                        audioSource.Play();
                    }

                Destroy(victimPoi);
                points_of_interest.RemoveAt(i);
                yield return new WaitForSeconds(0.5f);
            } 
            else if (poiType == "false") {
                string fakePoiTypeName = "Fake POI at " + cellName;

                GameObject fakePoi = cell.transform.Find(fakePoiTypeName)?.gameObject;

                Destroy(fakePoi);
                points_of_interest.RemoveAt(i);
                yield return new WaitForSeconds(0.5f);
            }
        }

        yield return null;
    }

    public IEnumerator placeNewPOI(List<NewStatusString> points_of_interest, Transform gridParent) {

        for (int i = points_of_interest.Count - 1; i >= 0; i--)
        {
            NewStatusString poi = points_of_interest[i];
            int poiCol = poi.position[0];
            int poiRow = poi.position[1];

            string cellName = $"Cell({poiCol},{poiRow})";
            GameObject cell = gridParent.Find(cellName)?.gameObject;

            string poiType = poi.new_value;
            if (poiType == "v" || poiType == "f")
            {
                string poiTypeName = "";
                if (poiType == "v") {
                    poiTypeName = "Victim";
                } else if (poiType == "f") {
                    poiTypeName = "Fake";
                }

                Vector3 position = cell.transform.position + new Vector3(0, 0.5f, 0);
                Quaternion rotation = Quaternion.Euler(-90, 0, 0);
                GameObject poiObject = UnityEngine.Object.Instantiate(poiPrefab, position, rotation);
                poiObject.transform.SetParent(cell.transform);
                poiObject.name = $"{poiTypeName} POI at " + cellName;
            }

            points_of_interest.RemoveAt(i);
            yield return new WaitForSeconds(0.5f);
        }
        yield return null;
    }

    public IEnumerator revealPOI(List<NewStatusString> points_of_interest, Transform gridParent) {
        
        for (int i = points_of_interest.Count - 1; i >= 0; i--)
        {
            NewStatusString poi = points_of_interest[i];
            int poiCol = poi.position[0];
            int poiRow = poi.position[1];

            string cellName = $"Cell({poiCol},{poiRow})";
            GameObject cell = gridParent.Find(cellName)?.gameObject;

            string poiType = poi.new_value;
            if (poiType == "reveal")
            {
                string fakePoiTypeName = "Fake POI at " + cellName;

                GameObject fakePoi = cell.transform.Find(fakePoiTypeName)?.gameObject;

                if (fakePoi != null) {
                    Destroy(fakePoi);
                    points_of_interest.RemoveAt(i);
                    GameObject poiObject = UnityEngine.Object.Instantiate(mouseDroidPrefab, cell.transform.position, Quaternion.Euler(-90, 0, 0));
                    poiObject.transform.SetParent(gridParent);
                    poiObject.name = $"Fake {mouseDroidPrefab.name}";
                    yield return new WaitForSeconds(0.5f);
                }
            }
            else if (poiType == "death") {
                string victimPoiTypeName = "Victim POI at " + cellName;

                GameObject victimPoi = cell.transform.Find(victimPoiTypeName)?.gameObject;

                if (!audioSource.isPlaying)
                    {
                        audioSource.Play();
                    }

                Destroy(victimPoi);
                points_of_interest.RemoveAt(i);
                yield return new WaitForSeconds(0.5f);
            } 
            else if (poiType == "false") {
                string fakePoiTypeName = "Fake POI at " + cellName;

                GameObject fakePoi = cell.transform.Find(fakePoiTypeName)?.gameObject;

                Destroy(fakePoi);
                points_of_interest.RemoveAt(i);
                yield return new WaitForSeconds(0.5f);
            }
            else if (poiType == "show_victim") {
                string victimPoiTypeName = "Victim POI at " + cellName;

                GameObject victimPoi = cell.transform.Find(victimPoiTypeName)?.gameObject;

                int randomIndex = UnityEngine.Random.Range(0, 2);
                GameObject selectedPrefab = randomIndex == 0 ? droidPrefab : clonePrefab;

                if (victimPoi != null) {
                    Destroy(victimPoi);
                    points_of_interest.RemoveAt(i);

                    Quaternion rotation = Quaternion.Euler(0, 0, 0);
                    Vector3 position = cell.transform.position;
                    if (selectedPrefab.name == "Clone Trooper") {
                        rotation = Quaternion.Euler(0, 180, 0);
                    }
                    else if (selectedPrefab.name == "R2D2") {
                        rotation = Quaternion.Euler(-90, 0, 180);
                        position = cell.transform.position + new Vector3(0, 0.21f, 0);
                    }

                    GameObject poiObject = UnityEngine.Object.Instantiate(selectedPrefab, position, rotation);
                    poiObject.transform.SetParent(gridParent);
                    poiObject.name = $"Victim {selectedPrefab.name}";
                    yield return new WaitForSeconds(0.5f);
                }
            }
        }

        yield return null;
    }
}

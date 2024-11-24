using System.Collections.Generic;

[System.Serializable]
public class Door
{
    public List<int> coord1;
    public List<int> coord2;
    public string status;
}

[System.Serializable]
public class InitialGameData 
{
    public int damage_points;
    public int people_lost;
    public int people_rescued;
    public int width;
    public int height;
    public List<List<double>> walls;
    public List<List<double>> fires;
    public List<List<string>> points_of_interest;
    public List<Door> doors; 
    public List<int[]> entry_points;
}
using System.Collections.Generic;

[System.Serializable]
public class NewGameData 
{
    public int damage_points;
    public int people_lost;
    public int people_rescued;
    public int width;
    public int height;
    public List<NewStatusInt> walls;
    public List<NewStatusDouble> fires;
    public List<NewStatusIntList> damage;
    public List<NewStatusString> points_of_interest;
    public List<NewStatusDoors> doors; 
    public bool simulation_finished;
}

[System.Serializable]
public class NewStatusInt 
{
    public List<int> position;
    public int new_value;
}

[System.Serializable]
public class NewStatusString 
{
    public List<int> position;
    public string new_value;
}

[System.Serializable]
public class NewStatusDouble 
{
    public List<int> position;
    public double new_value;
}

[System.Serializable]
public class NewStatusIntList 
{
    public List<int> position;
    public List<int> new_value;
}

[System.Serializable]
public class NewStatusDoors
{
    public List<List<int>> position;
    public string new_value;
}

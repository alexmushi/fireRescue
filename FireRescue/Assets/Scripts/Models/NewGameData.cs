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
    public List<NewExplosion> explosions;
    public List<ActionData> actions;
    public List<AgentPosition> agent_positions;
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

[System.Serializable]
public class NewExplosion
{
    public List<int> position;
}

[System.Serializable]
public class ActionData
{
    public int agent_id;
    public string action; // e.g., "move", "extinguish_fire", "open_door", etc.
    public List<int> position; // For actions involving a single position
    public List<int> from;     // For movement actions
    public List<int> to;       // For movement actions
    public List<List<int>> positions; // For actions involving multiple positions (e.g., open_door)
}

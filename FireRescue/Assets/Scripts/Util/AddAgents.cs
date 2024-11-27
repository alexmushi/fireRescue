using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AddAgents : MonoBehaviour
{
    public static AddAgents Instance { get; private set; }

    [SerializeField] private GameObject agent1Prefab;
    [SerializeField] private GameObject agent2Prefab;
    [SerializeField] private GameObject agent3Prefab;
    [SerializeField] private GameObject agent4Prefab;
    [SerializeField] private GameObject agent5Prefab;
    [SerializeField] private GameObject agent6Prefab;

    private Dictionary<int, GameObject> agentsDictionary = new Dictionary<int, GameObject>();
    private Dictionary<int, bool> agentCarryingVictim = new Dictionary<int, bool>();

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

    public IEnumerator AddAgentsToCells(List<AgentPosition> agent_positions, Transform gridParent)
    {
        foreach (AgentPosition agentPosition in agent_positions)
        {
            GameObject agentPrefab = getAgentPrefab(agentPosition.agentID);

            string cellName = $"Cell({agentPosition.position[0]},{agentPosition.position[1]})";
            GameObject cell = gridParent.Find(cellName)?.gameObject;

            Quaternion agentRotation = getAgentRotation(agentPosition.agentID);

            Vector3 cellPosition = cell.transform.position;
            if (agentPosition.agentID == 1) {
                cellPosition = cell.transform.position + new Vector3(-0.3f, 0, 0);
            }
            else if (agentPosition.agentID == 5) {
                cellPosition = cell.transform.position + new Vector3(0, 0.15f, 0);
            }

            GameObject agent = Instantiate(agentPrefab, cellPosition, agentRotation);
            // Add the agent to the dictionary
            agentsDictionary[agentPosition.agentID] = agent;
            agentCarryingVictim[agentPosition.agentID] = false;


            yield return new WaitForSeconds(0.5f);
        }

        yield return null;
    }

    // NEW METHOD: Move an agent from one position to another
    public IEnumerator MoveAgent(int agentID, List<int> fromPosition, List<int> toPosition, Transform gridParent)
    {
        if (agentsDictionary.TryGetValue(agentID, out GameObject agent))
        {
            string toCellName = $"Cell({toPosition[0]},{toPosition[1]})";
            GameObject toCell = gridParent.Find(toCellName)?.gameObject;

            if (toCell != null)
            {
                Vector3 startPosition = agent.transform.position;
                Vector3 targetPosition = toCell.transform.position;

                // Smooth movement
                float movementDuration = 0.5f;
                float elapsedTime = 0f;
                while (elapsedTime < movementDuration)
                {
                    agent.transform.position = Vector3.Lerp(startPosition, targetPosition, (elapsedTime / movementDuration));
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
                agent.transform.position = targetPosition;
            }
            else
            {
                Debug.LogWarning($"Cell {toCellName} not found for moving agent.");
            }
        }
        else
        {
            Debug.LogWarning($"Agent with ID {agentID} not found in dictionary.");
        }

        yield return null;
    }

    // NEW METHOD: Handle agent picking up a victim
    public IEnumerator PickUpVictim(int agentID, List<int> position, Transform gridParent)
    {
        if (agentsDictionary.TryGetValue(agentID, out GameObject agent))
        {
            // Set the agent's carrying state to true
            agentCarryingVictim[agentID] = true;
            yield return null;
        }
        else
        {
            Debug.LogWarning($"Agent with ID {agentID} not found in dictionary.");
            yield return null;
        }
    }

    // NEW METHOD: Handle agent dropping off a victim
    public IEnumerator DropVictim(int agentID, List<int> position, Transform gridParent)
    {
        if (agentsDictionary.TryGetValue(agentID, out GameObject agent))
        {
            // Check if the agent was carrying a victim
            if (agentCarryingVictim.ContainsKey(agentID) && agentCarryingVictim[agentID])
            {
                // Set the agent's carrying state to false
                agentCarryingVictim[agentID] = false;

                yield return null;
            }
            else
            {
                Debug.LogWarning($"Agent {agentID} was not carrying a victim.");
                yield return null;
            }
        }
        else
        {
            Debug.LogWarning($"Agent with ID {agentID} not found in dictionary.");
            yield return null;
        }
    }

    private GameObject getAgentPrefab(int agentID)
    {
        GameObject agentPrefab = null;
        switch (agentID)
        {
            case 1:
                agentPrefab = agent1Prefab;
                break;
            case 2:
                agentPrefab = agent2Prefab;
                break;
            case 3:
                agentPrefab = agent3Prefab;
                break;
            case 4:
                agentPrefab = agent4Prefab;
                break;
            case 5:
                agentPrefab = agent5Prefab;
                break;
            case 6:
                agentPrefab = agent6Prefab;
                break;
            default:
                Debug.LogWarning($"No prefab assigned for agent ID {agentID}");
                break;
        }
        return agentPrefab;
    }

    private Quaternion getAgentRotation(int agentID)
    {
        Quaternion agentRotation = Quaternion.identity;
        switch (agentID)
        {
            case 1:
                agentRotation = Quaternion.Euler(0, 180, 0);
                break;
            case 2:
                agentRotation = Quaternion.Euler(0, 180, 0);
                break;
            case 3:
                agentRotation = Quaternion.Euler(0, 180, 0);
                break;
            case 4:
                agentRotation = Quaternion.Euler(-90, 180, 0);
                break;
            case 5:
                agentRotation = Quaternion.Euler(0, 180, 0);
                break;
            case 6:
                agentRotation = Quaternion.Euler(90, 180, 0);
                break;
            default:
                Debug.LogWarning($"No rotation assigned for agent ID {agentID}");
                break;
        }
        return agentRotation;
    }
}

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
                cellPosition = cell.transform.position + new Vector3(0.3f, 0, 0);
            }
            else if (agentPosition.agentID == 5) {
                cellPosition = cell.transform.position + new Vector3(0, 0.15f, 0);
            }

            GameObject agent = Instantiate(agentPrefab, cellPosition, agentRotation);

            yield return new WaitForSeconds(0.5f);
        }

        yield return null;
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
        }
        return agentPrefab;
    }

    private Quaternion getAgentRotation(int agentID)
    {
        Quaternion agentRotation = Quaternion.identity;
        switch (agentID)
        {
            case 1:
                agentRotation = Quaternion.identity;
                break;
            case 2:
                agentRotation = Quaternion.identity;
                break;
            case 3:
                agentRotation = Quaternion.identity;
                break;
            case 4:
                agentRotation = Quaternion.Euler(-90, 0, 0);
                break;
            case 5:
                agentRotation = Quaternion.identity;
                break;
            case 6:
                agentRotation = Quaternion.Euler(90, 0, 0);
                break;
        }
        return agentRotation;
    }
}

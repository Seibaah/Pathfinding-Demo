using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathfindingEngine : MonoBehaviour
{
    public List<List<node>> adjacency_list;
    public List<GameObject> list_agents = new List<GameObject>(), vertex_list;
    public int agent_count;
    public int plans=0, replans=0, successes=0;

    private ReducedVisibilityGraph graph_script;


    private void Start()
    {
        graph_script = GameObject.Find("Graph").GetComponent<ReducedVisibilityGraph>();

        vertex_list = graph_script.vertex_list;

        agent_count = graph_script.agent_count;

        //making a deep copy of the adjacency list
        adjacency_list = graph_script.adjacency_list.ConvertAll(x => new List<node>(x).ConvertAll(y => new node(y)));

        //Spawns the agents
        for(int i=0; i<agent_count; i++)
        {
            AgentThread();
        }
    }

    //gets stats from all the agents every frame
    private void Update()
    {
        plans = 0; replans = 0; successes = 0;

        foreach(GameObject a in list_agents)
        {
            plans += a.GetComponent<Agent>().plans;
            replans += a.GetComponent<Agent>().recalcs;
            successes += a.GetComponent<Agent>().sucesses;
        }
    }

    //Initially considered to use parallel computing but lack of time made me ditch the idea
    private void AgentThread()
    {
        agent_count = graph_script.agent_count;

        SpawnAgent();
    }

    //spawns an agent and sets the gameobject and its components
    private void SpawnAgent()
    {
        GameObject agent = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        agent.GetComponent<MeshRenderer>().material.color = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);

        agent.transform.localScale = -new Vector3(0.3f, 0.5f, 0.3f);

        Collider c = agent.GetComponent<Collider>();
        c.isTrigger = true;

        agent.AddComponent<Rigidbody>();
        agent.GetComponent<Rigidbody>().useGravity = false;

        list_agents.Add(agent);

        agent.transform.name = "agent";
        agent.AddComponent<Agent>();
    }

    //updates stats on teh screen
    private void OnGUI()
    {
        GUI.color = new Color(1, 0, 0, 1);

        GUI.Label(new Rect(10, 10, 300, 50), "Running time" + System.Math.Round(Time.time));

        GUI.Label(new Rect(10, 100, 300, 50), "Paths planned " + plans);

        GUI.Label(new Rect(10, 200, 300, 50), "Paths replanned " + replans);

        GUI.Label(new Rect(10, 300, 300, 50), "Paths completed " +  successes);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//node class used to build the adjacency list later
public class node
{
    public node(string name, Vector3 pos, float s_cost, float t_cost)
    {
        NAME = name;
        POS = pos;
        S_COST = s_cost;
        T_COST = t_cost;
        F_COST = Mathf.Infinity;
        PARENT = -1;
    }

    //copy constructor
    public node(node n)
    {
        NAME = string.Copy(n.NAME);
        POS = new Vector3(n.POS.x, n.POS.y, n.POS.z);
        S_COST = n.S_COST;
        T_COST = n.T_COST;
        F_COST = n.F_COST;
        PARENT = n.PARENT;
    }

    public string NAME { get; }

    public Vector3 POS { get; }

    public float S_COST { get; set; }

    public float T_COST { get; set; }

    public float F_COST { get; set; }

    public int PARENT { get; set; }

    public override string ToString() => $"({NAME}, {POS}, {PARENT}, {S_COST}, {T_COST}, {F_COST})";

} 

public class ReducedVisibilityGraph : MonoBehaviour
{
    public GameObject obs_spawner, graph_manager, agent_manager;
    public List<GameObject> vertex_list = new List<GameObject>();
    public List<List<node>> adjacency_list = new List<List<node>>();
    public Sphere script;
    public bool list_valid = false;
    public int agent_count;

    private int updates = 2;
    private float gridSpacingOffset = 0.5f, delta = 0.25f;
    private ObstacleSpawner obs_spawner_script;
    private List<Obstacle> obstacle_list = new List<Obstacle>();
    private Vector3 gridOrigin = Vector3.zero;

    private void Start()
    {
        obs_spawner_script = GameObject.Find(obs_spawner.name).GetComponent<ObstacleSpawner>();
        obstacle_list = obs_spawner_script.obstacle_list;

        agent_manager = GameObject.Find("Agent Manager");

        SpawnObstacleEndpoints();

        StartCoroutine(GraphTimer());
    }

    private void Update()
    {
        if (updates>0)
        {
            foreach (GameObject v in GameObject.FindGameObjectsWithTag("Vertex"))
            {
                vertex_list.Add(v);
            }
            updates--;
        }
    }

    //builds and draws the reduced visibility graph
    IEnumerator GraphTimer()
    {
        while (true) 
        {
            yield return new WaitUntil(() => updates == 0);

            //DrawObstaclesEdges();   //draw obstacles colinear edges

            ValidateVertexList();   //removes dups from vertex list

            AddBitangentEdges();    //adds and draws bitenagent lines

            EnablePathFinding();    //graph is ready we can pathfind

            yield return new WaitForSecondsRealtime(1000f);
        }
       
    }

    //spawn the spheres the delimit the obstacles
    private void SpawnObstacleEndpoints()
    {
        int count = 0;
        foreach (Obstacle o in obstacle_list)
        {
            string name = "Obstacle"+count++;
            GameObject obj = new GameObject();
            obj.name = name;
            obj.transform.SetParent(graph_manager.transform, true);

            //creating obstacle endpoint vertices spheres for the reduced visibilty graph
            if (o.XDIR == 1 && o.YDIR == 1)
            {
                SpawnSphere((float)o.X - 0.5f - delta, 0.75f, -(float)o.Y + 0.5f + delta, 0f, 1f, 1f, 1f, "Graph", ""+1, obj);
                //vertex_list.Add(SpawnSphere((float)o.X + 0.5f, 2f, -(float)o.Y -0.5f, 0f, 1f, 1f, 1f, "Graph", "Inner Corner", obj));

                SpawnSphere((float)(o.X - 0.5f + o.XLENGTH + delta), 0.75f, -(float)o.Y + 0.5f + delta, 0f, 1f, 1f, 1f, "Graph", "" +2, obj);
                SpawnSphere((float)(o.X + 0.5f + o.XLENGTH + delta - 1f), 0.75f, -(float)o.Y - 0.5f - delta, 0f, 1f, 1f, 1f, "Graph", "" +3, obj);

                SpawnSphere((float)o.X + 0.5f + delta, 0.75f, -(float)(o.Y + 0.5f + o.YLENGTH - 1f + delta), 0f, 1f, 1f, 1f, "Graph", "" +4, obj);
                SpawnSphere((float)o.X - 0.5f - delta, 0.75f, -(float)(o.Y - 0.5f + o.YLENGTH + delta), 0f, 1f, 1f, 1f, "Graph", "" +5, obj);
                
            }
            else if (o.XDIR == -1 && o.YDIR == -1)
            {
                //vertex_list.Add(SpawnSphere((float)o.X - 0.5f, 2f, -(float)o.Y + 0.5f, 0f, 1f, 1f, 1f, "Graph", "Inner Corner", obj));
                SpawnSphere((float)o.X + 0.5f + delta, 0.75f, -(float)o.Y - 0.5f - delta, 0f, 1f, 1f, 1f, "Graph", "" +1, obj);

                SpawnSphere((float)(o.X + 0.5f - o.XLENGTH - delta), 0.75f, -(float)o.Y - 0.5f - delta, 0f, 1f, 1f, 1f, "Graph", "" + 2, obj);
                SpawnSphere((float)(o.X - 0.5f - o.XLENGTH + 1f - delta), 0.75f, -(float)o.Y + 0.5f + delta, 0f, 1f, 1f, 1f, "Graph", "" + 3, obj);

                SpawnSphere((float)o.X - 0.5f - delta, 0.75f, -(float)(o.Y - 0.5f - o.YLENGTH + 1f - delta), 0f, 1f, 1f, 1f, "Graph", "" + 4, obj);
                SpawnSphere((float)o.X + 0.5f + delta, 0.75f, -(float)(o.Y + 0.5f - o.YLENGTH - delta), 0f, 1f, 1f, 1f, "Graph", "" + 5, obj);
                
            }
            else if (o.XDIR == 1 && o.YDIR == -1)
            {
                //vertex_list.Add(SpawnSphere((float)o.X - 0.5f + 1f, 2f, -(float)o.Y + 0.5f, 0f, 1f, 0f, 1f, "Graph", "Inner Corner", obj));
                SpawnSphere((float)o.X + 0.5f - 1f - delta, 0.75f, -(float)o.Y - 0.5f - delta, 0f, 1f, 0f, 1f, "Graph", "" + 1, obj);
               
                SpawnSphere((float)o.X + 0.5f - 1f - delta, 0.75f, -(float)(o.Y + 0.5f - o.YLENGTH - delta), 0f, 1f, 0f, 1f, "Graph", "" + 2, obj);
                SpawnSphere((float)o.X - 0.5f + 1f + delta, 0.75f, -(float)(o.Y - 0.5f - o.YLENGTH + 1f - delta), 0f, 1f, 0f, 1f, "Graph", "" + 3, obj);

                SpawnSphere((float)(o.X - 0.5f + o.XLENGTH + delta), 0.75f, -(float)o.Y + 0.5f + delta, 0f, 1f, 0f, 1f, "Graph", "" + 4, obj);
                SpawnSphere((float)(o.X + 0.5f - 1f + o.XLENGTH + delta), 0.75f, -(float)o.Y - 0.5f - delta, 0f, 1f, 0f, 1f, "Graph", "" + 5, obj);



            }
            else if (o.XDIR == -1 && o.YDIR == 1)
            {
                SpawnSphere((float)o.X - 0.5f + 1f + delta, 0.75f, -(float)o.Y + 0.5f + delta, 0f, 0f, 1f, 1f, "Graph", "" + 1, obj);
                //vertex_list.Add(SpawnSphere((float)o.X + 0.5f - 1f, 2f, -(float)o.Y - 0.5f, 0f, 0f, 1f, 1f, "Graph", "Inner Corner", obj));

                SpawnSphere((float)o.X - 0.5f + 1f + delta, 0.75f, -(float)(o.Y - 0.5f + o.YLENGTH + delta), 0f, 0f, 1f, 1f, "Graph", "" + 2, obj);
                SpawnSphere((float)o.X + 0.5f - 1f - delta, 0.75f, -(float)(o.Y + 0.5f + o.YLENGTH - 1f + delta), 0f, 0f, 1f, 1f, "Graph", "" + 3, obj);
                
                SpawnSphere((float)(o.X + 0.5f - o.XLENGTH - delta), 0.75f, -(float)o.Y - 0.5f - delta, 0f, 0f, 1f, 1f, "Graph", "" + 4, obj);
                SpawnSphere((float)(o.X - 0.5f + 1f - o.XLENGTH - delta), 0.75f, -(float)o.Y + 0.5f + delta, 0f, 0f, 1f, 1f, "Graph", "" + 5, obj);

            }

        }
    }

    //draw colinear edges
    private void DrawObstaclesEdges()
    {
        for (int i=0; i<vertex_list.Count-1; i++)
        {
            int cur = int.Parse(vertex_list[i].transform.name);
            int next = int.Parse(vertex_list[i + 1].transform.name);
            if (cur - next != -1 && cur != 5)
            {
                continue;
            }
            else {
                if (cur != 5)
                {
                    //Debug.DrawRay(vertex_list[i].transform.position, vertex_list[i + 1].transform.position - vertex_list[i].transform.position, Color.cyan, 101);
                    DrawValidEdge(vertex_list[i].transform.position, vertex_list[i + 1].transform.position - vertex_list[i].transform.position, Color.cyan, 101);
                } else if (i - 4 >= 0)
                {
                    if (int.Parse(vertex_list[i - 4].transform.name) == 1)
                    {
                        //Debug.DrawRay(vertex_list[i].transform.position, vertex_list[i - 4].transform.position - vertex_list[i].transform.position, Color.cyan, 101);
                        DrawValidEdge(vertex_list[i].transform.position, vertex_list[i - 4].transform.position - vertex_list[i].transform.position, Color.cyan, 101);
                    }
                    else if (int.Parse(vertex_list[i - 3].transform.name) == 1)
                    {
                        //Debug.DrawRay(vertex_list[i].transform.position, vertex_list[i - 3].transform.position - vertex_list[i].transform.position, Color.cyan, 101);
                        DrawValidEdge(vertex_list[i].transform.position, vertex_list[i - 3].transform.position - vertex_list[i].transform.position, Color.cyan, 101);
                    }
                    else if (int.Parse(vertex_list[i - 2].transform.name) == 1)
                    {
                        //Debug.DrawRay(vertex_list[i].transform.position, vertex_list[i - 2].transform.position - vertex_list[i].transform.position, Color.cyan, 101);
                        DrawValidEdge(vertex_list[i].transform.position, vertex_list[i - 2].transform.position - vertex_list[i].transform.position, Color.cyan, 101);
                    }
                    else if (int.Parse(vertex_list[i - 1].transform.name) == 1)
                    {
                        //Debug.DrawRay(vertex_list[i].transform.position, vertex_list[i - 1].transform.position - vertex_list[i].transform.position, Color.cyan, 101);
                        DrawValidEdge(vertex_list[i].transform.position, vertex_list[i - 1].transform.position - vertex_list[i].transform.position, Color.cyan, 101);
                    }
                }

            }
        }
    }

    //draws a ray if not obstacle interferes
    private void DrawValidEdge(Vector3 start, Vector3 dir, Color c, int t)
    {
        bool drawLine = true;
        RaycastHit[] hits;
        hits = Physics.RaycastAll(start, dir, dir.magnitude);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.CompareTag("Obstacle"))
            {
                drawLine = false;
                break;
            }
        }

        if (drawLine == true)
        {
            //Debug.DrawRay(start, dir, c, t);
        }

    }

    //eliminate duplicates from the vertex list
    private void ValidateVertexList()
    {
        vertex_list.Clear();
        int i = 1;

        foreach (GameObject v in GameObject.FindGameObjectsWithTag("Vertex"))
        {
            //each vertex is named in numerical ascending order
            v.transform.name = "" + i;
            vertex_list.Add(v);
            i+=1;
        }

        list_valid = true;

        //Debug.Log(vertex_list.Count);
    }

    //add and draws bitangent edges
    private void AddBitangentEdges()
    {

        int i = 0;
        foreach (GameObject v0 in vertex_list)
        {
            adjacency_list.Add(new List<node>());

            foreach (GameObject vf in vertex_list)
            {
                bool drawLine = true;

                if (vf == v0) continue;

                RaycastHit[] hits, hits_rev;
                Vector3 dir = vf.transform.position - v0.transform.position;
                Vector3 dir_rev = v0.transform.position - vf.transform.position;
                hits = Physics.RaycastAll(v0.transform.position, dir, dir.magnitude*1.2f);
                hits_rev = Physics.RaycastAll(vf.transform.position, dir_rev, dir_rev.magnitude*1.2f);

                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider.CompareTag("Obstacle") || hit.collider.CompareTag("Border"))
                    {
                        drawLine = false;
                        break;
                    }
                }
                
                foreach (RaycastHit hit in hits_rev)
                {
                    if (hit.collider.CompareTag("Obstacle") || hit.collider.CompareTag("Border"))
                    {
                        drawLine = false;
                        break;
                    }
                }

                if (drawLine == true)
                {
                    adjacency_list[i].Add(new node(vf.transform.name, vf.transform.position, Mathf.Infinity, Mathf.Infinity));

                    //Debug.DrawRay(v0.transform.position, dir, Color.magenta, 1000);
                }
            }

            i += 1;
        }

        //Debug.Log(adjacency_list.Count);
    }

    //adds the pathfinding script to the pathfinding manager
    private void EnablePathFinding()
    {
        agent_manager.AddComponent<PathfindingEngine>();
    }

    //spawn a sphere
    private GameObject SpawnSphere(float posX, float posY, float posZ, float r, float g, float b, 
        float a, string layer, string name, GameObject parent)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        Vector3 spawnPosition = new Vector3(posX + gridSpacingOffset, posY, posZ - gridSpacingOffset) + gridOrigin;
        sphere.transform.SetParent(parent.transform, true);
        sphere.transform.position = spawnPosition;
        sphere.transform.localScale = -new Vector3(0.2f, 0.2f, 0.2f);

        sphere.GetComponent<MeshRenderer>().material.color = new Color(r, g, b, a);
        sphere.GetComponent<MeshFilter>().mesh.RecalculateNormals();

        sphere.layer = LayerMask.NameToLayer(layer);
        sphere.name = name;
        sphere.tag = "Vertex";

        sphere.AddComponent<Rigidbody>();
        sphere.GetComponent<Rigidbody>().useGravity = false;
        sphere.GetComponent<Collider>().isTrigger = true;

        sphere.AddComponent<Sphere>();

        return sphere;
    }

}

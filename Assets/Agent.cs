using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Agent : MonoBehaviour
{
    public int plans = 0, recalcs = 0, sucesses = 0;

    private int count, path_recalcs;
    private bool movement_enabled= false, collisions_enabled = false, pathfinding_failure = false, pathfinding_success = false;
    private Vector3 target, next, last_pos, secondary_goal;
    private node spawn_node;
    private GameObject dest, target_mark;
    private List<GameObject> vertex_list;
    private List<node> open_list, closed_list;
    private PathfindingEngine engine_script;
    private List<List<node>> adjacency_list;
    private Stack<Vector3> path;

    private void Start()
    {
        engine_script = GameObject.Find("Agent Manager").GetComponent<PathfindingEngine>();

        vertex_list = engine_script.vertex_list;

        dest = new GameObject();

        SetSpawn();

        StartCoroutine(PerpetualPathfinding());
    }

    //enables non stopping pathfinding
    IEnumerator PerpetualPathfinding()
    {
        while (true)
        {
            //reset flags
            path_recalcs = 0;
            pathfinding_failure = false;
            pathfinding_success = false;

            //making a deep copy of the adjacency list
            adjacency_list = engine_script.adjacency_list.ConvertAll(x => new List<node>(x).ConvertAll(y => new node(y)));

            //add starting node to the adj list
            adjacency_list.Insert(0, new List<node>());
            AddStartToAdjList(0, gameObject.transform.position, Color.green);

            count = adjacency_list.Count;
            count++;

            //add end node to the adjacency list
            SetTarget();
            adjacency_list.Add(new List<node>());

            //Add edge between start and target if possible
            AddDirectEdge();

            count = adjacency_list.Count;

            adjacency_list[count - 1].Add(new node("" + count, target, Mathf.Infinity, 0));

            //A* algorithm
            FindPath();

            //Mark the goal to reach
            target_mark = SpawnSphere(target.x, target.y, target.z, 1f, 1f, 1f, 1f, "Target", "Target", gameObject);

            //error checking in case of pathfinding failure
            if (path.Count <= 0)
            {
                pathfinding_failure = true;
            }
            else
            {
                next = path.Pop();
                movement_enabled = true;
            }

            //wait till destination reached or failure triggered
            yield return new WaitUntil(() => pathfinding_success == true || pathfinding_failure == true);

            if (pathfinding_failure == true)
            {
                recalcs++;
                plans += 3;
            }
            else if (pathfinding_success == true)
            {
                sucesses++;
                plans++;
            }

            //destroy the target_mark
            Destroy(target_mark);

            yield return new WaitForSeconds(Random.Range(0.1f, 0.5f));
        }
    }

    //only used for agent movement
    private void Update()
    {
        if (movement_enabled == true)
        {
            last_pos = gameObject.transform.position;
            MoveToGoal();
        }
        
    }

    //sets randomly the spawn location of the agent
    private void SetSpawn()
    {
        while (true)
        {
            bool invalid = false;
            float x0 = Random.Range(0.5f, 21.5f);
            float z0 = Random.Range(-17.5f, -0.5f);

            Vector3 spawn = new Vector3(x0, 0.55f, z0);

            //if spawning on an obstacle try a new position
            foreach (GameObject o in GameObject.FindGameObjectsWithTag("Obstacle"))
            {
                if (o.GetComponent<Collider>().bounds.Contains(spawn))
                {
                    invalid = true;
                    break;
                }
            }
            if (invalid == true)
            {
                continue;
            }

            gameObject.transform.position = new Vector3(x0, 0.55f, z0);

            //create starting node 
            spawn_node = new node("0", gameObject.transform.position, 0, 0);

            break;
        }    
    }

    //Add the starting node to the adj list
    private void AddStartToAdjList(int i, Vector3 v0, Color c)
    {
        foreach (GameObject vf in vertex_list)
        {
            bool addEdge = true;

            RaycastHit[] hits, hits_rev;
            Vector3 dir = vf.transform.position - v0;
            Vector3 dir_rev = v0 - vf.transform.position;
            hits = Physics.RaycastAll(v0, dir, dir.magnitude);
            hits_rev = Physics.RaycastAll(vf.transform.position, dir_rev, dir_rev.magnitude);

            //can't add edges that collide with the static terrain
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.CompareTag("Obstacle") || hit.collider.CompareTag("Border"))
                {
                    addEdge = false;
                    break;
                }
            }
            foreach (RaycastHit hit in hits_rev)
            {
                if (hit.collider.CompareTag("Obstacle") || hit.collider.CompareTag("Border"))
                {
                    addEdge = false;
                    break;
                }
            }

            //add a bitangent line to the adj list
            if (addEdge == true)
            {
                adjacency_list[i].Add(new node(vf.transform.name, vf.transform.position, Mathf.Infinity, Mathf.Infinity));

                //Debug.DrawRay(v0, dir, c, 1000);
            }
        }
    }

    //set the goal position
    private void SetTarget()
    {
        while (true)
        {
            bool invalid = false;
            float xf = Random.Range(-4.5f, 26.5f);
            float zf = Random.Range(-22.5f, 3.5f);

            Vector3 v = new Vector3(xf, 0.55f, zf);

            RaycastHit[] hits;
            Vector3 dir = v - gameObject.transform.position;
            hits = Physics.RaycastAll(gameObject.transform.position, dir, dir.magnitude);

            //prevent the goal to be out of bounds
            foreach(RaycastHit hit in hits)
            {
                if (hit.collider.CompareTag("Border"))
                {
                    invalid = true;
                    break;
                }
            }

            //prevent the goal being inside static terrain
            foreach (GameObject o in GameObject.FindGameObjectsWithTag("Obstacle"))
            {
                if (o.GetComponent<Collider>().bounds.Contains(v))
                {
                    invalid = true;
                    break;
                }
            }

            if (invalid == true)
            {
                continue;
            }

            AddGoalToAdjList(v, Color.blue);

            target = new Vector3(xf, 0.55f, zf);

            dest.transform.position = target;

            //Debug.Log("X: " + xf);
            //Debug.Log("Z: " + zf);
            break;
        }
    }

    //add end goal to the adj list
    private void AddGoalToAdjList(Vector3 v0, Color c)
    {
        int i = 1;
        foreach (GameObject vf in vertex_list)
        {
            
            bool addEdge = true;

            RaycastHit[] hits, hits_rev;
            Vector3 dir = vf.transform.position - v0;
            Vector3 dir_rev = v0 - vf.transform.position;
            hits = Physics.RaycastAll(v0, dir, dir.magnitude);
            hits_rev = Physics.RaycastAll(vf.transform.position, dir_rev, dir_rev.magnitude);

            //can't add an edge if it collides with static terrain
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.CompareTag("Obstacle") || hit.collider.CompareTag("Border"))
                {
                    addEdge = false;
                    break;
                }
            }
            foreach (RaycastHit hit in hits_rev)
            {
                if (hit.collider.CompareTag("Obstacle") || hit.collider.CompareTag("Border"))
                {
                    addEdge = false;
                    break;
                }
            }

            if (addEdge == true)
            {
                adjacency_list[i].Add(new node(""+count, v0, Mathf.Infinity, 0));

                //Debug.DrawRay(v0, dir, c, 1000);
            }

            i++;
        }
    }

    //try adding a edge between the start and goal is possible
    private void AddDirectEdge()
    {
        bool addEdge = true;
        RaycastHit[] hits;
        Vector3 v0 = gameObject.transform.position, dir = target - v0;
        hits = Physics.RaycastAll(v0, dir, dir.magnitude);

        foreach(RaycastHit hit in hits)
        {
            if (hit.transform.CompareTag("Obstacle") || hit.transform.CompareTag("Border"))
            {
                addEdge = false;
                break;
            }
        }

        if (addEdge == true)
        {
            adjacency_list[0].Add(new node(""+count, target, Mathf.Infinity, Mathf.Infinity));
            //Debug.DrawRay(v0, dir, Color.red, 1000);
        }
    }

    //A* algorithm were s_cost is distance from start and t_cost is distance from the goal
    private void FindPath()
    {
        //update t_costs
        UpdateTCost();

        //initialize lists
        open_list = new List<node>();
        closed_list = new List<node>();

        //add the initial node
        open_list.Add(spawn_node);

        node curr;

        while (true)
        {
            int i = GetLowestCostNode(open_list);

            if (open_list.Count <= i)
            {
                pathfinding_failure=true;
                break;
            }
            curr = open_list[i];

            closed_list.Add(curr);

            open_list.Remove(curr);
            
            if (curr.NAME.Equals(""+count))
            {
                closed_list.Reverse();

                //Trace back path
                TraceBackPath(closed_list);

                break;
            }

            foreach (node neighbor in adjacency_list[int.Parse(curr.NAME)])
            {
                //skip if in the closed list
                if (closed_list.Exists(x => x.NAME.Equals(neighbor.NAME)))
                {
                    continue;
                }

                //calc new cost to neighbor
                float new_cost_to_neighbor = curr.S_COST + (neighbor.POS - curr.POS).magnitude;

                if (!open_list.Exists(x => x.NAME.Equals(neighbor.NAME)))
                {
                    open_list.Add(neighbor);
                }

                //find neighbor in the open lisy
                int j = open_list.FindIndex(x => x.NAME.Equals(neighbor.NAME));
                
                if (new_cost_to_neighbor < open_list[j].S_COST)
                {
                    open_list[j].S_COST = new_cost_to_neighbor;
                    open_list[j].F_COST = open_list[j].S_COST + open_list[j].T_COST;
                    open_list[j].PARENT = int.Parse(curr.NAME);
                }
            }
            
        }
    }

    //retuns index of lowest cost node in passed list
    private int GetLowestCostNode(List<node> list)
    {
        float lowest_cost = Mathf.Infinity;
        int index=0;

        for (int i=0; i<list.Count; i++)
        {
            if (list[i].F_COST <= lowest_cost)
            {
                lowest_cost = list[i].F_COST;
                index = i;
            }
            if (list[i].NAME.Equals(""+count))
            {
                lowest_cost = list[i].F_COST;
                index = i;
            }
        }

        return index;
    }

    //updates T_cost of all vertices in the adj list
    private void UpdateTCost()
    {
        for(int i=0; i<adjacency_list.Count; i++)
        {
            for (int j = 0; j < adjacency_list[i].Count; j++)
            {
                float dist = (target - adjacency_list[i][j].POS).magnitude;
                adjacency_list[i][j].T_COST = dist;
            }
        }
    }

    //Builds path back after a* succesfully terminates
    private void TraceBackPath(List<node> list)
    {
        path = new Stack<Vector3>();
        node curr = list[0];
        while (!curr.NAME.Equals("0"))
        {
            path.Push(curr.POS);
            //Debug.Log(curr.ToString());
            int next = curr.PARENT;
            curr = list.Find(x => curr.PARENT.ToString().Equals(x.NAME));
        }
    }

    //handles frame movement of the agent
    private void MoveToGoal()
    {
        // Move our position a step closer to the target.
        float step = 4f*Time.deltaTime; // calculate distance to move
        transform.position = Vector3.MoveTowards(transform.position, next, step);

        //if at temp target reached target the next one in the path sequence
        if(Vector3.Distance(transform.position, next) < 0.01f)
        {
            if (path.Count > 0)
            {
                next = path.Pop();
            }
            else
            {
                //path done
                pathfinding_success = true;
                movement_enabled = false;
                Destroy(target_mark);
            }
            
        }
    }

    //spawn a sphere
    private GameObject SpawnSphere(float posX, float posY, float posZ, float r, float g, float b,
        float a, string layer, string name, GameObject parent)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        Vector3 spawnPosition = new Vector3(posX, posY, posZ);
        //sphere.transform.SetParent(parent.transform, true);
        sphere.transform.position = spawnPosition;
        sphere.transform.localScale = -new Vector3(0.2f, 0.2f, 0.2f);

        sphere.GetComponent<MeshRenderer>().material.color = gameObject.GetComponent<MeshRenderer>().material.color;
        sphere.GetComponent<MeshFilter>().mesh.RecalculateNormals();

        //sphere.layer = LayerMask.NameToLayer(layer);
        sphere.name = name;
        sphere.tag = "Target";

        sphere.AddComponent<Rigidbody>();
        sphere.GetComponent<Rigidbody>().useGravity = false;
        sphere.GetComponent<Collider>().isTrigger = true;

        return sphere;
    }

    //handles collision response
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Target") && !other.CompareTag("Vertex"))
        {
            StartCoroutine(CollisionHandler());
        }
    }

    //wait and recalculate path to same goal if blocked
    //if blocked for the 3rd time abandon and find a new goal
    IEnumerator CollisionHandler()
    {
        if (path_recalcs == 3)
        {
            pathfinding_failure = true;
            movement_enabled = false;
        }
        else
        {
            movement_enabled = false;
            path_recalcs++;
            yield return new WaitForSeconds(Random.Range(0.1f, 0.3f));
            FindPath();
            movement_enabled = true;

        }
    }
}

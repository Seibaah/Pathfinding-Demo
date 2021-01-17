using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sphere : MonoBehaviour
{
    public List<GameObject> list;

    private GameObject graph_manager;

    private void Start()
    {
        graph_manager = GameObject.Find("Graph");
        list = graph_manager.GetComponent<ReducedVisibilityGraph>().vertex_list;
    }

    //this class is only used to delete obstacles vertices is they collide with an adjacent obstacle
    public void OnTriggerEnter(Collider col)
    {
        graph_manager = GameObject.Find("Graph");
        list = graph_manager.GetComponent<ReducedVisibilityGraph>().vertex_list;

        if (col.CompareTag("Obstacle"))
        {
            list.Remove(gameObject);

            Destroy(gameObject);
        }
    }
}

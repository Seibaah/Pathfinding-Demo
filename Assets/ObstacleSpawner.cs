using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//struct that describes an L-shape obstacle
public struct Obstacle
{
    public Obstacle(int x, int y, int xDir, int yDir, int xLength, int yLength)
    {
        X = x;
        Y = y;
        XDIR = xDir;
        YDIR = yDir;
        XLENGTH = xLength;
        YLENGTH = yLength;
    }

    public double X { get; }
    public double Y { get; }
    public double XDIR { get; }
    public double YDIR { get; }
    public double XLENGTH { get; }
    public double YLENGTH { get; }

    public override string ToString() => $"({X}, {Y}, {XDIR}, {YDIR}, {XLENGTH}, {YLENGTH})";
}

public class ObstacleSpawner : MonoBehaviour
{
    public GameObject obstacle_manager;
    public List<Obstacle> obstacle_list = new List<Obstacle>();

    private float gridSpacingOffset = 0.5f;
    private float x0 = 0f, xf = 21f, y0 = 0f, yf = 17f;  //obstacle spawn area
    private Vector3 gridOrigin = Vector3.zero;

    private void Start()
    {
        SpawnObstacles();
    }

    //spawn 3-4 obstacles at random locations
    private void SpawnObstacles()
    {
        int count = Random.Range(3, 5);
        int[,] map = new int[19, 23]; //[row,col]
        while (count > 0)   //spawn 3 L-piece obstacles
        {
            //corner of the L-piece
            int x = (int)Random.Range(x0, xf);
            int y = (int)Random.Range(y0, yf);
            while (map[y, x] != 0)  //prevent spawn on top of another obstacle
            {
                x = (int)Random.Range(x0, xf);
                y = (int)Random.Range(y0, yf);
            }

            //randomizing directions of the L-piece
            int x_dir = 0, y_dir = 0;
            while (x_dir == 0 || y_dir == 0)
            {
                x_dir = Random.Range(-1, 2);    //x=1 means go right, -1 means go left
                y_dir = Random.Range(-1, 2);    //y=1 means go down, -1 means go up
            }

            //randomizing sizes of the L-piece branches
            int x_length = Random.Range(2, 7);
            int y_length = Random.Range(2, 7);

            //can we extend in those directions
            if (x - x_length < x0 || x + x_length > xf)
            {
                //out of bounds in x-axis, reroll
                continue;
            }
            if (y - y_length < y0 || y + y_length > yf)
            {
                //out of bounds in y-axis, reroll
                continue;
            }

            //can we create a min size L-piece at spawn location
            if (map[y, x] != 0 || map[y, x + x_dir] != 0 || map[y + y_dir, x] != 0)
            {
                //Debug.Log("Cant spawn L piece");
                continue;   //reroll the obstacle spawn
            }

            //prevent cutting into other obstacles with obstacles
            int dx = 0, dy = 0;
            bool x_check = false, y_check = false, canWrite = true;
            while (!x_check || !y_check)
            {
                if (map[y + dy, x] != 0)
                {
                    canWrite = false;
                    break;
                }
                if (map[y, x + dx] != 0)
                {
                    canWrite = false;
                    break;
                }

                if (Mathf.Abs(dx) < x_length)
                {
                    dx += x_dir;
                }
                else
                {
                    x_check = true;
                }
                if (Mathf.Abs(dy) < y_length)
                {
                    dy += y_dir;
                }
                else
                {
                    y_check = true;
                }
            }

            if (canWrite == false)
            {
                continue;
            }

            //save the obj onto the map
            int delta_x = 0, delta_y = 0;
            bool x_done = false, y_done = false;
            while (!x_done || !y_done)
            {
                if (delta_x == 0 && delta_y == 0)
                {
                    map[y, x] = 5;
                    delta_x += x_dir;
                    delta_y += y_dir;
                }
                else
                {
                    if (map[y + delta_y, x] == 0 && Mathf.Abs(delta_y) < y_length)
                    {
                        map[y + delta_y, x] = count;
                        delta_y += y_dir;
                    }
                    else 
                    {
                        y_done = true;
                    }
                    if (map[y, x + delta_x] == 0 && Mathf.Abs(delta_x) < x_length)
                    {
                        map[y, x + delta_x] = count;
                        delta_x += x_dir;
                    }
                    else
                    {
                        x_done = true;
                    }
                }

            }
            //create obstacle representation struct and add it to the world obstacle list
            Obstacle obs = new Obstacle(x, y, x_dir, y_dir, Mathf.Abs(delta_x), Mathf.Abs(delta_y));
            obstacle_list.Add(obs);
            //Debug.Log(obs.ToString());

            count--;
        }

        for (int col = 0; col < 22; col++)
        {
            for (int row = 0; row < 18; row++)
            {
                if (map[row, col] != 0)
                {
                    float color = map[row, col] / 5f;
                    SpawnCube(col, 0.5f, -row, color, 0.75f, 0.75f, 1f, "Obstacle");
                }
            }
        }
    }

    //spawn a cube
    private void SpawnCube(float posX, float posY, float posZ, float r, float g, float b, float a, string layer)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

        Vector3 spawnPosition = new Vector3(posX + gridSpacingOffset, posY, posZ - gridSpacingOffset) + gridOrigin;
        cube.transform.SetParent(obstacle_manager.transform, true);
        cube.transform.position = spawnPosition;

        cube.GetComponent<MeshRenderer>().material.color = new Color(r, g, b, a);
        cube.GetComponent<MeshFilter>().mesh.RecalculateNormals();

        cube.layer = LayerMask.NameToLayer(layer);
        cube.tag = "Obstacle";

        cube.AddComponent<Rigidbody>();
        cube.GetComponent<Rigidbody>().useGravity = false;
        cube.GetComponent<Collider>().isTrigger = true;
    }
}

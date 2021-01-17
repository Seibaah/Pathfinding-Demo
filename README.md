# A* Pathfinding (Demo-PC)

•	3-D Demo made in Unity

•	A* Algorithm on a reduced visibility graph 

•	Scalable agen capacity

A reduced visibility graph is built with colinear and bitangent lines between object and alcove vertices in the level. An adjacency list is built from this graph and a copy is passed to each agent. 

Agents spawn at a random place in the level, fix a random target and using A* find a path near optimal to arrive to their destination. 

Once arrived a new destination is recalculated, and the process starts again. If another agent blocks the path a recalculation happens to try and avoid a collision, after 3 recalculations the target is abandoned, and a new destination is chosen.

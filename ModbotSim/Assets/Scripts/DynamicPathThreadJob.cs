using UnityEngine;
using System.Collections.Generic;

// DynamicPathThreadJob is a class that implements the thread to dynamically calculate/modify
// the path of the car as it moves through the map
public class DynamicPathThreadJob : ThreadJob
{
	public Node startNode;
	public Node endNode;
	public Node destinationNode;
	private List<Vector3> pathWayPoints; 
	private HashSet<Vector3> closedNodes;
	private double pathLength;
	private bool useItemReduction;

	// <summary>
	// Constructor that initializes the start node and end node of the path planning
	// </summary>
	// <param name="startNode">Node object where the path planning starts from</param>
	// <param name="endNode">Node object representing where the path should move towards</param> 
	public DynamicPathThreadJob(Node startNode, Node endNode, HashSet<Vector3> closedNodes, double pathLength, bool useItemReduction) {
		this.startNode = startNode;
		this.endNode = endNode;
		this.closedNodes = closedNodes;
		destinationNode = null;
		this.pathLength = pathLength;
		this.useItemReduction = useItemReduction;
	}

	// <summary>
	// Performs an A* traversal from the start node to the end node; however, the end node
	// only provides a direction for the path finding, as once the current node in the 
	// traversal is pathLength away from the start node, the traversal is complete
	// </summary>
	protected override void ThreadFunction() {
		PriorityQueue<Node> open = new PriorityQueue<Node> (PathPlanningDataStructures.graph.Size ());
		Dictionary<Node, Node> came_from = new Dictionary<Node, Node> ();
		Dictionary<Node, float> cost_so_far = new Dictionary<Node, float> ();
		came_from.Add (startNode, null);
		cost_so_far.Add (startNode, 0);
		open.queue (PathPlanningDataStructures.heuristic.Estimate (startNode, useItemReduction), 
			startNode);
		while (open.getSize() > 0) {
			Node current = open.dequeue ();

			if (current.Equals (PathPlanningDataStructures.graph.endNode) || 
				Node.distanceBetweenNodes (startNode, current) >= pathLength) {
				if (came_from[current] == null) {
					came_from[current] = startNode;
				}
				destinationNode = current;
				break;
			}

			foreach (Node n in current.neighbors) {
				float graph_cost = cost_so_far [current] + Node.distanceBetweenNodes (current, n);
				if ((cost_so_far.ContainsKey (n) == false || graph_cost < cost_so_far [n]) && 
				closedNodes.Contains(n.position) == false) {
					cost_so_far [n] = graph_cost;
					float priority = graph_cost + 
						PathPlanningDataStructures.heuristic.Estimate (n, useItemReduction);
					open.queue (priority, n);
					came_from [n] = current;
				}
			}
		}
	
		//Put nodes of the path into the list
		pathWayPoints = new List<Vector3> ();
		Node currentNode = destinationNode;
		pathWayPoints.Add (currentNode.position);
		lock (PathPlanningDataStructures.globalLock) {
			PathPlanningDataStructures.nodeToCount [currentNode.position] += 1;
		}
		while (currentNode.Equals(startNode) == false) {
			currentNode = came_from [currentNode];
			pathWayPoints.Add (currentNode.position);
			lock (PathPlanningDataStructures.globalLock) {
				PathPlanningDataStructures.nodeToCount [currentNode.position] += 1;
			}
		}
		pathWayPoints.Reverse ();
	}

	// <summary>
	// Returns the list of path way points that were determined from ThreadFunction
	// </summary>
	public List<Vector3> getPathWayPoints() {
		return pathWayPoints;
	}

	// <summary>
	//	Returns already visited nodes in the A* traversal
	// </summary>
	public HashSet<Vector3> getClosedNodes() {
		return closedNodes;
	}
}
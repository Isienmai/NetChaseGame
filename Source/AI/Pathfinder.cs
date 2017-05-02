using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI
{
	//this class exists to implement various pathfinding algorithms that can be used by the heirarchical graph
	public static class Pathfinder
	{
		public static List<NodeIndex> AstarPathfind(Node startNode, Node endNode, List<Node> nodesToSearch)
		{
			//ASSUMPTIONS: 
			//				Start Node is on the same layer as the set of nodes to search
			//				End Node is on the same layer as the start Node, or one level above!

			//reset the path of all nodes
			foreach (Node n in nodesToSearch)
			{
				n.ResetPath();
			}

			WorldGraph fullAIGraph = WorldGraph.GetWorldGraph();

			//create the open and closed lists
			List<NodeIndex> openList = new List<NodeIndex>();
			List<NodeIndex> closedList = new List<NodeIndex>();

			//find the current layer being searched through
			int nodeListDepth = startNode.index.GetMaxDepth();
			bool endNodeOnSameLayer = nodeListDepth == endNode.index.GetMaxDepth();
			
			//currentNode is the node being examined
			NodeIndex currentNode;
			NodeIndex nextNode;

			openList.Add(startNode.index);

			while (openList.Count() > 0)
			{
				//Get the cheapest node in the open list
				currentNode = GetCheapestNode(openList, nodesToSearch, endNode.internalNodes);



				//if the goal has been found then retrace the path and return it
				if (currentNode.EqualAtDepth(endNode.index, endNode.index.GetMaxDepth()))
				{
					if(endNodeOnSameLayer)
					{
						//retrace the path from the end node
						return RetracePath(endNode, nodesToSearch);
					}
					else
					{
						return RetracePath(endNode.GetNode(currentNode), nodesToSearch);
					}
				}


				//if the goal has not been found then add the nodes at the end of all connections to the open list, and close the current node
				foreach (Connection edge in nodesToSearch[currentNode.GetLowestIndex()].connections)
				{
					nextNode = edge.destination;
					//ignore if the node has already been closed
					if (!closedList.Contains(nextNode))
					{
						//calculate the cost by looking at the accumulated cost, the connection cost, and the heuristic
						float totalCost = Math.Abs(nodesToSearch[currentNode.GetLowestIndex()].accumulatedCost + edge.traversalCost + fullAIGraph.topLevelNode.GetNode(nextNode).GetHeuristic(fullAIGraph.goalCoords));
						Node tempNode = GetNodeFromLists(nextNode, nodesToSearch, endNode.internalNodes);

												
						if (tempNode != null) 
						{
							//if the node is not currently in the open list then add it to the open list
							if (!openList.Contains(nextNode))
							{
								openList.Add(nextNode);

								//set the parent Id and the current cost
								tempNode.previousNode = currentNode;
								tempNode.accumulatedCost = totalCost;
							}
							//update the node in the list if the new path is faster
							else if (tempNode.accumulatedCost > totalCost)
							{
								//set the parent Id and the current cost
								tempNode.previousNode = currentNode;
								tempNode.accumulatedCost = totalCost;
							}
						}
						
					}

				}

				closedList.Add(currentNode);
				openList.Remove(currentNode);
			}

			return null; // If the goal is not found, return null
		}

		//return the list of nodes that the path consists of
		private static List<NodeIndex> RetracePath(Node destNode, List<Node> finalNodeList)
		{
			List<NodeIndex> finalPath = new List<NodeIndex>();
			NodeIndex index = destNode.index;

			finalPath.Add(index);
			index = destNode.previousNode;

			if (index == null) return finalPath;
			while (finalNodeList[index.GetLowestIndex()].previousNode != null)
			{
				finalPath.Add(index);
				finalNodeList[index.GetLowestIndex()].accumulatedCost = 0;

				index = finalNodeList[index.GetLowestIndex()].previousNode;
			}
			finalNodeList[index.GetLowestIndex()].accumulatedCost = 0;

			finalPath.Add(index);
			finalPath.Reverse();

			return finalPath;
		}

		//get the cheapest node in the open list
		private static NodeIndex GetCheapestNode(List<NodeIndex> currentOpenList, List<Node> nodes, List<Node> endNodeList)
		{
			if (currentOpenList.Count <= 0) return null;

			Node toReturn = GetNodeFromLists(currentOpenList[0], nodes, endNodeList);
			Node temp;

			float currentCost = toReturn.accumulatedCost, newCost;

			//for each node check if it is cheaper than the currently selected node
			//Take into account the heuristic value of both nodes
			foreach (NodeIndex aNode in currentOpenList)
			{
				temp = GetNodeFromLists(aNode, nodes, endNodeList);

				newCost = temp.accumulatedCost;

				if (newCost < currentCost)
				{
					toReturn = temp;
					currentCost = newCost;
				}
			}

			return toReturn.index;
		}

		//given a node index and two lists that COULD contain it, return the node from ONE of them
		private static Node GetNodeFromLists(NodeIndex nIndex, List<Node> list1, List<Node> list2)
		{
			if (nIndex == null) return null;
			if (list1 == null && list2 == null) return null;

			if ( list1.Count > 0 && nIndex.GetParentIndex() == list1[0].index.GetParentIndex())
			{
				if (list1.Count <= nIndex.GetLowestIndex()) return null;
				return list1[nIndex.GetLowestIndex()];
			}
			if ( list2.Count > 0 && nIndex.GetParentIndex() == list2[0].index.GetParentIndex())
			{
				if (list2.Count <= nIndex.GetLowestIndex()) return null;
				return list2[nIndex.GetLowestIndex()];
			}

			return null;
		}

		//This function is intended as a much faster alternative to A*, for use at the lowest node level
		//While it improves performance, it is also much less flexable than A*. It relies heavily on the assumption that any node on a rigidbody can be reached from any other node on that same rigidbody.
		//Either A* or some alternative will be required to properly handle fully dynamic environments.
		public static List<NodeIndex> GetNextDestNode(Node startNode, Node endNode, List<Node> nodesToSearch, Node topLevelNode)
		{
			List<NodeIndex> toReturn = new List<NodeIndex>();
			//if the destination is on the current rigidbody simply return the destination index
			if (endNode.index.GetParentIndex() == startNode.index.GetParentIndex())
			{
				toReturn.Add(endNode.index);
				return toReturn;
			}
			
			Node localDest = null;
			Node foreignDest = null;

			foreach(Node n in nodesToSearch)
			{
				if (localDest == null || (n.position - startNode.position).Length() < (localDest.position - startNode.position).Length())
				{
					foreach (Connection c in n.connections)
					{
						if (c.destination.EqualAtDepth(endNode.index, endNode.index.GetMaxDepth()))
						{
							localDest = n;
							foreignDest = topLevelNode.GetNode(c.destination);
						}
					}
				}				
			}

			if (startNode.index != localDest.index) toReturn.Add(localDest.index); 
			toReturn.Add(foreignDest.index);

			return toReturn;
		}
	}
}

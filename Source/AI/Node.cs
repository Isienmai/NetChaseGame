using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI
{
	//This class exists to index each node within the heirarchical graph
	public class NodeIndex : IEquatable<NodeIndex>
	{
		//every index is a list of the indexes of the parent nodes, followed by the index of the current node within the parent
		List<int> combinedIndex;
		
		


		public NodeIndex()
		{
			combinedIndex = new List<int>();
		}

		public NodeIndex(int topLevelIndex)
		{
			combinedIndex = new List<int>();
			combinedIndex.Add(topLevelIndex);
		}

		public NodeIndex(NodeIndex newIndex)
		{
			combinedIndex = new List<int>();
			foreach (int index in newIndex.combinedIndex)
			{
				AddNextLevelIndex(index);
			}
		}

		public NodeIndex(NodeIndex parentIndex, int localIndex) : this (parentIndex)
		{
			combinedIndex.Add(localIndex);
		}





		public void AddNextLevelIndex(int nextLevelIndex)
		{
			combinedIndex.Add(nextLevelIndex);
		}

		//check if an index is "depth" layers deep
		public bool IndexReachesLevel(int depth)
		{
			return combinedIndex.Count > depth;
		}

		//get the number of parent nodes plus one
		public int GetMaxDepth()
		{
			return combinedIndex.Count - 1;
		}

		//return the index of the parent node
		public NodeIndex GetParentIndex()
		{
			NodeIndex toReturn = new NodeIndex();
			for(int i = 0; i < combinedIndex.Count - 1; ++i)
			{
				toReturn.AddNextLevelIndex(combinedIndex[i]);
			}
			return toReturn;
		}

		//This function returns the current node index cut off at the croppingDepth
		public NodeIndex GetCroppedIndex(int croppingDepth)
		{
			NodeIndex toReturn = new NodeIndex();

			for(int i = 0; i < croppingDepth && i < combinedIndex.Count; ++i)
			{
				toReturn.AddNextLevelIndex(combinedIndex[i]);
			}

			return toReturn;
		}

		//return the index at a given depth
		public int GetIndexAtDepth(int depth)
		{
			if (IndexReachesLevel(depth)) return combinedIndex[depth];
			return -1;
		}

		//return the node's local index
		public int GetLowestIndex()
		{
			return combinedIndex[combinedIndex.Count - 1];
		}

		//return true if the current index and the input index match up to the provided depth
		public bool EqualAtDepth(NodeIndex rhs, int depth)
		{
			if (object.ReferenceEquals(rhs, null)) return false;
			if (depth < 0) return false;


			for(int i = 0; i <= depth; ++i)
			{
				if (this.GetIndexAtDepth(i) != rhs.GetIndexAtDepth(i)) return false;
			}

			return true;
		}


		//overload the equality operators to compare internal values, not references
		public bool Equals(NodeIndex other)
		{
			return this == other;
		}

		public static bool operator ==(NodeIndex lhs, NodeIndex rhs)
		{
			if (object.ReferenceEquals(rhs, null) && object.ReferenceEquals(lhs, null)) return true;
			if (object.ReferenceEquals(rhs, null)) return false;
			if (object.ReferenceEquals(lhs, null)) return false;

			if (lhs.GetMaxDepth() != rhs.GetMaxDepth()) return false;	
			
			for(int i = 0; i <= lhs.GetMaxDepth(); ++i)
			{
				if (lhs.GetIndexAtDepth(i) != rhs.GetIndexAtDepth(i)) return false;
			}

			return true;
		}

		public static bool operator !=(NodeIndex lhs, NodeIndex rhs)
		{
			return !(lhs == rhs);
		}
	}


	//allow different kinds of nodes to exist in the graph
	public enum NODE_TYPE
	{
		GROUP,
		RIGIDBODY,
		FLOOR,
		FALL,
		WALL
	}


	//this class is a heirarchical node
    public class Node
    {
		//the current node's index, type, a list of its connections, and its child nodes
		public NodeIndex index { get; protected set; }
		public NODE_TYPE type { get; private set; }
		public List<Connection> connections { get; protected set; }
		public List<Node> internalNodes { get; private set; }
		

		//These variables cache pathfinding requests, with an entry in the lists for each active AI
		public List<NodeIndex> lastDestinations { get; private set; }
		public List<NodeIndex> lastOrigins { get; private set; }
		public List<List<NodeIndex>> lastPaths { get; private set; }


		//the position of this node relative to it's parent (all node positions are OFFSETS, NOT WORLD SPACE LOCATIONS)
		public Physics.Vector2D position { get; private set; }

		//The following two variables are for temporary use during pathfinding
		public NodeIndex previousNode;
		public float accumulatedCost { get; set; }
		

		public Node(Physics.Vector2D aPosition, NodeIndex nodeIndex, NODE_TYPE nodeType)
		{
			type = nodeType;

			index = new NodeIndex(nodeIndex);
			connections = new List<Connection>();
			internalNodes = new List<Node>();


			lastDestinations = new List<NodeIndex>();
			lastOrigins = new List<NodeIndex>();
			lastPaths = new List<List<NodeIndex>>();

			position = aPosition;
		}
		//make a copy of the provided node
		public Node(Node baseNode)
		{
			index = baseNode.index;
			connections = baseNode.connections;
			internalNodes = baseNode.internalNodes;

			lastDestinations = baseNode.lastDestinations;
			lastOrigins = baseNode.lastOrigins;
			lastPaths = baseNode.lastPaths;

			position = baseNode.position;
		}

		//add a node to the list of child nodes
		public void AddNode(Node newNode)
		{
			internalNodes.Add(newNode);
		}

		//return the node described by the node index
		public Node GetNode(NodeIndex nodeToFind)
		{
			//if the requested node is the current node then return the current node
			if (index == nodeToFind) return this;

			//if the node isn't local then there's nothing to return
			if (!index.EqualAtDepth(nodeToFind, index.GetMaxDepth())) return null;

			//delegate the request to the child node specified in the nodeToFind index
			int nextNodeDown = nodeToFind.GetIndexAtDepth(index.GetMaxDepth() + 1);
			if (nextNodeDown >= internalNodes.Count || nextNodeDown < 0) return null;
			return internalNodes[nextNodeDown].GetNode(nodeToFind);
		}


		public void addConnection(Connection newConn)
		{
			connections.Add(newConn);
		}
		
		//used to reset this node's path information, so that it doesn't get carried across to the next pathfinding request
		public void ResetPath()
		{
			accumulatedCost = -1;
			previousNode = null;
		}

		public float GetHeuristic(Physics.Vector2D destination)
		{
			//Apply a simple euclidean distance heuristic
			//Intended to give nodes a higher cost the further they are from the final goal
			//If a different destination node is used as an argument then this heuristic may work better or worse
			WorldGraph mainGraph = WorldGraph.GetWorldGraph();
			Physics.Vector2D currentPosition = mainGraph.topLevelNode.GetNodePosition(index);
			return (currentPosition - destination).Length() * 10;
		}

		//get the node's world position
		public Physics.Vector2D GetNodePosition(Node node)
		{
			if (node == null) return null;
			return GetNodePosition(node.index);
		}

		//recursively add up the positions of all nodes in the specified index to get the position in world space
		//this way, if a parent node is moved then all of it's children will move with it automatically
		public Physics.Vector2D GetNodePosition(NodeIndex nodeIndex)
		{
			//if this node is the node specified return it's current position
			if (nodeIndex == index) return position;

			//if not then return the position of the child node specified in nodeIndex, plus this nodes position
			else return internalNodes[nodeIndex.GetIndexAtDepth(index.GetMaxDepth() + 1)].GetNodePosition(nodeIndex) + position;
		}


		//return the node from the lowest level that is closest to the specified location
		public Node GetNearestNode(Physics.Vector2D point)
		{
			//If this node is at the lowest depth then return it
			if(internalNodes == null || internalNodes.Count == 0)
			{
				return this;
			}
			
			Node nearestNode = internalNodes[0].GetNearestNode(point);
			Node tempNode;

			//get the nearest point from each child node and return the one which is closest to the specified point
			foreach(Node n in internalNodes)
			{
				tempNode = n.GetNearestNode(point);

				float currentDistance = (GetNodePosition(nearestNode) - point).Length();
				float newDistance = (GetNodePosition(tempNode) - point).Length();
				if (currentDistance > newDistance)
				{
					nearestNode = tempNode;
				}
			}

			return nearestNode;
		}


		//return the path from the start point to the destination point
		public List<NodeIndex> GetPath(int aiAgentIndex, NodeIndex startPoint, NodeIndex endPoint, Node topLevelNode)
		{
			if (lastPaths == null || lastPaths.Count <= aiAgentIndex) return null;

			if (startPoint == endPoint) return null;

			//if the start and end point are both in the same child node, delegate to that child
			int nextDepth = index.GetMaxDepth() + 1;
			if (startPoint.EqualAtDepth(endPoint, nextDepth))
			{
				if (startPoint.GetIndexAtDepth(nextDepth) >= internalNodes.Count) return null;
				if (startPoint.GetIndexAtDepth(nextDepth) < 0) return null;

				return internalNodes[startPoint.GetIndexAtDepth(nextDepth)].GetPath(aiAgentIndex, startPoint, endPoint, topLevelNode);
			}
			

			//if there is no cached path for the specified AI
			if( lastPaths[aiAgentIndex] == null || lastPaths[aiAgentIndex].Count < 1 || lastDestinations[aiAgentIndex] != endPoint || lastOrigins[aiAgentIndex] != startPoint)
			{
                bool present = false;
				
				//check the cached paths of the other AI's to see if any of them would be suitable
				for (int i = 0; i < lastOrigins.Count; ++i)
				{
					if (i != aiAgentIndex && (lastOrigins[i] == startPoint) && (lastDestinations[i] == endPoint))
                    {
                        lastPaths[aiAgentIndex] = new List<NodeIndex>(lastPaths[i]);
                        lastOrigins[aiAgentIndex] = startPoint;
                        lastDestinations[aiAgentIndex] = endPoint;

                        i = lastOrigins.Count;
                        present = true;
                    }
				}

				//if not then generate the path
				if(!present)
				{
					GeneratePath(aiAgentIndex, startPoint, endPoint, topLevelNode);
					if (lastPaths[aiAgentIndex] == null || lastPaths[aiAgentIndex].Count < 1) return null;
					lastDestinations[aiAgentIndex] = endPoint;
					lastOrigins[aiAgentIndex] = startPoint;
				}				
			}


			//if the next level is the deepest level then return this level's path
			if(startPoint.GetMaxDepth() == nextDepth)
			{
				return lastPaths[aiAgentIndex];
			}
			
			//If there is only one node in the path then something is wrong, because by this point the start and endpoint should be in different nodes
			if(lastPaths[aiAgentIndex].Count < 2) return null;

			
			int localStart = lastPaths[aiAgentIndex][0].GetIndexAtDepth(nextDepth);
			NodeIndex localDest = lastPaths[aiAgentIndex][1];

			//find the path within the first node of the current level path and return that
			//Once the path has been found, pathfind within it's first node to determine how to get to the second node of the path
			return internalNodes[localStart].GetPath(aiAgentIndex, startPoint, localDest, topLevelNode);
		}


		//populate the cached path of the given AI with the path from startPoint to endPoint
		private void GeneratePath(int aiAgentIndex, NodeIndex startPoint, NodeIndex endPoint, Node topLevelNode)
		{
			bool destinationWithinCurrentNode = startPoint.EqualAtDepth(endPoint, index.GetMaxDepth());
			
			Node start = internalNodes[startPoint.GetIndexAtDepth(index.GetMaxDepth() + 1)];
			Node end;
			if (!destinationWithinCurrentNode) end = topLevelNode.GetNode(endPoint);
			else end = internalNodes[endPoint.GetIndexAtDepth(index.GetMaxDepth() + 1)];
			

			//If pathfinding in the lowest layer use the cheaper pathfinding method, else use A*
			if (startPoint.GetMaxDepth() == index.GetMaxDepth() + 1)
			{
				lastPaths[aiAgentIndex] = Pathfinder.GetNextDestNode(start, end, internalNodes, topLevelNode);
			}
			else
			{
				lastPaths[aiAgentIndex] = Pathfinder.AstarPathfind(start, end, internalNodes);
			}

		}

		//add an item to each cached path list, then cascade to child nodes
		public void AddAIAgent()
		{
			lastDestinations.Add(new NodeIndex());
			lastOrigins.Add(new NodeIndex());
			lastPaths.Add(new List<NodeIndex>());

			foreach(Node childNode in internalNodes)
			{
				childNode.AddAIAgent();
			}

		}

		//remove the specified item from each cached path list, then cascade to child nodes
		public void RemoveAIAgent(int index)
		{
			if (lastPaths == null || lastPaths.Count <= index) return;

			lastDestinations.RemoveAt(index);
			lastOrigins.RemoveAt(index);
			lastPaths.RemoveAt(index);

			foreach (Node childNode in internalNodes)
			{
				childNode.RemoveAIAgent(index);
			}

		}
	}



	//This class represents the node of a moving platform (definition found in MovingPlatform.cs)
	//For simplicity, a moving platform simply moves back and forth between two points
	//this class was created to assist connection creation
	//Once the connections are created, it is up to the AIagent to determine when a given connection is usable(done by using the CanPlayerJumpToFrom()/CanPlayerFallToFrom() methods in the player class)
	public class NodeOnRails : Node
	{
		public Physics.Vector2D railPoint1 { get; private set;}
		public Physics.Vector2D railPoint2 { get; private set;}

		public NodeOnRails(Node coreNode, Physics.Vector2D point1, Physics.Vector2D point2) : base (coreNode)
		{
			railPoint1 = point1;
			railPoint2 = point2;
		}
	}	
}

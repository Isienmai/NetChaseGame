using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI
{
    public class WorldGraph
    {
		//This graph contains all the nodes and connections that an AI needs to navigate through the environment
		//The nodes are organised heirarchically, such that each rigidbody is treated as a single node, and contains multiple nodes on it's surface
		//These nodes are searched using A* search, with a simple heuristic of "distance to final destination". Improving this heuristic would produce better pathfinding results.

		//force WorldGraph to be a singleton, as there should only be one and it tends to be required by a lot of different classes
		private static WorldGraph worldGraphSingleton;

		//this is the top node of the graph. All other nodes are descendants of this node
		public Node topLevelNode;

		//keep track of the overall goal of all paths
		public Physics.Vector2D goalCoords { get; set; }
		//store one calculated path for each AI agent. (each path is a list of Nodes to traverse)
		public List<List<NodeIndex>> paths { get; private set; }

		public static WorldGraph GetWorldGraph()
		{
			if(worldGraphSingleton == null)
			{
				worldGraphSingleton = new WorldGraph();
			}
			return worldGraphSingleton;
		}

        private WorldGraph()
        {
			paths = new List<List<NodeIndex>>();
		}

		//create the graph from the rigidbodies in the physics engine
		public void PopulateGraph(Physics.PhysicsEngine physEng)
		{
			topLevelNode = new Node(new Physics.Vector2D(0, 0), new NodeIndex(0), NODE_TYPE.GROUP);
			GraphBuilder.PopulateGraph(physEng, topLevelNode);
		}		

		//find the path to the goal from a given position
		//the AI index is used to cache each AI's path for quick lookup
		public void GeneratePathToCurrentGoal(int aiAgentIndex, Physics.Vector2D start)
		{
			if (paths == null || paths.Count <= aiAgentIndex) return;

			Node startNode = topLevelNode.GetNearestNode(start);
			Node endNode = topLevelNode.GetNearestNode(goalCoords);

			if (startNode == null || endNode == null) return;

			List<NodeIndex> temp = topLevelNode.GetPath(aiAgentIndex, startNode.index, endNode.index, topLevelNode);

			//update the ai's path to the path retreived from the nodes
			paths[aiAgentIndex].Clear();
			if (temp != null)
			{
				paths[aiAgentIndex] = new List<NodeIndex>(temp);
			}
		}
		
		//obtain the next node that the specified AI should move to
		public Node GetNextDestination(int aiAgentIndex, Node currentPosition)
		{
			if (paths == null || paths.Count <= aiAgentIndex) return null;
			if (paths[aiAgentIndex] == null || paths[aiAgentIndex].Count == 0) return null;

			//if the current position is null, then the AI is not on any node. Return the start of the path.
			if (currentPosition == null) return topLevelNode.GetNode(paths[aiAgentIndex][0]);

			//if the AI is currently on a node, return the last node of the current rigidbody, or the first node of the next rigidbody
			NodeIndex finalNodeOnCurrentRB = null;
			for(int i = 0; i < paths[aiAgentIndex].Count; ++i)
			{
				if (finalNodeOnCurrentRB == null) finalNodeOnCurrentRB = paths[aiAgentIndex][i];
				else if(finalNodeOnCurrentRB.EqualAtDepth(paths[aiAgentIndex][i], finalNodeOnCurrentRB.GetMaxDepth() - 1))
				{
					finalNodeOnCurrentRB = paths[aiAgentIndex][i];
				}
			}

			return topLevelNode.GetNode(finalNodeOnCurrentRB);
		}		

		//convert a coordinate into it's corresponding node
		public Node GetNodeAtPoint(Physics.Vector2D point)
		{
			Node temp = topLevelNode.GetNearestNode(point);

			//if the node is not close enough to the specified point then return null
			if ((topLevelNode.GetNodePosition(temp) - point).Length() > 25) return null;
			return temp;
		}


		//add/remove an AI agent
		public void AddAIagent()
		{
			paths.Add(new List<NodeIndex>());
			topLevelNode.AddAIAgent();
		}
		public void RemoveAIagent(int index)
		{
			if (paths == null || paths.Count <= index) return;
			paths.RemoveAt(index);

			topLevelNode.RemoveAIAgent(index);
		}
    }	


	//this class exists to turn the rigidbodies in the physics engine into a node/connection graph
	//all graph generation is handled in here
	public static class GraphBuilder
	{
		//loop through all rigidbodies and add each one to the graph
		public static void PopulateGraph(Physics.PhysicsEngine physEng, Node topNode)
		{
			foreach (var rb in physEng.staticPhysicsObjects)
			{
				//create the rigibody's node
				topNode.AddNode(GraphBuilder.CreateNodesFromRB(rb, topNode.index, topNode.internalNodes.Count));
				AddInternalConnections(topNode.internalNodes[topNode.internalNodes.Count - 1]);

				//connect the new node to the previous nodes
				for (int i = 0; i < topNode.internalNodes.Count - 1; ++i)
				{
					GraphBuilder.AddConnections(topNode.internalNodes[topNode.internalNodes.Count - 1], topNode.internalNodes[i]);
				}
			}

			foreach(Physics.MovingPlatform mp in physEng.movingPlatforms)
			{
				//create the rigibody's node
				topNode.AddNode(new NodeOnRails(GraphBuilder.CreateNodesFromRB(mp.platform, topNode.index, topNode.internalNodes.Count), mp.point1, mp.point2));
				AddInternalConnections(topNode.internalNodes[topNode.internalNodes.Count - 1]);

				//connect the new node to the previous nodes
				for (int j = 0; j < topNode.internalNodes.Count - 1; ++j)
				{
					GraphBuilder.AddConnections(topNode.internalNodes[topNode.internalNodes.Count - 1], topNode.internalNodes[j]);
				}
			}

			//Note: this method does not cull connections that get blocked by new rigidbodies.
			//The naive solution to this would increase an already long graph generation time, and as such has not been implemented.
		}


		//given a rigidbody, create its node and its child nodes, then return its node
		public static Node CreateNodesFromRB(Physics.RigidBody theRB, NodeIndex parentIndex, int localIndex)
		{
			//create the node for the rigidbody
			Node rigidBody = new Node(theRB.Position, new NodeIndex(parentIndex, localIndex), NODE_TYPE.RIGIDBODY);

			//this temporary position is used to place the child nodes
			Physics.Vector2D nodePosition = new Physics.Vector2D();
			nodePosition.Y = theRB.Shape.ComputeAABB().MIN.Y - (Physics.Player.playerRadius + 1);


			//Create the leftmost fall node
			nodePosition.X = theRB.Shape.ComputeAABB().MIN.X - (Physics.Player.playerRadius + 1);			
			rigidBody.AddNode(new Node(new Physics.Vector2D(nodePosition), new NodeIndex(rigidBody.index, rigidBody.internalNodes.Count), NODE_TYPE.FALL));


			//Move the node position so it is not above a sheer drop
			nodePosition.X = theRB.Shape.ComputeAABB().MIN.X + (Physics.Player.playerRadius / 2);

			//Create the regular nodes an even distance apart
			float surfaceWidth = theRB.Shape.ComputeAABB().MAX.X - theRB.Shape.ComputeAABB().MIN.X;
			int numberOfNodes = (int)(surfaceWidth / (Physics.Player.playerRadius * 2) + 0.5);
			float step = (surfaceWidth - Physics.Player.playerRadius) / numberOfNodes;

			for (int i = 0; i <= numberOfNodes; ++i)
			{
				rigidBody.AddNode(new Node(new Physics.Vector2D(nodePosition), new NodeIndex(rigidBody.index, rigidBody.internalNodes.Count), NODE_TYPE.FLOOR));
				nodePosition.X += step;
			}


			//Create the rightmost fall node
			nodePosition.X = theRB.Shape.ComputeAABB().MAX.X + (Physics.Player.playerRadius + 1);
			rigidBody.AddNode(new Node(new Physics.Vector2D(nodePosition), new NodeIndex(rigidBody.index, rigidBody.internalNodes.Count), NODE_TYPE.FALL));


			return rigidBody;
		}

		//create all the connections between the child nodes of a rigidbody (it is assumed they can all walk to each other without interruption)
		public static void AddInternalConnections(Node rb)
		{
			Node lastNode = null;
			foreach(Node n in rb.internalNodes)
			{
				if(lastNode != null)
				{
					n.addConnection(new Connection(lastNode.index, (lastNode.position - n.position).Length(), CONNECTION_TYPE.WALK));
					lastNode.addConnection(new Connection(n.index, (lastNode.position - n.position).Length(), CONNECTION_TYPE.WALK));
				}
				lastNode = n;
			}
		}

		//add connections both ways between the two provided rigidbody nodes
		public static void AddConnections(Node rb1, Node rb2)
		{
			AddConnectionsFromTo(rb1, rb2);
			AddConnectionsFromTo(rb2, rb1);
		}

		//create the connection from rb1 to rb2 if there is one between their child nodes
		public static void AddConnectionsFromTo(Node rb1, Node rb2)
		{
			//find out if any of the node's children are connected to the other node's children
			//if not then exit early
			CONNECTION_TYPE connection = GetConnectionBetweenRigidBodies(rb1, rb2);
			if (connection == CONNECTION_TYPE.NONE) return;

			float cost = (rb1.position - rb2.position).Length();
			//other costs can be included here, such as taking into account the type of motion required (jump/fall/walk), or looking at the properties of the rigidbodies such as friction or bounciness
			rb1.addConnection(new Connection(rb2.index, cost, CONNECTION_TYPE.HIGH_LEVEL));			
		}

		//The following method assumes the nodes provided are rigidbodies with leaf nodes attached to them
		private static CONNECTION_TYPE GetConnectionBetweenRigidBodies(Node rigidBody1, Node rigidBody2)
		{
			WorldGraph mainGraph = WorldGraph.GetWorldGraph();
			CONNECTION_TYPE toReturn = CONNECTION_TYPE.NONE;
			List<Physics.Vector2D> nodePositions1, nodePositions2;

			//compare each node from rigidbody 1 to each node in rigidbody 2
			foreach(Node node1 in rigidBody1.internalNodes)
			{
				foreach(Node node2 in rigidBody2.internalNodes)
				{
					//Due to the presence of moving platforms, create lists of the different possible positions of each node
					nodePositions1 = new List<Physics.Vector2D>();
					nodePositions2 = new List<Physics.Vector2D>();

					//add the node's default positions
					nodePositions1.Add(mainGraph.topLevelNode.GetNodePosition(node1));
					nodePositions2.Add(mainGraph.topLevelNode.GetNodePosition(node2));

					//if rigidbody 1 moves between two points, add the position of node1 at BOTH those points to the list
					if(rigidBody1 is NodeOnRails)
					{
						nodePositions1.Add(
							mainGraph.topLevelNode.GetNodePosition(rigidBody1.index.GetParentIndex()) + ((NodeOnRails)rigidBody1).railPoint1 + node1.position);

						nodePositions1.Add(
							mainGraph.topLevelNode.GetNodePosition(rigidBody1.index.GetParentIndex()) + ((NodeOnRails)rigidBody1).railPoint2 + node1.position);
					}

					//if rigidbody 2 moves between two points, add the position of node2 when at BOTH those points to the list
					if (rigidBody2 is NodeOnRails)
					{
						nodePositions2.Add(
							mainGraph.topLevelNode.GetNodePosition(rigidBody2.index.GetParentIndex()) + ((NodeOnRails)rigidBody2).railPoint1 + node2.position);

						nodePositions2.Add(
							mainGraph.topLevelNode.GetNodePosition(rigidBody2.index.GetParentIndex()) + ((NodeOnRails)rigidBody2).railPoint2 + node2.position);
					}



					//at this point two lists have been created containing all the positions each node could occupy
					//loop through both lists to determine if any position combinations should be connected


					CONNECTION_TYPE temp;

					//loop through both lists, adding all possible connections to node1
					foreach(Physics.Vector2D pos1 in nodePositions1)
					{
						foreach (Physics.Vector2D pos2 in nodePositions2)
						{
							//find out if the two nodes can be connected
							temp = GetConnectionBetween(node1.type, node2.type, pos1, pos2);
							

							if (temp != CONNECTION_TYPE.NONE)
							{
								float cost = (pos1 - pos2).Length();

								//modify cost based on the type of connection
								switch(temp)
								{
									case CONNECTION_TYPE.JUMP:
										cost += 20;
										break;
									case CONNECTION_TYPE.FALL:
										cost += 5;
										break;
									default:
										cost += 2;
										break;
								}

								node1.addConnection(new Connection(node2.index, cost, temp));

								//change the return value to reflect the presence of a connection
								toReturn = CONNECTION_TYPE.HIGH_LEVEL;
							}

						}
					}		
				}
			}
			return toReturn;
		}


		//check to see if the player could jump or fall between the provided nodes
		private static CONNECTION_TYPE GetConnectionBetween(NODE_TYPE sourceNodeType, NODE_TYPE destNodeType, Physics.Vector2D sourceNodePosition, Physics.Vector2D destinationNodePosition)
		{
			//a fall node should never be the destination of a jump or a fall, just the sourcepoint for a fall
			if (destNodeType == NODE_TYPE.FALL) return CONNECTION_TYPE.NONE;

			Physics.Player examplePlayer = new Physics.Player();
			Physics.PhysicsEngine physEng = Physics.PhysicsEngine.GetPhysicsEngine();

			//if a fall connection is possible, return it
			if ((sourceNodeType == NODE_TYPE.FALL ||
				 sourceNodeType == NODE_TYPE.WALL) && 
				 physEng.CanPlayerFallFromTo(examplePlayer, sourceNodePosition, destinationNodePosition))
			{
				return CONNECTION_TYPE.FALL;
			}

			//if a jump connection is possible, return it
			if	((sourceNodeType == NODE_TYPE.FLOOR ||
				 sourceNodeType == NODE_TYPE.WALL) && 
				 physEng.CanPlayerJumpFromTo(examplePlayer, sourceNodePosition, destinationNodePosition))
			{
				return CONNECTION_TYPE.JUMP;
			}

			return CONNECTION_TYPE.NONE;
		}
	}
}

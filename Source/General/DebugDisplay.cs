using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace General
{
	//This class exists to display information for debugging purposes
	class DebugDisplay
	{
		//each bool determines if that piece of information should be displayed
		bool showCurrentPlayerJump;
		bool showNodes;
		bool showPath;
		bool showJumpRange;
		bool showFallRange;
		bool showJumpBoxCollisions;
		bool showFallBoxCollisions;
		bool showCollisionNormals;

		//acts the same as the above bools, except the value determines which rigidbody to show the information of
		int showLocalNodes;

		//Used when displaying jump arcs to give a specific destination to aim for
		Physics.Vector2D jumpDestination;

		//keep track of the player, AI's, node graph, and the set of static & dynamic objects
		Physics.Player playerCharacter;
		AI.AIManager mAImanager;
		Physics.PhysicsEngine physEng;
		AI.WorldGraph AIgraph;

		//Use the renderer to draw to the screen
		SimpleRenderer mRenderer;


		public DebugDisplay(Physics.Player thePlayer, AI.AIManager aManager, SimpleRenderer aRender)
		{
			//get references to the useful game classes
			playerCharacter = thePlayer;
			mAImanager = aManager;
			physEng = Physics.PhysicsEngine.GetPhysicsEngine();
			AIgraph = AI.WorldGraph.GetWorldGraph();

			mRenderer = aRender;

			//initialise the bools
			showCurrentPlayerJump = false;
			showNodes = false;
			showPath = false;
			showJumpRange = false;
			showFallRange = false;
			showJumpBoxCollisions = false;
			showFallBoxCollisions = false;
			showCollisionNormals = false;
			showLocalNodes = -1;

			jumpDestination = null;
		}

		//draw all currently active debug infos
		public void DisplayDebugGraphics(Graphics displayDevice)
		{
			if (showCollisionNormals) DrawAllCollisionNormals(displayDevice);
			if (showCurrentPlayerJump) DrawPlayerJump(displayDevice);
			if (showNodes) DrawRigidbodyGraph(displayDevice);
			if (showLocalNodes != -1) DrawLocalNodes(displayDevice);
			if (showPath)
			{
				DrawAIPath(displayDevice);
				DrawAIState(displayDevice);
			}
			if (showJumpRange) DrawJumpRange(displayDevice);
			if (showFallRange) DrawFallRange(displayDevice);
			if (showJumpBoxCollisions) DrawJumpCullingPoints(displayDevice);
			if (showFallBoxCollisions) DrawFallCullingPoints(displayDevice);
		}

		//for each dynamic object in the scene, draw the normal of its current collision (if it has one)
		private void DrawAllCollisionNormals(Graphics displayDevice)
		{
			Physics.Vector2D temp;
			foreach(Physics.RigidBody rb in physEng.dynamicPhysicsObjects)
			{
				temp = rb.collisionNormal;
				if(temp != null && temp.Length() != 0)
				{
					mRenderer.DrawArrow(rb.Position, rb.Position + temp * 100, displayDevice);
				}
			}
		}

		//Write the state that each AI agent is currently in to the left of the screen
		private void DrawAIState(Graphics displayDevice)
		{
			Physics.Vector2D drawLocation = new Physics.Vector2D(10,10);
			foreach (AI.AIEngine AIbrain in mAImanager.aiControllers) 
			{
				//setup the string to be written
				string currentState = "NULL";
				switch (AIbrain.currentState)
				{
					case AI.STATES.WAITING:
						currentState = "waiting";
						break;
					case AI.STATES.WALKING:
						currentState = "walking";
						break;
					case AI.STATES.JUMPING:
						currentState = "jumping";
						break;
					case AI.STATES.FALLING:
						currentState = "falling";
						break;
				}

				//write the string
				mRenderer.Draw(currentState, drawLocation, displayDevice);

				//update the position to draw to
				drawLocation.Y += 20;
			}			
		}

		//draw the player's jump arc if they jumped at this moment with no acceleration or deceleration
		private void DrawPlayerJump(Graphics displayDevice)
		{
			DrawJumpArc(displayDevice, playerCharacter, jumpDestination);
		}


		//draw the jump arc of a specified player to a specified destination
		//if specified destination is null then draw the jump assuming the player jumped with no acceleration or deceleration

		//This function is useful for debugging the "Player.GetJumpFromSourceToDest()", "Player.GetJumpYFromX()", and "Player.GetJumpXFromY()" methods
		private void DrawJumpArc(Graphics displayDevice, Physics.Player thePlayer, Physics.Vector2D goal)
		{
			//a temporary variable used in draw calls
			Physics.RigidBody temp = new Physics.RigidBody();
			
			//stores how long the player accelerates after hitting jump
			float accelerationTime = 0.0f;

			//if a goal was specified
			//	calculate the amount of time the player would need to accelerate after jumping in order to reach the goal
			//	draw the goal point
			if (goal != null)
			{
				accelerationTime = thePlayer.GetJumpFromSourceToDest(thePlayer.playerBody.Position, goal, physEng.gravity);

				//draw the goal
				temp.Shape = new Physics.Circle(6);
				temp.Shape.mColor = Color.Purple;
				temp.SetPosition(goal);
				mRenderer.Draw(temp, displayDevice);
			}

			//set the object to be drawn to a circle of radius 3
			temp.Shape = new Physics.Circle(3);


			//Temporary variable used to store the coordinates returned from the GetJumpYFromX() and GetJumpXFromY() methods
			Tuple<Physics.Vector2D, Physics.Vector2D> resultingPoints;

			//loop through the 1200 X coordinates around the player, calculating the Y position of the jump at that point
			//Draw a circle at each combined X,Y coordinate in Green
			temp.Shape.mColor = Color.Green;
			for (int i = -600; i < 600; i += 1)
			{
				//Get the two possible X,Y coords of the jump given a specific X coord
				resultingPoints = thePlayer.GetJumpYFromX(thePlayer.playerBody.Position, accelerationTime, 15.0f, physEng.gravity, thePlayer.playerBody.Position.X - i);

				//draw any valid returned coordinates
				if (resultingPoints.Item1 != null)
				{
					temp.SetPosition(resultingPoints.Item1);
					mRenderer.Draw(temp, displayDevice);
				}
				if (resultingPoints.Item2 != null)
				{
					temp.SetPosition(resultingPoints.Item2);
					mRenderer.Draw(temp, displayDevice);
				}
			}

			//loop through the 1200 Y coordinates around the player, calculating the X position of the jump at that point
			//Draw a circle at each combined X,Y coordinate in red
			temp.Shape.mColor = Color.OrangeRed;
			for (int i = -600; i < 600; i += 10)
			{
				//Get the two possible X,Y coords of the jump given a specific Y coord
				resultingPoints = thePlayer.GetJumpXFromY(thePlayer.playerBody.Position, accelerationTime, 15.0f, physEng.gravity, thePlayer.playerBody.Position.Y - i);

				//draw any valid returned coordinates
				if (resultingPoints.Item1 != null)
				{
					temp.SetPosition(resultingPoints.Item1);
					mRenderer.Draw(temp, displayDevice);
				}
				if (resultingPoints.Item2 != null)
				{
					temp.SetPosition(resultingPoints.Item2);
					mRenderer.Draw(temp, displayDevice);
				}
			}
		}

		//Draw the nodes and connections associated with all the rigidbodies, and any current AI paths through the rigidbody graph
		private void DrawRigidbodyGraph(Graphics displayDevice)
		{
			Physics.RigidBody temp = new Physics.RigidBody();
			temp.Shape = new Physics.Circle(5);

			AI.NodeIndex previousNode;

			//for each rigidbody, draw it's core node, all its child nodes, and all its connections
			foreach (AI.Node rb in AIgraph.topLevelNode.internalNodes)
			{
				//draw the node
				temp.SetPosition(AIgraph.topLevelNode.GetNodePosition(rb.index));
				temp.Shape.mColor = Color.DarkOrange;
				mRenderer.Draw(temp, displayDevice);

				//draw its children
				foreach (AI.Node nd in rb.internalNodes)
				{
					temp.SetPosition(AIgraph.topLevelNode.GetNodePosition(nd.index));
					temp.Shape.mColor = Color.Chocolate;
					mRenderer.Draw(temp, displayDevice);
				}

				//draw its connections
				foreach (AI.Connection rbconn in rb.connections)
				{
					mRenderer.DrawLine(rb.position, AIgraph.topLevelNode.GetNodePosition(rbconn.destination), displayDevice, Color.FromArgb(9, Color.Black));
				}

				//draw each Ai's current high level path
				if(AIgraph.topLevelNode.lastPaths != null)
				{
					foreach(List<AI.NodeIndex> lastPath in AIgraph.topLevelNode.lastPaths)
					{
						previousNode = null;
						if(lastPath != null)
						{
							foreach (AI.NodeIndex node in lastPath)
							{
								previousNode = AIgraph.topLevelNode.GetNode(node).previousNode;

								if (previousNode != null) mRenderer.DrawLine(AIgraph.topLevelNode.GetNodePosition(previousNode), AIgraph.topLevelNode.GetNodePosition(node), displayDevice, Color.FromArgb(129, Color.Black));

							}
						}						
					}
					
				}
				
			}
		}

		//Draw the local nodes and their connections of the rigidbody with index stored in "showLocalNodes"
		private void DrawLocalNodes(Graphics displayDevice)
		{
			Physics.RigidBody temp = new Physics.RigidBody();
			temp.Shape = new Physics.Circle(5);

			//reset showLocalNodes to it's null value if it has been incremented above the number of rigidbodies
			if (showLocalNodes >= AIgraph.topLevelNode.internalNodes.Count)
			{
				showLocalNodes = -1;
			}
			else
			{
				foreach (AI.Node nd in AIgraph.topLevelNode.internalNodes[showLocalNodes].internalNodes)
				{
					//draw the node
					temp.SetPosition(AIgraph.topLevelNode.GetNodePosition(nd.index));
					temp.Shape.mColor = Color.Chocolate;
					mRenderer.Draw(temp, displayDevice);

					//draw its connections
					foreach (AI.Connection ndconn in nd.connections)
					{
						if (ndconn.connType == AI.CONNECTION_TYPE.JUMP)
						{
							mRenderer.DrawLine(temp.Position, AIgraph.topLevelNode.GetNodePosition(ndconn.destination), displayDevice, Color.DarkOrange);
						}
						else if (ndconn.connType == AI.CONNECTION_TYPE.FALL)
						{
							mRenderer.DrawLine(temp.Position, AIgraph.topLevelNode.GetNodePosition(ndconn.destination), displayDevice, Color.DarkMagenta);
						}
						else
						{
							mRenderer.DrawLine(temp.Position, AIgraph.topLevelNode.GetNodePosition(ndconn.destination), displayDevice, Color.Black);
						}
					}
				}
			}			

			//also draw the player's current node, if the player has one
			AI.Node nearestNode = AIgraph.GetNodeAtPoint(playerCharacter.playerBody.Position);
			if(nearestNode != null)
			{
				temp.SetPosition(AIgraph.topLevelNode.GetNodePosition(nearestNode.index));
				temp.Shape.mColor = Color.Black;
				mRenderer.Draw(temp, displayDevice);
			}
		}


		//Draw the short term paths of all AI and display the current number of active AI
		private void DrawAIPath(Graphics displayDevice)
		{
			//write the number of AI currently active in the top right of the screen
			mRenderer.Draw(AIgraph.paths.Count().ToString(), new Physics.Vector2D(1200, 10), displayDevice);

			//draw all short term paths 
			Physics.RigidBody temp = new Physics.RigidBody();
			AI.NodeIndex previousNode;
			for(int i = 0; i < AIgraph.paths.Count(); ++i)
			{
				//Draw the AI's current destination node
				temp.Shape = new Physics.Circle(8);
				temp.Shape.mColor = Color.BlueViolet;
				temp.SetPosition(AIgraph.topLevelNode.GetNodePosition(AIgraph.GetNextDestination(i, AIgraph.GetNodeAtPoint(mAImanager.aiControllers[i].AIPlayer.playerBody.Position))));
				if (temp.Position != null) mRenderer.Draw(temp, displayDevice);

				//draw all nodes in the short term path
				temp.Shape.mColor = Color.Green;
				temp.Shape = new Physics.Circle(5);
				foreach (AI.NodeIndex node in AIgraph.paths[i])
				{
					//draw the node
					temp.SetPosition(AIgraph.topLevelNode.GetNodePosition(node));
					if (temp.Position != null) mRenderer.Draw(temp, displayDevice);

					//draw the connection between that node and the previous node
					previousNode = AIgraph.topLevelNode.GetNode(node).previousNode;
					if (previousNode != null) mRenderer.DrawArrow(AIgraph.topLevelNode.GetNodePosition(previousNode), temp.Position, displayDevice);

				}
			}			
		}

		//draw the current range of the player's jump by drawing a grid of dots to the screen
		//each dot it white if it cannot be jumped to and black if it can
		//This method DOES take into account collisions with rigidbodies
		private void DrawJumpRange(Graphics displayDevice)
		{
			Physics.RigidBody temp = new Physics.RigidBody();
			temp.Shape = new Physics.Circle(7);
			temp.SetPosition(new Physics.Vector2D());

			//draw a grid of dots 40 units apart
			for (int i = -700; i < 700; i += 40)
			{
				for (int j = -360; j < 400; j += 40)
				{
					//calculate the position of the dot
					temp.Position.X = playerCharacter.playerBody.Position.X + i;
					temp.Position.X -= temp.Position.X % 40;
					temp.Position.Y = playerCharacter.playerBody.Position.Y + j;
					temp.Position.Y -= temp.Position.Y % 40;

					//set colour based on ability to jump to calculated position
					if (physEng.CanPlayerJumpFromTo(playerCharacter, playerCharacter.playerBody.Position, temp.Position)) temp.Shape.mColor = Color.Black;
					else temp.Shape.mColor = Color.White;

					//draw a dot at that position
					mRenderer.Draw(temp, displayDevice);
				}
			}			
		}

		//the same as DrawJumpRange except it shows what areas the player can currently fall to
		private void DrawFallRange(Graphics displayDevice)
		{
			Physics.RigidBody temp = new Physics.RigidBody();
			temp.Shape = new Physics.Circle(7);
			temp.SetPosition(new Physics.Vector2D());

			//draw a grid of dots 40 units apart
			for (int i = -700; i < 700; i += 40)
			{
				for (int j = -360; j < 400; j += 40)
				{
					//calculate the position of the dot
					temp.Position.X = playerCharacter.playerBody.Position.X + i;
					temp.Position.X -= temp.Position.X % 40;
					temp.Position.Y = playerCharacter.playerBody.Position.Y + j;
					temp.Position.Y -= temp.Position.Y % 40;

					//set colour based on ability to fall to calculated position
					if (physEng.CanPlayerFallFromTo(playerCharacter, playerCharacter.playerBody.Position, temp.Position)) temp.Shape.mColor = Color.Black;
					else temp.Shape.mColor = Color.White;

					//draw a dot at that position
					mRenderer.Draw(temp, displayDevice);
				}
			}
		}

		//draw the points of the player's jump arc at each edge of every rigidbody
		//Used to debug the method that determines if a jump collides with a rigidbody
		//this method is effectively just copy/pasted from Player.JumpCollidesWithRB() with modifications to display the results
		private void DrawJumpCullingPoints(Graphics displayDevice)
		{
			Physics.RigidBody temp = new Physics.RigidBody();
			temp.Shape = new Physics.Circle(3);
			temp.Shape.mColor = Color.OrangeRed;

			//store the list of positions at each edge of a box rigidbody
			Tuple<Tuple<Physics.Vector2D, Physics.Vector2D, Physics.Vector2D, Physics.Vector2D>, Tuple<Physics.Vector2D, Physics.Vector2D, Physics.Vector2D, Physics.Vector2D>> possibleCollisionPoints;

			//for every static rigidbody
			foreach (Physics.RigidBody RB in physEng.staticPhysicsObjects)
			{
				//obtain the list of coordinates
				possibleCollisionPoints = playerCharacter.GetBoxCollisionPoints(RB, playerCharacter.playerBody.Position, 0.0f, 15.0f, physEng.gravity);

				//draw each valid coordinate as a small red circle
				if (possibleCollisionPoints != null)
				{
					temp.SetPosition(possibleCollisionPoints.Item1.Item1);
					if (temp.Position != null) mRenderer.Draw(temp, displayDevice);
					temp.SetPosition(possibleCollisionPoints.Item1.Item2);
					if (temp.Position != null) mRenderer.Draw(temp, displayDevice);
					temp.SetPosition(possibleCollisionPoints.Item1.Item3);
					if (temp.Position != null) mRenderer.Draw(temp, displayDevice);
					temp.SetPosition(possibleCollisionPoints.Item1.Item4);
					if (temp.Position != null) mRenderer.Draw(temp, displayDevice);

					temp.SetPosition(possibleCollisionPoints.Item2.Item1);
					if (temp.Position != null) mRenderer.Draw(temp, displayDevice);
					temp.SetPosition(possibleCollisionPoints.Item2.Item2);
					if (temp.Position != null) mRenderer.Draw(temp, displayDevice);
					temp.SetPosition(possibleCollisionPoints.Item2.Item3);
					if (temp.Position != null) mRenderer.Draw(temp, displayDevice);
					temp.SetPosition(possibleCollisionPoints.Item2.Item4);
					if (temp.Position != null) mRenderer.Draw(temp, displayDevice);
				}
			}
		}

		//the same as the above method but for falling
		//this method is effectively just copy/pasted from Player.FallCollidesWithRB() with modifications to display the results
		private void DrawFallCullingPoints(Graphics displayDevice)
		{
			Physics.RigidBody temp = new Physics.RigidBody();
			temp.Shape = new Physics.Circle(3);
			temp.Shape.mColor = Color.OrangeRed;

			//store the start and endpoints of the fall
			Physics.Vector2D source = playerCharacter.playerBody.Position;
			Physics.Vector2D destination = jumpDestination;
			if (destination == null) destination = new Physics.Vector2D(0, 0);
			Physics.Vector2D displacement = destination - source;

			Tuple<float, float> timeToFall = Physics.SuvatEquations.TfromSUA(displacement.Y, 0.0f, 98);

			if (timeToFall == null) return;
			if (float.IsNaN(timeToFall.Item1) && float.IsNaN(timeToFall.Item2)) return;

			//calculate the acceleration needed to fall to the destination
			float acceleration = Physics.SuvatEquations.AfromSUT(displacement.X, 0.0f, Math.Max(timeToFall.Item1, timeToFall.Item2));

			//calculate the four points where the path defined by acceleration could potentially collide with the rigidbody
			Tuple<float, float> timeToReach;

			float leftmostX;
			float leftmostY;

			float righttmostX;
			float righttmostY;

			float topY;
			float topX;

			float bottomY;
			float bottomX;

			//draw the points where the fall arc could collide with each rigidbody
			foreach (Physics.RigidBody RB in physEng.staticPhysicsObjects)
			{
				//obtain the X positions of the sides of the rigidbody
				leftmostX = (RB.Position.X + RB.Shape.ComputeAABB().MIN.X) - source.X;
				righttmostX = (RB.Position.X + RB.Shape.ComputeAABB().MAX.X) - source.X;
				//obtain the Y positions of the top and bottom of the rigidbody
				topY = (RB.Position.Y + RB.Shape.ComputeAABB().MIN.Y) - source.Y;
				bottomY = (RB.Position.Y + RB.Shape.ComputeAABB().MAX.Y) - source.Y;



				//calculate the time to reach the left side of the rigidbody
				timeToReach = Physics.SuvatEquations.TfromSUA(leftmostX, 0.0f, acceleration);

				//calculate the first Y position, draw the first point, calculate the second, draw the second point
				leftmostY = Physics.SuvatEquations.SfromUAT(0.0f, 98, timeToReach.Item1);
				if (!float.IsNaN(leftmostY))
				{
					mRenderer.Draw(temp, displayDevice);
					temp.SetPosition(new Physics.Vector2D(leftmostX, leftmostY) + source);
				}
				leftmostY = Physics.SuvatEquations.SfromUAT(0.0f, 98, timeToReach.Item2);
				if (!float.IsNaN(leftmostY))
				{
					mRenderer.Draw(temp, displayDevice);
					temp.SetPosition(new Physics.Vector2D(leftmostX, leftmostY) + source);
				}



				//calculate the time to reach the right side of the rigidbody
				timeToReach = Physics.SuvatEquations.TfromSUA(righttmostX, 0.0f, acceleration);
				
				//calculate the first Y position, draw the first point, calculate the second, draw the second point
				righttmostY = Physics.SuvatEquations.SfromUAT(0.0f, 98, timeToReach.Item1);
				if (!float.IsNaN(righttmostY))
				{
					mRenderer.Draw(temp, displayDevice);
					temp.SetPosition(new Physics.Vector2D(righttmostX, righttmostY) + source);
				}
				righttmostY = Physics.SuvatEquations.SfromUAT(0.0f, 98, timeToReach.Item2);
				if (!float.IsNaN(righttmostY))
				{
					mRenderer.Draw(temp, displayDevice);
					temp.SetPosition(new Physics.Vector2D(righttmostX, righttmostY) + source);
				}



				//calculate the time to reach the top of the rigidbody
				timeToReach = Physics.SuvatEquations.TfromSUA(topY, 0.0f, 98);

				//calculate the first X position, draw the first point, calculate the second, draw the second point
				topX = Physics.SuvatEquations.SfromUAT(0.0f, acceleration, timeToReach.Item1);
				if (!float.IsNaN(topX))
				{
					mRenderer.Draw(temp, displayDevice);
					temp.SetPosition(new Physics.Vector2D(topX, topY) + source);
				}
				topX = Physics.SuvatEquations.SfromUAT(0.0f, acceleration, timeToReach.Item2);
				if (!float.IsNaN(topX))
				{
					mRenderer.Draw(temp, displayDevice);
					temp.SetPosition(new Physics.Vector2D(topX, topY) + source);
				}



				//calculate the time to reach the bottom of the rigidbody
				timeToReach = Physics.SuvatEquations.TfromSUA(bottomY, 0.0f, 98);

				//calculate the first X position, draw the first point, calculate the second, draw the second point
				bottomX = Physics.SuvatEquations.SfromUAT(0.0f, acceleration, timeToReach.Item1);
				if (!float.IsNaN(bottomX))
				{
					mRenderer.Draw(temp, displayDevice);
					temp.SetPosition(new Physics.Vector2D(bottomX, bottomY) + source);
				}
				bottomX = Physics.SuvatEquations.SfromUAT(0.0f, acceleration, timeToReach.Item2);
				if (!float.IsNaN(bottomX))
				{
					mRenderer.Draw(temp, displayDevice);
					temp.SetPosition(new Physics.Vector2D(bottomX, bottomY) + source);
				}				
			}
			
		}


		//the following methods are used to toggle the different debug displays on and off
		public void TogglePlayerJumpDebug(){ showCurrentPlayerJump = !showCurrentPlayerJump; }

		public void ToggleAIPath() { showPath = !showPath; }

		public void ToggleJumpRangeDebug() { showJumpRange = !showJumpRange; }

		public void ToggleFallRangeDebug() { showFallRange = !showFallRange; }

		public void ToggleShowJumpBoxCollisions() { showJumpBoxCollisions = !showJumpBoxCollisions; }

		public void ToggleShowFallBoxCollisions() { showFallBoxCollisions = !showFallBoxCollisions; }

		public void ToggleShowCollisionNormals() { showCollisionNormals = !showCollisionNormals; }

		public void SetJumpDestination(Physics.Vector2D newDest) { jumpDestination = newDest; }

		public void ToggleNodesDebug() { showNodes = !showNodes; }

		public void ToggleNodeConnectionDisplay() { showLocalNodes += 1; }
	}
}

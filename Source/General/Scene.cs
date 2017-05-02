using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace General
{
	public enum GAMESTATE
	{
		RUNNING,
		WON,
		LOST,
		OTHER
	}
	
    class Scene
    {
		//Store the character controller by the player
        public Physics.Player thePlayer;
		
		//Top level AI classes
		public AI.AIManager theAIManager;
		public AI.WorldGraph AIGraph;

		//the physics engine
		public Physics.PhysicsEngine thePhysicsEngine;

		//the user interface, displays useful information to the player
		public UI headsUpDisplay;

		//store the current state of the gmae (has the player won or lost?)
		public GAMESTATE currentState;


        public Scene()
        {
			//initialise the physics engine and the AI graph singletons (this is the first initialisation of both)
			thePhysicsEngine = Physics.PhysicsEngine.GetPhysicsEngine();
			AIGraph = AI.WorldGraph.GetWorldGraph();

			//create the rigidbodies that make up the game world
			CreateObjects();

			//Create the player
			thePlayer = new Physics.Player(20.0f, 15000);
			thePlayer.playerBody.SetPosition(new Physics.Vector2D(0, 620));
            thePlayer.playerBody.Shape.mColor = System.Drawing.Color.Blue;
			thePhysicsEngine.addRigidBody(thePlayer.playerBody);
            thePhysicsEngine.playerCharacter = thePlayer;

			//create the AI manager
			theAIManager = new AI.AIManager(thePlayer, GlobalConstants.WINDOW_WIDTH, GlobalConstants.WINDOW_HEIGHT);
			//add the hardcoded AI spawn points
            CreateAISpawnLocations();

			//use the current level's rigidbodies to generate the nodes and connections of the AI graph
            AIGraph.PopulateGraph(thePhysicsEngine);
						
            headsUpDisplay = new UI();

			currentState = GAMESTATE.RUNNING;
		}		

        private void CreateObjects()
        {
            Physics.RigidBody newBody = new Physics.RigidBody();

			//declare the colours used
			System.Drawing.Color wallColour = System.Drawing.Color.DarkSlateGray;
			System.Drawing.Color furnitureColour = System.Drawing.Color.Brown;
			System.Drawing.Color cloudColour = System.Drawing.Color.LightGray;
			System.Drawing.Color liftColour = System.Drawing.Color.BlueViolet;
            System.Drawing.Color goalColour = System.Drawing.Color.Gold;
			System.Drawing.Color wreckingBallColour = System.Drawing.Color.Black;


			//create the ground floor	
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Box(1700, 80);
				newBody.Shape.mColor = wallColour;
				newBody.SetPosition(new Physics.Vector2D(400, 700));
				thePhysicsEngine.addRigidBody(newBody);
				//front desk
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Box(200, 40);
				newBody.Shape.mColor = furnitureColour;
				newBody.SetPosition(new Physics.Vector2D(1100, 640));
				thePhysicsEngine.addRigidBody(newBody);

			//Create the left wall
				//bottom bit
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Box(40, 650);
				newBody.Shape.mColor = wallColour;
				newBody.SetPosition(new Physics.Vector2D(0, 275));
				thePhysicsEngine.addRigidBody(newBody);

				//top bit
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Box(40, 300);
				newBody.Shape.mColor = wallColour;
				newBody.SetPosition(new Physics.Vector2D(0, -300));
				thePhysicsEngine.addRigidBody(newBody);

			//create the right wall
				//Top bit
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Box(40, 850);
				newBody.Shape.mColor = wallColour;
				newBody.SetPosition(new Physics.Vector2D(1200, -25));
				thePhysicsEngine.addRigidBody(newBody);

				//bottom bit
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Box(40, 200);
				newBody.Shape.mColor = wallColour;
				newBody.SetPosition(new Physics.Vector2D(1200, 650));
				thePhysicsEngine.addRigidBody(newBody);

				//breakaway bit
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Box(40, 149);
				newBody.Shape.mColor = wallColour;
				newBody.Mass = 4000.0f;
				newBody.SetPosition(new Physics.Vector2D(1200, 475));
				newBody.SetDynamic();
				thePhysicsEngine.addRigidBody(newBody);


			//create the first floor
				//floor
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Box(1000, 20);
				newBody.Shape.mColor = wallColour;
				newBody.SetPosition(new Physics.Vector2D(500, 550));
				thePhysicsEngine.addRigidBody(newBody);

				//stairs
				float width = 400, height = 20;
				float Xpos = 200, Ypos = 530;

				for(int i = 0; i < 9; ++i)
				{
					newBody = new Physics.RigidBody();
					newBody.Shape = new Physics.Box(width, height);
					newBody.Shape.mColor = wallColour;
					newBody.SetPosition(new Physics.Vector2D(Xpos, Ypos));
					thePhysicsEngine.addRigidBody(newBody);

					width -= 40;
					Ypos -= 20;
					Xpos -= 20;
				}

			//create the second floor
				//left bit
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Box(250, 20);
				newBody.Shape.mColor = wallColour;
				newBody.SetPosition(new Physics.Vector2D(425, 300));
				thePhysicsEngine.addRigidBody(newBody);
				//right bit
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Box(500, 20);
				newBody.Shape.mColor = wallColour;
				newBody.SetPosition(new Physics.Vector2D(950, 300));
				thePhysicsEngine.addRigidBody(newBody);
				//fallen bit
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Box(145, 20);
				newBody.Shape.mColor = wallColour;
				newBody.SetPosition(new Physics.Vector2D(620, 302));
				newBody.SetDynamic();
				thePhysicsEngine.addRigidBody(newBody);
				//girdir to hold the fallen piece up
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Box(10, 10);
				newBody.Shape.mColor = wallColour;
				newBody.SetPosition(new Physics.Vector2D(550, 313));
				thePhysicsEngine.addRigidBody(newBody);

			//create the fragmented floor
				//piece 1
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Box(130, 20);
				newBody.Shape.mColor = wallColour;
				newBody.SetPosition(new Physics.Vector2D(940, 220));
				thePhysicsEngine.addRigidBody(newBody);
				//piece 2
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Box(190, 20);
				newBody.Shape.mColor = wallColour;
				newBody.SetPosition(new Physics.Vector2D(650, 130));
				thePhysicsEngine.addRigidBody(newBody);

				//piece 3
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Box(100, 20);
				newBody.Shape.mColor = wallColour;
				newBody.SetPosition(new Physics.Vector2D(400, 90));
				thePhysicsEngine.addRigidBody(newBody);

				//extra step for balcony
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Box(50, 20);
				newBody.Shape.mColor = wallColour;
				newBody.SetPosition(new Physics.Vector2D(250, 20));
				thePhysicsEngine.addRigidBody(newBody);

			//create jumping "puzzle"
				//piece 4
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Box(80, 20);
				newBody.Shape.mColor = wallColour;
				newBody.SetPosition(new Physics.Vector2D(730, 50));
				thePhysicsEngine.addRigidBody(newBody);

				//piece 5
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Box(70, 10);
				newBody.Shape.mColor = wallColour;
				newBody.SetPosition(new Physics.Vector2D(850, -20));
				thePhysicsEngine.addRigidBody(newBody);

				//piece 6
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Box(30, 10);
				newBody.Shape.mColor = wallColour;
				newBody.SetPosition(new Physics.Vector2D(900, -80));
				thePhysicsEngine.addRigidBody(newBody);

				//piece 7
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Box(80, 10);
				newBody.Shape.mColor = wallColour;
				newBody.SetPosition(new Physics.Vector2D(1000, -50));
				thePhysicsEngine.addRigidBody(newBody);

				//piece 8
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Box(80, 10);
				newBody.Shape.mColor = wallColour;
				newBody.SetPosition(new Physics.Vector2D(1100, -170));
				thePhysicsEngine.addRigidBody(newBody);
			
				//piece 9
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Box(70, 10);
				newBody.Shape.mColor = wallColour;
				newBody.SetPosition(new Physics.Vector2D(800, -150));
				thePhysicsEngine.addRigidBody(newBody);

				//piece 9
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Box(30, 10);
				newBody.Shape.mColor = wallColour;
				newBody.SetPosition(new Physics.Vector2D(700, -30));
				thePhysicsEngine.addRigidBody(newBody);

				//piece 10
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Box(60, 10);
				newBody.Shape.mColor = wallColour;
				newBody.SetPosition(new Physics.Vector2D(600, -80));
				thePhysicsEngine.addRigidBody(newBody);
			
				//piece 11
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Box(40, 10);
				newBody.Shape.mColor = wallColour;
				newBody.SetPosition(new Physics.Vector2D(670, -160));
				thePhysicsEngine.addRigidBody(newBody);
			
				//piece 12
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Box(70, 10);
				newBody.Shape.mColor = wallColour;
				newBody.SetPosition(new Physics.Vector2D(950, -230));
				thePhysicsEngine.addRigidBody(newBody);

				//piece 13
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Box(130, 10);
				newBody.Shape.mColor = wallColour;
				newBody.SetPosition(new Physics.Vector2D(1120, -310));
				thePhysicsEngine.addRigidBody(newBody);


			//create balcony
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Box(400, 20);
				newBody.Shape.mColor = wallColour;
				newBody.SetPosition(new Physics.Vector2D(-50, -50));
				thePhysicsEngine.addRigidBody(newBody);
				//lip
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Box(10, 40);
				newBody.Shape.mColor = wallColour;
				newBody.SetPosition(new Physics.Vector2D(-245, -70));
				thePhysicsEngine.addRigidBody(newBody);

			//create clouds outside balcony
				//cloud 1
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Box(40, 40);
				newBody.Shape.mColor = cloudColour;
				newBody.SetPosition(new Physics.Vector2D(-350, -100));
				thePhysicsEngine.addRigidBody(newBody);


				//cloud 2
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Box(50, 50);
				newBody.Shape.mColor = cloudColour;
				newBody.SetPosition(new Physics.Vector2D(-490, -130));
				thePhysicsEngine.addRigidBody(newBody);

				//cloud 3
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Box(90, 70);
				newBody.Shape.mColor = cloudColour;
				newBody.SetPosition(new Physics.Vector2D(-600, -170));
				thePhysicsEngine.addRigidBody(newBody);

				//cloud 4
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Box(150, 100);
				newBody.Shape.mColor = cloudColour;
				newBody.SetPosition(new Physics.Vector2D(-800, -230));
				thePhysicsEngine.addRigidBody(newBody);

				//cloud 5
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Box(350, 160);
				newBody.Shape.mColor = cloudColour;
				newBody.SetPosition(new Physics.Vector2D(-300, -370));
				thePhysicsEngine.addRigidBody(newBody);

				//cloud 6
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Box(50, 30);
				newBody.Shape.mColor = cloudColour;
				newBody.SetPosition(new Physics.Vector2D(-600, -350));
				thePhysicsEngine.addRigidBody(newBody);

				//cloud 7
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Box(700, 500);
				newBody.Shape.mColor = cloudColour;
				newBody.SetPosition(new Physics.Vector2D(-1100, -700));
				thePhysicsEngine.addRigidBody(newBody);
				

			//create the roof
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Box(900, 40);
				newBody.Shape.mColor = wallColour;
				newBody.SetPosition(new Physics.Vector2D(450, -450));
				thePhysicsEngine.addRigidBody(newBody);

			
			//create the lifts
				Physics.MovingPlatform newPlatform;
				Physics.Shape platShape;

				platShape = new Physics.Box(60, 10);
				platShape.mColor = liftColour;
				newPlatform = new Physics.MovingPlatform(platShape, new Physics.Vector2D(180, -60), new Physics.Vector2D(180, -300), 30f, 1.0f);
				newPlatform.platform.staticFriction = 0.7f;
				newPlatform.platform.dynamicFriction = 0.6f;
				thePhysicsEngine.addMovingPlatform(newPlatform);

			
				platShape = new Physics.Box(60, 10);
				platShape.mColor = liftColour;
				newPlatform = new Physics.MovingPlatform(platShape, new Physics.Vector2D(700, -280), new Physics.Vector2D(240, -280), 50f, 1.0f);
				newPlatform.platform.staticFriction = 0.7f;
				newPlatform.platform.dynamicFriction = 0.6f;
				thePhysicsEngine.addMovingPlatform(newPlatform);

				//the final lift to the goal
				platShape = new Physics.Box(80, 20);
				platShape.mColor = liftColour;
				newPlatform = new Physics.MovingPlatform(platShape, new Physics.Vector2D(960, -800), new Physics.Vector2D(960, -400), 10f, 1.0f);
				newPlatform.platform.staticFriction = 0.7f;
				newPlatform.platform.dynamicFriction = 0.6f;
				thePhysicsEngine.addMovingPlatform(newPlatform);

            //The goal platform
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Box(200, 20);
				newBody.Shape.mColor = goalColour;
				newBody.type = Physics.RBTypes.GOAL;
				newBody.SetPosition(new Physics.Vector2D(800, -800));
				thePhysicsEngine.addRigidBody(newBody);


			//add the wrecking ball
				//the moving arm
				platShape = new Physics.Box(10, 10);
				platShape.mColor = liftColour;
				newPlatform = new Physics.MovingPlatform(platShape, new Physics.Vector2D(1300, 330), new Physics.Vector2D(1550, 330), 50f, 1.0f);
				newPlatform.platform.staticFriction = 1.0f;
				newPlatform.platform.dynamicFriction = 0.9f;
				thePhysicsEngine.addMovingPlatform(newPlatform);

				//the ball
				newBody = new Physics.RigidBody();
				newBody.Shape = new Physics.Circle(40);
				newBody.Shape.mColor = wreckingBallColour;
				newBody.SetPosition(new Physics.Vector2D(1300, 510));
				newBody.Mass = 2000;
				newBody.SetDynamic();
				thePhysicsEngine.addRigidBody(newBody);

				//the chain
				Physics.SpringJoint wreckingBallChain = new Physics.SpringJoint(newPlatform.platform, newBody);
				wreckingBallChain.RestLength = 180;
				wreckingBallChain.Stiffness = 1000000;
				wreckingBallChain.Dampen = 1;
				thePhysicsEngine.springs.Add(wreckingBallChain);
		}


        public void CreateAISpawnLocations()
        {
			//Add the coordinates of AI spawns to the AI manager
            theAIManager.AddSpawnLocation(new Physics.Vector2D(-430, 560));
            theAIManager.AddSpawnLocation(new Physics.Vector2D(60, 585));
            theAIManager.AddSpawnLocation(new Physics.Vector2D(1145, 560));
            theAIManager.AddSpawnLocation(new Physics.Vector2D(1140, 345));
            theAIManager.AddSpawnLocation(new Physics.Vector2D(60, 0));
            theAIManager.AddSpawnLocation(new Physics.Vector2D(1125, 90));
            theAIManager.AddSpawnLocation(new Physics.Vector2D(-70, -310));
            theAIManager.AddSpawnLocation(new Physics.Vector2D(1130, -280));
            theAIManager.AddSpawnLocation(new Physics.Vector2D(60, -390));
            theAIManager.AddSpawnLocation(new Physics.Vector2D(-700, -800));
        }

		//update the physics and AI
        public void Step(float dt)
        {			
            if(currentState == GAMESTATE.RUNNING)
            {
				//update the player's velocity based on the last set of inputs
                thePlayer.step(dt);

				//update the AI's goal position and progress all AI agents
                AIGraph.goalCoords = thePlayer.playerBody.Position;
                theAIManager.Step(dt);

				//update all rigidbodies positions and velocities
                thePhysicsEngine.step(dt);

				//keep the UI's "lives remaining" count updated
                headsUpDisplay.livesLeft = (int)((Physics.Circle)thePlayer.playerBody.Shape).Radius - 5;
				//Update the UI
                headsUpDisplay.Step(dt);

				//check the lose condition (the player has been shrunk below a radius of 6 or fallen off the map)
                if ((((Physics.Circle)thePlayer.playerBody.Shape).Radius < 6) ||
					(thePlayer.playerBody.Position.Y > 700))
                {
					currentState = GAMESTATE.LOST;

					headsUpDisplay.GameLost();
                }

				//check the win condition
                if(thePlayer.won)
                {
					currentState = GAMESTATE.WON;

					headsUpDisplay.GameWon();
                }
            }            
        }

		//create a net moving away from the player along the vector from the player to the mouse
		public void FireNet(Physics.Vector2D worldSpaceMouse)
		{
			if(currentState == GAMESTATE.RUNNING)
			{
				//get the direction of motion
				Physics.Vector2D direction = (worldSpaceMouse - thePlayer.playerBody.Position).Normalised();

				//get the distance from the centre of the player the centre of the net will be spawned
				float spawnDistFromPlayer = 20.0f;

				//create the net
				thePhysicsEngine.addNet(new Physics.DynamicNet((direction * spawnDistFromPlayer) + thePlayer.playerBody.Position, direction * thePlayer.gunForce));
			}
		}
    }
}

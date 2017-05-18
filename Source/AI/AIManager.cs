using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI
{
	public class AIManager
	{
		//recommended values of 8 during debug, and 30 during release, to keep the framerate low enough for the physics to remain stable.
        public const int MAX_AI_COUNT = 30;
        public const float timeBetweenSpawns = 2.0f;

		//store the size of the viewport
		public float windowHeight, windowWidth;


		private Physics.PhysicsEngine physEng;
		private AI.WorldGraph aiGraphManager;

		//this could be used to force AI agents to spawn within a certain range of the player
		public Physics.Player targetPlayer;

		public List<AI.AIEngine> aiControllers { get; private set; }
        public List<Physics.Vector2D> AISpawnLocations { get; private set; }
        public int lastSpawnPoint;

		//make sure AI aren't spawned too quickly as this could overwhelm the player
        private float timeSinceLastSpawn;

		//use a random number generator to decide which AI spawn point to use next
		private Random numberGenerator;

		public AIManager(Physics.Player aTargetPlayer, float width, float height)
		{
			windowHeight = height;
			windowWidth = width;

			numberGenerator = new Random();
			aiControllers = new List<AI.AIEngine>();
			targetPlayer = aTargetPlayer;

			physEng = Physics.PhysicsEngine.GetPhysicsEngine();
			aiGraphManager = AI.WorldGraph.GetWorldGraph();

            AISpawnLocations = new List<Physics.Vector2D>();

            timeSinceLastSpawn = 0.0f;
            lastSpawnPoint = 0;
        }

		public void Step(float dt)
		{
			//update each AI agent that has been spawned, and remove any AI that should be removed
			for(int i = 0; i < aiControllers.Count; ++i)
			{
				//remove the AI if it has shrunk too small, then skip to the next iteration of the loop
				if (((Physics.Circle)(aiControllers[i].AIPlayer.playerBody.Shape)).Radius < 6)
				{
					RemoveAIIndex(i);
					continue;
				}

				//update the ai's state and position
				aiControllers[i].step(dt, i);
				aiControllers[i].AIPlayer.step(dt);

				//remove the ai if it is out of bounds
				if (aiControllers[i].AIPlayer.playerBody.Position.Y > windowHeight) RemoveAIIndex(i);
			}

			//if the time since the last spawn is more than the time gap between spawns, spawn the next AI
            timeSinceLastSpawn += dt;
            if(timeSinceLastSpawn > timeBetweenSpawns)
            {
                int nextSpawnPoint = GetNextSpawnPointIndex();

                if(nextSpawnPoint != -1)
                {
                    AddAIAt(AISpawnLocations[GetNextSpawnPointIndex()]);
                    timeSinceLastSpawn = 0.0f;
                }                
            }

        }

		//get the next spawn point. This method can be modified to control where AI's get spawned
        public int GetNextSpawnPointIndex()
        {
			return numberGenerator.Next(0, AISpawnLocations.Count);
        }


        public void AddSpawnLocation(Physics.Vector2D newLocation)
        {
            AISpawnLocations.Add(newLocation);
        }

		//spawn an AI at a given coordinate
		public void AddAIAt(Physics.Vector2D spawnLocation)
		{
            if(aiControllers.Count < MAX_AI_COUNT)
            {
                Physics.Player newAIPlayer = new Physics.Player();
                newAIPlayer.playerBody.type = Physics.RBTypes.ENEMY;
                newAIPlayer.playerBody.SetPosition(spawnLocation);
                physEng.addRigidBody(newAIPlayer.playerBody);
                aiControllers.Add(new AI.AIEngine(newAIPlayer));

                aiGraphManager.AddAIagent();
            }			
		}

		//delete the last AI to be spawned
		public void RemoveLastAI()
		{
			if(aiControllers.Count > 0) RemoveAIIndex(aiControllers.Count - 1);
		}

		//delete the AI with the provided index
		public void RemoveAIIndex(int indexToRemove)
		{
			if (aiControllers.Count <= indexToRemove) return;
			physEng.dynamicPhysicsObjects.Remove(aiControllers[indexToRemove].AIPlayer.playerBody);
			aiControllers.RemoveAt(indexToRemove);
			aiGraphManager.RemoveAIagent(indexToRemove);
		}
	}
}

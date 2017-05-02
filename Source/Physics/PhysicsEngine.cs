using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Physics
{
	//A singleton storing all the physics objects (static/dynamic rigidbodies, springs, etc)
    public class PhysicsEngine
    {
		private static PhysicsEngine physEngSingleton;

		ImpulseCollisionResponder mCollisionResponder;
		public Vector2D gravity { get; private set; }

		public List<RigidBody> staticPhysicsObjects;
		public List<RigidBody> dynamicPhysicsObjects;
		public List<MovingPlatform> movingPlatforms;
		public List<SpringJoint> springs;

        public Player playerCharacter;
		//only allow one net for now
		public DynamicNet curentNet;


		private PhysicsEngine()
        {
			initialiseDefaults();
        }

		public static PhysicsEngine GetPhysicsEngine()
		{
			if (physEngSingleton == null)
			{
				physEngSingleton = new PhysicsEngine();
			}
			return physEngSingleton;
		}

        private void initialiseDefaults()
        {
            staticPhysicsObjects = new List<RigidBody>();
            dynamicPhysicsObjects = new List<RigidBody>();
			movingPlatforms = new List<MovingPlatform>();
			springs = new List<SpringJoint>();

			gravity = new Vector2D(0.0f, 98.0f);
            mCollisionResponder = new ImpulseCollisionResponder();
		}

		public void addRigidBody(RigidBody newRB)
		{
			//Add the rigidbody to its appropriate list
			if (!newRB.moveable)
			{
				staticPhysicsObjects.Add(newRB);
			}
			else
			{
				dynamicPhysicsObjects.Add(newRB);
			}			
		}

		//delete the previous Net and add the new one
		public void addNet(DynamicNet newNet)
		{
			if (curentNet != null) removeNet();

			foreach(RigidBody rb in newNet.netEdges)
			{
				dynamicPhysicsObjects.Add(rb);
			}

			foreach(SpringJoint sj in newNet.netRopes)
			{
				springs.Add(sj);
			}

			curentNet = newNet;
		}

		//delete the current net from the environment
		public void removeNet()
		{
			foreach (RigidBody rb in curentNet.netEdges)
			{
				dynamicPhysicsObjects.Remove(rb);
			}

			foreach (SpringJoint sj in curentNet.netRopes)
			{
				springs.Remove(sj);
			}
			curentNet = null;
		}

		public void addMovingPlatform(MovingPlatform newPlat)
		{
			movingPlatforms.Add(newPlat);
			dynamicPhysicsObjects.Add(newPlat.platform);
		}

		public void step(float dt)
		{			
			updatePositions(dt);
			updateCollisions();
		}

		//check if a player can jump from A to B, culling any jumps which collide with a rigidbody
		public bool CanPlayerJumpFromTo(Player playerToCheck, Vector2D source, Vector2D destination)
		{
			if (playerToCheck.CanJumpToFrom(source, destination, gravity))
			{				
				return CullJumpPoints(playerToCheck, source, destination);
			}
			return false;
		}

		//check if a player can fall from A to B, culling any falls which collide with a rigidbody
		public bool CanPlayerFallFromTo(Player playerToCheck, Vector2D source, Vector2D destination)
		{
			if (playerToCheck.CanFallToFrom(source, destination, gravity))
			{
				return CullFallPoints(playerToCheck, source, destination);
			}
			return false;
		}

		public bool CullJumpPoints(Player playerToCheck, Vector2D source, Vector2D destination)
		{
			foreach (RigidBody RB in staticPhysicsObjects)
			{
				//if the jump arc collides with the current rigid body then return false
				if (playerToCheck.JumpCollidesWithRB(RB, source, destination, gravity)) return false;
			}

			return true;
		}

		public bool CullFallPoints(Player playerToCheck, Vector2D source, Vector2D destination)
		{
			foreach (RigidBody RB in staticPhysicsObjects)
			{
				//if the jump arc collides with the current rigid body then return false
				if (playerToCheck.FallCollidesWithRB(RB, source, destination, gravity)) return false;
			}

			return true;
		}


		//check all dynamic objects for collisions against dynamic and static objects, then resolve them
		public void updateCollisions()
        {
            //Check collisions of all dynamic objects
            for (int i = dynamicPhysicsObjects.Count - 1; i >= 0; i--)
            {
                Physics.RigidBody a = dynamicPhysicsObjects[i];

				//against other dynamic objects
                for (int j = dynamicPhysicsObjects.Count - 1; j >= 0; j--)
                {
                    if (i != j)
                    {
                        Physics.RigidBody b = dynamicPhysicsObjects[j];


                        if (a.Shape.IsCollided(b.Shape, a.Position, b.Position))
                        {
                            Physics.Vector2D normal = a.Shape.GetCollisionNormal(b.Shape, a.Position, b.Position);
                            Physics.CollisionPairData pair = new Physics.CollisionPairData();
                            pair.BodyA = a;
                            pair.BodyB = b;
                            pair.ContactNormal = normal;

                            mCollisionResponder.AddCollisionPair(pair);
                        }
                    }
                }

				//and against all static objects
				for (int j = staticPhysicsObjects.Count - 1; j >= 0; j--)
				{
					Physics.RigidBody b = staticPhysicsObjects[j];

					if (a.Shape.IsCollided(b.Shape, a.Position, b.Position))
					{
						Physics.Vector2D normal = a.Shape.GetCollisionNormal(b.Shape, a.Position, b.Position);
						Physics.CollisionPairData pair = new Physics.CollisionPairData();
						pair.BodyA = a;
						pair.BodyB = b;
						pair.ContactNormal = normal;

						mCollisionResponder.AddCollisionPair(pair);
					}
				}

				mCollisionResponder.ResolveAllPairs(playerCharacter);
            }			
        }

		//update all moving platforms, springs, and dynamic objects
		public void updatePositions(float dt)
		{
			foreach (MovingPlatform mp in movingPlatforms)
			{
				mp.Step(dt);
			}

			//Step all Physics.SpringJoint props and update their position
			foreach (var spring in springs)
			{
				spring.ApplyForce();
			}

			//Step all Physics.RigidBody props and update their position
			foreach (var prop in dynamicPhysicsObjects)
			{
				prop.Step(dt, gravity);
			}
		}
    }
}

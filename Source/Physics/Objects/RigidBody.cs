using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Physics
{
    public enum RBTypes
    {
        ENVIRONMENT,
        PLAYER,
        ENEMY,
        GOAL,
		PLAYER_NET,
        OTHER
    }

	//Credit goes to the Game Behaviour Module Team for writing the original RigidBody class
	//This version has been extended to add functionality
	public class RigidBody
	{
		public Vector2D Position { get; private set; }                           //Position of the centre of mass
		public Vector2D LinearVelocity { get; set; }							//linear velocity, for translational motion
		public Vector2D Force { get; private set; }								//current force applied
		public float Mass { get; set; }

		//Allow rigidbodies to have different levels of static
		public bool moveable { get; set; }                    
		public bool reactToForce { get; set; }
		public bool obeysGravity { get; set; }
				
        public RBTypes type;


		public Shape Shape { get; set; }


		//rigidbody specific surface properties
		public float elasticity { get; set; }
		public float staticFriction { get; set; }
		public float dynamicFriction { get; set; }

		//impose a maximum speed to try and counter object tunnelling
		public int maxSpeed { get; set; }


		//store information about the current collision as a way to determine if the body is colliding
        public Vector2D collisionNormal { get; set; }       
        public RigidBody otherBody { get; set; }



		//This variable is a hack to allow the player's net to affect AI agents in the same way AI agent collisions affect the player
		public float multiUseCooldown;
 
		public RigidBody()
        {
			//initialise default variables
            Mass = 200.0f;
            Position = new Vector2D();
            LinearVelocity = new Vector2D();
            Force = new Vector2D();

            moveable = false;
			reactToForce = false;
			obeysGravity = false;

			collisionNormal = null;
			elasticity = 0.0f;

			staticFriction = 0.8f;
			dynamicFriction = 0.6f;

			maxSpeed = 500;
            otherBody = null;
            type = RBTypes.ENVIRONMENT;
			multiUseCooldown = 0.0f;

		}

		//update the rigidbodies position based on currently applied force, velocity, and gravity
        public void Step(float dt, Vector2D gravity)
        {
			//tick the cooldown
			if (multiUseCooldown > 0.0f) multiUseCooldown -= dt;

			//only update position if the object can move
            if (moveable)
            {
				Vector2D acceleration = Force / Mass;
				if (obeysGravity) acceleration += gravity;
				
				//update velocity, locking it below maximum speed
                LinearVelocity = LinearVelocity + acceleration * dt;
                if (LinearVelocity.Length() > maxSpeed) LinearVelocity = LinearVelocity.Normalised() * maxSpeed;


                //Update position based on velocity (uses Runge Kutta integration)
                Vector2D k1 = LinearVelocity + acceleration * dt;
                Vector2D k2 = LinearVelocity + k1 * (dt / 2);
                Vector2D k3 = LinearVelocity + k2 * (dt / 2);
                Vector2D k4 = LinearVelocity + k3 * dt;
				SetPosition(Position + (k1 + k2 * 2 + k3 * 2 + k4) * (dt / 6));

				//reset force
				Force.X = 0;
                Force.Y = 0;

				//reset collision information (assumes collision detection will take place immediately after stepping all rigidbodies)
				otherBody = null;
				collisionNormal = null;
            }              
        }

		//A regidibodies' position can only be set through this method
		//This is an attempt to force the rigidbodies' position to remain at the same reference
		//This allows the AI graph to reference a rigidbodies position directly, so the graph's node positions update automatically as their associated rigidbody moves.
		public void SetPosition(Vector2D newPosition)
		{
			if (newPosition == null)
			{
				Position = null;
				return;
			}

			if(Position == null)
			{
				Position = newPosition;
				return;
			}

			Position.X = newPosition.X;
			Position.Y = newPosition.Y;
		}


		public void SetForce(Vector2D newForce)
		{
			if (reactToForce)
			{
				Force.X = newForce.X;
				Force.Y = newForce.Y;
			}
		}

		//an easy way to make the rigidbody 100% dynamic
		public void SetDynamic()
		{
			moveable = true;
			obeysGravity = true;
			reactToForce = true;
		}
    }	
}

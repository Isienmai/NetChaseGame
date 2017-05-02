using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Physics
{
	//This class defines a platform that moves back and forth between two positions
	public class MovingPlatform
	{
		public RigidBody platform { get; private set; }
		public Vector2D point1 { get; private set; }
		public Vector2D point2 { get; private set; }

		//current direction of motion (should always be normalised)
		public Vector2D direction;

		private float currentSpeed;
		private float destSpeed;

		private float timeToTravel;
		private float timeSoFar;
		private float acceleration;

		//dampen time stores the time it takes to accelerate from 0 to the destSpeed
		private float dampenTime;

		public MovingPlatform(Shape aPlatform, Vector2D start, Vector2D end, float aSpeed, float dampen)
		{
			point1 = start;
			point2 = end;

			destSpeed = aSpeed;

			Vector2D displacement = point2 - point1;
			direction = displacement.Normalised();
			timeToTravel = displacement.Length() / destSpeed;
			timeSoFar = 0.0f;

			currentSpeed = 0;

			//if the dampen time is longer than half the total travel time then cut it down to half the travel time
			if (dampen * 2.0f > timeToTravel) dampen = timeToTravel / 2.0f;
			dampenTime = dampen;
			

			acceleration = SuvatEquations.AfromUTV(0, dampenTime, destSpeed);


			platform = new RigidBody();
			platform.Shape = aPlatform;
			platform.LinearVelocity = direction * currentSpeed;
			platform.moveable = true;
			platform.obeysGravity = false;
			platform.reactToForce = false;
			platform.SetPosition(point1);
		}


		public void Step(float dt)
		{
			timeSoFar += dt;

			//if the full path has been traversed, reset time so far and reverse direction
			if (timeSoFar > timeToTravel)
			{
				currentSpeed = 0.0f;
				direction *= -1;
				timeSoFar = 0.0f;
			}
			else if (timeSoFar < dampenTime) //accelerate
			{
				currentSpeed += acceleration * dt;
			}
			else if (timeToTravel - timeSoFar < dampenTime) //decelerate
			{
				currentSpeed -= acceleration * dt;
			}

			//update velocity
			platform.LinearVelocity = direction * currentSpeed;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Physics
{

    enum CONTROLS
    {
        LEFT = 0,
        RIGHT,
        UP,
        DOWN,
        GRAB,
        NUM_OF_CONTROLS
    };

	public class Player
	{
		//the default player radius (TODO: make this NOT be public and static)
		public static float playerRadius { get; } = 9.0f;
		
		public Physics.RigidBody playerBody { get; private set; }
				
		//cooldowns
		public float gunReload { get; private set; }
		public float collisionCooldown { get; private set; }

		//an array of bools is used to determine if a given input (up, down, left, right) is currently active
		bool[] controls = new bool[(int)CONTROLS.NUM_OF_CONTROLS];

		//these variables store various forces
		public float speed { get; private set; }
		public float gunForce { get; private set; }
		public float gravityAssistForce { get; private set; }
		public float jumpInitialVelocity { get; private set; }
		public float motionForce { get; private set; }
		
		//store if this player instance has won 
		public bool won { get; private set; }


		//initialise a default player
		public Player()
        {
            InitialiseDefaults(playerRadius, 25000);
        }

		//initialise a player with a specified radius
        public Player(float chosenRadius)
        {
            InitialiseDefaults(chosenRadius, 25000);
        }

		//initialise a player with a specified speed
        public Player(int chosenSpeed)
        {
            InitialiseDefaults(playerRadius, chosenSpeed);
        }

		//initialise a player with a speified speed and radius
        public Player(float chosenRadius, int chosenSpeed)
        {
            InitialiseDefaults(chosenRadius, chosenSpeed);
        }


        private void InitialiseDefaults(float chosenRadius, int chosenSpeed)
        {
			//create the player's rigidbody
            playerBody = new Physics.RigidBody();
            playerBody.type = RBTypes.PLAYER;
			playerBody.Shape = new Physics.Circle(chosenRadius);
            playerBody.Shape.mColor = System.Drawing.Color.Firebrick;
			playerBody.SetDynamic();
			playerBody.Mass = 120;

			//set speed to the input speed, and update all variables calculated using speed
            SetSpeed(chosenSpeed);
			
            for(int i = 0; i < (int)CONTROLS.NUM_OF_CONTROLS; ++i)
            {
                controls[i] = false;
            }

			gunForce = 300;
			gunReload = 0.0f;
            collisionCooldown = 0.0f;

            won = false;
        }

		//this function exists to keep updated all variables calculated using speed
        public void SetSpeed(int newValue)
        {
            speed = newValue;

            gravityAssistForce = speed;
            jumpInitialVelocity = -speed / 145;
            motionForce = speed;
        }

		//Use the current set of inputs to apply various forces to the player
        public void step(float dt)
        {
			Physics.Vector2D appliedForce = new Vector2D();

			//apply forces to the left and right
			if (controls[(int)CONTROLS.RIGHT]) appliedForce.X += motionForce;
            if (controls[(int)CONTROLS.LEFT]) appliedForce.X -= motionForce;

			//apply a jump if the player is on the ground
            if(OnGround() && controls[(int)CONTROLS.UP])
			{
				//create the jump vector
				Physics.Vector2D temp = playerBody.collisionNormal;
				temp.X *= -jumpInitialVelocity;
				temp.Y = jumpInitialVelocity;

				//apply the jump to the velocity
				playerBody.LinearVelocity += temp;
				controls[(int)CONTROLS.UP] = false;
			}
			else if(controls[(int)CONTROLS.DOWN])
			{
				//speed up the player's fall
				appliedForce.Y += gravityAssistForce;
			}

			//update the player's force to include these input forces
            playerBody.SetForce(playerBody.Force + appliedForce);
			
			//update all cooldowns
            if (gunReload > 0.01f) gunReload -= dt;
            if (collisionCooldown > 0.0f) collisionCooldown -= dt;
        }

		//determine if a given coordinate is within the player's jump range
		public bool CanJumpTo(Vector2D destination, Vector2D gravity)
		{
			return CanJumpToFrom(playerBody.Position, destination, gravity);
		}

		//determine if the current player could jump from a given coordinate to a given coordinate
		public bool CanJumpToFrom(Vector2D source, Vector2D destination, Vector2D gravity)
		{
			Vector2D relativeDestination = destination - source;
												
			//calculate the time to reach the apex of the jump, and the height of the apex
			float timeToApex = SuvatEquations.TfromUAV(jumpInitialVelocity, gravity.Y, 0.0f);
			float maxYRange = SuvatEquations.SfromUAT(jumpInitialVelocity, gravity.Y, timeToApex);
			
			//positions above the apex cannot be jumped to
			if (relativeDestination.Y < maxYRange) return false;


			//calculate the time it takes to reach the correct X coord travelling horizontally
			float horizontalAcceleration = GetHorizontalAcceleration() * relativeDestination.X/Math.Abs(relativeDestination.X);
			Tuple<float,float> timeToReach = SuvatEquations.TfromSUA( relativeDestination.X, playerBody.LinearVelocity.X, horizontalAcceleration);
            if(float.IsNaN(timeToReach.Item1)) return false;
			float largestTime = timeToReach.Item1;
			if (!float.IsNaN(timeToReach.Item2) && timeToReach.Item2 > timeToReach.Item1) largestTime = timeToReach.Item2;
			

			//If the point's X is reached before the furthest apex' X then it must be within range (Note: this is only true as long as the player's horizontal velocity isn't too large)
			if (largestTime <= timeToApex) return true;

			//Find the Y position of the jump at the destination X
			float timeFromApex = largestTime - timeToApex;
			float YAtDestX = maxYRange + SuvatEquations.SfromUAT(0, gravity.Y, timeFromApex);

			//If the destination Y is above the jump arc then it is not reachable
			if (YAtDestX > relativeDestination.Y) return false;

			return true;
		}

		//determine if the player can fall from a given coordinate to a given coordinate
		public bool CanFallToFrom(Vector2D source, Vector2D destination, Vector2D gravity)
		{
			Vector2D relativeDestination = destination - source;

			//cannot fall to a destination that is above the source
			if (relativeDestination.Y < 0) return false;


			//Calculate the time it takes to travel the horizontal difference
			float horizontalAcceleration = GetHorizontalAcceleration() * relativeDestination.X / Math.Abs(relativeDestination.X);
			Tuple<float, float> timeToReach = SuvatEquations.TfromSUA(relativeDestination.X, playerBody.LinearVelocity.X, horizontalAcceleration);
			if (float.IsNaN(timeToReach.Item1)) return false;
			float largestTime = timeToReach.Item1;
			if (!float.IsNaN(timeToReach.Item2) && timeToReach.Item2 > timeToReach.Item1) largestTime = timeToReach.Item2;

			
			//If the Y position after that time is below the destination's Y position then the destination cannot be fallen to
			float YAtDestX = SuvatEquations.SfromUAT(0, gravity.Y, largestTime);
			if (YAtDestX > relativeDestination.Y) return false;

			return true;
		}
		
		public float GetTotalJumpDuration(Vector2D source, Vector2D destination, Vector2D gravity)
		{
			//jump time is equal to the time it takes to traverse the required vertical displacement
			Vector2D displacement = destination - source;			
			return SuvatEquations.TfromSUA(displacement.Y, jumpInitialVelocity, gravity.Y).Item1;
		}

		public float GetHorizontalAcceleration()
		{
			return speed / playerBody.Mass;
		}



		//// Each jump is an initial jump impulse, followed by a period of acceleration, then a period without acceleration
		//// This method calculates the length of time the player must accelerate for in order for a jump from "source" to reach "destination"
		//// The returned value is positive for acceleration to the right, and negative for acceleration to the left
		public float GetJumpFromSourceToDest(Vector2D source, Vector2D destination, Vector2D gravity)
		{
			Vector2D displacement = destination - source;
			float horizontalVelocity = playerBody.LinearVelocity.X;
			float horizontalAcceleration = GetHorizontalAcceleration();

			float direction = 1;

			//if the destination is to the left of the source, reverse the velocity, displacement, and direction
			//This is intended to reduce the equation to a question of accelerate vs decelerate
			if(displacement.X < 0)
			{
				displacement.X *= -1;
				horizontalVelocity *= -1;
				direction *= -1;
			}

			float timeToReachDest = GetTotalJumpDuration(source, destination, gravity);

			//If the player is currently travelling in the correct direction
			if(horizontalVelocity > 0)
			{
				//if no acceleration is applied, will the player overshoot the mark?
				float naturalDistance = SuvatEquations.SfromUAT(horizontalVelocity, 0.0f, timeToReachDest);				
				if(naturalDistance > displacement.X)
				{
					//if yes, the player will need to decelerate
					direction *= -1;
					horizontalAcceleration *= -1;

					//If decelerating for the full duration of the jump still results in overshooting, then the jump cannot be made
					float minimumDistance = SuvatEquations.SfromUAT(horizontalVelocity, horizontalAcceleration, timeToReachDest);
					if (minimumDistance > displacement.X) return float.NaN;					

					//calculate the amount that will be overshot
					float simulatedDisplacement = naturalDistance - displacement.X;


					//find the distance undershot from maximum deceleration
					float undershoot = SuvatEquations.SfromUAT(0.0f, horizontalAcceleration * -1, timeToReachDest) - simulatedDisplacement;

					//find the time it takes to travel that distance
					float timeSpentUndershooting = SuvatEquations.TfromSUA(undershoot, 0.0f, horizontalAcceleration * -1).Item1;

					//the time spent decelerating is equal to the total jump time minus the undershoot time
					return (timeToReachDest - timeSpentUndershooting) * direction;
				}
			}

			//get the distance overshot at maximum acceleration
			float overshoot = SuvatEquations.SfromUAT(horizontalVelocity, horizontalAcceleration, timeToReachDest) - displacement.X;

			//calculate the time it takes to travel that distance
			float timeSpentOvershooting = SuvatEquations.TfromSUA(overshoot, 0.0f, horizontalAcceleration).Item1;

			//the time spent accelerating is equal to the total jump time minus the overshoot time
			return (timeToReachDest - timeSpentOvershooting) * direction;

		}

		//Checks to see if a jump from source to destination would collid with the provided rigidbody
		public bool JumpCollidesWithRB(RigidBody RB, Vector2D source, Vector2D destination, Vector2D gravity)
		{
			//Get the max Y displacement
			float maxYRange = SuvatEquations.SFromUVA(jumpInitialVelocity, 0.0f, gravity.Y);

			//Return false if the rigid body is outside the widest possible jump range
			if (maxYRange + source.Y - playerRadius > RB.Shape.ComputeAABB().MAX.Y + RB.Position.Y) return false;
			if (Math.Max(source.Y, destination.Y) + playerRadius < RB.Position.Y + RB.Shape.ComputeAABB().MIN.Y) return false;
			if (RB.Shape.ComputeAABB().MAX.X + RB.Position.X < Math.Min(source.X, destination.X) - playerRadius) return false;
			if (Math.Max(source.X, destination.X) + playerRadius < RB.Position.X + RB.Shape.ComputeAABB().MIN.X) return false;


			Vector2D displacement = destination - source;

			//calculate the jump's acceleration duration, and total duration
			float accelerationTime = GetJumpFromSourceToDest(source, destination, gravity);
			float totalJumpTime = GetTotalJumpDuration(source, destination, gravity);

			//if the jump is not valid, return that it does not collide with the rigidbody
			if (float.IsNaN(totalJumpTime) || totalJumpTime < 0) return false;


			//calculate the 8 points that could potentially intersect with the box (1 before and 1 after the jump's apex)
			Tuple<Tuple<Vector2D, Vector2D, Vector2D, Vector2D>, Tuple<Vector2D, Vector2D, Vector2D, Vector2D>> boxCollisionPoints = GetBoxCollisionPoints(RB, source, accelerationTime, totalJumpTime, gravity);
			if (boxCollisionPoints == null) return true;

			//check to see if the player would collide with the rigidbody at any of the calculated positions
			Shape temp = new Circle(playerRadius);
			if (RB.Shape.IsCollided(temp, RB.Position, boxCollisionPoints.Item1.Item1)) return true;
			if (RB.Shape.IsCollided(temp, RB.Position, boxCollisionPoints.Item1.Item2)) return true;
			if (RB.Shape.IsCollided(temp, RB.Position, boxCollisionPoints.Item1.Item3)) return true;
			if (RB.Shape.IsCollided(temp, RB.Position, boxCollisionPoints.Item1.Item4)) return true;

			if (RB.Shape.IsCollided(temp, RB.Position, boxCollisionPoints.Item2.Item1)) return true;
			if (RB.Shape.IsCollided(temp, RB.Position, boxCollisionPoints.Item2.Item2)) return true;
			if (RB.Shape.IsCollided(temp, RB.Position, boxCollisionPoints.Item2.Item3)) return true;
			if (RB.Shape.IsCollided(temp, RB.Position, boxCollisionPoints.Item2.Item4)) return true;
			

			return false;
		}

		//Checks to see if a fall from an input coordinate TO an input coordinate would collide with a given rigidbody
		public bool FallCollidesWithRB(RigidBody RB, Vector2D source, Vector2D destination, Vector2D gravity)
		{
			//Return false if the rigid body is outside the widest possible fall range
			if (source.Y - playerRadius > RB.Shape.ComputeAABB().MAX.Y + RB.Position.Y) return false;
			if (Math.Max(source.Y, destination.Y) + playerRadius < RB.Position.Y + RB.Shape.ComputeAABB().MIN.Y) return false;
			if (RB.Shape.ComputeAABB().MAX.X + RB.Position.X < Math.Min(source.X, destination.X) - playerRadius) return false;
			if (Math.Max(source.X, destination.X) + playerRadius < RB.Position.X + RB.Shape.ComputeAABB().MIN.X) return false;


			Vector2D displacement = destination - source;

			//calculate the fall time (return no collision if the fall time was invalid)
			Tuple<float,float> timeToFall = SuvatEquations.TfromSUA(displacement.Y, 0.0f, gravity.Y);			
			if (float.IsNaN(timeToFall.Item1) || timeToFall.Item1 < 0) return false;

			//calculate the average horizontal acceleration needed to fall to the destination
			float acceleration = SuvatEquations.AfromSUT(displacement.X, 0.0f, Math.Max(timeToFall.Item1, timeToFall.Item2));

			//calculate the four points where the path defined by acceleration could potentially collide with the rigidbody
			Tuple<float, float> timeToReach;

			//obtain the X positions of the sides of the rigidbody
			float leftmostX = (RB.Position.X + RB.Shape.ComputeAABB().MIN.X) - source.X;
			float righttmostX = (RB.Position.X + RB.Shape.ComputeAABB().MAX.X) - source.X;
			//obtain the Y positions of the top and bottom of the rigidbody
			float topY = (RB.Position.Y + RB.Shape.ComputeAABB().MIN.Y) - source.Y;
			float bottomY = (RB.Position.Y + RB.Shape.ComputeAABB().MAX.Y) - source.Y;



			//these coords will be estimated from the above coords
			float leftmostY;
			float righttmostY;
			float topX;
			float bottomX;
						


			Shape temp = new Circle(playerRadius);

			//calculate the time to reach the left side of the rigidbody
			timeToReach = Physics.SuvatEquations.TfromSUA(leftmostX, 0.0f, acceleration);

			//calculate the first Y position, check it for collision, calculate the second, check for collision
			leftmostY = Physics.SuvatEquations.SfromUAT(0.0f, 98, timeToReach.Item1);
			if (!float.IsNaN(leftmostY) && RB.Shape.IsCollided(temp, RB.Position, new Vector2D(leftmostX, leftmostY) + source)) return true;
			leftmostY = Physics.SuvatEquations.SfromUAT(0.0f, 98, timeToReach.Item2);
			if (!float.IsNaN(leftmostY) && RB.Shape.IsCollided(temp, RB.Position, new Vector2D(leftmostX, leftmostY) + source)) return true;



			//calculate the time to reach the right side of the rigidbody
			timeToReach = Physics.SuvatEquations.TfromSUA(righttmostX, 0.0f, acceleration);

			//calculate the first Y position, check it for collision, calculate the second, check for collision
			righttmostY = Physics.SuvatEquations.SfromUAT(0.0f, 98, timeToReach.Item1);
			if (!float.IsNaN(righttmostY) && RB.Shape.IsCollided(temp, RB.Position, new Vector2D(righttmostX, righttmostY) + source)) return true;
			righttmostY = Physics.SuvatEquations.SfromUAT(0.0f, 98, timeToReach.Item2);
			if (!float.IsNaN(righttmostY) && RB.Shape.IsCollided(temp, RB.Position, new Vector2D(righttmostX, righttmostY) + source)) return true;



			//calculate the time to reach the top of the rigidbody
			timeToReach = Physics.SuvatEquations.TfromSUA(topY, 0.0f, 98);

			//calculate the first X position, check it for collision, calculate the second, check for collision
			topX = Physics.SuvatEquations.SfromUAT(0.0f, acceleration, timeToReach.Item1);
			if (!float.IsNaN(topX) && RB.Shape.IsCollided(temp, RB.Position, new Vector2D(topX, topY) + source)) return true;
			topX = Physics.SuvatEquations.SfromUAT(0.0f, acceleration, timeToReach.Item2);
			if (!float.IsNaN(topX) && RB.Shape.IsCollided(temp, RB.Position, new Vector2D(topX, topY) + source)) return true;



			//calculate the time to reach the bottom of the rigidbody
			timeToReach = Physics.SuvatEquations.TfromSUA(bottomY, 0.0f, 98);

			//calculate the first X position, check it for collision, calculate the second, check for collision
			bottomX = Physics.SuvatEquations.SfromUAT(0.0f, acceleration, timeToReach.Item1);
			if (!float.IsNaN(bottomX) && RB.Shape.IsCollided(temp, RB.Position, new Vector2D(bottomX, bottomY) + source)) return true;
			bottomX = Physics.SuvatEquations.SfromUAT(0.0f, acceleration, timeToReach.Item2);
			if (!float.IsNaN(bottomX) && RB.Shape.IsCollided(temp, RB.Position, new Vector2D(bottomX, bottomY) + source)) return true;
			

			return false;
		}

		//Get the coordinates of the specified jump arc where it lines up with one of the edges of a given rigidbody
		public Tuple<Tuple<Vector2D,Vector2D,Vector2D,Vector2D>, Tuple<Vector2D, Vector2D, Vector2D, Vector2D>> GetBoxCollisionPoints(RigidBody RB, Vector2D source, float accelerationTime, float totalJumpTime, Vector2D gravity)
		{
			if (float.IsNaN(accelerationTime)) return null;
			
			//force acceleration time to be positive (negative times are not valid)
			int sign = (int)(accelerationTime / (float)Math.Abs(accelerationTime));
			accelerationTime *= sign;
			

			//calculate the possible collision locations during acceleration
			Vector2D acceleration = new Vector2D(GetHorizontalAcceleration() * sign, gravity.Y);
			Vector2D velocity = new Vector2D(playerBody.LinearVelocity.X, jumpInitialVelocity);
			Vector2D initialPosition = new Vector2D(source);
			
			Tuple<Vector2D, Vector2D, Vector2D, Vector2D> resultsDuringAcc = UsefulMathsFunctions.GetArcPosAtBoxEdges(RB, initialPosition, velocity, acceleration, accelerationTime);


			//calculate the possible collision locations after acceleration
			float YatAccEnd = SuvatEquations.SfromUAT(velocity.Y, acceleration.Y, accelerationTime);
			float XatAccEnd = SuvatEquations.SfromUAT(velocity.X, acceleration.X, accelerationTime);

			initialPosition.Y += YatAccEnd;
			initialPosition.X += XatAccEnd;

			velocity.X = SuvatEquations.VfromUAT(velocity.X, acceleration.X, accelerationTime);
			velocity.Y = SuvatEquations.VfromUAT(velocity.Y, acceleration.Y, accelerationTime);

			acceleration.X = 0;

			Tuple<Vector2D, Vector2D, Vector2D, Vector2D> resultsAftergAcc = UsefulMathsFunctions.GetArcPosAtBoxEdges(RB, initialPosition, velocity, acceleration, totalJumpTime - accelerationTime);

			//return the two sets of coordinates
			return new Tuple<Tuple<Vector2D, Vector2D, Vector2D, Vector2D>, Tuple<Vector2D, Vector2D, Vector2D, Vector2D>>(resultsDuringAcc, resultsAftergAcc);
		}

		//Get the X coordinates of an input jump at the specified Y coordinate
		public Tuple<Vector2D,Vector2D> GetJumpXFromY(Vector2D source, float accelerationTime, float totalJumpTime, Vector2D gravity, float destY)
		{
			if (float.IsNaN(accelerationTime)) return new Tuple<Vector2D, Vector2D>(null, null);

			//setup the required variables
			int sign = (int)(accelerationTime / (float)Math.Abs(accelerationTime));
			accelerationTime *= sign;

			Vector2D acceleration = new Vector2D(GetHorizontalAcceleration() * sign, gravity.Y);
			Vector2D velocity = new Vector2D(playerBody.LinearVelocity.X, jumpInitialVelocity);
			Vector2D initialPosition = new Vector2D(source);

			//get the XY coordinates from the curve during acceleration
			Tuple<Vector2D, Vector2D> possiblePositions1 = UsefulMathsFunctions.GetWorldspacePointAlongCurve(source, velocity, acceleration, accelerationTime, float.NaN, destY);
			

			//update the variables to the second half of the jump (post acceleration)
			float YatAccEnd = SuvatEquations.SfromUAT(velocity.Y, acceleration.Y, accelerationTime);
			float XatAccEnd = SuvatEquations.SfromUAT(velocity.X, acceleration.X, accelerationTime);

			initialPosition.Y += YatAccEnd;
			initialPosition.X += XatAccEnd;

			velocity.X = SuvatEquations.VfromUAT(velocity.X, acceleration.X, accelerationTime);
			velocity.Y = SuvatEquations.VfromUAT(velocity.Y, acceleration.Y, accelerationTime);

			acceleration.X = 0;

			//get the XY coordinates from the curve after acceleration
			Tuple<Vector2D, Vector2D> possiblePositions2 = UsefulMathsFunctions.GetWorldspacePointAlongCurve(initialPosition, velocity, acceleration, totalJumpTime - accelerationTime, float.NaN, destY);
			
			//Finally, select which results are to be returned (max of 2 valid results are assumed)
			if (possiblePositions1.Item1 != null && possiblePositions1.Item2 != null) return possiblePositions1;
			if (possiblePositions2.Item1 != null && possiblePositions2.Item2 != null) return possiblePositions2;

			if (possiblePositions1.Item1 != null && possiblePositions2.Item1 != null) return new Tuple<Vector2D, Vector2D>(possiblePositions1.Item1, possiblePositions2.Item1);
			if (possiblePositions1.Item2 != null && possiblePositions2.Item2 != null) return new Tuple<Vector2D, Vector2D>(possiblePositions1.Item2, possiblePositions2.Item2);

			if (possiblePositions1.Item1 != null && possiblePositions2.Item2 != null) return new Tuple<Vector2D, Vector2D>(possiblePositions1.Item1, possiblePositions2.Item2);
			if (possiblePositions1.Item2 != null && possiblePositions2.Item1 != null) return new Tuple<Vector2D, Vector2D>(possiblePositions1.Item2, possiblePositions2.Item1);

			if (possiblePositions1.Item1 != null) return new Tuple<Vector2D, Vector2D>(possiblePositions1.Item1, possiblePositions1.Item1);
			if (possiblePositions1.Item2 != null) return new Tuple<Vector2D, Vector2D>(possiblePositions1.Item2, possiblePositions2.Item2);

			if (possiblePositions2.Item1 != null) return new Tuple<Vector2D, Vector2D>(possiblePositions2.Item1, possiblePositions2.Item1);
			if (possiblePositions2.Item2 != null) return new Tuple<Vector2D, Vector2D>(possiblePositions2.Item2, possiblePositions2.Item2);

			return new Tuple<Vector2D, Vector2D>(null, null);			
		}

		//Get the Y coordinates of an input jump at the specified X coordinate
		public Tuple<Vector2D, Vector2D> GetJumpYFromX(Vector2D source, float accelerationTime, float totalJumpTime, Vector2D gravity, float destX)
		{
			if (float.IsNaN(accelerationTime)) return new Tuple<Vector2D, Vector2D>(null, null);

			//setup the required variables
			int sign = (int)(accelerationTime / (float)Math.Abs(accelerationTime));
			accelerationTime *= sign;

			Vector2D acceleration = new Vector2D(GetHorizontalAcceleration() * sign, gravity.Y);
			Vector2D velocity = new Vector2D(playerBody.LinearVelocity.X, jumpInitialVelocity);
			Vector2D initialPosition = new Vector2D(source);

			//get the XY coordinates from the curve during acceleration
			Tuple<Vector2D, Vector2D> possiblePositions1 = UsefulMathsFunctions.GetWorldspacePointAlongCurve(source, velocity, acceleration, accelerationTime, destX, float.NaN);

			//update the variables to the second half of the jump (post acceleration)
			float YatAccEnd = SuvatEquations.SfromUAT(velocity.Y, acceleration.Y, accelerationTime);
			float XatAccEnd = SuvatEquations.SfromUAT(velocity.X, acceleration.X, accelerationTime);

			initialPosition.Y += YatAccEnd;
			initialPosition.X += XatAccEnd;

			velocity.X = SuvatEquations.VfromUAT(velocity.X, acceleration.X, accelerationTime);
			velocity.Y = SuvatEquations.VfromUAT(velocity.Y, acceleration.Y, accelerationTime);

			acceleration.X = 0;

			//get the XY coordinates from the curve after acceleration
			Tuple<Vector2D, Vector2D> possiblePositions2 = UsefulMathsFunctions.GetWorldspacePointAlongCurve(initialPosition, velocity, acceleration, totalJumpTime - accelerationTime, destX, float.NaN);
			
			//Finally, select which results are to be returned (max of 2 valid results are assumed)
			if (possiblePositions1.Item1 != null && possiblePositions1.Item2 != null && possiblePositions1.Item1 != possiblePositions1.Item2) return possiblePositions1;
			if (possiblePositions2.Item1 != null && possiblePositions2.Item2 != null && possiblePositions2.Item1 != possiblePositions2.Item2) return possiblePositions2;

			if (possiblePositions1.Item1 != null && possiblePositions2.Item1 != null) return new Tuple<Vector2D, Vector2D>(possiblePositions1.Item1, possiblePositions2.Item1);
			if (possiblePositions1.Item2 != null && possiblePositions2.Item2 != null) return new Tuple<Vector2D, Vector2D>(possiblePositions1.Item2, possiblePositions2.Item2);

			if (possiblePositions1.Item1 != null && possiblePositions2.Item2 != null) return new Tuple<Vector2D, Vector2D>(possiblePositions1.Item1, possiblePositions2.Item2);
			if (possiblePositions1.Item2 != null && possiblePositions2.Item1 != null) return new Tuple<Vector2D, Vector2D>(possiblePositions1.Item2, possiblePositions2.Item1);

			if (possiblePositions1.Item1 != null) return new Tuple<Vector2D, Vector2D>(possiblePositions1.Item1, possiblePositions1.Item1);
			if (possiblePositions1.Item2 != null) return new Tuple<Vector2D, Vector2D>(possiblePositions1.Item2, possiblePositions2.Item2);

			if (possiblePositions2.Item1 != null) return new Tuple<Vector2D, Vector2D>(possiblePositions2.Item1, possiblePositions2.Item1);
			if (possiblePositions2.Item2 != null) return new Tuple<Vector2D, Vector2D>(possiblePositions2.Item2, possiblePositions2.Item2);

			return new Tuple<Vector2D, Vector2D>(null, null);			
		}

		//the player control methods
		public void MovePlayerRight() { controls[(int)CONTROLS.RIGHT] = true; }
		public void MovePlayerLeft() { controls[(int)CONTROLS.LEFT] = true; }
		public void MovePlayerUp() { if (OnGround()) controls[(int)CONTROLS.UP] = true; }
		public void MovePlayerDown() { controls[(int)CONTROLS.DOWN] = true; }

		public void EndPlayerRight() { controls[(int)CONTROLS.RIGHT] = false; }
		public void EndPlayerLeft() { controls[(int)CONTROLS.LEFT] = false; }
		public void EndPlayerUp() { controls[(int)CONTROLS.UP] = false; }
		public void EndPlayerDown() { controls[(int)CONTROLS.DOWN] = false; }

		//return true if the player is currently colliding with somethin
        private bool OnGround()
        {
			if (playerBody.collisionNormal == null || playerBody.collisionNormal.Length() == 0.0f) return false;
			return true;
        }
		
        public void CollideWithEnemy()
        {
            if(collisionCooldown <= 0.0f)
            {
				//reduce the player's size (if it gets too small then the player dies) and increase mobility
                ((Physics.Circle)playerBody.Shape).Radius -= 1;
                SetSpeed((int)speed + 1000);

                collisionCooldown = 1.0f;
            }
        }

		public void CollideWithGoal() { won = true; }
    }
}

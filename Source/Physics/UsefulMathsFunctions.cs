using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Physics
{
	//contains useful maths functions that don't really belong anywhere
	//mostly used by the jump and fall calculations in the Player class
	static public class UsefulMathsFunctions
	{
		//given a motion arc (defined by init velocity, acceleration, and time), return the coordinates where the arc passes through the edge of a given rigidbody (imagining rigidbody edges as inifitely long lines)
		//these coordinates can then be used to determine if the arc collides with the rigidbody
		static public Tuple<Vector2D, Vector2D, Vector2D, Vector2D> GetArcPosAtBoxEdges(RigidBody theBox, Vector2D initialLocation, Vector2D initialVelocity, Vector2D acceleration, float duration)
		{
			Vector2D topEdge = null, bottomEdge = null, leftEdge = null, rightEdge = null;

			Tuple<Vector2D, Vector2D> results;

			//get the position of the jump, along the top edge, closest to the centre of the box
			results = GetWorldspacePointAlongCurve(initialLocation, initialVelocity, acceleration, duration, float.NaN, theBox.Position.Y + theBox.Shape.ComputeAABB().MIN.Y);
			if (results.Item1 != null)
			{
				topEdge = results.Item1;
				if (results.Item2 != null && Math.Abs(results.Item2.X - theBox.Position.X) < Math.Abs(results.Item1.X - theBox.Position.X)) topEdge = results.Item2;
			}
			else
			{
				topEdge = results.Item2;
			}

			//get the position of the jump, along the bottom edge, closest to the centre of the box
			results = GetWorldspacePointAlongCurve(initialLocation, initialVelocity, acceleration, duration, float.NaN, theBox.Position.Y + theBox.Shape.ComputeAABB().MAX.Y);
			if (results.Item1 != null)
			{
				bottomEdge = results.Item1;
				if (results.Item2 != null && Math.Abs(results.Item2.X - theBox.Position.X) < Math.Abs(results.Item1.X - theBox.Position.X)) bottomEdge = results.Item2;
			}
			else
			{
				bottomEdge = results.Item2;
			}

			//get the position of the jump, along the left edge, closest to the centre of the box
			results = GetWorldspacePointAlongCurve(initialLocation, initialVelocity, acceleration, duration, theBox.Position.X + theBox.Shape.ComputeAABB().MIN.X, float.NaN);
			if (results.Item1 != null)
			{
				leftEdge = results.Item1;
				if (results.Item2 != null && Math.Abs(results.Item2.Y - theBox.Position.Y) < Math.Abs(results.Item1.Y - theBox.Position.Y)) leftEdge = results.Item2;
			}
			else
			{
				leftEdge = results.Item2;
			}

			//get the position of the jump, along the right edge, closest to the centre of the box
			results = GetWorldspacePointAlongCurve(initialLocation, initialVelocity, acceleration, duration, theBox.Position.X + theBox.Shape.ComputeAABB().MAX.X, float.NaN);
			if (results.Item1 != null)
			{
				rightEdge = results.Item1;
				if (results.Item2 != null && Math.Abs(results.Item2.Y - theBox.Position.Y) < Math.Abs(results.Item1.Y - theBox.Position.Y)) rightEdge = results.Item2;
			}
			else
			{
				rightEdge = results.Item2;
			}

			//return these coordinates
			return new Tuple<Vector2D, Vector2D, Vector2D, Vector2D>(topEdge, bottomEdge, leftEdge, rightEdge);
		}

		//given a motion (init vel, acc, duration) and either an X or a Y coordinate, calculate the coordinate that WASN'T given (note that one of X and Y is expected to be NaN)
		static public Tuple<Vector2D, Vector2D> GetWorldspacePointAlongCurve(Vector2D initialLocation, Vector2D initialVelocity, Vector2D acceleration, float duration, float Xposition, float Yposition)
		{
			float displacementX = Xposition - initialLocation.X;
			float displacementY = Yposition - initialLocation.Y;

			//calculate the relative positions
			Tuple<Vector2D, Vector2D> possiblePositions = UsefulMathsFunctions.GetPointAlongCurve(initialVelocity, acceleration, duration, displacementX, displacementY);

			//move the positions back into world space and return them
			if (possiblePositions.Item1 != null)
			{
				possiblePositions.Item1.X += initialLocation.X;
				possiblePositions.Item1.Y += initialLocation.Y;
			}

			if (possiblePositions.Item2 != null)
			{
				possiblePositions.Item2.X += initialLocation.X;
				possiblePositions.Item2.Y += initialLocation.Y;
			}
			return possiblePositions;
		}

		//find the coordinates along a curve at the specified X or Y position and return them
		static public Tuple<Vector2D, Vector2D> GetPointAlongCurve(Vector2D initialVelocity, Vector2D acceleration, float duration, float Xdisplacement, float Ydisplacement)
		{
			//if no coordinate is specified return null
			if (float.IsNaN(Ydisplacement) && float.IsNaN(Xdisplacement)) return new Tuple<Vector2D, Vector2D>(null, null);

			//find the possible coordinates from the Xdisplacement
			if(float.IsNaN(Ydisplacement))
			{
				Tuple<float, float> Yresults = GetOtherCoordFromCurve(initialVelocity.X, initialVelocity.Y, acceleration.X, acceleration.Y, duration, Xdisplacement);

				Vector2D coord1 = null;
				Vector2D coord2 = null;

				if (!float.IsNaN(Yresults.Item1)) coord1 = new Vector2D(Xdisplacement, Yresults.Item1);
				if (!float.IsNaN(Yresults.Item2)) coord2 = new Vector2D(Xdisplacement, Yresults.Item2);

				return new Tuple<Vector2D, Vector2D>(coord1, coord2);
			}

			//find the possible coordinates from the Ydisplacement
			if(float.IsNaN(Xdisplacement))
			{
				Tuple<float,float> Xresults = GetOtherCoordFromCurve(initialVelocity.Y, initialVelocity.X, acceleration.Y, acceleration.X, duration, Ydisplacement);

				Vector2D coord1 = null;
				Vector2D coord2 = null;

				if (!float.IsNaN(Xresults.Item1)) coord1 = new Vector2D(Xresults.Item1, Ydisplacement);
				if (!float.IsNaN(Xresults.Item2)) coord2 = new Vector2D(Xresults.Item2, Ydisplacement);

				return new Tuple<Vector2D, Vector2D>(coord1, coord2);
			}

			return new Tuple<Vector2D, Vector2D>(null, null);
		}

		//given the velocity, acceleration, duration, and coordinate of one axis, calculate the coordinate of the other axis from it's velocity and acceleration
		static private Tuple<float, float> GetOtherCoordFromCurve(float initialVelocity1, float initialVelocity2, float acceleration1, float acceleration2, float maxDuration, float coord1)
		{
			//time axis 1 takes to reach coord1
			Tuple<float,float> time = SuvatEquations.TfromSUA(coord1, initialVelocity1, acceleration1);

			float result1 = time.Item1;
			float result2 = time.Item2;
			
			//if either result is invalid, mark is as such
			if (result1 >= maxDuration) result1 = float.NaN;
			if (result2 >= maxDuration) result2 = float.NaN;
			if (result1 <= 0) result1 = float.NaN;
			if (result2 <= 0) result2 = float.NaN;

			//calculate the two possible positions of coord2
			float otherCoord1 = SuvatEquations.SfromUAT(initialVelocity2, acceleration2, result1);
			float otherCoord2 = SuvatEquations.SfromUAT(initialVelocity2, acceleration2, result2);

			return new Tuple<float,float>(otherCoord1, otherCoord2);
		}
	}
}

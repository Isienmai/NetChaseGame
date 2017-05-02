using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Physics
{
	//Credit goes to the Game Behaviour Module Team for writing the original Box class
	//This version has been extended to contain functions used in collision detection / resolution
	public class Box : Shape
    {
        public Vector2D[] Vertices;

        public Box()
        {
            InitialiseDefaults();

            Vertices[0] = new Vector2D(-10, -10);
            Vertices[1] = new Vector2D(10, -10);
            Vertices[2] = new Vector2D(10, 10);
            Vertices[3] = new Vector2D(-10, 10);
        }

        public Box(Vector2D v1, Vector2D v2, Vector2D v3, Vector2D v4)
        {
            InitialiseDefaults();

            Vertices[0] = v1;
            Vertices[1] = v2;
            Vertices[2] = v3;
            Vertices[3] = v4;
        }

        public Box(float width, float height)
        {
            InitialiseDefaults();

            Vertices[0] = new Vector2D(width / -2, height / -2);
            Vertices[1] = new Vector2D(width / 2, height / -2);
            Vertices[2] = new Vector2D(width / 2, height / 2);
            Vertices[3] = new Vector2D(width / -2, height / 2);
        }

        private void InitialiseDefaults()
        {
            Vertices = new Vector2D[4];
            mColor = System.Drawing.Color.CadetBlue;
        }

        public override AABB ComputeAABB()
        {
            if (this.aabb != null) return this.aabb;

            this.aabb = new AABB();

            aabb.MIN = new Vector2D(
                Math.Min(Math.Min(Vertices[0].X, Vertices[1].X), Math.Min(Vertices[2].X, Vertices[3].X)),
                Math.Min(Math.Min(Vertices[0].Y, Vertices[1].Y), Math.Min(Vertices[2].Y, Vertices[3].Y)));

            aabb.MAX = new Vector2D(
                Math.Max(Math.Max(Vertices[0].X, Vertices[1].X), Math.Max(Vertices[2].X, Vertices[3].X)),
                Math.Max(Math.Max(Vertices[0].Y, Vertices[1].Y), Math.Max(Vertices[2].Y, Vertices[3].Y)));

            return this.aabb;
        }

        public bool isPointInRect(Vector2D point)
        {
            return ((point.X > ComputeAABB().MIN.X) && (point.X < ComputeAABB().MAX.X) &&
                    (point.Y > ComputeAABB().MIN.Y) && (point.Y < ComputeAABB().MAX.Y));
        }


        //Locate the point on the edge of the box that is closest to the input point 
        public Vector2D vectorToNearestSide(Vector2D point)
        {
            Vector2D nearestPoint = nearestPointOnVector(point - Vertices[3], Vertices[0] - Vertices[3]) + Vertices[3];

            Vector2D tempNearest;

            //Loop through the remaining 3 sides and only store the point which is closest to the input
            for(int i = 0; i < 3; ++i)
            {
                tempNearest = nearestPointOnVector(point - Vertices[i], Vertices[i+1] - Vertices[i]) + Vertices[i];

                if ((tempNearest - point).Length() < (nearestPoint - point).Length()) nearestPoint = tempNearest;
            }

            return nearestPoint - point;
        }

		//Locate the position on the input vector that is closest to the input point
		//Note: The general concept used was found online, but I have lost the website I got it from
		//the general idea is to project the vector to the point onto the provided vector, getting the distance along the vector that the closest point is
		//then this point is calculated using the provided vector and the projection distance.
		private Vector2D nearestPointOnVector(Vector2D point, Vector2D vector)
        {
            float projectionLength = point.Dot(vector * (1/vector.Length()));

            //Take into account the possibility of nearest point being beyond the ends of the vector
            //In this case the nearest point is one of the endpoints of the vector
            if (projectionLength < 0) return new Vector2D(0,0);
            if (projectionLength > vector.Length()) return vector;
			

            return vector * (projectionLength / vector.Length());
        }


        public override bool IsCollided(Circle otherShape, Vector2D thisPosition, Vector2D otherPosition)
        {
            Vector2D relativeOtherPosition = otherPosition - thisPosition;

            return this.vectorToNearestSide(relativeOtherPosition).Length() < otherShape.Radius;
        }

		//simple AABB on AABB collision
        public override bool IsCollided(Box otherShape, Vector2D thisPosition, Vector2D otherPosition)
        {
            float widthThis = this.ComputeAABB().MAX.X - this.ComputeAABB().MIN.X;
            float widthOther = otherShape.ComputeAABB().MAX.X - otherShape.ComputeAABB().MIN.X;

            float heightThis = this.ComputeAABB().MAX.Y - this.ComputeAABB().MIN.Y;
            float heightOther = otherShape.ComputeAABB().MAX.Y - otherShape.ComputeAABB().MIN.Y;

            return ((Math.Abs(thisPosition.X - otherPosition.X) * 2 < widthThis + widthOther) &&
                (Math.Abs(thisPosition.Y - otherPosition.Y) * 2 < heightThis + heightOther));
        }


        public override Vector2D GetCollisionNormal(Circle otherShape, Vector2D thisPosition, Vector2D otherPosition)
        {
            Vector2D relativeOtherPosition = otherPosition - thisPosition;

			//special case if the two objects are at the same location
            if (relativeOtherPosition.Length() == 0) relativeOtherPosition = new Vector2D(1, 0);

			//if the centre is inside the rectangle, the normal would point the wrong way
			//This drastically reduces the tunnelling problem, allowing more AI to be running simultaneously
			if (this.isPointInRect(relativeOtherPosition))
			{
				return vectorToNearestSide(relativeOtherPosition) * -1;
			}

			return vectorToNearestSide(relativeOtherPosition);
        }

        public override Vector2D GetCollisionNormal(Box otherShape, Vector2D thisPosition, Vector2D otherPosition)
        {
            Vector2D relativeOtherPosition = otherPosition - thisPosition;
			if (relativeOtherPosition.Length() == 0) relativeOtherPosition = new Vector2D(1, 0);			

			//From that calculate the max and min points of the other box relative to this one
			Vector2D otherPositionMax = otherShape.ComputeAABB().MAX + relativeOtherPosition;
			Vector2D otherPositionMin = otherShape.ComputeAABB().MIN + relativeOtherPosition;

			//Get the width and height of the two boxes combined, and the width and height of the overlapping segment
			float totalWidth = Math.Max(this.ComputeAABB().MAX.X - otherPositionMin.X, otherPositionMax.X - this.ComputeAABB().MIN.X);
			float totalHeight = Math.Max(this.ComputeAABB().MAX.Y - otherPositionMin.Y, otherPositionMax.Y - this.ComputeAABB().MIN.Y);
			float overlappingWidth = Math.Min(this.ComputeAABB().MAX.X - otherPositionMin.X, otherPositionMax.X - this.ComputeAABB().MIN.X);			
			float overlappingHeight = Math.Min(this.ComputeAABB().MAX.Y - otherPositionMin.Y, otherPositionMax.Y - this.ComputeAABB().MIN.Y);
			
			Vector2D resultingNormal = new Vector2D(1, 1);

			//If the boxes overlap more vertically than horizontally, remove the Y component from the normal
			if (overlappingHeight > overlappingWidth) resultingNormal.Y = 0;
			//And vice versa for the X component
			if (overlappingHeight < overlappingWidth) resultingNormal.X = 0;

			//invert the normal's X direction if the point of collision is on the right of this box
			if (thisPosition.X - otherPosition.X < 1) resultingNormal.X *= -1;
			//invert the normal's Y direction if the point of collision is on the top of this box
			if (thisPosition.Y - otherPosition.Y < 1) resultingNormal.Y *= -1;
			
			return resultingNormal;
        }

        public override float GetCollisionPenDepth(Circle otherShape, Vector2D thisPosition, Vector2D otherPosition)
		{
			Vector2D relativeOtherPosition = otherPosition - thisPosition;
			if (relativeOtherPosition.Length() == 0) relativeOtherPosition = new Vector2D(1, 0);
			
			Vector2D vectorToLine = this.vectorToNearestSide(relativeOtherPosition);

			//if the circle's centre is inside the rectangle, moving it by the distance to the edge will still result in a penetration depth of the circles radius
			if (this.isPointInRect(relativeOtherPosition))
            {
				return (vectorToLine.Length() + otherShape.Radius);
			}
			
			return (otherShape.Radius - vectorToLine.Length());
        }

        public override float GetCollisionPenDepth(Box otherShape, Vector2D thisPosition, Vector2D otherPosition)
        {
            //Get the smaller X difference
            float XPenetration = Math.Min(Math.Abs((this.ComputeAABB().MIN.X + thisPosition.X) - (otherShape.ComputeAABB().MAX.X + otherPosition.X)),
                                          Math.Abs((otherShape.ComputeAABB().MIN.X + otherPosition.X) - (this.ComputeAABB().MAX.X + thisPosition.X)));

            //Get the smaller Y difference
            float YPenetration = Math.Min(Math.Abs((this.ComputeAABB().MIN.Y + thisPosition.Y) - (otherShape.ComputeAABB().MAX.Y + otherPosition.Y)),
                                          Math.Abs((otherShape.ComputeAABB().MIN.Y + otherPosition.Y) - (this.ComputeAABB().MAX.Y + thisPosition.Y)));

            return Math.Min(XPenetration, YPenetration);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Physics
{
	//Credit goes to the Game Behaviour Module Team for writing the original Circle class
	//This version has been extended to include functions used in collision detection / resolution
	public class Circle : Shape
    {
        public float Radius { get; set; }

        public Circle()
        {
            InitialiseDefaults();
            Radius = 15.0f;
        }

        public Circle(float aRadius)
        {
            InitialiseDefaults();
            Radius = aRadius;
        }

        private void InitialiseDefaults()
        {
            mColor = System.Drawing.Color.MediumSlateBlue;
        }

        public override AABB ComputeAABB()
        {
            if (this.aabb != null) return this.aabb;

            this.aabb = new AABB();

            aabb.MIN = new Vector2D( -1 * Radius, -1 * Radius);
            aabb.MAX = new Vector2D( Radius, Radius);

            return this.aabb;
        }


		//detect collision using the distance between two circles and their respective radii
        public override bool IsCollided(Circle otherShape, Vector2D thisPosition, Vector2D otherPosition)
        {
            return ((thisPosition - otherPosition).Length() < (this.Radius + otherShape.Radius));
        }
		//if the shortest distance between the centre of the circle and the edge of the box is less than the circles radius, they are colliding
        public override bool IsCollided(Box otherShape, Vector2D thisPosition, Vector2D otherPosition)
        {
            return otherShape.vectorToNearestSide(thisPosition - otherPosition).Length() < Radius;
        }


		//normal is simply the vector from the centre of one to the centre of the other
        public override Vector2D GetCollisionNormal(Circle otherShape, Vector2D thisPosition, Vector2D otherPosition)
        {
            if(thisPosition == otherPosition) return new Vector2D(1, 0);
            return thisPosition - otherPosition;
        }
		//delegate the calculation to the other shape, and return that calculated normal pointing the opposite direction
        public override Vector2D GetCollisionNormal(Box otherShape, Vector2D thisPosition, Vector2D otherPosition)
        {
            return otherShape.GetCollisionNormal(this, otherPosition, thisPosition) * -1;
        }


        public override float GetCollisionPenDepth(Circle otherShape, Vector2D thisPosition, Vector2D otherPosition)
        {
            //Penetration depth for two spheres is the distance between their centres minus the sum of their radii
            return (this.Radius + otherShape.Radius) - (thisPosition - otherPosition).Length();
        }
		//delegate this calculation to the other shape
        public override float GetCollisionPenDepth(Box otherShape, Vector2D thisPosition, Vector2D otherPosition)
        {
            return otherShape.GetCollisionPenDepth(this, otherPosition, thisPosition);
        }
    }
}

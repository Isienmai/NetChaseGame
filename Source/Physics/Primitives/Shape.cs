using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;

namespace Physics
{
	//Credit goes to the Game Behaviour Module Team for writing the original Shape class
	//This version has been extended to include functions used in collision detection / resolution
	public abstract class Shape
    {
        public System.Drawing.Color mColor;

        protected AABB aabb = null;
        public abstract AABB ComputeAABB();

		//call shape specific methods as implemented by children
		public bool IsCollided(Shape otherShape, Vector2D thisPosition, Vector2D otherPosition)
        {
			if (thisPosition == null || otherPosition == null) return false;

            if (otherShape is Circle) return IsCollided((Circle)otherShape, thisPosition, otherPosition);
            if (otherShape is Box) return IsCollided((Box)otherShape, thisPosition, otherPosition);

            //If the other shape has not been accounted for then return false
            return false;
        }
        public abstract bool IsCollided(Circle otherShape, Vector2D thisPosition, Vector2D otherPosition);
        public abstract bool IsCollided(Box otherShape, Vector2D thisPosition, Vector2D otherPosition);


		//call shape specific methods as implemented by children
        public Vector2D GetCollisionNormal(Shape otherShape, Vector2D thisPosition, Vector2D otherPosition)
        {
            if (otherShape is Circle) return GetCollisionNormal((Circle)otherShape, thisPosition, otherPosition);
            if (otherShape is Box) return GetCollisionNormal((Box)otherShape, thisPosition, otherPosition);

            //If the other shape has not been accounted for then return false
            return null;
        }
        public abstract Vector2D GetCollisionNormal(Circle otherShape, Vector2D thisPosition, Vector2D otherPosition);
        public abstract Vector2D GetCollisionNormal(Box otherShape, Vector2D thisPosition, Vector2D otherPosition);


		//call shape specific methods as implemented by children
		public float GetCollisionPenDepth(Shape otherShape, Vector2D thisPosition, Vector2D otherPosition)
        {
            if (otherShape is Circle) return GetCollisionPenDepth((Circle)otherShape, thisPosition, otherPosition);
            if (otherShape is Box) return GetCollisionPenDepth((Box)otherShape, thisPosition, otherPosition);

            //If the other shape has not been accounted for then return false
            return float.NaN;
        }
        public abstract float GetCollisionPenDepth(Circle otherShape, Vector2D thisPosition, Vector2D otherPosition);
        public abstract float GetCollisionPenDepth(Box otherShape, Vector2D thisPosition, Vector2D otherPosition);
    }
}

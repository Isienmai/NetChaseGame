using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Physics
{
	//Credit goes to the Game Behaviour Module Team for writing the original Vector2D class
	//This version has been extended slightly to include comparison and division operators
	public class Vector2D
    {
        public float X { get; set; }
        public float Y { get; set; }

        public Vector2D()
        {
            X = 0.0f; Y = 0.0f;
        }
        public Vector2D(Vector2D other)
        {
            X = other.X; Y = other.Y;
        }

        public Vector2D(float x, float y)
        {
            X = x; Y = y;
        }

		//if the operation won't be a division by zero, return this vector's normalised form
		public Vector2D Normalised()
		{
			
			if(this.Length() != 0)return this / this.Length();
			return this;
		}

        public static float operator *( Vector2D lhs, Vector2D rhs )
        {
            return rhs.X * rhs.X + lhs.Y * rhs.Y;
        }

        public static Vector2D operator *(Vector2D lhs, float s)
        {
            return new Vector2D(lhs.X * s, lhs.Y * s);
        }

		public static Vector2D operator /(Vector2D lhs, float s)
		{
			return new Vector2D(lhs.X / s, lhs.Y / s);
		}

        public static Vector2D operator +( Vector2D lhs, Vector2D rhs )
        {
            return new Vector2D(lhs.X + rhs.X, lhs.Y + rhs.Y);
        }

        public static Vector2D operator -( Vector2D lhs, Vector2D rhs )
        {
            return new Vector2D(lhs.X - rhs.X, lhs.Y - rhs.Y);
        }

		//overload the comparison operators to compare values, not references
		public static bool operator ==(Vector2D lhs, Vector2D rhs)
        {
			if (object.ReferenceEquals(rhs, null) && object.ReferenceEquals(lhs, null)) return true;
			if (object.ReferenceEquals(rhs, null)) return false;
			if (object.ReferenceEquals(lhs, null)) return false;
            return lhs.X == rhs.X && lhs.Y == rhs.Y;
        }
        public static bool operator !=(Vector2D lhs, Vector2D rhs)
		{
			if (object.ReferenceEquals(rhs, null) && object.ReferenceEquals(lhs, null)) return false;
			if (object.ReferenceEquals(rhs, null)) return true;
			if (object.ReferenceEquals(lhs, null)) return true;
			return lhs.X != rhs.X || lhs.Y != rhs.Y;
        }

		public float Dot(Vector2D rhs)
        {
            return X * rhs.X + Y * rhs.Y;
        }

        public float Cross(Vector2D rhs)
        {
            return X * rhs.Y - Y * rhs.X;
        }

        public float Length()
        {
            return (float)Math.Sqrt(this.X * this.X + this.Y * this.Y);
        }

        public float LengthSqr()
        {
            return this.X * this.X + this.Y * this.Y;
        }
    }
}

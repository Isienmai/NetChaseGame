using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Physics
{
	//This class is simply an implementation of all the suvat equations
	//It contains methods that calculate any one of the suvat variables from any three of the others
	public class SuvatEquations
	{
		//s = displacement
		//u = initial velocity
		//v = final velocity
		//a = acceleration
		//t = time

		//v = u + at
		public static float VfromUAT(float u, float a, float t)
		{
			return u + (a * t);
		}
		public static float UfromVAT(float v, float a, float t)
		{
			return v - (a * t);
		}
		public static float TfromUAV(float u, float a, float v)
		{
			return (v - u) / a;
		}
		public static float AfromUTV(float u, float t, float v)
		{
			return (v - u) / t;
		}


		//s = ut + 1/2 * at^2
		public static float SfromUAT(float u, float a, float t)
		{
			return (u * t) + (a * t * t) / 2;
		}
		public static float UfromSAT(float s, float a, float t)
		{
			return s / t - (a * t / 2);
		}
		public static float AfromSUT(float s, float u, float t)
		{
			return 2 * (s - u * t) / (t * t);
		}
		//turns into a quadratic equation. return both roots.
		//if the returned item1 is NaN then item2 MUST also be NaN
		public static Tuple<float,float> TfromSUA(float s, float u, float a)
		{
			//ax^2 + bx + c = 0
			//(a/2 * t^2) + (u * t) - s = 0
			float A = a / 2;
			float B = u;
			float C = s * -1;

			float discriminant = (B * B) - (4 * A * C);

			if (discriminant < 0) return new Tuple<float, float>(float.NaN, float.NaN);

			float result1 = float.NaN;
			float result2 = float.NaN;

			//calculation is much simplified if there is no acceleration
			if (A == 0)
			{
				result1 = s / u;
			}
			else
			{
				if (discriminant == 0)
				{
					result1 = (B * -1) / (2 * A);
				}
				else
				{
					result1 = ((B * -1) + (float)Math.Sqrt(discriminant)) / (2 * A);
					result2 = ((B * -1) - (float)Math.Sqrt(discriminant)) / (2 * A);
				}
			}

			return new Tuple<float, float>(result1, result2);
		}


		//s = 1/2(u + v)t
		public static float SfromUVT(float u, float v, float t)
		{
			return (u + v) * t / 2;
		}
		public static float UfromSVT(float s, float v, float t)
		{
			return 2 * (s / t) - v;
		}
		public static float VfromSUT(float s, float u, float t)
		{
			return 2 * (s / t) - u;
		}
		public static float TfromSUV(float s, float u, float v)
		{
			return (2 * s) / (u + v);
		}


		//v^2 = u^2 + 2as
		public static float VsquaredFromUAS(float u, float a, float s)
		{
			return (u * u) + (2 * a * s);
		}
		public static float UsquaredFromVAS(float v, float a, float s)
		{
			return (v * v) - (2 * a * s);
		}
		public static float AFromVUS(float v, float u, float s)
		{
			return ((v * v) - (u * u)) / (2 * s);
		}
		public static float SFromUVA(float u, float v, float a)
		{
			return ((v * v) - (u * u)) / (2 * a);
		}


		//s = vt - 1/2 * at^2
		public static float SfromVAT(float v, float a, float t)
		{
			return (v * t) - (a * t * t) / 2;
		}
		public static float VfromSAT(float s, float a, float t)
		{
			return (s/t) + (a * t) / 2;
		}
		public static float AfromSVT(float s, float v, float t)
		{
			return 2 * ((v * t) - s) / (t * t);
		}

		//turns into a quadratic equation. return both roots.
		//if the returned item1 is NaN then item2 MUST also be NaN
		public static Tuple<float,float> TfromSVA(float s, float v, float a)
		{
			//ax^2 + bx + c = 0
			//(-a/2 * t^2) + (v * t) - s = 0

			float A = (a * -1) / 2;
			float B = v;
			float C = s * -1;

			float discriminant = (B * B) - (4 * A * C);
			if (discriminant < 0) return new Tuple<float, float>(float.NaN, float.NaN);

			float result1 = float.NaN;
			float result2 = float.NaN;

			//calculation is much simplified if there is no acceleration
			if (A == 0)
			{
				result1 = s / v;
			}
			else
			{
				if (discriminant == 0)
				{
					result1 = (B * -1) / (2 * A);
				}
				else
				{
					result1 = ((B * -1) + (float)Math.Sqrt(discriminant)) / (2 * A);
					result2 = ((B * -1) - (float)Math.Sqrt(discriminant)) / (2 * A);
				}
			}


			return new Tuple<float, float>(result1, result2);
		}
	}
}

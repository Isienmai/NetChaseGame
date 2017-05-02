using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Physics
{
	//Credit goes to the Game Behaviour Module Team for writing the SpringJoint class
	//This version has been slightly modified to line up with modifications to the RigidBody class
	public class SpringJoint
    {
        public RigidBody BodyA { get; set; }
        public RigidBody BodyB { get; set; }
        public float Stiffness { get; set; }
        public float RestLength { get; set; }
        public float Dampen { get; set; }
        Vector2D appliedForce;

        public SpringJoint()
        {
            InitialiseDefaults();
        }

        public SpringJoint(RigidBody anchor, RigidBody bouncer)
        {
            InitialiseDefaults();

            BodyA = anchor;
            BodyB = bouncer;
        }

        private void InitialiseDefaults()
        {
            Stiffness = 10000.0f;  //stiffness of the spring
            RestLength = 10.0f;  //The rest length of the spring
            Dampen = 0.9f;      //Spring force dampen factor

            appliedForce = new Vector2D();
        }

        public void ApplyForce()
        {
			//Note: The spring force has a direction which is given by the vector between BodyB and BodyA
			// The magnitude of the spring force is caculated using the Hooke's law


			//Vector between the two masses attached to the spring
			Vector2D s_vec = BodyB.Position - BodyA.Position;

			if (s_vec.Length() == 0) return;


            //Distance between the two masses, i.e. the length of the spring
            float lengthDifference = s_vec.Length() - RestLength;


            //Compute the spring force based on Hooke's law
            float force = Stiffness * -1 * lengthDifference * Dampen;


            //Apply the spring force to the two bodies joined by the spring

            appliedForce = s_vec * (force / s_vec.Length());

			BodyA.SetForce(BodyA.Force - appliedForce);

			BodyB.SetForce(BodyB.Force + appliedForce);
        }
    }
}

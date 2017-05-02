using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Physics
{
	//Credit goes to the Game Behaviour Module Team for writing the CollisionPairData class
	public class CollisionPairData
    {
        public RigidBody BodyA
        {
            get;
            set;
        }
        public RigidBody BodyB
        {
            get;
            set;
        }

        public Vector2D ContactPoint
        {
            get;
            set;
        }

        public Vector2D ContactNormal
        {
            get;
            set;
        }


        public CollisionPairData()
        {
            BodyA = null;
            BodyB = null;
        }
    }
}

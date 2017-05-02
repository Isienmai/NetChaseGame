using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Physics
{
	//a simple grid of springs connecting small boxes looks sort of like a net
    public class DynamicNet
    {
        public PhysicsEngine physicsOwner;

		//the boxes and springs that form the net
        public List<RigidBody> netEdges;
        public List<SpringJoint> netRopes;
		
        public DynamicNet(Vector2D initalLocation, Vector2D initalVelocity)
        {
            netEdges = new List<RigidBody>();
            netRopes = new List<SpringJoint>();
            physicsOwner = PhysicsEngine.GetPhysicsEngine();
            CreateNet(initalLocation, initalVelocity);
        }


		//initialise a net centred on the provided location, with the initial velocity
        private void CreateNet(Vector2D initalLocation, Vector2D initalVelocity)
        {
			float cubeSize = 5.0f;
			int width = 5;
			int height = 5;
			float restLength = 15;

			RigidBody tempSquare;
			

			//loop through the width and height of the net
			for(int i = 0; i < height; ++i)
			{
				for(int j = 0; j < width; ++j)
				{
					//create the next box and add it to the list of boxes
					tempSquare = new RigidBody();
					tempSquare.Shape = new Box(cubeSize, cubeSize);
					tempSquare.Shape.mColor = System.Drawing.Color.LightSteelBlue;
					tempSquare.type = RBTypes.PLAYER_NET;
					tempSquare.SetPosition(initalLocation + new Vector2D((j - 2) * cubeSize, (i - 2) * cubeSize));
					tempSquare.LinearVelocity = initalVelocity;
					tempSquare.SetDynamic();
					tempSquare.Mass = 20;
					netEdges.Add(tempSquare);


					//add the spring to the block on the left
					if(j > 0)
					{
						netRopes.Add(new SpringJoint(netEdges[i * width + j], netEdges[i * width + j - 1]));
						netRopes[netRopes.Count - 1].RestLength = restLength;
						netRopes[netRopes.Count - 1].Stiffness = 1000;
					}

					//add a spring to the block above
					if(i > 0)
					{
						netRopes.Add(new SpringJoint(netEdges[i * width + j], netEdges[(i - 1) * width + j]));
						netRopes[netRopes.Count - 1].RestLength = restLength;
						netRopes[netRopes.Count - 1].Stiffness = 1000;
					}
				}
			}
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;

namespace General
{
	//Credit goes to the Game Behaviour Module Team for writing the original SimpleRenderer class
	//This version has been extended slightly to add functionality, including writing text and allowing a colour to be specified when drawing lines
    class SimpleRenderer
    {
		//The brush and pen used by calls to the Graphics object
        private System.Drawing.SolidBrush mBrush;
        private System.Drawing.Pen mPen;

		//The font used by any text
		private System.Drawing.Font mFont;
		//The location of the camera within the game world
		public Physics.Vector2D cameraLocation;


		public SimpleRenderer()
        {
            mBrush = new SolidBrush(Color.Black);
            mPen = new Pen(Color.Black, 4);
			mPen.Alignment = System.Drawing.Drawing2D.PenAlignment.Center;

			//Note: requires the Arial font to be installed for the game to run
			mFont = new Font("Arial", 16);

			//Camera location defaults to 0,0
			cameraLocation = new Physics.Vector2D();
        }

        public void Draw(Physics.RigidBody rb, Graphics g)
        {
			//colour is dependant on the colour specified in the rigidbody
			mBrush.Color = rb.Shape.mColor;

			//a different draw method is needed for boxes than for circles
			if (rb.Shape is Physics.Circle)
            {
                float radius = ((Physics.Circle)rb.Shape).Radius;
                float diameter = ((Physics.Circle)rb.Shape).Radius * 2.0f; 

				//draw an ellipse with width and height that match the diameter of the circle
				//the position of the camera is used to determine where the object is drawn
                g.FillEllipse(mBrush, 
                    rb.Position.X - radius + cameraLocation.X,
                    rb.Position.Y - radius + cameraLocation.Y,
                    diameter,
                    diameter
                );
            }
            else if (rb.Shape is Physics.Box )
			{
				//Create the four points that are to be drawn
				PointF[] pts = new PointF[4];
                Physics.Box box = ((Physics.Box)rb.Shape);
                int index = 0;

				//the position of the camera is used to determine where the object is drawn
				foreach (Physics.Vector2D vert in box.Vertices)
                {
                    pts[index].X = rb.Position.X + vert.X + cameraLocation.X;
                    pts[index++].Y = rb.Position.Y + vert.Y + cameraLocation.Y;
                }

				//fill the polygon defined by the above points
                g.FillPolygon(mBrush, pts);
            }
        }

		//Draw a given point as a circle with radius 3, with colour Azure
        public void Draw(Physics.Vector2D point, Graphics g)
        {
            Physics.RigidBody temp = new Physics.RigidBody();
            temp.SetPosition(point);
            temp.Shape = new Physics.Circle(3);
			temp.Shape.mColor = Color.Azure;
            Draw(temp, g);
        }

		//Draw a line with the default colour LightSteelBlue
        public void DrawLine(Physics.Vector2D p1, Physics.Vector2D p2, Graphics g)
        {
			DrawLine(p1, p2,g, Color.LightSteelBlue);
        }

		//Draw a line between two points with a specified colour
		public void DrawLine(Physics.Vector2D p1, Physics.Vector2D p2, Graphics g, Color chosenColour)
		{
			mPen.Color = chosenColour;
			mPen.EndCap = System.Drawing.Drawing2D.LineCap.NoAnchor;

			g.DrawLine(mPen,
				new Point((int)p1.X + (int)cameraLocation.X, (int)p1.Y + (int)cameraLocation.Y),
			new Point((int)p2.X + (int)cameraLocation.X, (int)p2.Y + (int)cameraLocation.Y));
		}

		//Draw an arrow between two points with the default colour of LightSteelBlue
		public void DrawArrow(Physics.Vector2D p1, Physics.Vector2D p2, Graphics g)
        {
            mPen.Color = Color.LightSteelBlue;
            mPen.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;

            g.DrawLine(mPen,
                new Point((int)p1.X + (int)cameraLocation.X, (int)p1.Y + (int)cameraLocation.Y),
            new Point((int)p2.X + (int)cameraLocation.X, (int)p2.Y + (int)cameraLocation.Y));

        }

		//draw a spring joint as a DarkGrey line between the two rigidbodies of the spring
        public void Draw(Physics.SpringJoint spring, Graphics g)
        {
			DrawLine(spring.BodyA.Position, spring.BodyB.Position, g, Color.DarkGray);
        }

		//draw the provided text with the default colour of black
		public void Draw(string textToDisplay, Physics.Vector2D position, Graphics g)
		{
            Draw(textToDisplay, position, g, Color.Black);
		}

        public void Draw(string textToDisplay, Physics.Vector2D position, Graphics g, Color chosenColour)
        {
            mBrush.Color = chosenColour;
            g.DrawString(textToDisplay, mFont, mBrush, position.X, position.Y);
        }

		//Draw a red eight pointed star of lines around the provided position
        public void DrawAISpawn(Physics.Vector2D position, Graphics g)
        {
            //offset is the distance between the centre and the outer edges of the star
            float offset = 20.0f;
            float halfOffset = offset / 2.0f;
			Color starColour = Color.Red;

			//create the 8 points of the pentagram
			Physics.Vector2D point1, point2, point3, point4, point5, point6, point7, point8;

            point1 = position + new Physics.Vector2D(-halfOffset, -offset);
            point2 = position + new Physics.Vector2D(halfOffset, -offset);

            point3 = position + new Physics.Vector2D(-offset, -halfOffset);
            point4 = position + new Physics.Vector2D(offset, -halfOffset);

            point5 = position + new Physics.Vector2D(-offset, halfOffset);
            point6 = position + new Physics.Vector2D(offset, halfOffset);

            point7 = position + new Physics.Vector2D(-halfOffset, offset);
            point8 = position + new Physics.Vector2D(halfOffset, offset);

			//draw lines between the 8 points
            DrawLine(point1, point6, g, starColour);
            DrawLine(point6, point5, g, starColour);
            DrawLine(point5, point2, g, starColour);
            DrawLine(point2, point8, g, starColour);
            DrawLine(point8, point3, g, starColour);
            DrawLine(point3, point4, g, starColour);
            DrawLine(point4, point7, g, starColour);
            DrawLine(point7, point1, g, starColour);
        }
    }
}

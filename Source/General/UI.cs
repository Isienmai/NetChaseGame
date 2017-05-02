using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace General
{
	//store and display information to the player
    class UI
    {
		//defines the colour that text is drawn in
        public Color messageColour;

		//the locations of each piece of info displayed
        public Physics.Vector2D messageLocation;
        public Physics.Vector2D timerLocation;
        public Physics.Vector2D livesLocation;

		//the information to be displayed
        public float timeRunning;
        public string message;
        public int livesLeft;

        public UI()
        {
			//initialise everything
            message = "Climb the tower!";
            livesLeft = 12;
            timeRunning = 0.0f;
            messageColour = Color.Black;
			
            messageLocation = new Physics.Vector2D(GlobalConstants.WINDOW_WIDTH / 2.0f, 20);
            timerLocation = new Physics.Vector2D(GlobalConstants.WINDOW_WIDTH  - 160, 50);
            livesLocation = new Physics.Vector2D(GlobalConstants.WINDOW_WIDTH - 160, 100);
        }

        public void Step(float dt)
        {
            timeRunning += dt;

			//after five seconds have passed, update the message to display the next handy hint
            if(timeRunning > 5.0f)
            {
                message = "Watch out for the enemies";
            }
        }

		//update the display with the failure message
        public void GameLost()
        {
            message = "You Died";
            messageColour = Color.Red;
            messageLocation.Y = GlobalConstants.WINDOW_HEIGHT / 2.0f;
        }

		//update the display with the victory message
		public void GameWon()
        {
            message = "You Escaped!";
            messageColour = Color.Green;
            messageLocation.Y = GlobalConstants.WINDOW_HEIGHT / 2.0f;
        }

		//draw all the UI information
        public void DrawUI(SimpleRenderer aRenderer, Graphics g)
        {
            aRenderer.Draw(message, messageLocation, g, messageColour);
            aRenderer.Draw("Time: " + ((int)timeRunning).ToString(), timerLocation, g);
            aRenderer.Draw("Lives: " + livesLeft.ToString(), livesLocation, g);
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace General
{
    public partial class MainWindow : Form
    {
		//Used to draw objects within the window
        private SimpleRenderer mRenderer;
		//Used to display debug information to the screen on request
		private DebugDisplay dbgDisp;
		//Contains all the game elements (rigidbodies, players, AI, win/lose state, etc)
		private Scene mScene;

		//Used to make the physics framerate independant
        private Timer mDrawTimer, mPhysTimer;
		private System.Diagnostics.Stopwatch timer;

        public MainWindow()
        {
			InitializeComponent();

			//Set the sky colour
			this.BackColor = Color.SkyBlue;

			//create the renderer and the scene
            mRenderer = new SimpleRenderer();
            mScene = new Scene();

            //Set the graphics timer to update every 10ms
            mDrawTimer = new Timer();
            mDrawTimer.Interval = 10;
            mDrawTimer.Tick += new EventHandler(TickGraphics);
            mDrawTimer.Enabled = true;

            //Set the Physics timer to update ever 5ms
            mPhysTimer = new Timer();
            mPhysTimer.Interval = 5;
            mPhysTimer.Tick += new EventHandler(TickPhysics);
            mPhysTimer.Enabled = true;

			//create the debug display
			dbgDisp = new DebugDisplay(mScene.thePlayer, mScene.theAIManager, mRenderer);

			//Use timer to keep track of time between physics updates
			timer = new System.Diagnostics.Stopwatch();
			timer.Start();
		}

        private void MainWindow_Paint(object sender, PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;            

			//Update the position of the camera
            mRenderer.cameraLocation = (mScene.thePlayer.playerBody.Position - new Physics.Vector2D(GlobalConstants.WINDOW_WIDTH / 2, GlobalConstants.WINDOW_HEIGHT / 2)) * -1;
			//Do not allow the camera below a certain position
            if (mRenderer.cameraLocation.Y < 0) mRenderer.cameraLocation.Y = 0;


			//draw all AI spawn locations
			foreach (Physics.Vector2D spawn in mScene.theAIManager.AISpawnLocations)
			{
				mRenderer.DrawAISpawn(spawn, graphics);
			}

			//Draw all physical objects in the scene
			foreach (var prop in mScene.thePhysicsEngine.dynamicPhysicsObjects)
            {
                mRenderer.Draw(prop, graphics);
            }
			foreach (var prop in mScene.thePhysicsEngine.staticPhysicsObjects)
			{
				mRenderer.Draw(prop, graphics);
			}

			//draw all spring joints
			foreach (var spring in mScene.thePhysicsEngine.springs)
			{
				mRenderer.DrawLine(spring.BodyA.Position, spring.BodyB.Position, graphics);
			}


			//draw any debug information to the screen
			dbgDisp.DisplayDebugGraphics(graphics);

			//Draw the HUD
            mScene.headsUpDisplay.DrawUI(mRenderer, graphics);	
		}		

		private void TickGraphics(object sender, EventArgs e)
        {
			//Re-draw the screen
            Invalidate();
        }

        private void TickPhysics(object sender, EventArgs e)
        {
			//Get the time since the START of the last physics update
			float timeChange = timer.ElapsedMilliseconds;
			//restarting the timer AFTER the physics update causes slowdown if the physics is strenuous
			timer.Restart();

			//Update the physics
            mScene.Step(timeChange/700.0f);
        }

        private void MainWindow_MouseDown(object sender, MouseEventArgs e)
        {
            Point mouseloc = e.Location;

            switch (e.Button)
            {
				//Shoot the Net on leftclick
                case System.Windows.Forms.MouseButtons.Left:
					mScene.FireNet(new Physics.Vector2D(mouseloc.X, mouseloc.Y) - mRenderer.cameraLocation);
					break;
				//set the debug jump destination on rightclick ( only visible when drawing player jump arc )
				case System.Windows.Forms.MouseButtons.Right:
					dbgDisp.SetJumpDestination(new Physics.Vector2D(mouseloc.X, mouseloc.Y) - mRenderer.cameraLocation);
					break;
                default:
                    break;
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
				//Player controls (allow both arrow keys and WASD controls)
				//Keydown initiates motion
                case Keys.Left:
                case Keys.A:
                    mScene.thePlayer.MovePlayerLeft();
                    break;
                case Keys.Right:
                case Keys.D:
                    mScene.thePlayer.MovePlayerRight();
                    break;
                case Keys.W:
                case Keys.Space:
                case Keys.Up:                    
                    mScene.thePlayer.MovePlayerUp();
                    break;
                case Keys.S:
                case Keys.Down:
                    mScene.thePlayer.MovePlayerDown();
                    break;

				//Debug display controls (toggle different debug info on and off)
				case Keys.P:
					dbgDisp.TogglePlayerJumpDebug();
					break;
				case Keys.I:
					dbgDisp.ToggleNodesDebug();
					break;
				case Keys.K:
					dbgDisp.ToggleNodeConnectionDisplay();
					break;
				case Keys.U:
					dbgDisp.ToggleAIPath();
					break;
				case Keys.Y:
					dbgDisp.ToggleJumpRangeDebug();
					break;
				case Keys.H:
					dbgDisp.ToggleFallRangeDebug();
					break;
				case Keys.T:
					dbgDisp.ToggleShowJumpBoxCollisions();
					break;
				case Keys.G:
					dbgDisp.ToggleShowFallBoxCollisions();
					break;
				case Keys.R:
					dbgDisp.ToggleShowCollisionNormals();
					break;
				
				//remove the current jump destination
				case Keys.ControlKey:
					dbgDisp.SetJumpDestination(null);
					break;
				default:
                    break;
            }
        }
        
        private void MainWindow_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
			{
				//Player controls (allow both arrow keys and WASD controls)
				//KeyUp ends motion
				case Keys.Left:
                case Keys.A:
                    mScene.thePlayer.EndPlayerLeft();
                    break;
                case Keys.Right:
                case Keys.D:
                    mScene.thePlayer.EndPlayerRight();
                    break;
                case Keys.W:
                case Keys.Space:
                case Keys.Up:
                    mScene.thePlayer.EndPlayerUp();
                    break;
                case Keys.S:
                case Keys.Down:
                    mScene.thePlayer.EndPlayerDown();
                    break;
                default:
                    break;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI
{
	//the different actions the AI can perform
	//note that it is possible to perform multiple actions at once
    enum ACTIONS
    {
        DO_NOTHING = 0x0,
        MOVE_LEFT = 0x1,
        MOVE_RIGHT = 0x2,
        MOVE_UP = 0x4,
        MOVE_DOWN = 0x8
    };

	//the different states of the FSM
    public enum STATES
    {
        WAITING,
        WALKING,
        JUMPING,
        FALLING
    }

	//a combination of action and duration
	//this defines how long a given action (or combination of actions) is to be performed
    class act_dur
    {
        public int action;
        public float duration;

        public act_dur(int argAction, float argDuration)
        {
            action = argAction;
            duration = argDuration;
        }
    }

	//This AI operates by keeping a list of input controls that it sends to its "Player" class
	//Every step, the AI applies the control at the top of the list, and removes the dt from that control's countdown. Once a countdown hits zero that control is removed from the list.
	//The rest of the AI populates this list, aiming to direct itself towards a destination location (taken from the graph)
	public class AIEngine
    {
		//the player that this AI agent will control
        public Physics.Player AIPlayer { get; private set; }
		Physics.PhysicsEngine physEng;

		//the list of actions that are currently being executed
		//adding a new "act_dur" to this list WILL result in the specified action being carried out for the specified duration
        List<act_dur> actionPlan;

        public STATES currentState { get; private set; }

		//this bool is used to make sure a jump is set up only ONCE when in the JUMP state
		bool currentlyJumping;
		//any state that remains active for a set period of time should use this float to manage it's duration
		float currentStateCooldown;

		//add a maximum walking speed to allow some AI to be faster than others
		float walkingSpeed;

		//these are used for pathfinding
		Node currentNode;
		Node destinationNode;
        public WorldGraph mainGraph;
        
        public AIEngine(Physics.Player thePlayer)
		{
			physEng = Physics.PhysicsEngine.GetPhysicsEngine();
			mainGraph = WorldGraph.GetWorldGraph();

			AIPlayer = thePlayer;

			actionPlan = new List<act_dur>();

            currentState = STATES.WAITING;
            currentStateCooldown = 0.0f;
			walkingSpeed = 500f;
			destinationNode = null;
			currentlyJumping = false;
		}

		//get the latest path information
		//update the AI's state
		//update the actionList according to the AI's current state
		//apply the action at the top of the list and remove that action from the list if necessary
        public void step(float dt, int currentAgentIndex)
		{
			//tell the graph to prepare a path for this AI
			mainGraph.GeneratePathToCurrentGoal(currentAgentIndex, AIPlayer.playerBody.Position);			
			currentNode = mainGraph.GetNodeAtPoint(AIPlayer.playerBody.Position);
			destinationNode = mainGraph.GetNextDestination(currentAgentIndex, currentNode);
			

			currentState = UpdateAIState();			
			switch (currentState)
            {
                case STATES.WAITING:
                    stepWaiting(dt);
                    break;
                case STATES.WALKING:
                    stepWalking(dt);
                    break;
                case STATES.JUMPING:
                    stepJumping(dt);
                    break;
                case STATES.FALLING:
                    stepFalling(dt);
                    break;
            };


			//apply the next action in the action plan
			if (actionPlan != null && actionPlan.Count != 0)
			{
				applyAction(actionPlan[0].action);
				actionPlan[0].duration -= dt;

				if (actionPlan[0].duration <= 0)
				{
					actionPlan.RemoveAt(0);
				}
			}
        }


		//send commands to this AI's "Player" according to the action specified
		void applyAction(int action)
		{
			if (action != (int)ACTIONS.DO_NOTHING)
			{
				if ((action & (int)ACTIONS.MOVE_LEFT) != 0) AIPlayer.MovePlayerLeft();
				else AIPlayer.EndPlayerLeft();

				if ((action & (int)ACTIONS.MOVE_RIGHT) != 0) AIPlayer.MovePlayerRight();
				else AIPlayer.EndPlayerRight();

				if ((action & (int)ACTIONS.MOVE_UP) != 0) AIPlayer.MovePlayerUp();
				else AIPlayer.EndPlayerUp();

				if ((action & (int)ACTIONS.MOVE_DOWN) != 0) AIPlayer.MovePlayerDown();
				else AIPlayer.EndPlayerDown();
			}
			else
			{
				AIPlayer.EndPlayerLeft();
				AIPlayer.EndPlayerRight();
				AIPlayer.EndPlayerUp();
				AIPlayer.EndPlayerDown();
			}
		}

		//update the current AI state based on the current AI state
		private STATES UpdateAIState()
		{
			switch(currentState)
			{
				case STATES.WAITING:
					return GetStateGivenWaiting();
				case STATES.WALKING:
					return GetStateGivenWalking();
				case STATES.JUMPING:
					return GetStateGivenJumping();
				case STATES.FALLING:
					return GetStateGivenFalling();
				default:
					return STATES.WAITING;
			}
		}


		//The following four methods determine AI activity during each state

		//When waiting, move towards the current node. 
		//If a jump is possible from the node but the AI is slightly off and cannot jump, moving towards the node should move the AI to a position from which the jump is possible
		private void stepWaiting(float dt)
        {
			if (currentNode == null) return;
			
			Physics.Vector2D displacement = mainGraph.topLevelNode.GetNodePosition(currentNode) - AIPlayer.playerBody.Position;

			if(displacement.X < 0) actionPlan.Add(new act_dur((int)ACTIONS.MOVE_LEFT, 0.0f));
			else actionPlan.Add(new act_dur((int)ACTIONS.MOVE_RIGHT, 0.0f));
		}

		//move left/right towards the destination node.
		//if hard deceleration would result in overshooting the destination, decelerate.
		//This is to stop the AI from going too fast and driving off the edge of a platform
        private void stepWalking(float dt)
		{
			if (destinationNode == null) return;
			Physics.Vector2D displacement = mainGraph.topLevelNode.GetNodePosition(destinationNode) - AIPlayer.playerBody.Position;

			//Get the direction of the displacement (-1 = dest is to the left)
			float sign = displacement.X / Math.Abs(displacement.X);

			//Get the displacement if the AI were to hard brake
			float naturalDisplacement = Physics.SuvatEquations.SFromUVA(AIPlayer.playerBody.LinearVelocity.X, 0.0f, AIPlayer.GetHorizontalAcceleration()) * -1;
			float currentDirectionOfMotion = 1;
			
			//force natural displacement to be positive
			if(AIPlayer.playerBody.LinearVelocity.X != 0) currentDirectionOfMotion = AIPlayer.playerBody.LinearVelocity.X / Math.Abs(AIPlayer.playerBody.LinearVelocity.X);
			naturalDisplacement *= currentDirectionOfMotion;
			
			//If the displacement with deceleration results in overshooting, slow down
			if (displacement.X * sign < naturalDisplacement * sign)
			{
				//decelerate
				if (sign > 0) actionPlan.Add(new act_dur((int)ACTIONS.MOVE_LEFT, 0.0f));
				else actionPlan.Add(new act_dur((int)ACTIONS.MOVE_RIGHT, 0.0f));
			}
			else
			{
				//if travelling the wrong way, or travelling below walkingSpeed, accelerate towards the destination
				if(currentDirectionOfMotion != sign || Math.Abs(AIPlayer.playerBody.LinearVelocity.X) < walkingSpeed)
				{
					if (sign < 0) actionPlan.Add(new act_dur((int)ACTIONS.MOVE_LEFT, 0.0f));
					else actionPlan.Add(new act_dur((int)ACTIONS.MOVE_RIGHT, 0.0f));
				}
				else
				{
					actionPlan.Add(new act_dur((int)ACTIONS.DO_NOTHING, 0.0f));
				}
			}
		}

		//the first time this is called, the jump to the destination is calculated and the jump itself is put on the action plan
		//every subsequent call then exits early until the JUMP state is exited
        private void stepJumping(float dt)
        {
			currentStateCooldown -= dt;

			//if the jump has already been set up then exit. Nothing more need be done
			if (currentlyJumping) return;

			//make sure the following code is only run once per jump
			currentlyJumping = true;

			//set the time this state will exit to be the same as the time the jump should end
			currentStateCooldown = AIPlayer.GetTotalJumpDuration(mainGraph.topLevelNode.GetNodePosition(currentNode), mainGraph.topLevelNode.GetNodePosition(destinationNode), physEng.gravity);

			//initiate the jump
			actionPlan.Add(new act_dur((int)ACTIONS.MOVE_UP, 0.0f));

			//calculate the acceleration duration and apply it
			float accelerationDuration = AIPlayer.GetJumpFromSourceToDest(mainGraph.topLevelNode.GetNodePosition(currentNode), mainGraph.topLevelNode.GetNodePosition(destinationNode), physEng.gravity);
			if(accelerationDuration < 0) actionPlan.Add(new act_dur((int)ACTIONS.MOVE_LEFT, accelerationDuration * -1));
			else actionPlan.Add(new act_dur((int)ACTIONS.MOVE_RIGHT, accelerationDuration));

			//end the jump by ceasing all player inputs
			actionPlan.Add(new act_dur((int)ACTIONS.DO_NOTHING, 0.0f));
		}

		//use logic similar to stepWalking, move towards the destination X
		//the intent is to approximate the velocity needed to fall in a straight line to the destination
		//this is done by determining if the AI is going faster or slower than the intended line
		//then the AI is accelerated or decelerated towards this line
        private void stepFalling(float dt)
		{
			//note that destination node is determined based on straight line distance
			//this function would be improved if it's destination was chosen from the list of destinations it is capable of falling to
			if (destinationNode == null) return;
			Physics.Vector2D displacement = mainGraph.topLevelNode.GetNodePosition(destinationNode) - AIPlayer.playerBody.Position;

			//Get the direction of the displacement (-1 = dest is to the left)
			float sign = displacement.X / Math.Abs(displacement.X);

			float fallDuration = Physics.SuvatEquations.TfromSUA(displacement.Y, AIPlayer.playerBody.LinearVelocity.Y, physEng.gravity.Y).Item1;
			float currentDisplacement = Physics.SuvatEquations.SfromUAT(AIPlayer.playerBody.LinearVelocity.X, 0.0f, fallDuration) * -1;
			float currentDirectionOfMotion = 1;

			if (AIPlayer.playerBody.LinearVelocity.X != 0) currentDirectionOfMotion = AIPlayer.playerBody.LinearVelocity.X / Math.Abs(AIPlayer.playerBody.LinearVelocity.X);
			currentDisplacement *= currentDirectionOfMotion;
						
			
			//If the displacement with acceleration results in undershooting, then accelerate
			if(displacement.X * sign > currentDisplacement * sign)
			{
				if (sign < 0) actionPlan.Add(new act_dur((int)ACTIONS.MOVE_LEFT, 0.0f));
				if (sign > 0) actionPlan.Add(new act_dur((int)ACTIONS.MOVE_RIGHT, 0.0f));
			}
			//else decelerate
			else
			{
				if (sign > 0) actionPlan.Add(new act_dur((int)ACTIONS.MOVE_LEFT, 0.0f));
				if (sign < 0) actionPlan.Add(new act_dur((int)ACTIONS.MOVE_RIGHT, 0.0f));
			}
		}




		//The following methods are used to determine if the AI could be in a given state

		//only start waiting if there is no valid destination node
		private bool CanBecomeWaiting()
		{
			return destinationNode == null;
		}

		//walking is a possible state if the current node and destination node are on the same rigidbody
		private bool CanBecomeWalking()
		{
			if (currentNode == null) return false;
			if (destinationNode == null) return false;

			return currentNode.index.GetParentIndex() == destinationNode.index.GetParentIndex();
		}

		//jumping is only possible if the player is colliding with something and the jump arc is allowed by the physics engine
		private bool CanBecomeJumping()
		{
			if (currentNode == null) return false;
			if (destinationNode == null) return false;

			//can't jump if the player isn't colliding with anything
			if (AIPlayer.playerBody.collisionNormal == null) return false;

			//can only jump if the jump path from the start to the end is possible
			if (physEng.CanPlayerJumpFromTo(AIPlayer, AIPlayer.playerBody.Position, mainGraph.topLevelNode.GetNodePosition(destinationNode))) return true;

			//if in doubt, no jumping
			return false;			
		}

		//falling is only possible if the AI is not colliding with anything, or it is moving down regardless of a collision
		private bool CanBecomeFalling()
		{
			//if there is no current node then the AI can't be on a rigidbody, and hence must be falling
			if (currentNode == null) return true;
			if (destinationNode == null) return false;

			//if the AI isn't colliding with anything then it must be falling
			if (AIPlayer.playerBody.collisionNormal == null) return true;

			//if the AI is travelling down at speed then it is probably falling
			if (AIPlayer.playerBody.LinearVelocity.Y > 10) return true;

			//if in doubt, the AI is not falling
			return false;
		}



		//The following methods are used to determine what state the AI should change to given it's current state
		private STATES GetStateGivenWaiting()
		{
			if (CanBecomeWalking()) return STATES.WALKING;
			if (CanBecomeJumping()) return STATES.JUMPING;
			if (CanBecomeFalling()) return STATES.FALLING;

			return STATES.WAITING;
		}

		private STATES GetStateGivenWalking()
		{
			if (CanBecomeWalking()) return STATES.WALKING;

			//prioritise falling. If a node can be fallen to OR jumped to, falling will be faster
			if (CanBecomeFalling()) return STATES.FALLING;
			if (CanBecomeJumping()) return STATES.JUMPING;

			return STATES.WAITING;
		}

		private STATES GetStateGivenJumping()
		{
			//if a jump is still ongoing then stay in the jump state
			if (currentStateCooldown > 0) return STATES.JUMPING;

			//if not then take note the jump has ended and return to waiting
			currentlyJumping = false;
			return STATES.WAITING;
		}

		private STATES GetStateGivenFalling()
		{
			//if falling, continue falling if possible
			if (CanBecomeFalling()) return STATES.FALLING;
			if (CanBecomeWalking()) return STATES.WALKING;

			return STATES.WAITING;
		}
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Physics
{
	//Credit goes to the Game Behaviour Module Team for writing the original ImpulseCollisionResponder class
	//This version has been extended to include the collision resolution code and to handle special case collisions
	public class ImpulseCollisionResponder
    {
        private List<CollisionPairData> mCollisionPairs;

        public ImpulseCollisionResponder()
        {
            mCollisionPairs = new List<CollisionPairData>();
        }

        public void AddCollisionPair(CollisionPairData pair)
        {
            RigidBody A = pair.BodyA;
            RigidBody B = pair.BodyB;

            //Only add the new pair if it is not already present in the list (avoid resolving the same collision twice)
            foreach (var p in mCollisionPairs)
            {
                if ((Object.ReferenceEquals(A,p.BodyA) && Object.ReferenceEquals(B,p.BodyB))||(Object.ReferenceEquals(A,p.BodyB) && Object.ReferenceEquals(B,p.BodyA)))
                {
                    return;
                }
            }

            mCollisionPairs.Add(pair);       
        }

        public void Resolve(CollisionPairData data)
        {
			//if neither object reacts to force then neither should be affected by the collision
			if (!data.BodyA.reactToForce && !data.BodyB.reactToForce) return;
			
			
			//COLLISION RESOLUTION


			//Credit goes to http://www.randygaul.net/2013/03/27/game-physics-engine-part-1-impulse-resolution/ for many of the algorithms used in this method
			//Calculate and resolve the collision described by the CollisionPairData
			Vector2D combinedVelocity = data.BodyA.LinearVelocity - data.BodyB.LinearVelocity;

			//normalise the contact normal
			data.ContactNormal = data.ContactNormal.Normalised();

			float combinedVelInNormal = combinedVelocity.Dot(data.ContactNormal);

			//Do nothing if the objects are moving away from each other
			if (combinedVelInNormal > 0) return;


			float elasticity = Math.Min(data.BodyA.elasticity, data.BodyB.elasticity);
			float invMassA = 1 / data.BodyA.Mass;
			float invMassB = 1 / data.BodyB.Mass;
			//if either object doesn't react to collisions, treat its mass as infinite
			if (!data.BodyA.reactToForce) invMassA = 0;
			if (!data.BodyB.reactToForce) invMassB = 0;


			//Calculate the impulse
			float impulseN = -1 * (1 + elasticity) * combinedVelInNormal;
			impulseN /= invMassA + invMassB;

			//Calculate the new velocities for both bodies
			Vector2D newAVelocity = data.BodyA.LinearVelocity + data.ContactNormal * impulseN * invMassA;
			Vector2D newBVelocity = data.BodyB.LinearVelocity - data.ContactNormal * impulseN * invMassB;

			data.BodyA.collisionNormal = data.ContactNormal;
			data.BodyA.otherBody = data.BodyB;
			data.BodyB.collisionNormal = data.ContactNormal * -1;
			data.BodyB.otherBody = data.BodyA;

			//Calculate the amount each body needs to move away from the other to be properly separated

			float penetrationAllowance = 0.01f;
			float percentageToMove = 1f;

			float distanceToMove = Math.Max(data.BodyA.Shape.GetCollisionPenDepth(data.BodyB.Shape, data.BodyA.Position, data.BodyB.Position) - penetrationAllowance, 0.0f) * percentageToMove;


			Vector2D displacement = data.ContactNormal * distanceToMove;


			//React differently if either body is static
			if (data.BodyA.reactToForce && data.BodyB.reactToForce)
			{
				//Move each body away from the other by a percentage of the calculated penetration depth
				//This percentage is determined by the ratio between the bodies masses
				data.BodyA.LinearVelocity = newAVelocity;
				data.BodyA.SetPosition(data.BodyA.Position + (displacement * data.BodyB.Mass / (data.BodyA.Mass + data.BodyB.Mass)));

				data.BodyB.LinearVelocity = newBVelocity;
				data.BodyB.SetPosition(data.BodyB.Position - (displacement * data.BodyA.Mass / (data.BodyA.Mass + data.BodyB.Mass)));
			}
			else if (!data.BodyA.reactToForce)
			{
				//reflect the velocity in the normal (reflection = direction - 2(direction.normal) * normal)
				data.BodyB.LinearVelocity = newBVelocity;
				data.BodyB.SetPosition(data.BodyB.Position - displacement);
			}
			else if (!data.BodyB.reactToForce)
			{
				//reflect the velocity in the normal (reflection = direction - 2(direction.normal) * normal)
				data.BodyA.LinearVelocity = newAVelocity;
				data.BodyA.SetPosition(data.BodyA.Position + displacement);
			}




			//FRICTION RESOLUTION

			//apply friction to each of the two colliding bodies
			//using coulomb friction as described in https://gamedevelopment.tutsplus.com/tutorials/how-to-create-a-custom-2d-physics-engine-friction-scene-and-jump-table--gamedev-7756

			float staticFrictionA = data.BodyA.staticFriction, staticFrictionB = data.BodyB.staticFriction;
			float dynamicFrictionA = data.BodyA.dynamicFriction, dynamicFrictionB = data.BodyB.dynamicFriction;


			Vector2D tangent = combinedVelocity - data.ContactNormal * combinedVelInNormal;
			tangent = tangent.Normalised();

			//Calculate the impulse
			float impulseT = -1 * combinedVelocity.Dot(tangent);
			impulseT /= invMassA + invMassB;


			float mu = (float)Math.Sqrt(staticFrictionA * staticFrictionA + staticFrictionB * staticFrictionB);
			Vector2D frictionImpulse;
			if (Math.Abs(impulseT) < impulseN * mu)
			{
				frictionImpulse = tangent * impulseT;
			}
			else
			{
				float dynamicFriction = (float)Math.Sqrt(dynamicFrictionA * dynamicFrictionA + dynamicFrictionB * dynamicFrictionB);
				frictionImpulse = tangent * -1 * impulseN * dynamicFriction;
			}

			data.BodyA.LinearVelocity = data.BodyA.LinearVelocity + frictionImpulse * invMassA;

			data.BodyB.LinearVelocity = data.BodyB.LinearVelocity - frictionImpulse * invMassB;
		}

		//resolve all collisions in the list
        public void ResolveAllPairs(Player thePlayerCharacter)
        {
            foreach(var p in mCollisionPairs)
            {
                Resolve(p);

				//specify special collision conditions
				//This method should really be replacing with something more expandable, but works for now (due to the low number of unique collision actions)
                if ((p.BodyA.type == RBTypes.PLAYER && p.BodyB.type == RBTypes.ENEMY) ||
                    (p.BodyB.type == RBTypes.PLAYER && p.BodyA.type == RBTypes.ENEMY))
                {
                    thePlayerCharacter.CollideWithEnemy();
                }
                else if ((p.BodyA.type == RBTypes.PLAYER && p.BodyB.type == RBTypes.GOAL) ||
                    (p.BodyB.type == RBTypes.PLAYER && p.BodyA.type == RBTypes.GOAL))
                {
                    thePlayerCharacter.CollideWithGoal();
                }
				else if ((p.BodyA.type == RBTypes.ENEMY && p.BodyB.type == RBTypes.PLAYER_NET) ||
					(p.BodyB.type == RBTypes.ENEMY && p.BodyA.type == RBTypes.PLAYER_NET))
				{
					float netCooldownTime = 1.0f;
					if (p.BodyA.type == RBTypes.ENEMY && !(p.BodyA.multiUseCooldown > 0.0f))
					{
						((Physics.Circle)p.BodyA.Shape).Radius -= 0.5f;
						p.BodyA.multiUseCooldown = netCooldownTime;
					}
					else if (p.BodyB.type == RBTypes.ENEMY && !(p.BodyB.multiUseCooldown > 0.0f))
					{
						((Physics.Circle)p.BodyB.Shape).Radius -= 0.5f;
						p.BodyB.multiUseCooldown = netCooldownTime;
					}
				}
			}

            mCollisionPairs.Clear();
        }
    }
}

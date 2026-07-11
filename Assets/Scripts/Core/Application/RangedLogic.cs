using System;

namespace PrismIsland.Application
{
    public class RangedLogic : IEnemyBehaviorLogic
    {
        private float aggroRange;
        private float shootRange;
        private float shootPrepTime;
        private float retreatTime;
        private float postDashCooldown;

        public RangedLogic(float aggro, float sRange, float sPrep, float rTime, float postCool)
        {
            aggroRange = aggro;
            shootRange = sRange;
            shootPrepTime = sPrep;
            retreatTime = rTime;
            postDashCooldown = postCool;
        }

        public void Update(EnemyBrain brain, float deltaTime, float distanceToPlayer, bool hasTarget)
        {
            if (!hasTarget)
            {
                if (brain.CurrentState != EnemyState.Wandering)
                {
                    brain.ChangeState(EnemyState.Wandering);
                }
                return;
            }

            switch (brain.CurrentState)
            {
                case EnemyState.Wandering:
                    if (distanceToPlayer <= aggroRange)
                    {
                        brain.ChangeState(EnemyState.Chasing);
                    }
                    break;

                case EnemyState.Chasing:
                    if (distanceToPlayer <= shootRange)
                    {
                        brain.ChangeState(EnemyState.PreparingShoot, shootPrepTime);
                    }
                    break;

                case EnemyState.PreparingShoot:
                    brain.StateTimer -= deltaTime;
                    if (brain.StateTimer <= 0f)
                    {
                        brain.RequestShoot();
                        brain.ChangeState(EnemyState.Retreating, retreatTime);
                    }
                    break;

                case EnemyState.Retreating:
                    brain.StateTimer -= deltaTime;
                    if (brain.StateTimer <= 0f)
                    {
                        brain.ChangeState(EnemyState.PostDashCooldown, postDashCooldown);
                    }
                    break;

                case EnemyState.PostDashCooldown:
                    brain.StateTimer -= deltaTime;
                    if (brain.StateTimer <= 0f)
                    {
                        brain.ChangeState(EnemyState.Chasing);
                    }
                    break;
            }
        }
    }
}

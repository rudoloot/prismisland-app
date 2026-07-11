using System;

namespace PrismIsland.Application
{
    public class DasherLogic : IEnemyBehaviorLogic
    {
        private float aggroRange;
        private float dashRange;
        private float dashPrepTime;
        private float dashDuration;
        private float postDashCooldown;

        public DasherLogic(float aggro, float dRange, float dPrep, float dDur, float dCool)
        {
            aggroRange = aggro;
            dashRange = dRange;
            dashPrepTime = dPrep;
            dashDuration = dDur;
            postDashCooldown = dCool;
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
                    if (distanceToPlayer <= dashRange)
                    {
                        brain.ChangeState(EnemyState.PreparingDash, dashPrepTime);
                    }
                    break;

                case EnemyState.PreparingDash:
                    brain.StateTimer -= deltaTime;
                    if (brain.StateTimer <= 0f)
                    {
                        brain.ChangeState(EnemyState.Dashing, dashDuration);
                    }
                    break;

                case EnemyState.Dashing:
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

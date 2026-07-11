using System;

namespace PrismIsland.Application
{
    public interface IEnemyBehaviorLogic
    {
        void Update(EnemyBrain brain, float deltaTime, float distanceToPlayer, bool hasTarget);
    }
}

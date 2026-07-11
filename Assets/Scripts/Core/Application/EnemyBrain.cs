using System;

namespace PrismIsland.Application
{
    public enum EnemyState
    {
        Wandering,
        Chasing,
        PreparingDash,
        Dashing,
        PostDashCooldown,
        PreparingShoot,
        Retreating
    }

    public enum EnemyBehavior
    {
        Dasher,
        Ranged
    }

    public class EnemyBrain
    {
        public EnemyState CurrentState { get; private set; }
        public float StateTimer { get; set; }

        private IEnemyBehaviorLogic logic;

        public event Action<EnemyState> OnStateChanged;
        public event Action OnShootRequested;

        public EnemyBrain(IEnemyBehaviorLogic behaviorLogic)
        {
            logic = behaviorLogic;
            CurrentState = EnemyState.Wandering;
        }

        public void SetLogic(IEnemyBehaviorLogic newLogic)
        {
            logic = newLogic;
        }

        public void ChangeState(EnemyState newState, float timer = 0f)
        {
            CurrentState = newState;
            StateTimer = timer;
            OnStateChanged?.Invoke(newState);
        }

        public void RequestShoot()
        {
            OnShootRequested?.Invoke();
        }

        public void Update(float deltaTime, float distanceToPlayer, bool hasTarget)
        {
            if (logic != null)
            {
                logic.Update(this, deltaTime, distanceToPlayer, hasTarget);
            }
        }
    }
}

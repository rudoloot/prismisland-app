using System;

namespace PrismIsland.Domain
{
    public class PlayerStatsModel
    {
        public int CurrentLevel { get; private set; }
        public float CurrentExp { get; private set; }
        public float ExpToNextLevel { get; private set; }
        public int StatPoints { get; set; }
        public int MaxLevel { get; private set; }

        public int Strength { get; set; }
        public int Intelligence { get; set; }
        public int Agility { get; set; }
        public int Vitality { get; set; }
        public int Charisma { get; set; }
        public int Luck { get; set; }

        public float MaxHealth { get; private set; }
        public float CurrentHealth { get; private set; }

        public bool IsDead => CurrentHealth <= 0;

        // Events
        public event Action OnHealthChanged;
        public event Action OnExpChanged;
        public event Action<int> OnLevelUp;
        public event Action OnDied;

        public PlayerStatsModel(float maxHealth = 100f, int maxLevel = 80, float baseExp = 50f)
        {
            CurrentLevel = 1;
            CurrentExp = 0f;
            ExpToNextLevel = baseExp;
            MaxLevel = maxLevel;
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;

            Strength = 10;
            Intelligence = 10;
            Agility = 10;
            Vitality = 10;
            Charisma = 10;
            Luck = 10;
            StatPoints = 0;
        }

        public void TakeDamage(float amount)
        {
            if (IsDead) return;

            CurrentHealth -= amount;
            if (CurrentHealth <= 0)
            {
                CurrentHealth = 0;
                OnHealthChanged?.Invoke();
                OnDied?.Invoke();
            }
            else
            {
                OnHealthChanged?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            if (IsDead) return;

            CurrentHealth += amount;
            if (CurrentHealth > MaxHealth) CurrentHealth = MaxHealth;
            OnHealthChanged?.Invoke();
        }

        public void AddExp(float amount)
        {
            if (CurrentLevel >= MaxLevel) return;

            CurrentExp += amount;

            while (CurrentExp >= ExpToNextLevel && CurrentLevel < MaxLevel)
            {
                CurrentExp -= ExpToNextLevel;
                CurrentLevel++;
                StatPoints++;
                ExpToNextLevel *= 1.1f; // 10% increase per level
                OnLevelUp?.Invoke(CurrentLevel);
            }
            OnExpChanged?.Invoke();
        }

        public void IncreaseStat(StatType stat)
        {
            if (StatPoints <= 0) return;
            StatPoints--;
            
            switch (stat)
            {
                case StatType.Strength: Strength++; break;
                case StatType.Intelligence: Intelligence++; break;
                case StatType.Agility: Agility++; break;
                case StatType.Vitality: Vitality++; break;
                case StatType.Charisma: Charisma++; break;
                case StatType.Luck: Luck++; break;
            }
            // Trigger an event for UI update if needed
            OnExpChanged?.Invoke(); // Reusing this or adding OnStatsChanged
        }
    }

    public enum StatType
    {
        Strength, Intelligence, Agility, Vitality, Charisma, Luck
    }
}

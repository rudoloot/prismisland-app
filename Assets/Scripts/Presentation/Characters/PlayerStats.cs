using UnityEngine;
using PrismIsland.Domain;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    public PlayerStatsModel Model { get; private set; }

    [Header("Initial Settings")]
    public float maxHealth = 100f;
    public int maxLevel = 80;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Initialize pure C# model
        Model = new PlayerStatsModel(maxHealth, maxLevel, 50f);
        
        Model.OnDied += HandleDeath;
    }

    void OnDestroy()
    {
        if (Model != null)
        {
            Model.OnDied -= HandleDeath;
        }
    }

    // Facade methods for Unity systems that still rely on PlayerStats Component directly
    public float currentHealth => Model.CurrentHealth;
    public float currentExp => Model.CurrentExp;
    public int currentLevel => Model.CurrentLevel;
    public float expToNextLevel => Model.ExpToNextLevel;
    public int statPoints => Model.StatPoints;
    public int strength => Model.Strength;
    public int intelligence => Model.Intelligence;
    public int agility => Model.Agility;
    public int vitality => Model.Vitality;
    public int charisma => Model.Charisma;
    public int luck => Model.Luck;

    public void TakeDamage(float amount)
    {
        Model.TakeDamage(amount);
        Debug.Log($"Player took {amount} damage! Current HP: {Model.CurrentHealth}");
    }

    public void AddExp(float amount)
    {
        Model.AddExp(amount);
    }

    private void HandleDeath()
    {
        Debug.Log("Player Died! Game Over.");
        // TODO: Handle game over state, stop time, show UI, etc.
    }
}

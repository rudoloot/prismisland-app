using UnityEngine;
using System;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [Header("Time Settings")]
    public float realSecondsPerHour = 30f; 
    public int currentDay = 1;
    public int currentHour = 10; // Start at 10 AM

    public float currentTimeOfDay; // 0.0 to 24.0

    public Action OnNewHour;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        currentTimeOfDay = currentHour;
    }

    void Update()
    {
        float timeIncrement = (Time.deltaTime / realSecondsPerHour);
        currentTimeOfDay += timeIncrement;

        if (currentTimeOfDay >= 24f)
        {
            currentTimeOfDay -= 24f;
            currentDay++;
        }

        int newHour = Mathf.FloorToInt(currentTimeOfDay);
        if (newHour != currentHour)
        {
            currentHour = newHour;
            Debug.Log($"New Hour! It is now {currentHour}:00, Day {currentDay}");
            OnNewHour?.Invoke();
        }
    }
}

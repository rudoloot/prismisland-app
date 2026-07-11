using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    public Light sunLight;
    public Light moonLight;

    [Header("Intensity Settings")]
    public float sunMaxIntensity = 2.0f; // 더 화사하게 상향
    public float moonMaxIntensity = 0.4f;

    void Update()
    {
        if (TimeManager.Instance == null || sunLight == null || moonLight == null) return;

        float timeOfDay = TimeManager.Instance.currentTimeOfDay; 

        float sunriseHour = 6f;
        float sunsetHour = 22f; // 10 PM
        
        float angle = 0f;
        
        if (timeOfDay >= sunriseHour && timeOfDay <= sunsetHour)
        {
            float dayLength = sunsetHour - sunriseHour;
            float progress = (timeOfDay - sunriseHour) / dayLength;
            angle = progress * 180f; // 0 to 180 degrees
        }
        else
        {
            float nightTime = timeOfDay;
            if (nightTime < sunriseHour) nightTime += 24f; 
            
            float nightLength = (sunriseHour + 24f) - sunsetHour; 
            float progress = (nightTime - sunsetHour) / nightLength;
            angle = 180f + progress * 180f; // 180 to 360 degrees
        }

        sunLight.transform.rotation = Quaternion.Euler(angle, -90f, 0f);
        moonLight.transform.rotation = Quaternion.Euler(angle + 180f, -90f, 0f);

        float normalizedAngle = angle % 360f;
        if (normalizedAngle < 0f) normalizedAngle += 360f;

        if (normalizedAngle >= 0f && normalizedAngle <= 180f)
        {
            // Day
            // 밝기가 아주 빠르게 오르도록 조정 (아침 8시~9시경 이미 최고 밝기 도달)
            float intensityMultiplier = Mathf.Clamp01(Mathf.Sin(normalizedAngle * Mathf.Deg2Rad) * 2f);
            sunLight.intensity = sunMaxIntensity * intensityMultiplier;
            moonLight.intensity = 0f;
            sunLight.enabled = true;
            moonLight.enabled = false;

            // 해 뜰 때/질 때(0도, 180도 부근)는 강렬한 주황색, 조금만 올라와도 완전한 화창한 낮 색상
            Color sunRiseColor = new Color(1.0f, 0.4f, 0.05f); // 더 짙은 노을/일출색
            Color sunDayColor = new Color(1.0f, 0.98f, 0.95f); // 깨끗하고 화사한 아침빛
            
            float colorLerp = Mathf.Clamp01(Mathf.Sin(normalizedAngle * Mathf.Deg2Rad) * 2.5f);
            sunLight.color = Color.Lerp(sunRiseColor, sunDayColor, colorLerp);
            
            // 환경광(그림자 밝기)도 더 화창하게 조정
            RenderSettings.ambientLight = Color.Lerp(new Color(0.4f, 0.2f, 0.1f), new Color(0.8f, 0.8f, 0.85f), colorLerp);
        }
        else
        {
            // Night
            float intensityMultiplier = Mathf.Sin((normalizedAngle - 180f) * Mathf.Deg2Rad);
            sunLight.intensity = 0f;
            moonLight.intensity = moonMaxIntensity * intensityMultiplier;
            sunLight.enabled = false;
            moonLight.enabled = true;
            
            moonLight.color = new Color(0.6f, 0.8f, 1.0f); // 푸른 달빛
            RenderSettings.ambientLight = Color.Lerp(new Color(0.1f, 0.1f, 0.15f), new Color(0.2f, 0.2f, 0.25f), intensityMultiplier);
        }
    }
}

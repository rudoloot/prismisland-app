using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;          

    [Header("Position Settings")]
    // 💡 public을 유지하되, 게임 시작 시 에디터에 배치된 상태를 기준으로 자동 계산되게 만들 겁니다.
    public Vector3 offset; 
    
    [Header("Smooth Settings")]
    public float smoothTime = 0.2f;   
    
    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        // ✨ [추가] 게임이 시작되는 순간, 에디터에서 맞춰놓은 카메라와 플레이어 사이의 거리를 자동으로 offset으로 저장합니다!
        if (target != null)
        {
            offset = transform.position - target.position;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }
}
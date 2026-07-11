using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f; // 부드러운 회전 속도 변수 추가

    public bool IsMoving { get; private set; }

    private WeaponController wc;

    void Start()
    {
        wc = GetComponent<WeaponController>();
        CreateVisualBody();
    }

    private void CreateVisualBody()
    {
        Renderer existing = GetComponentInChildren<Renderer>();
        if (existing != null) 
        {
            existing.enabled = true; // 이미 있으면 활성화
            return;
        }

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visual.transform.SetParent(this.transform);
        visual.transform.localPosition = new Vector3(0, 1f, 0); // 캡슐 중심으로 올림
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = new Vector3(1f, 1f, 1f);

        Collider col = visual.GetComponent<Collider>();
        if (col != null) Destroy(col);
    }

    void Update()
    {
        // 1. 키보드 입력 받기 (PC 테스트용) 및 모바일 입력 받기
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        if (PrismIsland.Application.MobileInputManager.Instance != null) {
            Vector2 mobileInput = PrismIsland.Application.MobileInputManager.Instance.MovementInput;
            if (mobileInput.magnitude > 0.1f) {
                moveX = mobileInput.x;
                moveZ = mobileInput.y;
            }
        }

        // 2. 이동 방향 벡터 생성 및 정규화
        Vector3 movement = new Vector3(moveX, 0f, moveZ).normalized;
        
        float currentMoveSpeed = moveSpeed;
        if (InventoryManager.Instance != null && InventoryManager.Instance.TotalWeight > InventoryManager.Instance.MaxWeight) {
            currentMoveSpeed *= 0.5f; // 50% penalty
        }

        IsMoving = movement.magnitude > 0.1f;

        // 3. 캐릭터 이동 처리
        transform.Translate(movement * currentMoveSpeed * Time.deltaTime, Space.World);

        // ✨ [수정] 4. 조준 중이면 적을 바라보고, 아니면 이동 방향을 바라보게 하기
        if (wc != null && wc.CurrentTarget != null)
        {
            Vector3 lookDir = (wc.CurrentTarget.transform.position - transform.position).normalized;
            lookDir.y = 0f; // 평면 회전만 유지
            if (lookDir != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime * 2f); // 적을 볼때는 조금 더 빠르게 회전
            }
        }
        else if (movement != Vector3.zero)
        {
            // 이동 방향을 바라보는 목표 회전값(방향) 계산
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            
            // 현재 회전에서 목표 회전까지 시간에 따라 부드럽게 회전(Slerp)
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}
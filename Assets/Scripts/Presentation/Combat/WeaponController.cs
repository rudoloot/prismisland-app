using UnityEngine;
using PrismIsland.Data;
using System.Collections;

using PrismIsland.Domain;
using PrismIsland.Application;

[System.Serializable]
public class WeaponState
{
    public EquipSlot slot;
    public WeaponDataSO weaponData;
    public WeaponModel model;
    public float stoppedTimer;
    
    public void Initialize(ItemDataSO data)
    {
        weaponData = data as WeaponDataSO;
        if (weaponData != null && weaponData.combatType != CombatType.None)
        {
            model = new WeaponModel(
                weaponData.id, 
                weaponData.maxAmmo, 
                weaponData.attackRate, 
                weaponData.damage, 
                weaponData.attackRange, 
                weaponData.attackAngle,
                weaponData.combatType == PrismIsland.Data.CombatType.Ranged
            );
        }
        else
        {
            model = null;
        }
    }
}

public class WeaponController : MonoBehaviour
{
    [Header("Weapon Settings")]
    public GameObject projectilePrefab;
    
    public WeaponState rightWeapon = new WeaponState { slot = EquipSlot.R_Weapon };
    public WeaponState leftWeapon = new WeaponState { slot = EquipSlot.L_Weapon };

    private PlayerMovement pm;
    public Enemy CurrentTarget { get; private set; }

    void Start()
    {
        pm = GetComponent<PlayerMovement>();
    }

    public void EquipWeapons(WeaponDataSO rWeaponData, WeaponDataSO lWeaponData)
    {
        EquipSingleWeapon(rightWeapon, rWeaponData);
        EquipSingleWeapon(leftWeapon, lWeaponData);
    }
    
    private void EquipSingleWeapon(WeaponState state, WeaponDataSO data)
    {
        state.Initialize(data);
        
        if (state.model != null && state.model.IsRanged)
        {
            // Auto reload on equip if possible
            ItemDataSO bulletItem = ItemDatabase.GetItem("Bullet");
            int available = 0;
            if (bulletItem != null && InventoryManager.Instance != null) {
                available = InventoryManager.Instance.GetItemCount(bulletItem);
            }
            state.model.Update(100f, Time.time, available); // Force instant reload calculation
        }
    }

    void Update()
    {
        Enemy nearestEnemy = FindNearestEnemy();
        CurrentTarget = nearestEnemy;
        
        UpdateWeaponState(rightWeapon, nearestEnemy);
        UpdateWeaponState(leftWeapon, nearestEnemy);
    }
    
    void UpdateWeaponState(WeaponState state, Enemy nearestEnemy)
    {
        if (state.model == null) return;

        int totalBullets = 0;
        ItemDataSO bItem = ItemDatabase.GetItem("Bullet");
        if (bItem != null && InventoryManager.Instance != null) {
            totalBullets = InventoryManager.Instance.GetItemCount(bItem);
        }

        state.model.Update(Time.deltaTime, Time.time, totalBullets);

        if (nearestEnemy != null)
        {
            state.stoppedTimer = 0f;
            if (Vector3.Distance(transform.position, nearestEnemy.transform.position) <= state.model.AttackRange) {
                if (state.model.CanAttack(Time.time))
                {
                    Attack(state, nearestEnemy);
                    state.model.RecordAttack(Time.time);
                }
            }
        }
        else
        {
            if (state.model.IsRanged && pm != null && !pm.IsMoving)
            {
                state.stoppedTimer += Time.deltaTime;
                if (state.model.CurrentAmmo < state.model.MaxAmmo && state.model.CurrentAmmo < totalBullets && state.stoppedTimer >= 1.0f)
                {
                    state.model.StartReload();
                }
            }
            else
            {
                state.stoppedTimer = 0f;
            }
        }
    }
    
    Enemy FindNearestEnemy()
    {
        float maxRange = 0f;
        if (rightWeapon.model != null && rightWeapon.model.AttackRange > maxRange) maxRange = rightWeapon.model.AttackRange;
        if (leftWeapon.model != null && leftWeapon.model.AttackRange > maxRange) maxRange = leftWeapon.model.AttackRange;
        
        if (maxRange == 0f) return null;
        
        Collider[] hits = Physics.OverlapSphere(transform.position, maxRange);
        Enemy nearest = null;
        float shortestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    nearest = enemy;
                }
            }
        }
        return nearest;
    }
    
    void Attack(WeaponState state, Enemy target)
    {
        if (state.model == null) return;

        if (!state.model.IsRanged)
        {
            float sweepAngle = state.model.AttackAngle;
            PrismIsland.Application.CombatSystem.ProcessMeleeAttack(
                transform.position, 
                transform.forward, 
                state.model.AttackRange, 
                sweepAngle, 
                state.model.Damage, 
                (enemy, dir) => {
                    enemy.TakeDamage(state.model.Damage, dir, 12f);
                }
            );

            ShowSweepCone(state.model.AttackRange, sweepAngle);

            if (EquipmentManager.Instance != null) {
                GameObject visual = EquipmentManager.Instance.GetWeaponVisual(state.slot);
                if (visual != null) {
                    StartCoroutine(SweepWeaponRoutine(visual));
                }
            }
        }
        else if (state.model.IsRanged)
        {
            if (projectilePrefab == null) return;
            
            GameObject visual = null;
            if (EquipmentManager.Instance != null) visual = EquipmentManager.Instance.GetWeaponVisual(state.slot);
            
            Transform fp = null;
            if (visual != null) fp = visual.transform.Find("FirePoint");
            
            Vector3 spawnPos = fp != null ? fp.position : (transform.position + Vector3.up * 1f);
            Vector3 direction = (target.transform.position - spawnPos).normalized;
            direction.y = 0f;

            if (state.model.AttackAngle > 0f) {
                float spread = UnityEngine.Random.Range(-state.model.AttackAngle / 2f, state.model.AttackAngle / 2f);
                direction = Quaternion.Euler(0, spread, 0) * direction;
            }

            GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            Projectile pScript = proj.GetComponent<Projectile>();
            if (pScript != null)
            {
                pScript.SetDirection(direction, state.model.Damage, state.model.AttackRange);
                
                // 발사 시점에 인벤토리의 총알 소모
                ItemDataSO bulletItem = ItemDatabase.GetItem("Bullet");
                if (bulletItem != null && InventoryManager.Instance != null) {
                    InventoryManager.Instance.RemoveItem(bulletItem, 1);
                }
            }
        }
    }

    private IEnumerator SweepWeaponRoutine(GameObject weaponVisual)
    {
        float duration = 0.3f;
        float elapsed = 0f;
        
        Quaternion originalRot = weaponVisual.transform.localRotation;
        Quaternion startRot = originalRot * Quaternion.Euler(0f, 70f, 0f);
        Quaternion endRot = originalRot * Quaternion.Euler(0f, -70f, 0f);
        
        weaponVisual.transform.localRotation = startRot;
        
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            weaponVisual.transform.localRotation = Quaternion.Slerp(startRot, endRot, elapsed / duration);
            yield return null;
        }
        
        weaponVisual.transform.localRotation = originalRot;
    }

    private void ShowSweepCone(float radius, float angle)
    {
        GameObject coneObj = new GameObject("SweepCone");
        coneObj.transform.position = transform.position + Vector3.up * 0.1f;
        coneObj.transform.rotation = transform.rotation; 

        MeshFilter mf = coneObj.AddComponent<MeshFilter>();
        MeshRenderer mr = coneObj.AddComponent<MeshRenderer>();
        
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = new Color(1f, 0f, 0f, 0.4f); 
        mr.material = mat;

        Mesh mesh = new Mesh();
        int segments = 10; 
        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero; 

        float currentAngle = -angle / 2f;
        float angleStep = angle / segments;

        for (int i = 0; i <= segments; i++)
        {
            float rad = currentAngle * Mathf.Deg2Rad;
            vertices[i + 1] = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad)) * radius;
            currentAngle += angleStep;
        }

        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mf.mesh = mesh;

        StartCoroutine(FadeAndDestroy(coneObj, mr, 0.2f));
    }

    private IEnumerator FadeAndDestroy(GameObject obj, MeshRenderer mr, float duration)
    {
        float elapsed = 0f;
        Color startColor = mr.material.color;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / duration);
            mr.material.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }
        
        Destroy(obj);
    }
    
    void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
        if (rightWeapon.weaponData != null)
        {
            DrawWeaponRangeGizmo(rightWeapon, Color.red);
        }
        if (leftWeapon.weaponData != null)
        {
            DrawWeaponRangeGizmo(leftWeapon, Color.blue);
        }
#endif
    }

#if UNITY_EDITOR
    private void DrawWeaponRangeGizmo(WeaponState state, Color color)
    {
        UnityEditor.Handles.color = new Color(color.r, color.g, color.b, 0.2f);
        if (state.weaponData.combatType == PrismIsland.Data.CombatType.Ranged)
        {
            UnityEditor.Handles.DrawSolidDisc(transform.position, Vector3.up, state.weaponData.attackRange);
        }
        else
        {
            Vector3 forward = transform.forward;
            float angle = state.weaponData.attackAngle;
            Vector3 leftRay = Quaternion.Euler(0, -angle / 2f, 0) * forward;
            UnityEditor.Handles.DrawSolidArc(transform.position, Vector3.up, leftRay, angle, state.weaponData.attackRange);
        }
    }
#endif
}

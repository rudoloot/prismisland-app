using UnityEngine;
using PrismIsland.Data;
using PrismIsland.Application;
using UnityEngine.AI;
using System.Collections.Generic;

public class Enemy : MonoBehaviour
{
    private NavMeshAgent agent;
    private EnemyBrain brain;

    [Header("Data")]
    public EnemyDataSO enemyData;

    [Header("Stats (Overridden if Data is set)")]
    public float maxHealth = 30f;
    public float currentHealth;
    public float moveSpeed = 3f;
    public float damageToPlayer = 10f;
    public float attackRange = 2f;
    public float damageCooldown = 1f;

    [Header("Aggro & Wander Settings")]
    public float aggroRange = 15f;
    public float wanderRadius = 4f;
    public float wanderSpeed = 1.5f;
    public float wanderWaitTime = 2f;

    private class AttackContext {
        public EnemyAttackConfig config;
        public IEnemyBehaviorLogic logic;
        public float lastAttackTime;
    }
    private List<AttackContext> attackContexts = new List<AttackContext>();
    private AttackContext currentAttack;
    private float lastGlobalAttackTime = 0f;

    private float lastMeleeAttackTime = 0f;

    private Vector3 dashTargetDir;

    private Transform target;
    private float lastDamageTime;
    private Vector3 startPosition;
    private Vector3 wanderTarget;

    private Vector3 knockbackVelocity = Vector3.zero;

    void Start()
    {
        CreateVisualBody();
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = moveSpeed;
        }

        if (enemyData != null)
        {
            Init(enemyData);
        }
        else
        {
            currentHealth = maxHealth;
            ApplyColor(Color.red);
            InitializeBrain();
        }

        startPosition = transform.position;
        SetNewWanderTarget();
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
        }
    }

    public void Init(EnemyDataSO data)
    {
        if (data == null) return;
        enemyData = data;

        maxHealth = data.maxHealth;
        moveSpeed = data.moveSpeed;
        // damage, attackRange, attackRate are now in enemyData.attacks
        
        currentHealth = maxHealth;

        if (agent != null) agent.speed = moveSpeed;

        ApplyColor(data.enemyColor);
        transform.localScale = new Vector3(data.scale, data.scale, data.scale);
        InitializeBrain();
    }

    private void InitializeBrain()
    {
        if (brain != null) {
            brain.OnStateChanged -= HandleStateChanged;
            brain.OnShootRequested -= FireProjectile;
        }

        attackContexts.Clear();
        currentAttack = null;

        if (enemyData != null && enemyData.attacks != null && enemyData.attacks.Count > 0)
        {
            foreach (var config in enemyData.attacks)
            {
                if (config.behaviorData == null) continue;
                IEnemyBehaviorLogic logic = null;
                if (config.behaviorData is DasherBehaviorDataSO dasher)
                {
                    logic = new DasherLogic(enemyData.detectionRange, config.attackRange, dasher.dashPrepTime, dasher.dashDuration, dasher.postDashCooldown);
                }
                else if (config.behaviorData is RangedBehaviorDataSO ranged)
                {
                    logic = new RangedLogic(enemyData.detectionRange, config.attackRange, ranged.shootPrepTime, ranged.retreatTime, 1f);
                }
                
                if (logic != null)
                {
                    attackContexts.Add(new AttackContext { config = config, logic = logic, lastAttackTime = 0f });
                }
            }
        }
        else
        {
            // Fallback for testing without data
            IEnemyBehaviorLogic logic = new DasherLogic(aggroRange, 6f, 0.5f, 0.4f, 1f);
            brain = new EnemyBrain(logic);
            brain.OnStateChanged += HandleStateChanged;
            brain.OnShootRequested += FireProjectile;
            return;
        }

        if (attackContexts.Count > 0) currentAttack = attackContexts[0];

        brain = new EnemyBrain(currentAttack?.logic);
        brain.OnStateChanged += HandleStateChanged;
        brain.OnShootRequested += FireProjectile;
    }

    private void HandleStateChanged(EnemyState newState)
    {
        if (newState == EnemyState.PreparingDash || newState == EnemyState.PreparingShoot)
        {
            if (currentAttack != null) currentAttack.lastAttackTime = Time.time;
            lastGlobalAttackTime = Time.time;
        }

        if (newState == EnemyState.Wandering)
        {
            SetNewWanderTarget();
        }
        else if (newState == EnemyState.PreparingDash)
        {
            if (target != null)
            {
                dashTargetDir = (target.position - transform.position).normalized;
                dashTargetDir.y = 0f;
            }
            if (agent != null && agent.enabled) agent.isStopped = true;
        }
        else if (newState == EnemyState.PreparingShoot)
        {
            if (agent != null && agent.enabled) agent.isStopped = true;
        }
        else if (newState == EnemyState.Retreating)
        {
            if (target != null)
            {
                dashTargetDir = (transform.position - target.position).normalized;
                dashTargetDir.y = 0f;
            }
        }
        else if (newState == EnemyState.PostDashCooldown)
        {
            if (agent != null && agent.enabled) agent.isStopped = true;
        }
    }

    private void CreateVisualBody()
    {
        Renderer existing = GetComponentInChildren<Renderer>();
        if (existing != null) 
        {
            existing.enabled = true; // 기존 렌더러 활성화
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

    private void ApplyColor(Color col)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach(Renderer r in renderers)
        {
            r.enabled = true; // 렌더러 강제 활성화
            if (r.material.HasProperty("_BaseColor"))
                r.material.SetColor("_BaseColor", col);
            else
                r.material.color = col;
        }
    }

    void SetNewWanderTarget()
    {
        Vector2 rand = Random.insideUnitCircle * wanderRadius;
        wanderTarget = startPosition + new Vector3(rand.x, 0f, rand.y);
        if (brain != null && brain.CurrentState == EnemyState.Wandering) {
            brain.StateTimer = wanderWaitTime;
        }
    }

    void Update()
    {
        if (knockbackVelocity.magnitude > 0.1f) {
            if (agent != null && agent.enabled) agent.Move(knockbackVelocity * Time.deltaTime);
            else transform.position += knockbackVelocity * Time.deltaTime;
            
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, Time.deltaTime * 10f);
            
            // 넉백 중에는 잠시 행동 중지 (옵션)
            if (knockbackVelocity.magnitude > 2f) return;
        }

        if (target == null) return;
        if (brain == null) return;

        float distance = Vector3.Distance(transform.position, target.position);
        
        // --- MULTI-ATTACK SELECTION LOGIC ---
        if (brain.CurrentState == EnemyState.Wandering || brain.CurrentState == EnemyState.Chasing)
        {
            float globalCooldown = enemyData != null ? enemyData.globalAttackCooldown : 1f;
            if (Time.time >= lastGlobalAttackTime + globalCooldown)
            {
                foreach (var ctx in attackContexts)
                {
                    float cooldown = ctx.config.attackRate > 0 ? (1f / ctx.config.attackRate) : 0f;
                    if (distance <= ctx.config.attackRange && Time.time >= ctx.lastAttackTime + cooldown)
                    {
                        if (currentAttack != ctx) {
                            currentAttack = ctx;
                            brain.SetLogic(ctx.logic);
                        }
                        break;
                    }
                }
            }
        }
        // ------------------------------------

        float currentMeleeCooldown = 1f;
        float currentDamage = damageToPlayer;
        if (currentAttack != null) {
            currentMeleeCooldown = currentAttack.config.attackRate > 0 ? (1f / currentAttack.config.attackRate) : 0f;
            currentDamage = currentAttack.config.damage;
        }

        if (distance <= 1.5f && Time.time >= lastMeleeAttackTime + currentMeleeCooldown)
        {
            PlayerStats stats = target.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.TakeDamage(currentDamage);
                lastMeleeAttackTime = Time.time;
            }
        }

        brain.Update(Time.deltaTime, distance, true);

        // Movement based on state
        switch (brain.CurrentState)
        {
            case EnemyState.Wandering:
                if (agent != null && agent.enabled)
                {
                    agent.isStopped = false;
                    agent.speed = wanderSpeed;
                    agent.SetDestination(wanderTarget);

                    if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
                    {
                        brain.StateTimer -= Time.deltaTime;
                        if (brain.StateTimer <= 0f) SetNewWanderTarget();
                    }
                }
                else
                {
                    if (Vector3.Distance(transform.position, wanderTarget) > 0.1f)
                    {
                        transform.position = Vector3.MoveTowards(transform.position, wanderTarget, wanderSpeed * Time.deltaTime);
                        transform.LookAt(wanderTarget);
                    }
                    else
                    {
                        brain.StateTimer -= Time.deltaTime;
                        if (brain.StateTimer <= 0f) SetNewWanderTarget();
                    }
                }
                break;

            case EnemyState.Chasing:
                if (agent != null && agent.enabled)
                {
                    agent.isStopped = false;
                    agent.speed = moveSpeed;
                    agent.SetDestination(target.position);
                }
                else
                {
                    transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);
                    transform.LookAt(target);
                }
                break;

            case EnemyState.Dashing:
                if (currentAttack != null && currentAttack.config.behaviorData is DasherBehaviorDataSO dasher)
                {
                    float spd = dasher.dashSpeed;
                    if (agent != null && agent.enabled) agent.Move(dashTargetDir * spd * Time.deltaTime);
                    else transform.position += dashTargetDir * spd * Time.deltaTime;
                }
                break;

            case EnemyState.PreparingShoot:
                transform.LookAt(target);
                break;

            case EnemyState.Retreating:
                if (currentAttack != null && currentAttack.config.behaviorData is RangedBehaviorDataSO ranged)
                {
                    float rSpeed = ranged.retreatSpeed;
                    if (agent != null && agent.enabled) agent.Move(dashTargetDir * rSpeed * Time.deltaTime);
                    else transform.position += dashTargetDir * rSpeed * Time.deltaTime;
                }
                transform.LookAt(target);
                break;
        }
    }

    private void FireProjectile()
    {
        GameObject prefab = null;
        float size = 1f;
        float damage = damageToPlayer;
        float maxDist = 20f;

        if (currentAttack != null) {
            damage = currentAttack.config.damage;
            if (currentAttack.config.behaviorData != null) {
                size = currentAttack.config.behaviorData.hitboxSize;
                if (currentAttack.config.behaviorData is RangedBehaviorDataSO r) {
                    maxDist = currentAttack.config.attackRange * 2f;
                    prefab = r.projectilePrefab;
                }
            }
        }

        GameObject proj;
        if (prefab != null) {
            proj = Instantiate(prefab);
        } else {
            proj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Renderer r = proj.GetComponent<Renderer>();
            if (r != null)
            {
                Material m = new Material(Shader.Find("Sprites/Default"));
                m.color = Color.red;
                r.material = m;
            }
        }
        
        Vector3 spawnPos = transform.position + transform.forward * 0.5f;
        proj.transform.position = spawnPos;

        Vector3 targetCenter = target.position; 
        Vector3 aimDir = (targetCenter - spawnPos).normalized;
        proj.transform.rotation = Quaternion.LookRotation(aimDir);

        proj.transform.localScale = Vector3.one * size * 0.3f;
        
        Collider col = proj.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb == null) rb = proj.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        EnemyProjectile ep = proj.GetComponent<EnemyProjectile>();
        if (ep == null) ep = proj.AddComponent<EnemyProjectile>();
        ep.speed = 4f; 
        ep.damage = damage;
        ep.maxDistance = maxDist;
    }

    public void TakeDamage(float amount, Vector3 knockbackDir = default, float knockbackForce = 0f)
    {
        currentHealth -= amount;
        
        if (knockbackForce > 0f && knockbackDir != Vector3.zero) {
            knockbackVelocity = knockbackDir.normalized * knockbackForce;
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (PlayerStats.Instance != null) {
            PlayerStats.Instance.AddExp(5f);
        }
        
        if (enemyData != null && enemyData.dropTable != null) {
            foreach (var drop in enemyData.dropTable) {
                if (UnityEngine.Random.value <= drop.dropChance) {
                    GameObject prefab = Resources.Load<GameObject>("IronPickup");
                    if (prefab != null && drop.item != null) {
                        GameObject dropObj = Instantiate(prefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
                        dropObj.name = drop.item.itemName + "_Pickup";
                        ItemPickup ip = dropObj.GetComponent<ItemPickup>();
                        if (ip != null) {
                            ip.itemData = drop.item;
                            ip.amount = UnityEngine.Random.Range(drop.minAmount, drop.maxAmount + 1);
                        }
                        Renderer r = dropObj.GetComponent<Renderer>();
                        if (r != null) {
                            if (r.material.HasProperty("_BaseColor"))
                                r.material.SetColor("_BaseColor", drop.item.fallbackColor);
                            else
                                r.material.color = drop.item.fallbackColor;
                        }
                    }
                }
            }
        }
        else 
        {
            Debug.LogWarning("Enemy Data is missing! No items dropped. Please assign EnemyDataSO.");
        }
        
        Destroy(gameObject);
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (Time.time >= lastDamageTime + damageCooldown)
            {
                PlayerStats playerStats = other.GetComponent<PlayerStats>();
                if (playerStats != null)
                {
                    playerStats.TakeDamage(damageToPlayer);
                    lastDamageTime = Time.time;
                }
            }
        }
    }
}

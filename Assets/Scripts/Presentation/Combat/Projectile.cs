using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float speed = 30f;
    public float damage = 10f;
    public float lifeTime = 3f;

    private Vector3 moveDirection;
    private bool isEnemyProjectile;

    public void SetDirection(Vector3 dir, float dmg, float range = 0f, bool isEnemy = false)
    {
        moveDirection = dir.normalized;
        transform.forward = moveDirection;
        damage = dmg;
        isEnemyProjectile = isEnemy;
        
        if (range > 0f) {
            lifeTime = (range * 2f) / speed;
        }
    }

    private Vector3 previousPosition;

    void Start()
    {
        previousPosition = transform.position;
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        Vector3 step = moveDirection * speed * Time.deltaTime;
        
        // 고속 투사체가 빗겨가거나 뚫고 지나가는 것(Tunneling)을 방지하기 위한 SphereCast
        RaycastHit hit;
        if (Physics.SphereCast(previousPosition, 0.25f, moveDirection, out hit, step.magnitude))
        {
            if (isEnemyProjectile)
            {
                PlayerStats player = hit.collider.GetComponentInParent<PlayerStats>();
                if (player != null)
                {
                    player.TakeDamage(damage);
                    Destroy(gameObject);
                    return;
                }
                else if (!hit.collider.isTrigger && hit.collider.GetComponentInParent<Enemy>() == null)
                {
                    transform.position = hit.point;
                    Destroy(gameObject);
                    return;
                }
            }
            else
            {
                Enemy enemy = hit.collider.GetComponentInParent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage, moveDirection, 8f);
                    Destroy(gameObject);
                    return;
                }
                else if (!hit.collider.isTrigger && hit.collider.GetComponentInParent<PlayerStats>() == null)
                {
                    // 플레이어나 다른 트리거가 아닌 벽 등에 맞았을 때 소멸
                    transform.position = hit.point;
                    Destroy(gameObject);
                    return;
                }
            }
        }

        transform.position += step;
        previousPosition = transform.position;
    }

    void OnTriggerEnter(Collider other)
    {
        if (isEnemyProjectile)
        {
            PlayerStats player = other.GetComponentInParent<PlayerStats>();
            if (player != null)
            {
                player.TakeDamage(damage);
                Destroy(gameObject);
            }
        }
        else
        {
            Enemy enemy = other.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage, moveDirection, 8f);
                Destroy(gameObject);
            }
        }
    }
}

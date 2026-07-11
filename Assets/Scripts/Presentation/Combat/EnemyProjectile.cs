using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    public float speed = 4f;
    public float damage = 10f;
    public float maxDistance = 16f;
    
    private Vector3 startPos;
    private Vector3 previousPosition;

    void Start()
    {
        startPos = transform.position;
        previousPosition = transform.position;
        
        // Disable Rigidbody to rely on manual translation and SphereCast
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);
    }

    void Update()
    {
        if (Vector3.Distance(startPos, transform.position) > maxDistance)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 step = transform.forward * speed * Time.deltaTime;
        
        RaycastHit hit;
        // 반경 0.15f 짜리 SphereCast를 쏘아 궤적상의 모든 물체를 완벽하게 검사합니다.
        if (Physics.SphereCast(previousPosition, 0.15f, transform.forward, out hit, step.magnitude))
        {
            PlayerStats stats = hit.collider.GetComponentInParent<PlayerStats>();
            if (stats != null)
            {
                stats.TakeDamage(damage);
                Destroy(gameObject);
                return;
            }
            else if (hit.collider.GetComponentInParent<Enemy>() == null && !hit.collider.isTrigger)
            {
                Destroy(gameObject);
                return;
            }
        }

        transform.position += step;
        previousPosition = transform.position;
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerStats stats = other.GetComponentInParent<PlayerStats>();
        if (stats != null)
        {
            stats.TakeDamage(damage);
            Destroy(gameObject);
        }
        else if (other.GetComponentInParent<Enemy>() == null && !other.isTrigger) 
        {
            Destroy(gameObject);
        }
    }
}

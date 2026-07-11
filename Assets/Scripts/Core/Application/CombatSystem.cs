using System;
using UnityEngine;
using PrismIsland.Domain;

namespace PrismIsland.Application
{
    public class CombatSystem
    {
        public static void ProcessMeleeAttack(Vector3 origin, Vector3 forward, float range, float sweepAngle, float damage, Action<Enemy, Vector3> onHit)
        {
            Collider[] hits = Physics.OverlapSphere(origin, range);
            foreach (Collider hit in hits)
            {
                Enemy enemy = hit.GetComponent<Enemy>();
                if (enemy != null)
                {
                    Vector3 dirToEnemy = (enemy.transform.position - origin).normalized;
                    dirToEnemy.y = 0;

                    float angle = Vector3.Angle(forward, dirToEnemy);
                    if (angle <= sweepAngle / 2f)
                    {
                        onHit?.Invoke(enemy, dirToEnemy);
                    }
                }
            }
        }
    }
}

using UnityEngine;

public class EnemyRange : EnemyBase
{
    [Header("===== 원거리 (탄막) 개별 설정 =====")]
    [Tooltip("인스펙터에서 직접 조절하는 공격 쿨타임")]
    public float rangeAttackCooldown = 1.5f;

    [Tooltip("투사체 날아가는 속도")]
    public float projectileSpeed = 8f;

    [Tooltip("투사체가 발사될 위치")]
    public Transform firePoint;

    [Tooltip("한 번에 발사할 총알 개수")]
    [Range(1, 15)]
    public int projectileCount = 1;

    [Tooltip("총알이 퍼지는 각도")]
    [Range(0f, 90f)]
    public float spreadAngle = 15f;

    private float rangeTimer;

    protected override void Attack()
    {
        // [추가] 공격 사거리 안에서 멈췄을 때 플레이어를 부드럽게 쳐다보게 함
        if (target != null)
        {
            Vector3 lookDir = (target.position - transform.position).normalized;
            lookDir.y = 0; // 몬스터가 위아래로 기우뚱하지 않게 Y축만 회전

            if (lookDir != Vector3.zero)
            {
                // 15f는 회전 속도입니다. 더 빨리 돌게 하고 싶으면 숫자를 키우세요!
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 15f);
            }
        }

        rangeTimer += Time.deltaTime;

        if (rangeTimer >= rangeAttackCooldown)
        {
            anim.SetTrigger("Attack");
            rangeTimer = 0;
        }
    }

    public void FireProjectileEvent()
    {
        if (target == null || firePoint == null) return;

        // 플레이어의 허리 높이 조준
        Vector3 targetPostion = target.position + Vector3.up * 1f;

        // 입(firePoint)에서 타겟을 향하는 대각선 방향 계산
        Vector3 centerDirection = (targetPostion - firePoint.position).normalized;

        float startAngle = -spreadAngle * (projectileCount - 1) / 2f;

        for (int i = 0; i < projectileCount; i++)
        {
            float currentAngle = startAngle + (spreadAngle * i);

            // 기준 방향을 Y축으로 회전시켜 부채꼴 형성
            Vector3 finalDirection = Quaternion.Euler(0, currentAngle, 0) * centerDirection;

            ProjectileManager.Instance.FireProjectile(
                firePoint.position,
                finalDirection,
                projectileSpeed,
                enemyData.damage
            );
        }
    }
}
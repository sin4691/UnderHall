using System.Collections;
using UnityEngine;

public class BossDragon : EnemyBase
{
    [Header("Boss Attack Settings")]
    public float attackCooldown = 3f;

    private bool isAttacking = false;

    protected override void Start()
    {
        base.Start();
        StartCoroutine(BossThinkRoutine());
    }

    // 보스 AI 상태 판단 루틴
    IEnumerator BossThinkRoutine()
    {
        while (!isDead)
        {
            yield return new WaitForSeconds(0.2f);

            if (isAttacking || target == null || !agent.isOnNavMesh) continue;

            float distance = Vector3.Distance(transform.position, target.position);

            if (distance <= attackRange)
            {
                // 사거리 진입 시 즉시 정지 후 공격 시작
                agent.isStopped = true;
                StartCoroutine(ExecuteRandomAttack());
            }
            else
            {
                // 사거리 밖일 경우 타겟 추적 재개
                agent.isStopped = false;
                agent.SetDestination(target.position);
            }
        }
    }

    // 랜덤 공격 패턴 실행
    IEnumerator ExecuteRandomAttack()
    {
        isAttacking = true;
        agent.isStopped = true;

        // 공격 전 타겟 방향 응시
        Vector3 lookPos = new Vector3(target.position.x, transform.position.y, target.position.z);
        transform.LookAt(lookPos);

        int patternIndex = Random.Range(0, 2);

        if (patternIndex == 0)
        {
            anim.SetTrigger("Attack"); // 일반 공격

            //  보스 일반 공격 VFX 실행
            VFXManager.Instance.PlayBossAttack(transform.position, transform.forward);

            yield return new WaitForSeconds(2.0f); // 애니메이션 시간 대기
        }
        else
        {
            anim.SetTrigger("BreatheFire"); // 브레스 공격

            // 보스 브레스 VFX 실행
            VFXManager.Instance.PlayBossBreath(transform.position, transform.forward);

            yield return new WaitForSeconds(3.5f); // 애니메이션 시간 대기
        }

        // 공격 상태 해제 및 이동 재개 준비
        agent.isStopped = false;
        isAttacking = false;

        yield return new WaitForSeconds(attackCooldown); // 다음 공격까지 대기
    }
}
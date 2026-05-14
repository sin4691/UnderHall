using System.Collections;
using UnityEngine;

public class BossDragon : EnemyBase
{
    [Header("Boss Attack Settings")]
    public float attackCooldown = 3f;

    [Header("Phase 2 Settings")]
    public float flyHeight = 5f;
    public float dashSpeed = 35f;
    private bool isPhase2 = false;
    private bool isFlying = false;
    private bool isAttacking = false;

    //[VFX/feat]창우_ Boss 전용 공격 이펙트 포인트 (머리)
    [Header("Boss Bones")]
    [SerializeField] private Transform headBone;

    protected override void Start()
    {
        base.Start();
        StartCoroutine(BossThinkRoutine());
    }

    protected override void Attack() { }

    public override void TakeDamage(float damage)
    {
        if (isDead || isFlying) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die(); // 여기서 즉시 Die 호출
        }
        else
        {
            anim.SetTrigger("Hurt");
            if (vfxPoint != null)
                VFXManager.Instance.PlayMonsterHit(vfxPoint.position, Vector3.up, gameObject);
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true; // 1순위: 사망 플래그부터 세우기

        StopAllCoroutines(); // 2순위: 모든 행동 즉시 정지

        // 3순위: 모든 물리/AI 컴포넌트 즉시 파괴/비활성화
        agent.isStopped = true;
        agent.enabled = false;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // 4순위: 애니메이터 초기화 (공격 트리거가 남아있을 수 있으므로 리셋)
        anim.ResetTrigger("Attack");
        anim.ResetTrigger("BreatheFire");
        anim.SetTrigger("Death");
        anim.SetFloat("MoveSpeed", 0);

        if (vfxPoint != null)
            VFXManager.Instance.PlayMonsterDeath(vfxPoint.position, gameObject);
    }

    IEnumerator BossThinkRoutine()
    {
        while (!isDead) // 죽으면 이 루프 자체가 끝남
        {
            yield return new WaitForSeconds(0.2f);
            if (isDead || isAttacking || target == null || !agent.isOnNavMesh) continue;

            if (!isPhase2 && currentHealth <= enemyData.maxHealth * 0.5f)
                isPhase2 = true;

            float distance = Vector3.Distance(transform.position, target.position);
            if (distance <= attackRange)
            {
                StartCoroutine(ExecuteRandomAttack());
            }
            else
            {
                if (isFlying) continue;
                agent.isStopped = false;
                agent.SetDestination(target.position);
            }
        }
    }

    IEnumerator ExecuteRandomAttack()
    {
        // 시작하자마자 죽었는지 확인 (0.001초 차이 방어)
        if (isDead || isAttacking) yield break;

        isAttacking = true;
        agent.isStopped = true;

        Vector3 lookPos = new Vector3(target.position.x, transform.position.y, target.position.z);
        transform.LookAt(lookPos);

        if (!isFlying && !isDead)
        {
            int maxPattern = isPhase2 ? 3 : 2;
            int patternIndex = Random.Range(0, maxPattern);

            if (patternIndex == 0)
            {
                anim.SetTrigger("Attack");
                VFXManager.Instance.PlayBossAttack(transform.position, transform.forward);
                yield return StartCoroutine(BossSingleHit(0.5f));
            }
            else if (patternIndex == 1)
            {
                anim.SetTrigger("BreatheFire");

                // [VFX/feat]창우_ 보스 불뿜기 패턴 이펙트 재생 (머리에서 앞으로)
                VFXManager.Instance.PlayBossBreath(headBone, transform.forward);

                yield return StartCoroutine(BossBreathHit(0.5f, 3f));
            }
            else if (patternIndex == 2)
            {
                yield return StartCoroutine(DashAndSlamPattern());
            }
        }

        // 공격 시퀀스 끝난 직후에 죽었는지 또 확인!
        if (isDead) yield break;

        yield return new WaitForSeconds(1.0f); // 후딜레이

        isAttacking = false;
        agent.isStopped = false;
        yield return new WaitForSeconds(attackCooldown);
    }

    // --- (이하 패턴 코드는 동일하되 중간중간 isDead 체크 추가) ---

    IEnumerator DashAndSlamPattern()
    {
        if (isDead) yield break;
        isFlying = false;
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        anim.CrossFade("Idle Takeoff", 0.2f);
        yield return StartCoroutine(MoveTowardPlayerWithDamage(0.8f));
        if (isDead) yield break;

        yield return new WaitForSeconds(0.4f);
        if (isDead) yield break;

        anim.CrossFade("Idle Takeoff", 0.2f);
        yield return StartCoroutine(MoveTowardPlayerWithDamage(0.8f));
        if (isDead) yield break;

        if (col != null) col.isTrigger = false;
        isFlying = true;

        while (agent.baseOffset < flyHeight + 3f)
        {
            if (isDead) yield break;
            agent.baseOffset += Time.deltaTime * 12f;
            yield return null;
        }

        anim.CrossFade("FlyDive", 0.1f);
        while (agent.baseOffset > 0)
        {
            if (isDead) yield break;
            agent.baseOffset -= Time.deltaTime * 25f;
            yield return null;
        }

        agent.baseOffset = 0f;
        if (isDead) yield break;

        anim.CrossFade("Idle Landing", 0.1f);
        VFXManager.Instance.PlayBossAttack(transform.position, transform.forward);
        yield return StartCoroutine(BossSingleHit(0.1f));

        yield return new WaitForSeconds(1.5f);
        isFlying = false;
        if (!isDead) anim.CrossFade("Idle", 0.2f);
    }

    // --- (나머지 MoveTowardPlayerWithDamage, BossSingleHit, BossBreathHit는 이전과 동일하나 상단에 if(isDead) yield break; 한 줄씩 추가) ---

    IEnumerator BossSingleHit(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (isDead) yield break; // 죽었는데 때리기 금지
        Vector3 hitPos = transform.position + (transform.forward * 2f) + (Vector3.up * 1.5f);
        Collider[] hits = Physics.OverlapSphere(hitPos, 2f);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                hit.GetComponent<Player>()?.TakeDamage(enemyData.damage);
                break;
            }
        }
    }

    IEnumerator BossBreathHit(float delay, float duration)
    {
        yield return new WaitForSeconds(delay);
        float timer = 0f;
        while (timer < duration)
        {
            if (isDead) yield break; // 죽었는데 불 뿜기 금지
            Vector3 hitPos = transform.position + (transform.forward * 3.5f) + (Vector3.up * 1.5f);
            Collider[] hits = Physics.OverlapSphere(hitPos, 3f);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    hit.GetComponent<Player>()?.TakeDamage(enemyData.damage * 0.5f);
                    break;
                }
            }
            yield return new WaitForSeconds(0.5f);
            timer += 0.5f;
        }
    }

    IEnumerator MoveTowardPlayerWithDamage(float duration)
    {
        float t = 0;
        bool hasHitThisDash = false;
        while (t < duration)
        {
            if (isDead) yield break; // 죽었는데 돌진 금지
            Vector3 dashDir = (target.position - transform.position).normalized;
            if (dashDir != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dashDir), Time.deltaTime * 12f);

            transform.position += transform.forward * dashSpeed * Time.deltaTime;

            if (!hasHitThisDash)
            {
                Collider[] hits = Physics.OverlapSphere(transform.position + Vector3.up, 3f);
                foreach (var hit in hits)
                {
                    if (hit.CompareTag("Player"))
                    {
                        hit.GetComponent<Player>()?.TakeDamage(enemyData.damage);
                        hasHitThisDash = true;
                        break;
                    }
                }
            }
            t += Time.deltaTime;
            yield return null;
        }
    }
}
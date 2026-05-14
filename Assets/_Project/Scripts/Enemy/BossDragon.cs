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
    private bool isCooldown = false;

    // [VFX/feat]창우_ Boss 전용 공격 이펙트 포인트 (머리)
    [Header("Boss Bones")]
    [SerializeField] private Transform headBone;

    [Header("Bite Attack Settings (Red Gizmo)")]
    public float biteHitRadius = 1f;
    public float biteHitOffset = 5f;
    public float biteHitHeight = 1.5f;

    [Header("Breath Settings (Blue Gizmo)")]
    public float breathDuration = 4.5f;
    public float breathHitRadius = 1.5f;
    public float breathHitOffset = 8f;
    public float breathHitHeight = -0.5f;

    // [추가] 물어뜯기 횟 카운트
    private int biteCount = 0;

    protected override void Start()
    {
        base.Start();
        StartCoroutine(BossThinkRoutine());
    }

    // [추가] 부모의 매 프레임 행동(이동, 회전 등)을 통제
    protected override void Update()
    {
        if (isDead) return;

        if(isAttacking)
        {
            agent.velocity = Vector3.zero;
            return;
        }
        // 공격 중이거나 비행 중일 때는 부모의 로직(회전 등)을 완전 무시 (얼음 상태)
        if (isAttacking || isFlying)
        {
            if (agent.enabled)
            {
                agent.updateRotation = false;
                agent.velocity = Vector3.zero;
            }

            anim.SetFloat("MoveSpeed", 0f);

            return;
        }

        if (agent.enabled) agent.updateRotation = true;
        // 공격 중이 아닐 때만 부모의 기본 로직을 실행하여 플레이어를 추적
        base.Update();
    }

    protected override void Attack() { }

    public override void TakeDamage(float damage)
    {
        if (isDead || isFlying) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
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
        isDead = true;

        StopAllCoroutines();
        StartCoroutine(DieRoutine());
    }

    IEnumerator DieRoutine()
    {
        agent.isStopped = true;
        agent.enabled = false;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        anim.ResetTrigger("Attack");
        anim.ResetTrigger("BreatheFire");
        anim.SetTrigger("Death");
        anim.SetFloat("MoveSpeed", 0);

        yield return new WaitForSeconds(3f);

        if (vfxPoint != null)
            VFXManager.Instance.PlayMonsterDeath(vfxPoint.position, gameObject);
        else
            Destroy(gameObject);
    }

    IEnumerator BossThinkRoutine()
    {
        while (!isDead)
        {
            yield return new WaitForSeconds(0.2f);

            if (isDead || isAttacking || isCooldown || target == null || !agent.isOnNavMesh) continue;

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
        if (isDead || isAttacking) yield break;

        isAttacking = true;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // 공격 시작 시 플레이어 방향을 한 번만 딱 바라봄 (이후에는 안 따라감)
        Vector3 lookPos = new Vector3(target.position.x, transform.position.y, target.position.z);
        transform.LookAt(lookPos);

        agent.nextPosition = transform.position;

        if (!isFlying && !isDead)
        {
            if (biteCount < 3)
            {
                // 물어뜯기 공격 (0~2회차)
                anim.SetTrigger("Attack");
                VFXManager.Instance.PlayBossAttack(transform.position, transform.forward);
                yield return StartCoroutine(BossSingleHit(0.5f));
                biteCount++;
            }
            else
            {
                // 3번 물어뜯은 후 (3회차)
                biteCount = 0;

                // 2페이즈라면 브레스 대신 돌진이 나올 확률 반반
                if (isPhase2 && Random.value > 0.5f)
                {
                    yield return StartCoroutine(DashAndSlamPattern());
                }
                else
                {
                    // 브레스 발사
                    anim.SetTrigger("BreatheFire");
                    // 실제 이펙트 재생은 애니메이션 이벤트 OnBreathEffectStart에서 수행
                    yield return StartCoroutine(BossBreathHit(0.5f, breathDuration));
                }
            }
        }

        if (isDead) yield break;
        yield return new WaitForSeconds(1.0f);

        isAttacking = false;
        agent.isStopped = false;

        isCooldown = true;
        yield return new WaitForSeconds(attackCooldown);
        isCooldown = false;
    }

    // 애니메이션 이벤트 함수: 입 벌리는 프레임에 맞춰 호출
    public void OnBreathEffectStart()
    {
        if (isDead || headBone == null) return;

        // [수정 완료] 몸통 방향(transform.forward) 대신 지정된 뼈대 방향(headBone.forward)으로 발사
        VFXManager.Instance.PlayBossBreath(headBone, headBone.forward);
    }

    IEnumerator DashAndSlamPattern()
    {
        if (isDead) yield break;
        isFlying = false;
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        anim.CrossFade("Ground.Idle Takeoff", 0.2f);
        yield return StartCoroutine(MoveTowardPlayerWithDamage(0.8f));
        if (isDead) yield break;

        yield return new WaitForSeconds(0.4f);
        if (isDead) yield break;

        anim.CrossFade("Ground.Idle Takeoff", 0.2f);
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

        anim.CrossFade("Ground.FlyDive", 0.1f);
        while (agent.baseOffset > 0)
        {
            if (isDead) yield break;
            agent.baseOffset -= Time.deltaTime * 25f;
            yield return null;
        }

        agent.baseOffset = 0f;
        if (isDead) yield break;

        anim.CrossFade("Ground.Idle Landing", 0.1f);
        VFXManager.Instance.PlayBossAttack(transform.position, transform.forward);
        yield return StartCoroutine(BossSingleHit(0.1f));

        yield return new WaitForSeconds(1.5f);
        isFlying = false;
        if (!isDead) anim.CrossFade("Ground.Ground Locomotion", 0.2f);
    }

    IEnumerator BossSingleHit(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (isDead) yield break;

        Vector3 hitPos = transform.position + (transform.forward * biteHitOffset) + (Vector3.up * biteHitHeight);
        Collider[] hits = Physics.OverlapSphere(hitPos, biteHitRadius);
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
            if (isDead) yield break;
            if (headBone != null)
            {
                Matrix4x4 matrix = Matrix4x4.TRS(headBone.position, transform.rotation, Vector3.one);

                float halfLength = breathHitOffset / 2f;
                float halfThickness = breathHitRadius / 2f;

                Vector3 boxCenterLocal = new Vector3(0f, breathHitHeight, halfLength);
                Vector3 boxCenterWorld = matrix.MultiplyPoint3x4(boxCenterLocal);

                Vector3 halfExtents = new Vector3(halfThickness, halfThickness, halfLength);
                Quaternion boxRotation = transform.rotation;

                Collider[] hits = Physics.OverlapBox(boxCenterWorld, halfExtents, boxRotation);
                foreach (var hit in hits)
                {
                    if (hit.CompareTag("Player"))
                    {
                        hit.GetComponent<Player>()?.TakeDamage(enemyData.damage * 0.5f);
                    }
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
        Vector3 dashDir = (target.position - transform.position).normalized;
        dashDir.y = 0;
        if (dashDir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dashDir);

        while (t < duration)
        {
            if (isDead) yield break;
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

        if (agent.enabled)
        {
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out hit, 10f, UnityEngine.AI.NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.4f);
        Gizmos.DrawWireSphere(transform.position + (transform.forward * biteHitOffset) + (Vector3.up * biteHitHeight), biteHitRadius);

        if (headBone != null)
        {
            Gizmos.color = new Color(0, 0, 1, 0.4f);
            Matrix4x4 oldGizmoMatrix = Gizmos.matrix;
            Matrix4x4 matrix = Matrix4x4.TRS(headBone.position, transform.rotation, Vector3.one);

            float halfLength = breathHitOffset / 2f;
            Vector3 localCenter = new Vector3(0f, breathHitHeight, halfLength);
            Gizmos.matrix = matrix * Matrix4x4.Translate(localCenter);

            Gizmos.DrawWireCube(Vector3.zero, new Vector3(breathHitRadius, breathHitRadius, breathHitOffset));

            Gizmos.matrix = oldGizmoMatrix;
        }
    }
}
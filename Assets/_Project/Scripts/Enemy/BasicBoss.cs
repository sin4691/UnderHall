using System.Collections;

using UnityEngine;

using UnityEngine.AI;



public class BasicBoss : EnemyBase

{

    [Header("─ 보스 기본 설정 ─")]

    public float attackDamage = 20f;

    public float attackCooldown = 2f;

    public float attackRadius = 3.0f;

    public Transform attackPoint;



    [Header("─ 브레스 패턴 설정 ─")]
    public float breathCooldown = 3.5f;
    public Transform breathPoint;
    public Vector2 boxSize = new Vector2(5f, 10f); // 💡 가로, 세로 크기
    public Vector3 boxOffset = new Vector3(0, 0, 5f); // 💡 보스 앞쪽으로 얼마나 튀어나올지
    public float boxHeight = 2f; // 💡 장판의 높이
    public float closeRangeDamage = 10.0f;
    public GameObject closeRangeVFX;



    private int basicAttackCount = 0;

    private int nextBreathThreshold;

    private bool isAttacking = false;



    [Header("─ 등장 연출 설정 ─")]

    public float startHeight = 15f;

    private bool isAwake = false;

    public float dropSpeed = 30f;

    private Vector3 targetLandingPosition;



    [Header("─ 2페이즈 돌진 패턴 설정 ─")]

    public float dashSpeed = 35f;

    public float dashDuration = 0.5f;

    public float dashCooldown = 1.5f;



    private bool isPhase2 = false;

    private int phase2AttackIndex = 0;

    private bool isPhaseTransitioning = false;



    protected override void Start()

    {

        base.Start();

        transform.position += new Vector3(0, startHeight, 0);

        agent.enabled = false;

        SetNextBreathThreshold();

        if (closeRangeVFX != null) closeRangeVFX.SetActive(false);

        StartCoroutine(BossThinkRoutine());

    }



    protected override void Update()

    {

        if (isDead) return;



        if (!isAwake)

        {

            if (target != null && enemyData != null && Vector3.Distance(transform.position, target.position) <= enemyData.detectionRange)

            {

                isAwake = true;

                targetLandingPosition = target.position + (target.forward * 10.0f);

                StartCoroutine(DropDownRoutine());

            }

            return;

        }



        if (isAttacking || isPhaseTransitioning)

        {

            if (agent.enabled) { agent.isStopped = true; agent.velocity = Vector3.zero; }

            anim.SetFloat("MoveSpeed", 0f);

            return;

        }



        if (target != null && agent.enabled && !agent.isStopped)

        {

            Vector3 lookDir = target.position - transform.position;

            lookDir.y = 0;

            if (lookDir != Vector3.zero)

                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 15f);

            anim.SetFloat("MoveSpeed", agent.velocity.magnitude);

        }

        else anim.SetFloat("MoveSpeed", 0f);

    }



    public override void TakeDamage(float damage)

    {

        if (isDead || isPhaseTransitioning) return;

        base.TakeDamage(damage);

        if (!isPhase2 && enemyData != null && currentHealth <= enemyData.maxHealth * 0.5f) StartCoroutine(Phase2TransitionRoutine());

    }



    IEnumerator Phase2TransitionRoutine()

    {

        isPhase2 = true; isPhaseTransitioning = true; isAttacking = true;

        if (agent.enabled) { agent.isStopped = true; agent.velocity = Vector3.zero; }

        anim.SetTrigger("Attack");

        yield return new WaitForSeconds(2.0f);

        isAttacking = false; isPhaseTransitioning = false;

    }



    IEnumerator BossThinkRoutine()

    {

        while (!isDead)

        {

            yield return new WaitForSeconds(0.2f);

            if (!isAwake || isAttacking || isPhaseTransitioning || target == null || !agent.isOnNavMesh) continue;



            if (Vector3.Distance(transform.position, target.position) <= attackRange)

            {

                if (basicAttackCount >= nextBreathThreshold)

                {

                    if (isPhase2) StartCoroutine(ExecutePhase2Pattern());

                    else StartCoroutine(BreathAttackRoutine());

                }

                else StartCoroutine(AttackRoutine());

            }

            else { agent.isStopped = false; agent.SetDestination(target.position); }

        }

    }



    IEnumerator ExecutePhase2Pattern()

    {

        if (phase2AttackIndex == 0) { yield return StartCoroutine(GroundDashRoutine()); phase2AttackIndex = 1; }

        else { yield return StartCoroutine(BreathAttackRoutine()); phase2AttackIndex = 0; }

    }



    IEnumerator DropDownRoutine()

    {

        SetGhostMode(true);

        while (Vector3.Distance(transform.position, targetLandingPosition) > 0.1f)

        {

            transform.position = Vector3.MoveTowards(transform.position, targetLandingPosition, dropSpeed * Time.deltaTime);

            yield return null;

        }

        transform.position = targetLandingPosition;

        anim.SetTrigger("Land");

        yield return new WaitForSeconds(2.0f);

        SetGhostMode(false); agent.enabled = true;

    }



    IEnumerator AttackRoutine()

    {

        isAttacking = true;

        agent.isStopped = true; agent.velocity = Vector3.zero;

        StartCoroutine(SmoothFaceTarget(0.5f, 15f));

        anim.SetTrigger("Attack");

        basicAttackCount++;

        yield return new WaitForSeconds(1.0f + attackCooldown);

        isAttacking = false;

    }



    IEnumerator BreathAttackRoutine()
    {
        isAttacking = true;
        agent.isStopped = true; agent.velocity = Vector3.zero;
        anim.SetTrigger("Breath");
        StartCoroutine(SmoothFaceTarget(4.0f, 2.0f));

        yield return new WaitForSeconds(1f);

        if (closeRangeVFX != null) { closeRangeVFX.SetActive(true); }

        float timer = 0f;
        float totalDuration = 6.0f;

        Vector3 boxCenter = transform.position + transform.rotation * boxOffset;
        Vector3 halfExtents = new Vector3(boxSize.x / 2, boxHeight / 2, boxSize.y / 2);

        while (timer < totalDuration)
        {
            if (isDead) yield break;

            Collider[] hits = Physics.OverlapBox(boxCenter, halfExtents, transform.rotation);
            foreach (Collider hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    hit.GetComponent<Player>()?.TakeDamage(closeRangeDamage);
                }
            }

            yield return new WaitForSeconds(0.2f);
            timer += 0.2f;
        }

        if (closeRangeVFX != null) closeRangeVFX.SetActive(false);

        basicAttackCount = 0;
        SetNextBreathThreshold();
        yield return new WaitForSeconds(breathCooldown);
        isAttacking = false;
    }



    IEnumerator GroundDashRoutine()

    {

        isAttacking = true;

        if (agent.enabled) { agent.isStopped = true; agent.velocity = Vector3.zero; }

        SetGhostMode(true);



        anim.SetTrigger("FlyFast");

        yield return StartCoroutine(MoveTowardPlayerWithDamage(dashDuration));

        yield return new WaitForSeconds(0.2f);



        if (!isDead)

        {

            anim.SetTrigger("FlyFast");

            yield return StartCoroutine(MoveTowardPlayerWithDamage(dashDuration));

        }



        yield return new WaitForSeconds(dashCooldown);

        SetGhostMode(false);

        if (agent.enabled) agent.isStopped = false;

        anim.CrossFade("Idle", 0.1f);

        basicAttackCount = 0; SetNextBreathThreshold();

        isAttacking = false;

    }



    // 💡 변경된 돌진 로직: 정지 조건 없이 시간 동안 쭉 직진

    IEnumerator MoveTowardPlayerWithDamage(float duration)

    {

        float t = 0;

        bool hasHitThisDash = false;

        if (target == null) yield break;



        Vector3 dashDir = (target.position - transform.position).normalized;

        dashDir.y = 0;

        transform.rotation = Quaternion.LookRotation(dashDir);



        while (t < duration)

        {

            if (isDead) yield break;



            // 멈춤 로직(stopDistance 등)을 삭제하여 맵 끝까지 돌진

            transform.position += transform.forward * dashSpeed * Time.deltaTime;



            if (!hasHitThisDash)

            {

                Collider[] hits = Physics.OverlapSphere(transform.position + Vector3.up, 2.5f);

                foreach (Collider hitCol in hits)

                {

                    if (hitCol.CompareTag("Player"))

                    {

                        hitCol.GetComponent<Player>()?.TakeDamage(20.0f);
                        hasHitThisDash = true;
                        break; ;

                    }

                }

            }

            t += Time.deltaTime; yield return null;

        }

    }



    private void SetGhostMode(bool isGhost)

    {

        if (target == null) return;

        Collider[] bossCols = GetComponentsInChildren<Collider>();

        Collider[] playerCols = target.GetComponentsInChildren<Collider>();

        foreach (Collider bCol in bossCols)

            foreach (Collider pCol in playerCols)

                if (bCol != null && pCol != null) Physics.IgnoreCollision(bCol, pCol, isGhost);

    }



    IEnumerator SmoothFaceTarget(float duration, float rotationSpeed)

    {

        float timer = 0f;

        while (timer < duration)

        {

            if (target != null)

            {

                Vector3 direction = (target.position - transform.position).normalized; direction.y = 0;

                if (direction != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * rotationSpeed);

            }

            timer += Time.deltaTime; yield return null;

        }

    }



    private void SetNextBreathThreshold() => nextBreathThreshold = Random.Range(2, 6);



    public void OnAttackHit()

    {

        if (target == null) return;

        if (attackPoint != null) VFXManager.Instance.PlayBossAttack(attackPoint.position, transform.forward);



        Collider[] hitPlayers = Physics.OverlapSphere(attackPoint.position, attackRadius);

        foreach (Collider hit in hitPlayers)

        {

            if (hit.CompareTag("Player")) hit.GetComponent<Player>()?.TakeDamage(attackDamage);

        }

    }



    public void OnBreathFire() { if (breathPoint != null) VFXManager.Instance.PlayBossBreath(breathPoint, breathPoint.forward); }

    protected override void Attack() { }



    private void OnDrawGizmosSelected()
    {

        if (attackPoint != null) 
        {
            Gizmos.color = Color.red; 
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }
        Gizmos.color = Color.red;
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position + transform.rotation * boxOffset, transform.rotation, Vector3.one);
        Gizmos.matrix = rotationMatrix;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(boxSize.x, boxHeight, boxSize.y));
    }

}
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using DG.Tweening; // DOTween 추가

public class BasicBoss : EnemyBase
{
    [Header("─ 보스 기본 설정 ─")]
    public float attackDamage = 20f;
    public float attackCooldown = 2f;
    public float attackRadius = 3.0f;
    public Transform attackPoint; // 기존 방식 흔적 (안 쓰지만 에러 방지용으로 냅둠)
    public Vector3 attackOffset = new Vector3(0, 1f, 2f); // 추가된 공격 위치 오프셋

    [Header("─ 브레스 패턴 설정 (부채꼴) ─")]
    public float breathCooldown = 3.5f;
    public Transform breathPoint;

    public float breathRange = 12f;      // 브레스가 뻗어나가는 최대 거리
    public float breathAngle = 90f;      // 브레스 부채꼴 각도 (90도면 넓게 퍼짐)

    public float closeRangeDamage = 10.0f;
    public GameObject closeRangeVFX;

    private int basicAttackCount = 0;
    private int nextBreathThreshold;
    private bool isAttacking = false;
    private bool isBreathActive = false;

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

    [Header("─ 도약 내려찍기 (하데스식 매운맛) ─")]
    public float leapTriggerDistance = 8.0f;
    public float leapCooldown = 6.0f;
    private float lastLeapTime = -10f;
    public float leapDamage = 30f;
    public float leapRadius = 4.0f;
    public float leapHeight = 12f;
    public float leapHangTime = 0.8f;
    public GameObject warningVFX;
    public GameObject slamVFX;
    private bool isLeaping = false;

    //창우_[추가] 2페이즈 연출용 DOTween 메쉬 및 옵션 변수들
    [Header("─ 2페이즈 코드로 짜는 연출 세팅 ─")]
    public GameObject backSpinesMesh;
    public Vector3 originalSpinesScale = Vector3.one;
    public float spinesGrowDuration = 6f;
    [Space(5)]
    public GameObject headMaskMesh;
    public Vector3 originalMaskScale = Vector3.one;
    public Vector3 originalMaskLocalPos = Vector3.zero;
    public Vector3 maskStartOffset = new Vector3(0f, 0.5f, -0.3f);
    public float maskAssembleDuration = 0.6f;

    protected override void Start()
    {
        base.Start();

        isSuperArmor = true; // 보스 상시 슈퍼아머 적용

        transform.position += new Vector3(0, startHeight, 0);
        agent.enabled = false;
        SetNextBreathThreshold();
        if (closeRangeVFX != null) closeRangeVFX.SetActive(false);
        StartCoroutine(BossThinkRoutine());
    }

    protected override void Update()
    {
        if (isDead) return;

        timer += Time.deltaTime; // 일반 공격 쿨타임 타이머 갱신

        if (!isAwake)
        {
            if (target != null && enemyData != null && Vector3.Distance(transform.position, target.position) <= enemyData.detectionRange)
            {
                isAwake = true;
                if (UIManager.Instance != null)
                    UIManager.Instance.ShowBossUI(enemyData.maxHealth);
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
            Vector3 moveDir = agent.desiredVelocity;
            moveDir.y = 0;

            if (moveDir.sqrMagnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 3f);
            }

            anim.SetFloat("MoveSpeed", agent.velocity.magnitude);
        }
        else
        {
            anim.SetFloat("MoveSpeed", 0f);
        }
    }

    public override void TakeDamage(float damage, bool isCritical = false)
    {
        if (isDead || isPhaseTransitioning) return;

        // 1. 데미지 입기 (패턴 중인지 평상시인지 구분)
        if (isBreathActive || isLeaping)
        {
            currentHealth -= damage;

            if (DamageNumberSpawner.Instance != null)
                DamageNumberSpawner.Instance.Show(damage, transform.position, isCritical, gameObject);
        }
        else
        {
            base.TakeDamage(damage, isCritical);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateBossHealth(currentHealth);
        }

        // 3. 2페이즈 변신 체크
        if (!isPhase2 && enemyData != null && currentHealth <= enemyData.maxHealth * 0.5f)
        {
            StartCoroutine(Phase2TransitionRoutine());
        }
    }

    // ── 코드로 연출하는 2페이즈 돌입 시퀀스 ───────────────────
    IEnumerator Phase2TransitionRoutine()
    {
        //isPhase2 = true; isPhaseTransitioning = true; isAttacking = true;
        //창우_[변경] 기존의 획일화된 단순 대기 방식(yield return new WaitForSeconds(2.0f);)을 완전히 들어내고
        //창우_ 아래와 같이 unscaledDeltaTime 캐싱 기반 시퀀스 및 DOTween 연출 구조로 강화했습니다.

        isPhase2 = true;
        isPhaseTransitioning = true;
        isAttacking = true;

        if (agent.enabled) { agent.isStopped = true; agent.velocity = Vector3.zero; }

        // 1. 플레이어 카메라를 보스한테 강제로 고정 및 부드러운 줌인
        Transform originalFollow = null;
        Transform originalLookAt = null;

        if (CameraManager.Instance != null && CameraManager.Instance.virtualCamera != null)
        {
            originalFollow = CameraManager.Instance.virtualCamera.Follow;
            originalLookAt = CameraManager.Instance.virtualCamera.LookAt;

            CameraManager.Instance.virtualCamera.Follow = this.transform;
            CameraManager.Instance.virtualCamera.LookAt = this.transform;

            CameraManager.Instance.ZoomTo(40f, 0.3f);
        }

        // 2. 기 모으는 울부짖기 모션 발동!
        //anim.SetTrigger("Attack");
        //yield return new WaitForSeconds(2.0f);
        //isAttacking = false; isPhaseTransitioning = false;

        // [창우_ 델타타임 캐싱] 애니메이션 발동 후 카메라 줌인 대기 (0.2초)
        float elapsedTime = 0f;
        while (elapsedTime < 0.2f)
        {
            elapsedTime += Time.unscaledDeltaTime; // 퍼즈/슬로우 모션 왜곡을 무시하고 현실 시간 누적
            yield return null;
        }

        // 3. [시간 제어] 주변 환경을 극단적인 초슬로우 모션으로 전환하여 연출 집중도 극대화
        // Time.timeScale = 0.02f; // 슬로우모션 제거를 위해 주석 처리 혹은 삭제 처리되었습니다.

        // 4. 등가시(Spines)  트윈 재생
        if (backSpinesMesh != null)
        {
            backSpinesMesh.SetActive(true);
            backSpinesMesh.transform.localScale = Vector3.zero;
            backSpinesMesh.transform.DOScale(originalSpinesScale, spinesGrowDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true); // DOTween 환경도 오직 현실 시간 기준으로 작동
        }

        // [창우_ 델타타임 캐싱] 등가시가 자라나는 지정 시간 대기 (spinesGrowDuration * 0.6f)
        elapsedTime = 0f;
        float targetTime = spinesGrowDuration * 0.6f;
        while (elapsedTime < targetTime)
        {
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        // 5. 머리가면(Mask) 
        if (headMaskMesh != null)
        {
            headMaskMesh.SetActive(true);
            headMaskMesh.transform.localScale = Vector3.zero;
            headMaskMesh.transform.localPosition = originalMaskLocalPos + maskStartOffset;

            headMaskMesh.transform.DOScale(originalMaskScale, maskAssembleDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);

            headMaskMesh.transform.DOLocalMove(originalMaskLocalPos, maskAssembleDuration)
                .SetEase(Ease.OutBounce) // 철컥하고 튕기는 안착감 부여
                .SetUpdate(true);
        }

        // [창우_ 델타타임 캐싱]  (maskAssembleDuration + 0.5f)
        elapsedTime = 0f;
        targetTime = maskAssembleDuration + 0.5f;
        while (elapsedTime < targetTime)
        {
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        // 6. 카메라 원래대로 복구 (다시 플레이어 비추기)
        if (CameraManager.Instance != null && CameraManager.Instance.virtualCamera != null)
        {
            CameraManager.Instance.virtualCamera.Follow = originalFollow;
            CameraManager.Instance.virtualCamera.LookAt = originalLookAt;
            CameraManager.Instance.ZoomTo(60f, 0.4f);
        }

        // [창우_ 델타타임 캐싱] 카메라가 플레이어에게 온전히 돌아갈 때까지 복귀 대기 (0.4초)
        elapsedTime = 0f;
        while (elapsedTime < 0.4f)
        {
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        // 7. 연출 완전히 종료, 게임 정상 타임스케일 재개
        // Time.timeScale = 1f; // 앞에서 변경하지 않았으므로 정상 속도 유지를 위해 주석 처리되었습니다.
        isAttacking = false;
        isPhaseTransitioning = false;
    }

    IEnumerator BossThinkRoutine()
    {
        while (!isDead)
        {
            yield return new WaitForSeconds(0.2f);

            if (!isAwake || isAttacking || isBreathActive || isPhaseTransitioning || target == null || !agent.isOnNavMesh)
                continue;

            float dist = Vector3.Distance(transform.position, target.position);

            if (dist >= leapTriggerDistance && Time.time >= lastLeapTime + leapCooldown)
            {
                int jumpCount = isPhase2 ? 3 : 1;
                StartCoroutine(LeapAndSlamRoutine(jumpCount));
                continue;
            }

            if (dist <= attackRange)
            {
                if (basicAttackCount >= nextBreathThreshold)
                {
                    if (isPhase2) StartCoroutine(ExecutePhase2Pattern());
                    else StartCoroutine(BreathAttackRoutine());
                }
                else if (timer >= attackCooldown)
                {
                    isAttacking = true;
                    StartCoroutine(AttackRoutine());
                }
            }
            else
            {
                agent.isStopped = false;
                agent.SetDestination(target.position);
            }
        }
    }

    IEnumerator LeapAndSlamRoutine(int jumpCount)
    {
        isAttacking = true;
        isLeaping = true;

        if (agent.enabled) { agent.isStopped = true; agent.enabled = false; }
        SetGhostMode(true);

        for (int i = 0; i < jumpCount; i++)
        {
            anim.SetTrigger("Jump");

            // 창우_ [추가] 공중으로 뛰어오르는 순간 공중 공격 이펙트 재생 (FlyAttack)
            if (VFXManager.Instance != null && vfxPoint != null)
            {
                VFXManager.Instance.PlayBossFlyAttack(vfxPoint);
            }

            Vector3 startPos = transform.position;
            Vector3 peakPos = startPos + Vector3.up * leapHeight;

            float t = 0;
            while (t < 0.3f)
            {
                if (isDead) yield break;
                transform.position = Vector3.Lerp(startPos, peakPos, t / 0.3f);
                t += Time.deltaTime;
                yield return null;
            }

            float currentHangTime = (i == 0) ? leapHangTime : leapHangTime * 0.4f;

            Vector3 targetPos = target.position;
            targetPos.y = startPos.y;

            if (warningVFX != null)
            {
                warningVFX.transform.position = targetPos + Vector3.up * 0.1f;
                warningVFX.SetActive(true);
            }

            yield return new WaitForSeconds(currentHangTime);

            targetPos = target.position;
            targetPos.y = startPos.y;
            if (warningVFX != null) warningVFX.transform.position = targetPos + Vector3.up * 0.1f;

            Vector3 lookDir = targetPos - transform.position;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDir);
            }

            //창우_[추가] 공중에서 플레이어를 향해 수직 낙하(다이브)를 시작하는 타이밍 (FlyDive)
            if (VFXManager.Instance != null && vfxPoint != null)
            {
                VFXManager.Instance.PlayBossDive(vfxPoint);
            }

            t = 0;
            Vector3 dropStartPos = transform.position;
            while (t < 0.15f)
            {
                if (isDead) yield break;
                transform.position = Vector3.Lerp(dropStartPos, targetPos, t / 0.15f);
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            transform.position = targetPos;

            anim.SetTrigger("Land");

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("Boss_Slam_Sound");
            }

            //창우_[추가] 바닥 밟는 순간 화면 진동
            if (CameraManager.Instance != null)
            {
                CameraManager.Instance.ShakeOnBossSlam();
            }
            if (VFXManager.Instance != null)
                VFXManager.Instance.PlayBossSlam(targetPos);


            if (warningVFX != null) warningVFX.SetActive(false);

            //창우_[변경] 바닥 착지 후 쾅! 터지는 슬램 연출을 VFX 매니저의 지상 공격 풀링으로 변경
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.PlayBossAttack(targetPos, transform.forward);
            }

            if (slamVFX != null)
            {
                slamVFX.transform.position = targetPos;
                slamVFX.SetActive(true);
            }

            Collider[] hits = Physics.OverlapSphere(targetPos, leapRadius);
            foreach (Collider hit in hits)
            {
                if (hit.CompareTag("Player")) hit.GetComponent<Player>()?.TakeDamage(leapDamage);
            }

            if (i < jumpCount - 1) yield return new WaitForSeconds(0.3f);
        }

        yield return new WaitForSeconds(0.5f);

        lastLeapTime = Time.time;
        SetGhostMode(false);
        agent.enabled = true;
        isLeaping = false;
        isAttacking = false;
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
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // 기존 주석 유지: SmoothFaceTarget 실행
        yield return StartCoroutine(SmoothFaceTarget(0.4f, 5f));

        anim.SetTrigger("Attack");
        basicAttackCount++;

        yield return new WaitForSeconds(0.6f);

        isAttacking = false;
        timer = 0f;
    }

    IEnumerator BreathAttackRoutine()
    {
        isAttacking = true;
        isBreathActive = true;

        agent.isStopped = true; agent.velocity = Vector3.zero;
        anim.SetTrigger("Breath");

        yield return new WaitForSeconds(1.0f);

        if (closeRangeVFX != null) { closeRangeVFX.SetActive(true); }

        float timerForBreath = 0f;
        float totalDuration = 6f;
        float damageTickRate = 0.2f;
        float nextDamageTime = 0f;

        while (timerForBreath < totalDuration)
        {
            if (isDead) yield break;

            if (timerForBreath >= nextDamageTime)
            {
                Collider[] hits = Physics.OverlapSphere(transform.position, breathRange);
                foreach (Collider hit in hits)
                {
                    if (hit.CompareTag("Player"))
                    {
                        Vector3 dirToPlayer = (hit.transform.position - transform.position).normalized;
                        dirToPlayer.y = 0;

                        Vector3 forward = transform.forward;
                        forward.y = 0;

                        float angle = Vector3.Angle(forward, dirToPlayer);

                        if (angle <= breathAngle / 2f)
                        {
                            hit.GetComponent<Player>()?.TakeDamage(closeRangeDamage);
                        }
                    }
                }
                nextDamageTime += damageTickRate;
            }

            timerForBreath += Time.deltaTime;
            yield return null;
        }

        if (closeRangeVFX != null) closeRangeVFX.SetActive(false);

        basicAttackCount = 0;
        SetNextBreathThreshold();

        // 기존 주석 유지: 브레스 후 후딜레이 대기
        yield return new WaitForSeconds(0.8f);

        isBreathActive = false;
        isAttacking = false;
    }

    IEnumerator GroundDashRoutine()
    {
        isAttacking = true;
        if (agent.enabled) { agent.isStopped = true; agent.velocity = Vector3.zero; }
        SetGhostMode(true);

        anim.SetTrigger("FlyFast");

        // 창우_ 보스의 브레스 위치인 vfxPoint를 통째로 넘겨서 그 자리에 부착합니다.
        if (VFXManager.Instance != null && vfxPoint != null)
        {
            VFXManager.Instance.PlayBossRush(vfxPoint);
        }

        yield return StartCoroutine(MoveTowardPlayerWithDamage(dashDuration));
        yield return new WaitForSeconds(0.2f);

        if (!isDead)
        {
            anim.SetTrigger("FlyFast");

            // 창우_2타 돌진 때도 vfxPoint에 부착
            if (VFXManager.Instance != null && vfxPoint != null)
            {
                VFXManager.Instance.PlayBossRush(vfxPoint);
            }

            yield return StartCoroutine(MoveTowardPlayerWithDamage(dashDuration));
        }

        yield return new WaitForSeconds(dashCooldown);
        SetGhostMode(false);
        if (agent.enabled) agent.isStopped = false;
        anim.CrossFade("Idle", 0.1f);
        basicAttackCount = 0; SetNextBreathThreshold();
        isAttacking = false;
    }

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
            transform.position += transform.forward * dashSpeed * Time.deltaTime;
            if (!hasHitThisDash)
            {
                Collider[] hits = Physics.OverlapSphere(transform.position + Vector3.up, 2.5f);
                foreach (Collider hitCol in hits)
                {
                    if (hitCol.CompareTag("Player"))
                    {
                        hitCol.GetComponent<Player>()?.TakeDamage(10.0f);
                        hasHitThisDash = true; break;
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
        float t = 0f;
        while (t < duration)
        {
            if (target != null)
            {
                Vector3 direction = (target.position - transform.position).normalized; direction.y = 0;
                if (direction != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * rotationSpeed);
            }
            t += Time.deltaTime; yield return null;
        }
    }

    private void SetNextBreathThreshold() => nextBreathThreshold = Random.Range(2, 6);

    public void OnAttackHit()
    {
        if (target == null) return;

        Vector3 hitCenter = transform.position + transform.rotation * attackOffset;

        if (VFXManager.Instance != null) VFXManager.Instance.PlayBossAttack(hitCenter, transform.forward);

        Collider[] hitPlayers = Physics.OverlapSphere(hitCenter, attackRadius);
        foreach (Collider hit in hitPlayers)
            if (hit.CompareTag("Player")) hit.GetComponent<Player>()?.TakeDamage(attackDamage);
    }

    public void OnBreathFire()
    {
        if (breathPoint != null)
        {
            VFXManager.Instance.PlayBossBreath(breathPoint, breathPoint.forward);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("Boss_Breath_Sound");
        }
    }
    protected override void Attack() { }

    protected override void OnDrawGizmosSelected()
    {
        Vector3 hitCenter = transform.position + transform.rotation * attackOffset;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(hitCenter, attackRadius);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Vector3 forward = transform.forward;
        forward.y = 0;

        Vector3 leftBoundary = Quaternion.Euler(0, -breathAngle / 2f, 0) * forward;
        Vector3 rightBoundary = Quaternion.Euler(0, breathAngle / 2f, 0) * forward;

        Gizmos.DrawRay(transform.position, leftBoundary * breathRange);
        Gizmos.DrawRay(transform.position, rightBoundary * breathRange);
        Gizmos.DrawRay(transform.position, forward * breathRange);

        Gizmos.DrawWireSphere(transform.position, breathRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, leapRadius);
    }

    protected override void Die()
    {
        base.Die();

        if (UIManager.Instance != null)
            UIManager.Instance.HideBossUI();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ClearGame();
        }
    }
}
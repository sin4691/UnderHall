using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// VFX 통합 매니저 (오브젝트 풀링 적용)
/// 씬에 빈 오브젝트 만들고 이 스크립트 붙이면 됨
/// 이름: VFXManager
///
/// [플레이어 담당]
/// 검 휘두를 때:        VFXManager.Instance.PlayWeaponSwing(weaponVFXPoint.position, transform.forward);
/// 스킬 루프 시작:      VFXManager.Instance.PlayWeaponSkillLoop(skillVFXPoint.position, transform.forward);
/// 스킬 루프 종료:      VFXManager.Instance.StopWeaponSkillLoop();
/// 스킬 종료 폭발:      VFXManager.Instance.PlayWeaponSkillExplosion(skillVFXPoint.position, transform.forward);
/// 공격 맞았을 때:      VFXManager.Instance.PlayAttackHit(hitPos, hitNormal);
/// 대시 시작할 때:      VFXManager.Instance.PlayDash(dashVFXPoint.position, dashDirection, dashDuration);
/// 플레이어 피격:       VFXManager.Instance.PlayPlayerHit(vfxPoint.position, Vector3.up, gameObject);
/// 플레이어 사망:       VFXManager.Instance.PlayPlayerDeath(vfxPoint.position, gameObject);
/// 부활 시작:           VFXManager.Instance.PlayPlayerResurrectStart(vfxPoint.position);
/// 부활 완료:           VFXManager.Instance.PlayPlayerResurrectEnd(vfxPoint.position);
///
/// [몬스터 담당]
/// 몬스터 피격:         VFXManager.Instance.PlayMonsterHit(vfxPoint.position, Vector3.up, gameObject);
/// 몬스터 사망:         VFXManager.Instance.PlayMonsterDeath(vfxPoint.position, gameObject);
/// </summary>
public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [Header("─ 검/무기 이펙트 ─")]
    [SerializeField] GameObject weaponSwingPrefab;
    [SerializeField] GameObject weaponSkillPrefab;
    [SerializeField] GameObject weaponSkillExplosionPrefab;

    [Header("─ 타격 이펙트 ─")]
    [SerializeField] GameObject attackHitSparkPrefab;
    [SerializeField] GameObject attackImpactPrefab;

    [Header("─ 대시 이펙트 ─")]
    [SerializeField] GameObject dashStartPrefab;
    [SerializeField] GameObject dashTrailPrefab;
    [SerializeField] GameObject dashEndPrefab;

    [Header("─ 플레이어 피격/사망 ─")]
    [SerializeField] GameObject playerHitPrefab;
    [SerializeField] GameObject playerDeathPrefab;

    [Header("─ 플레이어 부활 ─")]
    [SerializeField] GameObject playerResurrectStartPrefab;
    [SerializeField] GameObject playerResurrectEndPrefab;

    [Header("─ 몬스터 피격/사망 ─")]
    [SerializeField] GameObject monsterHitPrefab;
    [SerializeField] GameObject monsterDeathPrefab;
    [SerializeField] GameObject monsterDeathSmokePrefab;

    [Header("─ 풀링 설정 ─")]
    [SerializeField] int poolSizePerPrefab = 5;

    [Header("─ 피격 플래시 설정 ─")]
    [SerializeField] Color playerHitColor = new Color(1f, 0.15f, 0.15f, 1f);
    [SerializeField] Color monsterHitColor = new Color(1f, 0.3f, 0.1f, 1f);
    [SerializeField] float hitFlashDuration = 0.12f;

    [Header("─ 사망 Dissolve 설정 ─")]
    [SerializeField] float dissolveDuration = 1.2f;

    Dictionary<GameObject, Queue<GameObject>> pool = new Dictionary<GameObject, Queue<GameObject>>();
    Transform poolRoot;

    static readonly int HitBlendID = Shader.PropertyToID("_HitEffectBlend");
    static readonly int HitColorID = Shader.PropertyToID("_HitColor");
    static readonly int FadeAmountID = Shader.PropertyToID("_FadeAmount");

    // ─────────────────────────────────────────
    // 초기화
    // ─────────────────────────────────────────

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        poolRoot = new GameObject("VFX_Pool").transform;
        poolRoot.SetParent(transform);

        PrewarmPool(weaponSwingPrefab);
        PrewarmPool(weaponSkillPrefab);
        PrewarmPool(weaponSkillExplosionPrefab);
        PrewarmPool(attackHitSparkPrefab);
        PrewarmPool(attackImpactPrefab);
        PrewarmPool(dashStartPrefab);
        PrewarmPool(dashTrailPrefab);
        PrewarmPool(dashEndPrefab);
        PrewarmPool(playerHitPrefab);
        PrewarmPool(playerDeathPrefab);
        PrewarmPool(playerResurrectStartPrefab);
        PrewarmPool(playerResurrectEndPrefab);
        PrewarmPool(monsterHitPrefab);
        PrewarmPool(monsterDeathPrefab);
        PrewarmPool(monsterDeathSmokePrefab);
    }

    void PrewarmPool(GameObject prefab)
    {
        if (prefab == null) return;
        pool[prefab] = new Queue<GameObject>();
        for (int i = 0; i < poolSizePerPrefab; i++)
            pool[prefab].Enqueue(CreatePoolObject(prefab));
    }

    GameObject CreatePoolObject(GameObject prefab)
    {
        var go = Instantiate(prefab, poolRoot);
        go.SetActive(false);
        return go;
    }

    // ─────────────────────────────────────────
    // 풀 관리
    // ─────────────────────────────────────────

    /// <summary>
    /// useUnscaledTime: true → WaitForSecondsRealtime 사용.
    /// 슬로우모션(timeScale 변경) 중에도 이펙트가 정상 수명으로 재생되어야 할 때 true.
    /// </summary>
    GameObject GetFromPool(GameObject prefab, Vector3 position, Vector3 direction, bool useUnscaledTime = false)
    {
        if (prefab == null) return null;
        if (!pool.ContainsKey(prefab))
            pool[prefab] = new Queue<GameObject>();

        GameObject go = pool[prefab].Count > 0
            ? pool[prefab].Dequeue()
            : CreatePoolObject(prefab);

        go.transform.position = position;
        go.transform.rotation = direction != Vector3.zero
            ? Quaternion.LookRotation(direction) * prefab.transform.rotation
            : prefab.transform.rotation;

        go.SetActive(true);

        var ps = go.GetComponent<ParticleSystem>();
        if (ps != null) ps.Play();

        float duration = ps != null
            ? ps.main.duration + ps.main.startLifetime.constantMax
            : 2f;

        StartCoroutine(useUnscaledTime
            ? ReturnToPoolUnscaled(go, prefab, duration)
            : ReturnToPool(go, prefab, duration));

        return go;
    }

    IEnumerator ReturnToPool(GameObject go, GameObject prefab, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnObject(go, prefab);
    }

    IEnumerator ReturnToPoolUnscaled(GameObject go, GameObject prefab, float delay)
    {
        yield return new WaitForSecondsRealtime(delay); // timeScale 무시
        ReturnObject(go, prefab);
    }

    void ReturnObject(GameObject go, GameObject prefab)
    {
        if (go == null) return;
        go.SetActive(false);
        go.transform.SetParent(poolRoot);
        if (pool.ContainsKey(prefab))
            pool[prefab].Enqueue(go);
    }

    // ─────────────────────────────────────────
    // 무기 이펙트
    // ─────────────────────────────────────────

    public void PlayWeaponSwing(Vector3 position, Vector3 direction)
        => GetFromPool(weaponSwingPrefab, position, direction);

    // ─────────────────────────────────────────
    // 스킬 이펙트 (루프 + 잔상 즉시 제거)
    // ─────────────────────────────────────────

    private GameObject activeWeaponSkillInstance;

    /// <summary>
    /// 스킬 루프 이펙트 — SpinRoutine tickTimer마다 호출.
    /// 직전 인스턴스를 즉시 풀로 반환하고 새 인스턴스를 스폰해서 잔상을 제거합니다.
    /// </summary>
    public void PlayWeaponSkillLoop(Transform targetTransform)
    {
        if (activeWeaponSkillInstance != null)
        {
            ReturnObject(activeWeaponSkillInstance, weaponSkillPrefab);
            activeWeaponSkillInstance = null;
        }

        activeWeaponSkillInstance = GetFromPool(weaponSkillPrefab, targetTransform.position, targetTransform.forward);
        if (activeWeaponSkillInstance != null)
        {
            activeWeaponSkillInstance.transform.SetParent(targetTransform);
            activeWeaponSkillInstance.transform.localPosition = Vector3.zero;
            activeWeaponSkillInstance.transform.localRotation = Quaternion.identity;
        }
    }

    /// <summary>스킬 종료 시 마지막 잔상까지 즉시 제거 — SpinRoutine 끝/CancelAttack에서 호출</summary>
    public void StopWeaponSkillLoop()
    {
        if (activeWeaponSkillInstance != null)
        {
            ReturnObject(activeWeaponSkillInstance, weaponSkillPrefab);
            activeWeaponSkillInstance = null;
        }
    }

    /// <summary>스킬 종료 폭발 이펙트 — 우클릭을 떼는 순간 1회 호출</summary>
    public void PlayWeaponSkillExplosion(Vector3 position, Vector3 direction)
        => GetFromPool(weaponSkillExplosionPrefab, position, direction);

    // ─────────────────────────────────────────
    // 타격 이펙트
    // ─────────────────────────────────────────

    public void PlayAttackHit(Vector3 position, Vector3 normal)
    {
        GetFromPool(attackHitSparkPrefab, position, normal);
        GetFromPool(attackImpactPrefab, position, normal);
    }

    // ─────────────────────────────────────────
    // 대시 이펙트
    // ─────────────────────────────────────────

    public void PlayDash(Vector3 position, Vector3 direction, float dashDuration)
    {
        GetFromPool(dashStartPrefab, position, direction);
        StartCoroutine(DashTrailRoutine(position, direction, dashDuration));
    }

    IEnumerator DashTrailRoutine(Vector3 startPos, Vector3 direction, float dashDuration)
    {
        float elapsed = 0f;
        float interval = 0.05f;
        while (elapsed < dashDuration)
        {
            GetFromPool(dashTrailPrefab, startPos, direction);
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }
        GetFromPool(dashEndPrefab, startPos, direction);
    }

    // ─────────────────────────────────────────
    // 플레이어 피격/사망
    // ─────────────────────────────────────────

    public void PlayPlayerHit(Vector3 position, Vector3 normal, GameObject playerObj)
    {
        GetFromPool(playerHitPrefab, position, normal);
        StartCoroutine(FlashRoutine(playerObj, playerHitColor));
    }

    public void PlayPlayerDeath(Vector3 position, GameObject playerObj)
    {
        GetFromPool(playerDeathPrefab, position, Vector3.up);
        StartCoroutine(DissolveRoutine(playerObj));
    }

    // ─────────────────────────────────────────
    // 플레이어 부활
    // ─────────────────────────────────────────

    /// <summary>부활 시작 — 슬로우모션 중이므로 UnscaledTime으로 수명 계산</summary>
    public void PlayPlayerResurrectStart(Vector3 position)
        => GetFromPool(playerResurrectStartPrefab, position, Vector3.up, useUnscaledTime: true);

    /// <summary>부활 완료 — 타임스케일 복구 후 호출</summary>
    public void PlayPlayerResurrectEnd(Vector3 position)
        => GetFromPool(playerResurrectEndPrefab, position, Vector3.up);

    // ─────────────────────────────────────────
    // 몬스터 피격/사망
    // ─────────────────────────────────────────

    public void PlayMonsterHit(Vector3 position, Vector3 normal, GameObject monsterObj)
    {
        GetFromPool(monsterHitPrefab, position, normal);
        StartCoroutine(FlashRoutine(monsterObj, monsterHitColor));
    }

    public void PlayMonsterDeath(Vector3 position, GameObject monsterObj)
    {
        GetFromPool(monsterDeathPrefab, position, Vector3.up);
        GetFromPool(monsterDeathSmokePrefab, position, Vector3.up);
        StartCoroutine(DissolveRoutine(monsterObj));
    }

    // ─────────────────────────────────────────
    // 내부 코루틴
    // ─────────────────────────────────────────

    IEnumerator FlashRoutine(GameObject target, Color color)
    {
        if (target == null) yield break;
        var mats = GetMaterials(target);
        foreach (var m in mats)
        {
            if (m.HasProperty(HitColorID)) m.SetColor(HitColorID, color);
            if (m.HasProperty(HitBlendID)) m.SetFloat(HitBlendID, 1f);
        }
        yield return new WaitForSeconds(hitFlashDuration);
        if (target == null) yield break;
        foreach (var m in mats)
            if (m.HasProperty(HitBlendID))
                m.SetFloat(HitBlendID, 0f);
    }

    IEnumerator DissolveRoutine(GameObject target)
    {
        if (target == null) yield break;
        var mats = GetMaterials(target);
        float elapsed = 0f;
        while (elapsed < dissolveDuration)
        {
            if (target == null) yield break;
            float t = elapsed / dissolveDuration;
            foreach (var m in mats)
                if (m.HasProperty(FadeAmountID))
                    m.SetFloat(FadeAmountID, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        foreach (var m in mats)
            if (m.HasProperty(FadeAmountID))
                m.SetFloat(FadeAmountID, 1f);
        yield return new WaitForSeconds(0.1f);
        Destroy(target);
    }

    Material[] GetMaterials(GameObject target)
    {
        var renderers = target.GetComponentsInChildren<Renderer>();
        var list = new List<Material>();
        foreach (var r in renderers)
            foreach (var m in r.materials)
                list.Add(m);
        return list.ToArray();
    }
}
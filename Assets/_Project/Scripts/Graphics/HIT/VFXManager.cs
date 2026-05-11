using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// VFX 통합 매니저 (오브젝트 풀링 적용)
/// 씬에 빈 오브젝트 만들고 이 스크립트 붙이면 됨
/// 이름: VFXManager
///
/// [플레이어 담당]
/// 검 휘두를 때:    VFXManager.Instance.PlayWeaponSwing(weaponVFXPoint.position, transform.forward);
/// 스킬 쓸 때:      VFXManager.Instance.PlayWeaponSkill(skillVFXPoint.position, transform.forward);
/// 공격 맞았을 때:  VFXManager.Instance.PlayAttackHit(hitPos, hitNormal);
/// 대시 시작할 때:  VFXManager.Instance.PlayDash(dashVFXPoint.position, dashDirection, dashDuration);
/// 플레이어 피격:   VFXManager.Instance.PlayPlayerHit(vfxPoint.position, Vector3.up, gameObject);
/// 플레이어 사망:   VFXManager.Instance.PlayPlayerDeath(vfxPoint.position, gameObject);
/// 부활 시작:       VFXManager.Instance.PlayPlayerResurrectStart(vfxPoint.position);
/// 부활 완료:       VFXManager.Instance.PlayPlayerResurrectEnd(vfxPoint.position);
///
/// [몬스터 담당]
/// 몬스터 피격:     VFXManager.Instance.PlayMonsterHit(vfxPoint.position, Vector3.up, gameObject);
/// 몬스터 사망:     VFXManager.Instance.PlayMonsterDeath(vfxPoint.position, gameObject);
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
    [SerializeField] GameObject playerResurrectStartPrefab; // 쓰러지는 순간
    [SerializeField] GameObject playerResurrectEndPrefab;   // 일어나는 순간

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

    Dictionary<GameObject, Queue<GameObject>> pool
        = new Dictionary<GameObject, Queue<GameObject>>();
    Transform poolRoot;

    static readonly int HitBlendID = Shader.PropertyToID("_HitEffectBlend");
    static readonly int HitColorID = Shader.PropertyToID("_HitColor");
    static readonly int FadeAmountID = Shader.PropertyToID("_FadeAmount");

    // ─────────────────────────────────────────
    // 초기화
    // ─────────────────────────────────────────

    void Awake()
    {
        //if (Instance != null) { Destroy(gameObject); return; }
        //Instance = this;

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }

        poolRoot = new GameObject("VFX_Pool").transform;
        poolRoot.SetParent(transform);


        PrewarmPool(weaponSkillExplosionPrefab); // 새로 추가

        PrewarmPool(weaponSwingPrefab);
        PrewarmPool(weaponSkillPrefab);
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


    // 새로 추가: 폭발 스킬 재생 함수
    public void PlayWeaponSkillExplosion(Vector3 position, Vector3 direction)
    {
        GetFromPool(weaponSkillExplosionPrefab, position, direction);
    }

    GameObject GetFromPool(GameObject prefab, Vector3 position, Vector3 direction)
    {
        if (prefab == null) return null;
        if (!pool.ContainsKey(prefab))
            pool[prefab] = new Queue<GameObject>();

        GameObject go = pool[prefab].Count > 0
            ? pool[prefab].Dequeue()
            : CreatePoolObject(prefab);

        // 1. 위치 설정
        go.transform.position = position;

        // 2. 회전 설정 (프리팹의 로컬 회전값을 보정치로 사용)
        if (direction != Vector3.zero)
        {
            // 전달받은 방향(direction)을 바라보게 하되, 
            // 프리팹 자체에 설정된 회전값(prefab.transform.rotation)을 곱해서 방향을 보정함
            go.transform.rotation = Quaternion.LookRotation(direction) * prefab.transform.rotation;
        }
        else
        {
            // 방향이 없으면 프리팹 기본 회전값 그대로 사용
            go.transform.rotation = prefab.transform.rotation;
        }

        go.SetActive(true);

        var ps = go.GetComponent<ParticleSystem>();
        if (ps != null) ps.Play();

        // 파티클의 수명을 계산해 자동으로 풀에 반환
        float duration = ps != null
            ? ps.main.duration + ps.main.startLifetime.constantMax
            : 2f;

        StartCoroutine(ReturnToPool(go, prefab, duration));
        return go;
    }

    IEnumerator ReturnToPool(GameObject go, GameObject prefab, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (go == null) yield break;
        go.SetActive(false);
        go.transform.SetParent(poolRoot);
        if (pool.ContainsKey(prefab))
            pool[prefab].Enqueue(go);
    }

    // ─────────────────────────────────────────
    // 무기 이펙트
    // ─────────────────────────────────────────

    public void PlayWeaponSwing(Vector3 position, Vector3 direction)
    {
        GetFromPool(weaponSwingPrefab, position, direction);
    }

    public void PlayWeaponSkill(Vector3 position, Vector3 direction)
    {
        GetFromPool(weaponSkillPrefab, position, direction);
    }

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

    /// <summary>부활 시작 — 쓰러지는 순간 호출</summary>
    public void PlayPlayerResurrectStart(Vector3 position)
    {
        GetFromPool(playerResurrectStartPrefab, position, Vector3.up);
    }

    /// <summary>부활 완료 — 일어나는 순간 호출</summary>
    public void PlayPlayerResurrectEnd(Vector3 position)
    {
        GetFromPool(playerResurrectEndPrefab, position, Vector3.up);
    }

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
    // 스킬 이펙트 (잔상 즉시 제거용)
    // ─────────────────────────────────────────

    // 현재 활성화된 스킬 이펙트 인스턴스를 추적
    private GameObject activeWeaponSkillInstance;

    /// <summary>
    /// 스킬처럼 반복 재생되는 이펙트 전용.
    /// 직전 인스턴스를 즉시 풀로 반환하고 새 인스턴스를 스폰합니다.
    /// </summary>
    public void PlayWeaponSkillLoop(Vector3 position, Vector3 direction)
    {
        // 직전 이펙트 즉시 강제 반환
        if (activeWeaponSkillInstance != null)
        {
            activeWeaponSkillInstance.SetActive(false);
            activeWeaponSkillInstance.transform.SetParent(poolRoot);
            if (pool.ContainsKey(weaponSkillPrefab))
                pool[weaponSkillPrefab].Enqueue(activeWeaponSkillInstance);
            activeWeaponSkillInstance = null;
        }

        activeWeaponSkillInstance = GetFromPool(weaponSkillPrefab, position, direction);
    }

    /// <summary>스킬 종료 시 마지막 잔상까지 즉시 제거</summary>
    public void StopWeaponSkillLoop()
    {
        if (activeWeaponSkillInstance != null)
        {
            activeWeaponSkillInstance.SetActive(false);
            activeWeaponSkillInstance.transform.SetParent(poolRoot);
            if (pool.ContainsKey(weaponSkillPrefab))
                pool[weaponSkillPrefab].Enqueue(activeWeaponSkillInstance);
            activeWeaponSkillInstance = null;
        }
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
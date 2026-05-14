using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// VFX 통합 매니저 (오브젝트 풀링 적용)
/// 씬에 빈 오브젝트 만들고 이 스크립트 붙이면 됨
/// 이름: VFXManager
/// [몬스터 담당]
/// 몬스터 피격:         VFXManager.Instance.PlayMonsterHit(vfxPoint.position, Vector3.up, gameObject);
/// 몬스터 사망:         VFXManager.Instance.PlayMonsterDeath(vfxPoint.position, gameObject);
/// [보스 드래곤]
/// 지상 공격:           VFXManager.Instance.PlayBossAttack(vfxPoint.position, transform.forward);
/// 지상 브레스:         VFXManager.Instance.PlayBossBreath(vfxPoint.position, transform.forward);
/// 공중 공격:           VFXManager.Instance.PlayBossFlyAttack(vfxPoint.position, transform.forward);
/// 공중 브레스:         VFXManager.Instance.PlayBossFlyBreath(vfxPoint.position, transform.forward);
/// 공중 다이브:         VFXManager.Instance.PlayBossDive(vfxPoint.position, transform.forward);
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

    [Header("─ 몬스터 공격 이펙트 ─")]
    [SerializeField] GameObject monsterAttackPrefab;

    [Header("─ 몬스터 피격/사망 ─")]
    [SerializeField] GameObject monsterHitPrefab;
    [SerializeField] GameObject monsterDeathPrefab;
    [SerializeField] GameObject monsterDeathSmokePrefab;

    [Header("─ 보스 지상 이펙트 ─")]
    [SerializeField] GameObject bossAttackPrefab;       // Attack / Attack02
    [SerializeField] GameObject bossBreathPrefab;       // BreatheFire

    [Header("─ 보스 공중 이펙트 ─")]
    [SerializeField] GameObject bossFlyAttackPrefab;    // FlyAttack
    [SerializeField] GameObject bossFlyBreathPrefab;    // FlyBreatheFire
    [SerializeField] GameObject bossDivePrefab;         // FlyDive

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

        PrewarmPool(monsterAttackPrefab);
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
        PrewarmPool(bossAttackPrefab);
        PrewarmPool(bossBreathPrefab);
        PrewarmPool(bossFlyAttackPrefab);
        PrewarmPool(bossFlyBreathPrefab);
        PrewarmPool(bossDivePrefab);
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
        yield return new WaitForSecondsRealtime(delay);
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
    // 스킬 이펙트
    // ─────────────────────────────────────────

    private GameObject activeWeaponSkillInstance;
    private GameObject activeWeaponSkillPrefab;

    public void PlayWeaponSkillLoop(Transform targetTransform)
    {
        StopWeaponSkillLoop();
        activeWeaponSkillPrefab = weaponSkillPrefab;
        activeWeaponSkillInstance = GetFromPool(weaponSkillPrefab, targetTransform.position, targetTransform.forward);
        if (activeWeaponSkillInstance != null)
        {
            activeWeaponSkillInstance.transform.SetParent(targetTransform);
            activeWeaponSkillInstance.transform.localPosition = Vector3.zero;
            activeWeaponSkillInstance.transform.localRotation = weaponSkillPrefab.transform.localRotation;
        }
    }

    public void StopWeaponSkillLoop()
    {
        if (activeWeaponSkillInstance != null && activeWeaponSkillPrefab != null)
        {
            ReturnObject(activeWeaponSkillInstance, activeWeaponSkillPrefab);
            activeWeaponSkillInstance = null;
            activeWeaponSkillPrefab = null;
        }
    }

    public void PlayWeaponSkillExplosion(Transform targetTransform)
    {
        StopWeaponSkillLoop();
        activeWeaponSkillPrefab = weaponSkillExplosionPrefab;
        activeWeaponSkillInstance = GetFromPool(weaponSkillExplosionPrefab, targetTransform.position, targetTransform.forward);
        if (activeWeaponSkillInstance != null)
        {
            activeWeaponSkillInstance.transform.SetParent(targetTransform);
            activeWeaponSkillInstance.transform.localPosition = Vector3.zero;
            activeWeaponSkillInstance.transform.localRotation = weaponSkillExplosionPrefab.transform.localRotation;
        }
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
        //StartCoroutine(DissolveRoutine(playerObj));
    }

    // ─────────────────────────────────────────
    // 플레이어 부활
    // ─────────────────────────────────────────

    public void PlayPlayerResurrectStart(Vector3 position)
        => GetFromPool(playerResurrectStartPrefab, position, Vector3.up, useUnscaledTime: true);

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

    //-------------------------------------------------
    // 몬스터 공격
    //-------------------------------------------------
    public void PlayMonsterAttack(Vector3 position, Vector3 direction)
    {
        GetFromPool(monsterAttackPrefab, position, direction);
    }

    // ─────────────────────────────────────────
    // 보스 이펙트
    // ─────────────────────────────────────────

    /// <summary>지상 일반 공격 (Attack / Attack02) — BossDragon에서 호출</summary>
    public void PlayBossAttack(Vector3 position, Vector3 direction)
        => GetFromPool(bossAttackPrefab, position, direction);

    /// <summary>지상 브레스 (BreatheFire) — BossDragon에서 호출</summary>
    //public void PlayBossBreath(Vector3 position, Vector3 direction)
    //    => GetFromPool(bossBreathPrefab, position, direction);
    public void PlayBossBreath(Transform head, Vector3 direction) // Vector3 대신 Transform을 받음
    {
        GameObject go = GetFromPool(bossBreathPrefab, head.position, direction);
        go.transform.SetParent(head); // 머리에 부착
        go.transform.localPosition = Vector3.zero; // 위치 초기화
        go.transform.localRotation = Quaternion.identity; // 회전 초기화
    }
    /// <summary>공중 공격 (FlyAttack) — BossDragon에서 호출</summary>
    public void PlayBossFlyAttack(Vector3 position, Vector3 direction)
        => GetFromPool(bossFlyAttackPrefab, position, direction);

    /// <summary>공중 브레스 (FlyBreatheFire) — BossDragon에서 호출</summary>
    public void PlayBossFlyBreath(Vector3 position, Vector3 direction)
        => GetFromPool(bossFlyBreathPrefab, position, direction);

    /// <summary>공중 다이브 (FlyDive) — BossDragon에서 호출</summary>
    public void PlayBossDive(Vector3 position, Vector3 direction)
        => GetFromPool(bossDivePrefab, position, direction);

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
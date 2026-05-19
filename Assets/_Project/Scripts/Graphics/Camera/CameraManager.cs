using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// [세팅 방법]
/// 1. CameraManager 스크립트를 아무 오브젝트에 부착
/// 2. CinemachineCamera 오브젝트에 CinemachineImpulseSource 컴포넌트 추가
/// 3. CinemachineCamera 오브젝트에 CinemachineImpulseListener 컴포넌트 추가
/// 4. Inspector에서 virtualCamera, impulseSource 슬롯 연결
///
/// [ImpulseSource Inspector 추천 세팅]
/// - Impulse Shape    : Custom Curve (또는 Sine)
/// - Impulse Duration : 0.2
/// - Dissipation Rate : 0.25
/// </summary>
public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance;

    [Header("Cinemachine 연결")]
    public CinemachineCamera virtualCamera;
    public CinemachineImpulseSource impulseSource;

    private float defaultFOV;



    [Header("피격 흔들림")]
    [Tooltip("좌우 흔들림 강도")]
    public float hitShakeX = 0.4f;
    [Tooltip("상하 흔들림 강도 (작게 유지)")]
    public float hitShakeY = 0.08f;

    [Header("타격 흔들림")]
    [Tooltip("좌우 흔들림 강도")]
    public float attackShakeX = 0.15f;
    [Tooltip("상하 흔들림 강도 (작게 유지)")]
    public float attackShakeY = 0.03f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        if (virtualCamera == null)
            virtualCamera = FindAnyObjectByType<CinemachineCamera>();

        if (impulseSource == null && virtualCamera != null)
            impulseSource = virtualCamera.GetComponent<CinemachineImpulseSource>();

        if (virtualCamera != null)
            defaultFOV = virtualCamera.Lens.FieldOfView;
    }



    // ── 피격 시 흔들림 (좌우 위주) ──
    public void ShakeOnHit()
    {
        if (impulseSource == null) return;

        // 랜덤 좌우 방향 + 약한 상하
        float randomX = Random.value > 0.5f ? hitShakeX : -hitShakeX;
        Vector3 impulseDir = new Vector3(randomX, hitShakeY, 0f);

        impulseSource.GenerateImpulse(impulseDir);
    }

    // ── 타격 시 흔들림 (좌우 위주) ──
    public void ShakeOnAttack()
    {
        if (impulseSource == null) return;

        // 타격 방향 반대로 살짝 튕기는 느낌
        float randomX = Random.value > 0.5f ? attackShakeX : -attackShakeX;
        Vector3 impulseDir = new Vector3(randomX, attackShakeY, 0f);

        impulseSource.GenerateImpulse(impulseDir);
    }

    // ── 공격 방향 기반 흔들림 (더 정확한 타격감) ────
    // PlayerAttack에서 타격 방향을 넘겨줄 때 사용
    public void ShakeOnAttackDirectional(Vector3 attackDir)
    {
        if (impulseSource == null) return;

        // 공격 방향의 수평 성분만 사용
        Vector3 horizontal = new Vector3(attackDir.x, 0f, attackDir.z).normalized;

        // 카메라 기준 좌우로 변환
        Vector3 camRight = Camera.main.transform.right;
        float dot = Vector3.Dot(horizontal, camRight);

        Vector3 impulseDir = new Vector3(dot * attackShakeX, attackShakeY, 0f);
        impulseSource.GenerateImpulse(impulseDir);
    }
}
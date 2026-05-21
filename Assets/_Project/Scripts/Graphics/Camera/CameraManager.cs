using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance;

    [Header("Cinemachine 연결")]
    public CinemachineCamera virtualCamera;
    public CinemachineImpulseSource impulseSource;

    private float defaultFOV;

    [Header("피격 흔들림")]
    public float hitShakeX = 0.4f;
    public float hitShakeY = 0.08f;

    [Header("타격 흔들림")]
    public float attackShakeX = 0.15f;
    public float attackShakeY = 0.03f;

    
    [Header("─ 전체 화면 피격 플래시 (UI) ─")]
    [Tooltip("화면 전체를 덮는 UI 오버레이 패널 이미지")]
    [SerializeField] private Image screenFlashImage;

    [Tooltip("피격 시 순간적으로 변할 얕은 붉은색")]
    [SerializeField] private Color hitFlashColor = new Color(1f, 0f, 0f, 0.4f);

    [SerializeField] private float flashInDuration = 0.03f;
    [SerializeField] private float flashOutDuration = 0.25f;

    [Header("─ 글로벌 볼륨 왜곡 설정 (유니티 6) ─")]
    [Tooltip("피격 순간 테두리가 번지는 왜곡 세기 (0 ~ 1)")]
    [SerializeField] private float hitDistortionStrength = 0.8f;

    // 유니티 6 글로벌 볼륨 조작용 변수들
    private Volume globalVolume;
    private ChromaticAberration chromaticAberration;
    private Tweener distortTweener;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        FindAndResetCamera();
        FindAndResetVolume();
        ResetFlashImage();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[CameraManager] 새 씬 로드됨: {scene.name}. 카메라 및 볼륨을 재배정합니다.");
        FindAndResetCamera();
        FindAndResetVolume(); //씬이 바뀔 때마다 볼륨 컴포넌트도 새로 찾아야 Missing 에러가 안 납니다.
        ResetFlashImage();
    }

    private void FindAndResetCamera()
    {
        virtualCamera = FindAnyObjectByType<CinemachineCamera>();

        if (virtualCamera != null)
        {
            impulseSource = virtualCamera.GetComponent<CinemachineImpulseSource>();
            defaultFOV = virtualCamera.Lens.FieldOfView;
            Debug.Log($"[CameraManager] 새로운 가상 카메라({virtualCamera.gameObject.name}) 연결 성공!");
        }
        else
        {
            impulseSource = null;
            Debug.Log("[CameraManager] 현재 씬에 CinemachineCamera가 없습니다.");
        }
    }

   
    private void FindAndResetVolume()
    {
        globalVolume = FindAnyObjectByType<Volume>();

        if (globalVolume != null && globalVolume.profile != null)
        {
            // 글로벌 볼륨 프로필에서 색수차(Chromatic Aberration) 효과를 가져옵니다.
            if (globalVolume.profile.TryGet(out chromaticAberration))
            {
                chromaticAberration.intensity.value = 0f; // 초기값 투명하게 세팅
                Debug.Log("[CameraManager] 유니티 6 글로벌 볼륨 색수차(Chromatic Aberration) 연결 성공!");
            }
        }
        else
        {
            chromaticAberration = null;
            Debug.Log("[CameraManager] 현재 씬에 볼륨 또는 포스트 프로세싱 프로필이 없습니다.");
        }
    }

    private void ResetFlashImage()
    {
        if (screenFlashImage != null)
        {
            screenFlashImage.DOKill();
            screenFlashImage.color = new Color(hitFlashColor.r, hitFlashColor.g, hitFlashColor.b, 0f);
        }
    }

    /// <summary>
    /// 플레이어 피격 시 호출 (카메라 진동 + UI 점멸 + 유니티6 화면 왜곡)
    /// </summary>
    public void ShakeOnHit()
    {
        // 1. 시네머신 카메라 흔들기
        if (impulseSource != null)
        {
            float randomX = Random.value > 0.5f ? hitShakeX : -hitShakeX;
            Vector3 impulseDir = new Vector3(randomX, hitShakeY, 0f);
            impulseSource.GenerateImpulse(impulseDir);
        }

        // 2. UI 전체 화면 붉은색 쫀득 점멸 (두트윈 시퀀스)
        if (screenFlashImage != null)
        {
            screenFlashImage.DOKill();

            Sequence flashSequence = DOTween.Sequence(screenFlashImage);
            flashSequence
                .Append(screenFlashImage.DOColor(hitFlashColor, flashInDuration).SetEase(Ease.OutQuad))
                .Append(screenFlashImage.DOColor(new Color(hitFlashColor.r, hitFlashColor.g, hitFlashColor.b, 0f), flashOutDuration).SetEase(Ease.InQuad));
        }

 
        if (chromaticAberration != null)
        {
            if (distortTweener != null) distortTweener.Kill(); // 연타 피격 시 트윈 초기화

            // 피격되는 프레임에 쾅! 하고 색수차 왜곡을 강하게 줍니다.
            chromaticAberration.intensity.value = hitDistortionStrength;

            // 두트윈을 이용해 서서히 원래 깨끗한 화면(0f)으로 스르륵 돌려놓습니다.
            distortTweener = DOTween.To(() => chromaticAberration.intensity.value,
                                        x => chromaticAberration.intensity.value = x,
                                        0f,
                                        flashOutDuration).SetEase(Ease.InQuad);
        }
    }

    public void ShakeOnAttack()
    {
        if (impulseSource == null) return;
        float randomX = Random.value > 0.5f ? attackShakeX : -attackShakeX;
        Vector3 impulseDir = new Vector3(randomX, attackShakeY, 0f);
        impulseSource.GenerateImpulse(impulseDir);
    }

    public void ShakeOnAttackDirectional(Vector3 attackDir)
    {
        if (impulseSource == null) return;
        Vector3 horizontal = new Vector3(attackDir.x, 0f, attackDir.z).normalized;
        Vector3 camRight = Camera.main.transform.right;
        float dot = Vector3.Dot(horizontal, camRight);
        Vector3 impulseDir = new Vector3(dot * attackShakeX, attackShakeY, 0f);
        impulseSource.GenerateImpulse(impulseDir);
    }
}
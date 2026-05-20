using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement; 

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

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 중요: 씬이 로드될 때마다 실행될 함수를 이벤트에 등록합니다.
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
        // 싱글톤 오브젝트가 혹시라도 파괴될 때 이벤트 메모리 누수 방지
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        // 최초 시작 시 카메라 찾기
        FindAndResetCamera();
    }

    // 씬이 새로 열릴 때마다 유니티가 자동으로 실행해 주는 함수
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[CameraManager] 새 씬 로드됨: {scene.name}. 카메라 레퍼런스를 재배정합니다.");
        FindAndResetCamera();
    }

    // 현재 씬에 있는 새로운 시네머신 카메라를 찾아 세팅하는 헬퍼 함수
    private void FindAndResetCamera()
    {
        // 씬이 바뀌었으므로 기존 무효화된 카메라를 싹 무시하고 새로 찾습니다.
        virtualCamera = FindAnyObjectByType<CinemachineCamera>();

        if (virtualCamera != null)
        {
            impulseSource = virtualCamera.GetComponent<CinemachineImpulseSource>();
            defaultFOV = virtualCamera.Lens.FieldOfView;
            Debug.Log($"[CameraManager] 새로운 가상 카메라({virtualCamera.gameObject.name}) 연결 성공!");
        }
        else
        {
            // 타이틀 씬 같이 시네머신 카메라가 원래 없는 씬을 위한 예외 처리
            impulseSource = null;
            Debug.Log("[CameraManager] 현재 씬에 CinemachineCamera가 없습니다.");
        }
    }

    // ── 이하 셰이크 코드들ShakeOnHit, ShakeOnAttack 
    public void ShakeOnHit()
    {
        if (impulseSource == null) return;
        float randomX = Random.value > 0.5f ? hitShakeX : -hitShakeX;
        Vector3 impulseDir = new Vector3(randomX, hitShakeY, 0f);
        impulseSource.GenerateImpulse(impulseDir);
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
using UnityEngine;
using UnityEngine.InputSystem; 

public class HadesCameraTarget : MonoBehaviour
{
    [Header("Target References")]
    [SerializeField] private Transform playerTransform; // 플레이어 본체의 Transform

    [Header("Camera Movement Settings")]
    [Range(0f, 0.5f)]
    [SerializeField] private float mouseInfluence = 0.08f; // 마우스가 카메라를 당기는 힘 (은은하게 수정)
    [SerializeField] private float maxMouseDistance = 3f;   // 카메라 타겟이 캐릭터로부터 떨어질 수 있는 최대 거리
    [SerializeField] private float smoothTime = 0.25f;      // 부드러운 정도 (가만히 있을 때 꿀렁임 방지)

    private Camera mainCamera;
    private Vector3 currentVelocity; // SmoothDamp 내부 연산용 변수

    private void Start()
    {
        // 부모(CameraSystem) 밑에서 독립시켜 월드 좌표 꼬임을 완벽 차단합니다.
        transform.SetParent(null);

        // 메인 카메라를 코드로 정확하게 찾아 둡니다.
        mainCamera = Camera.main;

        // 3. 프리팹 유실 대비: 플레이어 자동 검색 주입
        if (playerTransform == null)
        {
            // GameManager가 있다면 싱글톤으로 먼저 안전하게 가져옵니다.
            if (GameManager.Instance != null && GameManager.Instance.player != null)
            {
                playerTransform = GameManager.Instance.player.transform;
                Debug.Log($"[{gameObject.name}] GameManager를 통해 Player를 자동 연결했습니다.");
            }
            else
            {
                // 플랜 B: 태그로 찾기
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                {
                    playerTransform = playerObj.transform;
                    Debug.Log($"[{gameObject.name}] 'Player' 태그 오브젝트를 자동 연결했습니다.");
                }
                else
                {
                    Debug.LogWarning($"[{gameObject.name}] 씬에서 플레이어를 찾을 수 없습니다! 태그나 GameManager를 확인하세요.");
                }
            }
        }
    }

    private void LateUpdate()
    {
        // 이제 mainCamera가 null이 아니므로 무사히 통과합니다!
        if (playerTransform == null || mainCamera == null) return;

        // 1. 신형 인풋 시스템 방식으로 현재 마우스의 스크린 좌표 읽기
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

        // 2. 마우스 스크린 좌표를 레이캐스트를 통해 3D 월드 바닥 좌표로 변환
        Ray ray = mainCamera.ScreenPointToRay(mouseScreenPos);
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0, playerTransform.position.y, 0));

        if (groundPlane.Raycast(ray, out float enterDistance))
        {
            Vector3 mouseWorldPos = ray.GetPoint(enterDistance);

            // 3. 플레이어에서 마우스를 향하는 방향 벡터 계산 (Y축 평면화)
            Vector3 targetDirection = mouseWorldPos - playerTransform.position;
            targetDirection.y = 0;

            // 4. 설정한 영향력(Influence)을 적용하되, 최대 제한 거리를 넘지 않도록 세팅
            float targetDistance = targetDirection.magnitude * mouseInfluence;
            targetDistance = Mathf.Min(targetDistance, maxMouseDistance);

            // 5. 최종 목표 지점 계산
            Vector3 desiredPosition = playerTransform.position + targetDirection.normalized * targetDistance;

            // 6. SmoothDamp로 프레임 떨림 없이 부드럽게 좌표 추적
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref currentVelocity,
                smoothTime
            );
        }
    }
}
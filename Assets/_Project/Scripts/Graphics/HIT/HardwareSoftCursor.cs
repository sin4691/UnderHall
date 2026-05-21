using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using DG.Tweening;

public class HardwareSoftCursor : MonoBehaviour
{
    private RectTransform rectTransform;
    private Image cursorImage;
    private Camera mainCamera;

    [Header("커서 이미지 에셋")]
    public Sprite cursorA_Normal;  // 평소 이미지 (A)
    public Sprite cursorB_Attack;  // 몬스터 위 이미지 (B)

    [Header("색상 커스텀")]
    public Color normalColor = Color.white;  // 기본 색상 (A의 색상)
    public Color attackClickColor = Color.red; // 몬스터 위에서 클릭했을 때 색상 (B의 색상)

    [Header("몬스터 감지 레이어")]
    public LayerMask enemyLayer;

    private bool isOverEnemy = false;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        cursorImage = GetComponent<Image>();
        mainCamera = Camera.main;

        Cursor.visible = false;

        if (cursorA_Normal != null)
        {
            cursorImage.sprite = cursorA_Normal;
            cursorImage.color = normalColor;
        }
    }

    // ★ 마우스 싱크 문제를 잡기 위해 렌더링 직전 단계인 LateUpdate로 위치 이동을 옮깁니다.
    void LateUpdate()
    {
        // 1. 마우스 위치 실시간 동기화
        Vector2 mousePos = Mouse.current.position.ReadValue();
        rectTransform.position = mousePos;

        // 2. 몬스터 레이캐스트 체크
        CheckEnemyUnderCursor(mousePos);

        // 3. 클릭 입력 처리 및 쫀득한 두트윈 연출
        HandleMouseClick();
    }

    private void CheckEnemyUnderCursor(Vector2 mouseScreenPos)
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(mouseScreenPos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, enemyLayer))
        {
            if (!isOverEnemy)
            {
                isOverEnemy = true;

                // [변경 포인트] 몬스터 위에 올라갔을 때 모양은 B로 바뀌지만, 색상은 A의 기본 색상(흰색)을 유지합니다.
                ChangeCursorShape(cursorB_Attack, normalColor, 1.1f);
            }
        }
        else
        {
            if (isOverEnemy)
            {
                isOverEnemy = false;

                // 몬스터에게서 벗어나면 다시 A 모양, A 색상으로 복귀
                ChangeCursorShape(cursorA_Normal, normalColor, 1.0f);
            }
        }
    }

    private void HandleMouseClick()
    {
        // 마우스를 누른 바로 그 프레임에 작동
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            rectTransform.DOKill();
            cursorImage.DOKill();

            // 공통 크기 튕김 연출 (쫀득하게 크기 키우기)
            rectTransform.localScale = Vector3.one * 1.3f;
            rectTransform.DOScale(1f, 0.12f).SetEase(Ease.OutQuad);

            if (isOverEnemy)
            {
                // ★ [핵심 기믹] 몬스터 위에서 클릭했다면, 순간적으로 B의 색상(붉은색)으로 강렬하게 변경!
                cursorImage.color = attackClickColor;
                // 클릭 후 0.15초 동안 다시 은은하게 평소 색상(A색)으로 돌아오게 만듭니다.
                cursorImage.DOColor(normalColor, 0.15f);
            }
            else
            {
                // 일반 땅바닥을 클릭했을 때는 가볍게 흰색/황금색으로 반짝였다가 돌아오는 연출 (선택 사항)
                cursorImage.color = Color.white;
                cursorImage.DOColor(normalColor, 0.1f);
            }
        }
    }

    /// <summary>
    /// 마우스 외형 모양을 스윽 바꿔주는 헬퍼 함수
    /// </summary>
    private void ChangeCursorShape(Sprite newSprite, Color targetColor, float targetScale)
    {
        if (cursorImage == null || newSprite == null) return;

        cursorImage.DOKill();
        rectTransform.DOKill();

        cursorImage.sprite = newSprite;
        cursorImage.color = targetColor;
        rectTransform.DOScale(targetScale, 0.08f).SetEase(Ease.OutSine);
    }
}
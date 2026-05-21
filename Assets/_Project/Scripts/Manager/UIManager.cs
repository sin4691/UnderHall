using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening; //  [추가] 두트윈 네임스페이스

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Health UI")]
    public Image healthImage;
    public TextMeshProUGUI healthText;
    //창우_삭제 public GameObject interactPromptUI;  // 인스펙터에서 드래그

    //창우_[추가] HP 바 전체를 담고 있는 부모 RectTransform (인스펙터에서 드래그)
    [SerializeField] private RectTransform healthBarContainer;

    [Header("─ UI 피격 셰이크 설정 ─")]
    [SerializeField] private float shakeDuration = 0.2f;  // 흔들리는 시간
    [SerializeField] private float shakeStrength = 15f;   // 흔들리는 세기 (픽셀 단위)
    [SerializeField] private int shakeVibrato = 15;       // 진동 횟수
    [SerializeField] private float shakeRandomness = 90f; // 랜덤성


    // ─────────────────────────────────────────
    // 창우_ 대시 & 우클릭 스킬 쿨타임 UI 변수
    // ─────────────────────────────────────────
    [Header("─ 쿨타임 UI 설정 ─")]
    [Tooltip("대시 쿨타임 어두운 이미지 (Fill Method: Radial 360 추천)")]
    [SerializeField] private Image dashCooltimeImage;
    [Tooltip("스페이스바 대시 지시등 텍스트")]
    [SerializeField] private TextMeshProUGUI dashIndicatorText;

    [Tooltip("우클릭 스킬 쿨타임 어두운 이미지 (Fill Method: Radial 360 추천)")]
    [SerializeField] private Image skillCooltimeImage;
    [Tooltip("우클릭 스킬 지시등 텍스트")]
    [SerializeField] private TextMeshProUGUI skillIndicatorText;

    //창우_삭제 public void ShowInteractPrompt()
    //창우_[추가] 피격 시 점멸할 (얕은) 붉은색 설정
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.4f, 0.4f, 1f);

    [Header("─ Interaction UI ─")]
    public GameObject interactPromptUI;


    //창우_두트윈 캐싱용 변수 (새 연출 시작 시 기존 연출을 깔끔하게 끄기 위함)
    private Tweener dashCoolTweener;
    private Sequence dashIndicatorSequence;

    private Tweener skillCoolTweener;
    private Sequence skillIndicatorSequence;

    private void Awake()
    {

        //창우_삭제 if (interactPromptUI != null)
        //          interactPromptUI.SetActive(true);

        if (Instance == null)
        {
            Instance = this;
            // 만약 씬 전환 시에도 UI를 유지하고 싶다면 DontDestroyOnLoad를 켜도 됩니다.
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void Start()
    {
        //창우_시작할 때는 쿨타임이 안 돌고 있으므로 지시등 활성화 및 초기화
        ResetCooltimeUI();
    }
    private void ResetCooltimeUI()
    {
        if (dashCooltimeImage != null) dashCooltimeImage.fillAmount = 0f;
        if (skillCooltimeImage != null) skillCooltimeImage.fillAmount = 0f;

        //창우_스킬 지시등 깜빡임 세팅 동작
        TriggerIndicatorReady(dashIndicatorText, ref dashIndicatorSequence);
        TriggerIndicatorReady(skillIndicatorText, ref skillIndicatorSequence);
    }

    //변경_public void HideInteractPrompt()
    public void ShowInteractPrompt()
    {
        //창우_삭제if (interactPromptUI != null)
        //         interactPromptUI.SetActive(false);

        if (interactPromptUI != null) interactPromptUI.SetActive(true);
    }

    public void HideInteractPrompt()
    {
        //창우_삭제if (Instance == null) Instance = this;
        //else Destroy(gameObject);

        if (interactPromptUI != null) interactPromptUI.SetActive(false);
    }

    public void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        if (healthImage != null)
        {
            healthImage.fillAmount = currentHealth / maxHealth;
        }

        if (healthText != null)
        {
            healthText.text = $"{Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";
        }
    }

    /// <summary>
    /// 창우_ 플레이어 피격 시 호출할 UI 흔들기 함수
    /// </summary>
    public void PlayHealthBarShake()
    {
        if (healthBarContainer == null || healthImage == null) return;

        DOTween.Kill(healthBarContainer);
        DOTween.Kill(healthImage);

        //흔들기 전 위치 저장 후 복구
        Vector2 originalPos = healthBarContainer.anchoredPosition;
        healthBarContainer.anchoredPosition = originalPos;

        healthBarContainer.DOShakeAnchorPos( // ← DOShakePosition 대신 이걸로
            duration: shakeDuration,
            strength: shakeStrength,
            vibrato: shakeVibrato,
            randomness: shakeRandomness,
            fadeOut: true
        ).OnComplete(() =>
            healthBarContainer.anchoredPosition = originalPos // ← 끝나면 원래 위치로
        );

        Sequence flashSequence = DOTween.Sequence(healthImage);
        flashSequence
            .Append(healthImage.DOColor(hitFlashColor, 0.05f).SetEase(Ease.OutFlash))
            .Append(healthImage.DOColor(Color.white, 0.15f).SetEase(Ease.InFlash));
    }
    // ─────────────────────────────────────────
    // 창우_두트윈 기반 쿨타임 UI 핵심 로직
    // ─────────────────────────────────────────

    /// <summary>
    /// 대시 쿨타임 연출 시작 (대시 사용 직후 호출)
    /// </summary>
    public void StartDashCooltime(float duration)
    {
        if (dashCooltimeImage == null) return;

        // 1. 기존에 돌고 있던 대시 관련 두트윈 싹 다 정리 (안전장치)
        dashCoolTweener.Kill();
        dashIndicatorSequence.Kill();

        // 2. 이미지 꽉 채우고 지시등은 투명하게 끄기
        dashCooltimeImage.fillAmount = 1f;
        if (dashIndicatorText != null)
        {
            dashIndicatorText.transform.localScale = Vector3.one;
            dashIndicatorText.alpha = 0.3f; // 쿨타임 중엔 어둡게 비활성화 느낌
        }

        // 3. 두트윈으로 fillAmount를 정해진 시간 동안 0으로 스무스하게 깎음
        dashCoolTweener = dashCooltimeImage.DOFillAmount(0f, duration)
            .SetEase(Ease.Linear)
            .SetUpdate(false) // 일시정지(Time.timeScale=0) 시 쿨타임도 멈추도록 설정
            .OnComplete(() =>
            {
                // 4. 완료되면 깜빡이 지시등 온!
                TriggerIndicatorReady(dashIndicatorText, ref dashIndicatorSequence);
            });
    }

    /// <summary>
    /// 창우_우클릭 특수 공격 쿨타임 연출 시작 (스킬이 완전히 끝나고 쿨타임 도는 시점 호출)
    /// </summary>
    public void StartSkillCooltime(float duration)
    {
        if (skillCooltimeImage == null) return;

        skillCoolTweener.Kill();
        skillIndicatorSequence.Kill();

        skillCooltimeImage.fillAmount = 1f;
        if (skillIndicatorText != null)
        {
            skillIndicatorText.transform.localScale = Vector3.one;
            skillIndicatorText.alpha = 0.3f;
        }

        skillCoolTweener = skillCooltimeImage.DOFillAmount(0f, duration)
            .SetEase(Ease.Linear)
            .SetUpdate(false); // 일시정지 시 스킬 쿨타임도 정지

        skillCoolTweener.OnComplete(() =>
        {
            TriggerIndicatorReady(skillIndicatorText, ref skillIndicatorSequence);
        });
    }

    /// <summary>
    /// 창우_쿨타임 완료 시 지시등을 쫀득하게 튕기고 반짝이게 만드는 공용 연출 함수
    /// </summary>
    private void TriggerIndicatorReady(TextMeshProUGUI textComponent, ref Sequence targetSequence)
    {
        if (textComponent == null) return;

        textComponent.transform.localScale = Vector3.one;
        textComponent.alpha = 1f;

        // 시퀀스 생성 및 조립
        targetSequence = DOTween.Sequence();

        // 쾅 하고 커졌다가 돌아오면서 무한 요요 깜빡임
        targetSequence
            .Append(textComponent.transform.DOScale(1.3f, 0.1f).SetEase(Ease.OutQuad))
            .Append(textComponent.transform.DOScale(1f, 0.12f).SetEase(Ease.InQuad))
            .AppendCallback(() =>
            {
                // 팅기는 연출이 끝나면 그 자리에서 알파값 무한 요요 반복 (깜빡임 구현)
                textComponent.DOFade(0.3f, 0.5f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetLink(textComponent.gameObject); // 오브젝트 파괴 방지 예외처리
            });
    }
}




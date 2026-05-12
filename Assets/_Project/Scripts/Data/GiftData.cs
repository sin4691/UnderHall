using UnityEngine;

[CreateAssetMenu(fileName = "GiftData", menuName = "Game/Gift Data")]
public class GiftData : ScriptableObject
{
    // 확정 아님!
    [Header("===== 기본 정보 =====")]

    [Tooltip("은혜 이름")]
    public string giftName;

    [Tooltip("효과 설명 (UI에 표시)")]
    [TextArea(2, 4)]
    public string description;

    [Tooltip("아이콘")]
    public Sprite icon;

    [Header("===== 분류 =====")]

    [Tooltip("어떤 행동에 적용되는지")]
    public GiftCategory category;

    [Header("===== 효과 =====")]

    [Tooltip("효과 종류")]
    public GiftEffect effect;

    [Tooltip("효과 수치 (% 또는 절대값)")]
    public float value;
}

public enum GiftCategorys
{
    Attack,      // 공격
    Dash,        // 대쉬
    Passive      // 패시브
}

public enum GiftEffect
{
    // 공격
    DamageIncrease,
    AttackSpeedIncrease,
    AttackRangeIncrease,

    // 대쉬
    DashCooldownReduce,
    DashDamage,
    DashCountIncrease,

    // 패시브
    MaxHealthIncrease,
    MoveSpeedIncrease,
    GoldGainIncrease,
    Lifesteal
}
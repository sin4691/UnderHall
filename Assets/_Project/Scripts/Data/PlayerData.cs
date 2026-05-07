using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "Game/Player Data")]
public class PlayerData : ScriptableObject
{
    [Header("===== 기본 스탯 =====")]

    [Tooltip("최대 체력")]
    [Range(50, 500)]
    public float maxHealth = 100f;

    [Tooltip("기본 공격 데미지")]
    [Range(1, 100)]
    public float damage = 10f;

    [Tooltip("이동 속도")]
    [Range(1, 20)]
    public float moveSpeed = 5f;

    [Header("===== 공격 =====")]

    [Tooltip("기본 공격 쿨다운 (초)")]
    [Range(0.1f, 3f)]
    public float attackCooldown = 0.5f;

    [Tooltip("기본 공격 사거리")]
    [Range(0.5f, 10f)]
    public float attackRange = 1.5f;

    [Tooltip("특수 공격 데미지 배율")]
    [Range(1f, 5f)]
    public float specialAttackMultiplier = 3f;

    [Tooltip("특수 공격 쿨다운 (초)")]
    [Range(1f, 30f)]
    public float specialAttackCooldown = 5f;

    [Header("===== 대쉬 =====")]

    [Tooltip("대쉬 속도")]
    [Range(5f, 30f)]
    public float dashSpeed = 15f;

    [Tooltip("대쉬 지속 시간 (초)")]
    [Range(0.1f, 1f)]
    public float dashDuration = 0.2f;

    [Tooltip("대쉬 횟수")]
    [Range(1, 4)]
    public int maxDashCount = 2;

    [Tooltip("대쉬 후 다시 사용 가능까지 시간 (초)")]
    [Range(0.1f, 5f)]
    public float dashCooldown = 1f;

    [Tooltip("대쉬 중 무적 시간 (초)")]
    [Range(0.05f, 1f)]
    public float dashInvincibilityTime = 0.2f;

    [Header("===== 피격 =====")]

    [Tooltip("피격 후 무적 시간 (초)")]
    [Range(0.1f, 2f)]
    public float hitInvincibilityTime = 0.5f;
}
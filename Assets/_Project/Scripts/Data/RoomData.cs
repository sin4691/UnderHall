using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "RoomData", menuName = "Game/Room Data")]
public class RoomData : ScriptableObject
{
    [Header("===== 기본 정보 =====")]

    [Tooltip("방 이름 (식별용)")]
    public string roomName;

    [Tooltip("방 타입")]
    public RoomType roomType;

    [Header("===== 방 프리팹 =====")]

    [Tooltip("이 방의 프리팹 (벽, 바닥 등)")]
    public GameObject roomPrefab;

    [Header("===== 적 스폰 =====")]

    [Tooltip("이 방에 스폰할 적 종류 + 수")]
    public List<EnemySpawnInfo> enemiesToSpawn;

    [Header("===== 보상 =====")]

    [Tooltip("클리어 시 강화 선택지 수")]
    [Range(1, 5)]
    public int BuffChoiceCount = 3;
}

[System.Serializable]
public class EnemySpawnInfo
{
    [Tooltip("적 데이터")]
    public EnemyData enemyData;

    [Tooltip("스폰 수")]
    [Range(1, 20)]
    public int count = 1;
}

public enum RoomType
{
    Combat,      // 전투방
    Rest,        // 휴식방 (회복방)
    Boss,        // 보스방
    Start        // 시작방
}
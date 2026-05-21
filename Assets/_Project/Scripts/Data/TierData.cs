using System.Collections.Generic;
using UnityEngine;

public enum EnemyType
{
    Anubis,
    Bruiser,
    Range,
    Dragon
}

[System.Serializable]
public class EnemyPrefabMapping
{
    public EnemyType enemyType;
    public GameObject enemyPrefab;
}

[System.Serializable]
public class SpawnInfo
{
    public EnemyType enemyType;
    public int count;
}

[System.Serializable]
public class WaveData
{
    public List<SpawnInfo> spawnInfos;
}

[CreateAssetMenu(fileName = "TierData", menuName = "Scriptable Objects/TierData")]
public class TierData : ScriptableObject
{
    [Header("===== 몬스터 프리팹 등록 (여기에만 넣으세요) =====")]
    public List<EnemyPrefabMapping> enemyPool;

    [Header("===== 웨이브 설정 (드롭다운) =====")]
    public WaveData wave1;
    public WaveData wave2;
    public WaveData wave3;

    public GameObject GetEnemyPrefab(EnemyType type)
    {
        foreach (var mapping in enemyPool)
        {
            if (mapping.enemyType == type)
            {
                return mapping.enemyPrefab;
            }
        }

        Debug.LogWarning($"[TierData] {type} 에 해당하는 프리팹이 최상단에 등록되지 않았습니다!");
        return null;
    }
}
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class SpawnInfo
{
    public GameObject enemyPrefab; 
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
    public WaveData wave1;
    public WaveData wave2;
    public WaveData wave3;
}

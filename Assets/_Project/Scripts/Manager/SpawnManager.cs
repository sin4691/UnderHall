using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [Header("Wave Settings")]
    public TierData tierData;
    public GameObject magicCircleVFX;
    public GameObject spawnPoofVFX;
    public float spawnDelay = 1.5f;

    [Header("Room References")]
    public Door exitDoor;

    private List<Transform> spawnPoints = new List<Transform>();
    private List<GameObject> activeEnemies = new List<GameObject>();

    private int currentWave = 1;
    private bool isSpawning = false;
    private bool isRoomCleared = false;
    private bool hasRoomStarted = false;

    private void Awake()
    {
        Transform[] allChildren = transform.root.GetComponentsInChildren<Transform>();

        foreach (Transform child in allChildren)
        {
            if (child.CompareTag("EnemySpawnPoint"))
            {
                spawnPoints.Add(child);
            }
        }

        // 디버그 1: 스폰 포인트 개수 확인
        Debug.Log($"[디버그 1] {gameObject.name} 맵 전체에서 찾은 스폰 포인트 개수: {spawnPoints.Count}개");
    }

    public void StartRoom()
    {
        // 디버그 2: 티어 데이터 정상 확인
        Debug.Log($"[디버그 2] StartRoom 호출됨! Tier Data 존재 여부: {tierData != null}");

        if (tierData == null)
        {
            Debug.LogWarning("TierData가 없습니다! 바로 문을 엽니다.");
            ClearRoom();
            return;
        }
        hasRoomStarted = true;
        StartCoroutine(SpawnWaveRoutine(currentWave));
    }

    private void Update()
    {
        if (!hasRoomStarted || isRoomCleared || isSpawning) return;

        int beforeCount = activeEnemies.Count;
        activeEnemies.RemoveAll(enemy => enemy == null || !enemy.GetComponent<Collider>().enabled);

        // 디버그 3: 몬스터가 비정상적으로 즉사했는지 확인
        if (beforeCount > 0 && activeEnemies.Count == 0)
        {
            Debug.Log($"[디버그 3] {currentWave}웨이브 몬스터 전멸 감지!");
        }

        if (activeEnemies.Count == 0)
        {
            currentWave++;

            if (currentWave > 3)
            {
                ClearRoom();
            }
            else
            {
                StartCoroutine(SpawnWaveRoutine(currentWave));
            }
        }
    }

    private IEnumerator SpawnWaveRoutine(int wave)
    {
        isSpawning = true;
        WaveData currentWaveData = null;

        if (wave == 1) currentWaveData = tierData.wave1;
        else if (wave == 2) currentWaveData = tierData.wave2;
        else if (wave == 3) currentWaveData = tierData.wave3;

        if (currentWaveData == null || currentWaveData.spawnInfos.Count == 0)
        {
            // 디버그 4: 웨이브 데이터가 비어있는지 확인
            Debug.LogWarning($"[디버그 4] {wave}웨이브에 설정된 몬스터가 0마리입니다! 바로 다음 웨이브로 넘어갑니다.");
            isSpawning = false;
            yield break;
        }

        List<GameObject> enemiesToSpawn = new List<GameObject>();
        foreach (var info in currentWaveData.spawnInfos)
        {
            for (int i = 0; i < info.count; i++)
            {
                enemiesToSpawn.Add(info.enemyPrefab);
            }
        }

        // 디버그 5: 스폰 대기 중인 몬스터 마릿수 확인
        Debug.Log($"[디버그 5] {wave}웨이브 스폰 시작! 총 {enemiesToSpawn.Count}마리 스폰 예정.");

        List<Transform> availablePoints = new List<Transform>(spawnPoints);

        foreach (GameObject enemyPrefab in enemiesToSpawn)
        {
            if (availablePoints.Count == 0)
            {
                Debug.LogWarning("스폰 자리가 부족해서 남은 몬스터를 소환할 수 없습니다!");
                break;
            }

            int randomIndex = Random.Range(0, availablePoints.Count);
            Transform selectedPoint = availablePoints[randomIndex];

            availablePoints.RemoveAt(randomIndex);

            StartCoroutine(SpawnSingleEnemy(enemyPrefab, selectedPoint.position));
        }

        yield return new WaitForSeconds(spawnDelay + 0.1f);
        isSpawning = false;
    }

    private IEnumerator SpawnSingleEnemy(GameObject enemyPrefab, Vector3 spawnPos)
    {
        GameObject vfx = null;
        if (magicCircleVFX != null)
        {
            vfx = Instantiate(magicCircleVFX, spawnPos, Quaternion.Euler(0, 0, 0));
        }

        yield return new WaitForSeconds(spawnDelay);

        if (vfx != null) Destroy(vfx);

        if (spawnPoofVFX != null)
        {
            // (파티클 길이에 맞춰 2f 숫자를 조절하세요)
            GameObject poof = Instantiate(spawnPoofVFX, spawnPos, Quaternion.Euler(-90, 0, 0));
            Destroy(poof, 2f);
        }

        GameObject spawnedEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        activeEnemies.Add(spawnedEnemy);

        // 디버그 6: 정상 스폰 확인
        Debug.Log($"[디버그 6] {spawnedEnemy.name} 한 마리 스폰 완료!");
    }

    private void ClearRoom()
    {
        isRoomCleared = true;
        Debug.Log("[디버그 7] 모든 웨이브 클리어! 문을 엽니다.");
        if (exitDoor != null)
        {
            exitDoor.UnlockDoor();
        }
    }
}
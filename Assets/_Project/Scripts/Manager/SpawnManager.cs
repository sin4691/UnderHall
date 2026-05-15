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
    public Door[] exitDoors;

    [Header("Reward Settings")]
    public GameObject rewardMaxHealthPrefab;
    public GameObject rewardGiftPrefab;
    public GameObject rewardGoldPrefab;

    [Header("Reward Icon Database")]
    public Sprite healthIcon;
    public Sprite giftIcon;
    public Sprite goldIcon;

    [HideInInspector]
    public RewardType currentRoomReward; // GameManager가 맵 넘길 때 세팅해줍니다.

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
    }

    public void StartRoom()
    {
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

        List<Transform> availablePoints = new List<Transform>(spawnPoints);

        foreach (GameObject enemyPrefab in enemiesToSpawn)
        {
            if (availablePoints.Count == 0) break;

            int randomIndex = Random.Range(0, availablePoints.Count);
            Transform selectedPoint = availablePoints[randomIndex];

            availablePoints.RemoveAt(randomIndex);
            StartCoroutine(SpawnSingleEnemy(enemyPrefab, selectedPoint.position, selectedPoint.rotation));
        }

        yield return new WaitForSeconds(spawnDelay + 0.1f);
        isSpawning = false;
    }

    private IEnumerator SpawnSingleEnemy(GameObject enemyPrefab, Vector3 spawnPos, Quaternion spawnRot)
    {
        GameObject vfx = null;
        if (magicCircleVFX != null)
        {
            vfx = Instantiate(magicCircleVFX, spawnPos, Quaternion.Euler(0, 0, 0));
        }

        yield return new WaitForSeconds(spawnDelay);

        if (vfx != null) Destroy(vfx);
        Quaternion reversedRot = spawnRot * Quaternion.Euler(0f, -180f, 0f);
        if (spawnPoofVFX != null)
        {
            GameObject poof = Instantiate(spawnPoofVFX, spawnPos, Quaternion.Euler(-90, 0, 0));
            Destroy(poof, 2f);
        }

        GameObject spawnedEnemy = Instantiate(enemyPrefab, spawnPos, reversedRot);
        activeEnemies.Add(spawnedEnemy);
    }

    private void ClearRoom()
    {
        isRoomCleared = true;
        Debug.Log("[디버그 7] 모든 웨이브 클리어! 보상을 스폰합니다.");

        //창우_ 방 클리어 VFX (SpawnManager 위치 기준)
        if (VFXManager.Instance != null)
            VFXManager.Instance.PlayRoomClear(transform.position);

        StartCoroutine(ClearRoomRoutine());//창우_클리어 VFX 재생 후 보상 스폰하는 코루틴

        //원래코드_SpawnReward();
    }

    private IEnumerator ClearRoomRoutine()//창우_ 클리어 VFX 재생 후 딜레이 주는 코루틴
    {
        //창우_클리어 VFX 먼저 재생
        if (VFXManager.Instance != null)
            VFXManager.Instance.PlayRoomClear(transform.position);
        //창우_이 딜레이 동안 클리어 VFX만 나옴
        yield return new WaitForSeconds(1.2f); //창우_원하는 시간으로 조절
        //창우_딜레이 후 보상 스폰
        StartCoroutine(SpawnRewardRoutine()); //창우_코루틴으로 변경
    }


    //원래코드_private void SpawnReward()
    private IEnumerator SpawnRewardRoutine() //창우_ 보상 스폰도 코루틴으로 변경 (VFX 재생 후 딜레이 주기 위해)
    {
        // 1. 맵에서 보상이 스폰될 위치(빈 오브젝트) 찾기
        GameObject spawnPoint = GameObject.FindGameObjectWithTag("RewardSpawnPoint");
        Vector3 spawnPos = spawnPoint != null ? spawnPoint.transform.position : transform.position;

        GameObject prefabToSpawn = null;
        switch (currentRoomReward)
        {
            case RewardType.MaxHealth: prefabToSpawn = rewardMaxHealthPrefab; break;
            case RewardType.Gift: prefabToSpawn = rewardGiftPrefab; break;
            case RewardType.Gold: prefabToSpawn = rewardGoldPrefab; break;
        }

        if (prefabToSpawn != null)
        {
            //창우_VFX 먼저 재생
            if (VFXManager.Instance != null)
                VFXManager.Instance.PlayRewardAppear(spawnPos);
            //창우_VFX 재생 후 딜레이 뒤에 보상 등장
            yield return new WaitForSeconds(1.2f); // 원하는 시간으로 조절

            GameObject rewardItem = Instantiate(prefabToSpawn, spawnPos, Quaternion.Euler(45, -45, 0));

            //창우_둥둥 띄우기 시작
            StartCoroutine(FloatReward(rewardItem));

            RewardInteractable rewardScript = rewardItem.GetComponent<RewardInteractable>();
            if (rewardScript != null)
            {
                rewardScript.Initialize(this, currentRoomReward);
            }
        }
        else
        {
            Debug.LogWarning("보상 프리팹이 등록되지 않았습니다! 보상 없이 강제로 문을 엽니다.");
            OnRewardCollected();
        }
    }
    //창우_둥둥 효과
    private IEnumerator FloatReward(GameObject reward)
    {
        if (reward == null) yield break;

        Vector3 basePos = reward.transform.position;
        float floatHeight = 0.3f;   // 위아래 진폭
        float floatSpeed = 2f;      // 위아래 속도

        while (reward != null)
        {
            float newY = basePos.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
            reward.transform.position = new Vector3(basePos.x, newY, basePos.z);
            yield return null;
        }
    }

    public void OnRewardCollected()
    {
        Debug.Log("보상 획득 완료! 다음 방 보상을 배정하고 문을 엽니다.");

        AssignNextRoomRewards();

        if (exitDoors != null && exitDoors.Length > 0)
        {
            foreach (Door door in exitDoors)
            {
                if (door != null) door.UnlockDoor();
            }
        }
    }

    private void AssignNextRoomRewards()
    {
        List<RewardType> availableRewards = new List<RewardType>
        {
            RewardType.MaxHealth,
            RewardType.Gift,
            RewardType.Gold
        };

        for (int i = 0; i < availableRewards.Count; i++)
        {
            RewardType temp = availableRewards[i];
            int randomIndex = Random.Range(i, availableRewards.Count);
            availableRewards[i] = availableRewards[randomIndex];
            availableRewards[randomIndex] = temp;
        }

        int rewardIndex = 0;
        foreach (Door door in exitDoors)
        {
            if (door != null)
            {
                if (rewardIndex >= availableRewards.Count)
                {
                    Debug.LogWarning("문의 개수가 보상 종류(3개)보다 많습니다. 일부 문은 보상이 세팅되지 않습니다.");
                    break;
                }

                RewardType selectedReward = availableRewards[rewardIndex];
                rewardIndex++;

                Sprite selectedSprite = null;
                switch (selectedReward)
                {
                    case RewardType.MaxHealth: selectedSprite = healthIcon; break;
                    case RewardType.Gift: selectedSprite = giftIcon; break;
                    case RewardType.Gold: selectedSprite = goldIcon; break;
                }

                door.SetNextRoomReward(selectedReward, selectedSprite);
            }
        }
    }
}
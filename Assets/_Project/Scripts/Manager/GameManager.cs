using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player & UI")]
    public GameObject player;
    public CanvasGroup fadeCanvasGroup;
    public float fadeDuration = 1f;
    public float blackScreenDuration = 0.5f;

    [Header("Stage Settings")]
    public List<GameObject> normalRoomPrefabs;
    public GameObject bossRoomPrefab;
    public int roomsBeforeBoss = 5;

    [Header("Current State")]
    public GameObject currentMapInstance;
    private int currentRoomCount = 0;
    private bool isTransitioning = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void Start()
    {
        Debug.Log("[GameManager] 1. 게임 시작됨! 첫 번째 방을 확인합니다.");

        // 1. 방을 잘 찾았는지 확인
        if (currentMapInstance == null)
        {
            Debug.Log("[GameManager] 2. 인스펙터가 비어있어서 씬에서 직접 스폰 매니저를 찾습니다.");
            SpawnManager foundManager = FindAnyObjectByType<SpawnManager>();

            if (foundManager != null)
            {
                currentMapInstance = foundManager.transform.root.gameObject;
                Debug.Log($"[GameManager] 3. 방을 자동으로 찾았습니다: {currentMapInstance.name}");
            }
            else
            {
                Debug.LogError("[GameManager] 씬 전체를 뒤졌는데 SpawnManager가 안 보입니다!");
            }
        }
        else
        {
            Debug.Log($"[GameManager] 2. 인스펙터에 미리 등록된 방을 사용합니다: {currentMapInstance.name}");
        }

        // 2. 스폰 매니저에게 명령을 제대로 내리는지 확인
        if (currentMapInstance != null)
        {
            SpawnManager spawnManager = currentMapInstance.GetComponentInChildren<SpawnManager>();
            if (spawnManager != null)
            {
                Debug.Log("[GameManager] 4. 스폰 매니저를 찾았습니다! StartRoom 명령을 발사합니다!");
                spawnManager.StartRoom();
            }
            else
            {
                Debug.LogError($"[GameManager] {currentMapInstance.name} 방 안에 SpawnManager 스크립트가 안 붙어있습니다!");
            }
        }
    }
    public void GoToNextRoom()
    {
        if (isTransitioning) return;

        currentRoomCount++;
        GameObject nextMapToLoad = null;

        if (currentRoomCount <= roomsBeforeBoss)
        {
            int randomIndex = Random.Range(0, normalRoomPrefabs.Count);
            nextMapToLoad = normalRoomPrefabs[randomIndex];
        }
        else if (currentRoomCount == roomsBeforeBoss + 1)
        {
            nextMapToLoad = bossRoomPrefab;
        }
        else
        {
            Debug.Log("보스 클리어! 다음 스테이지나 엔딩을 준비하세요.");
            return;
        }

        StartCoroutine(MapTransitionRoutine(nextMapToLoad));
    }

    private IEnumerator MapTransitionRoutine(GameObject nextMapPrefab)
    {
        isTransitioning = true;

        Player p = player.GetComponent<Player>();
        if (p != null)
        {
            p.attack.CancelAttack();
        }
        yield return StartCoroutine(Fade(1f));

        if (currentMapInstance != null)
        {
            Destroy(currentMapInstance);
            yield return null;
        }

        currentMapInstance = Instantiate(nextMapPrefab, Vector3.zero, Quaternion.identity);

        Transform spawnPoint = currentMapInstance.transform.Find("SpawnPoint");
        if (spawnPoint != null)
        {
            Rigidbody rb = player.GetComponent<Rigidbody>();

            Vector3 safePos = spawnPoint.position + new Vector3(0f, 1.0f, 0f);

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                player.transform.position = safePos;
                rb.position = safePos;
                rb.rotation = spawnPoint.rotation;
                Physics.SyncTransforms();
            }
            else
            {
                player.transform.position = safePos;
                player.transform.rotation = spawnPoint.rotation;
                Physics.SyncTransforms();
            }
        }
        else
        {
            Debug.LogWarning("새 맵 프리팹 안에 'SpawnPoint'라는 이름의 오브젝트가 없습니다!");
        }
        yield return new WaitForSeconds(blackScreenDuration);

        yield return StartCoroutine(Fade(0f));

        SpawnManager spawnManager = currentMapInstance.GetComponentInChildren<SpawnManager>();
        if (spawnManager != null)
        {
            spawnManager.StartRoom();
        }

        isTransitioning = false;
    }

    private IEnumerator Fade(float targetAlpha)
    {
        if (fadeCanvasGroup == null) yield break;

        float startAlpha = fadeCanvasGroup.alpha;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
    }
}
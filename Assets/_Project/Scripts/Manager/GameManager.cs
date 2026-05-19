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
    public StageData stageData;

    [Header("Current State")]
    public GameObject currentMapInstance;
    private int currentRoomIndex = 0;
    private bool isTransitioning = false;

    private RewardType upcomingReward;
    public PlayerData playerData;

    private void Awake()
    {
        if (playerData != null) playerData.LoadFromDevice();
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        upcomingReward = (RewardType)Random.Range(0, 3);

        if (player != null)
        {
            Player p = player.GetComponent<Player>();
            if (p != null && p.playerData != null)
            {
                p.playerData.ResetRunData();
            }
        }

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 1f;
            fadeCanvasGroup.blocksRaycasts = true; 
        }
    }

    private void Start()
    {
        playerData.ResetRunData();

        Debug.Log("[GameManager] 게임 시작됨! 첫 번째 방을 설정합니다.");
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGM("Stage_BGM");
        }

        if (fadeCanvasGroup != null)
        {
            StartCoroutine(Fade(0f));
        }
        if (currentMapInstance == null)
        {
            SpawnManager foundManager = FindAnyObjectByType<SpawnManager>();
            if (foundManager != null)
            {
                currentMapInstance = foundManager.transform.root.gameObject;
            }
            else
            {
                Debug.LogError("[GameManager] 씬에 SpawnManager가 없습니다!");
            }
        }

        if (currentMapInstance != null)
        {
            SpawnManager spawnManager = currentMapInstance.GetComponentInChildren<SpawnManager>();
            if (spawnManager != null)
            {
                spawnManager.currentRoomReward = upcomingReward;
                spawnManager.StartRoom();
            }
        }
    }

    // Door에서 넘겨준 보상 타입을 받아옵니다.
    public void GoToNextRoom(RewardType selectedReward)
    {
        if (isTransitioning) return;

        upcomingReward = selectedReward;
        currentRoomIndex++;

        if (stageData != null && currentRoomIndex < stageData.roomSequence.Count)
        {
            GameObject nextMapToLoad = stageData.roomSequence[currentRoomIndex];
            StartCoroutine(MapTransitionRoutine(nextMapToLoad));
        }
        else
        {
            Debug.Log("모든 스테이지 클리어! 골드를 정산합니다.");
            Player p = player.GetComponent<Player>();
            if (p != null) p.CommitGoldToSO();
        }
    }

    private IEnumerator MapTransitionRoutine(GameObject nextMapPrefab)
    {
        isTransitioning = true;

        Player p = player.GetComponent<Player>();
        if (p != null) p.attack.CancelAttack();

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

        yield return new WaitForSeconds(blackScreenDuration);
        yield return StartCoroutine(Fade(0f));

        SpawnManager spawnManager = currentMapInstance.GetComponentInChildren<SpawnManager>();
        if (spawnManager != null)
        {
            // 방금 기억해둔 보상을 새 매니저에게 전달하고 전투 시작
            spawnManager.currentRoomReward = upcomingReward;
            spawnManager.StartRoom();
        }

        isTransitioning = false;
    }

    public IEnumerator Fade(float targetAlpha)
    {
        if (fadeCanvasGroup == null) yield break;
        float startAlpha = fadeCanvasGroup.alpha;
        float time = 0f;

        fadeCanvasGroup.blocksRaycasts = true;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = targetAlpha;

        if (targetAlpha == 0f)
        {
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }
}
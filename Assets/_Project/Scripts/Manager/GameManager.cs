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

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        Debug.Log("[GameManager] 게임 시작됨! 첫 번째 방을 설정합니다.");

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
            if (spawnManager != null) spawnManager.StartRoom();
        }
    }

    public void GoToNextRoom()
    {
        if (isTransitioning) return;

        currentRoomIndex++;

        if (stageData != null && currentRoomIndex < stageData.roomSequence.Count)
        {
            GameObject nextMapToLoad = stageData.roomSequence[currentRoomIndex];
            StartCoroutine(MapTransitionRoutine(nextMapToLoad));
        }
        else
        {
            Debug.Log("모든 스테이지를 클리어했습니다! 결과를 준비하세요.");
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
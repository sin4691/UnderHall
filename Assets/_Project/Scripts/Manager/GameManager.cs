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
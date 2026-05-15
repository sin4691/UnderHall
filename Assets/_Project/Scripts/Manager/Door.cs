using UnityEngine;

public class Door : MonoBehaviour
{
    private bool isLocked = true;
    public bool IsLocked => isLocked;

    [Header("Next Room Reward")]
    public RewardType nextRewardType;
    // public SpriteRenderer rewardIconRenderer; // 필요 시 UI 아이콘을 띄울 때 사용

    public void SetNextRoomReward(RewardType reward)
    {
        nextRewardType = reward;
        // 이 부분에 문 위에 보상 아이콘을 바꾸는 코드를 넣으시면 됩니다.
    }

    public void UnlockDoor()
    {
        isLocked = false;
        Debug.Log($"문이 열렸습니다! (다음 방 보상: {nextRewardType})");
    }

    public void Interact()
    {
        GameManager.Instance.GoToNextRoom(nextRewardType);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Player player = other.GetComponent<Player>();
        if (player != null) player.SetNearbyDoor(this);

        if (!isLocked && UIManager.Instance != null)
            UIManager.Instance.ShowInteractPrompt();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Player player = other.GetComponent<Player>();
        if (player != null) player.ClearNearbyDoor(this);

        if (UIManager.Instance != null)
            UIManager.Instance.HideInteractPrompt();
    }

    void OnDestroy()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.HideInteractPrompt();
    }
}
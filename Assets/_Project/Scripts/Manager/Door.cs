using UnityEngine;

public class Door : MonoBehaviour
{
    private bool isLocked = true;
    public bool IsLocked => isLocked;

    [Header("Next Room Reward")]
    public RewardType nextRewardType;

    [Header("Visual Settings")]
    public SpriteRenderer rewardIconRenderer;
    public void SetNextRoomReward(RewardType type, Sprite icon)
    {
        nextRewardType = type;

        if (rewardIconRenderer != null)
        {
            rewardIconRenderer.sprite = icon;
        }
    }

   
    public void UnlockDoor()
    {
        isLocked = false;
        if (rewardIconRenderer != null) rewardIconRenderer.color = Color.white;
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
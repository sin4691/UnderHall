using UnityEngine;

public class Door : MonoBehaviour
{
    private bool isLocked = true;
    public bool IsLocked => isLocked;

    public void UnlockDoor()
    {
        isLocked = false;
        Debug.Log("문이 열렸습니다!");
    }

    public void Interact()
    {
        GameManager.Instance.GoToNextRoom();
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
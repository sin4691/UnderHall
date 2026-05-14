using UnityEngine;

public class Door : MonoBehaviour
{
    public bool isLocked = true; 

    public void UnlockDoor()
    {
        isLocked = false;
        Debug.Log("문이 열렸습니다!");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isLocked && other.CompareTag("Player"))
        {
            GameManager.Instance.GoToNextRoom();
        }
    }
}
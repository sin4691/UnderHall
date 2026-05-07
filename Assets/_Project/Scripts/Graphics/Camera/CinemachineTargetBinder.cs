using UnityEngine;
using Unity.Cinemachine; 

public class CinemachineTargetBinder : MonoBehaviour
{
    private CinemachineCamera _vcam; 

    void Start()
    {
        _vcam = GetComponent<CinemachineCamera>();

        // "Player" 태그로 플레이어를 찾음
        GameObject player = GameObject.FindWithTag("Player");

        if (player != null)
        {
            _vcam.Follow = player.transform;
            _vcam.LookAt = player.transform;
        }
    }
}
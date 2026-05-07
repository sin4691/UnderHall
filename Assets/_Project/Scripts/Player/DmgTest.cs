using UnityEngine;

public class DmgTest : MonoBehaviour
{
    [Header("Monster Stats")]
    public float currentHealth = 50f;

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        Debug.Log($"몬스터 피격! 들어온 데미지: {damage} / 남은 체력: {currentHealth}");

        if (currentHealth <= 0)
        {
            Debug.Log("몬스터 처치됨!");
            Destroy(gameObject);
        }
    }
}
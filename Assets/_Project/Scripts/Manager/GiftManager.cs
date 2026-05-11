using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

[System.Serializable]
public class GiftInfo
{
    public GiftType type;
    public string giftName;
    [TextArea] public string description;
}

public class GiftManager : MonoBehaviour
{
    public PlayerData playerData;

    [Header("UI 연결 (Canvas)")]
    public GameObject giftUIPanel; // 기프트 선택창 전체 패널
    public Button[] giftButtons;   // 3개의 선택 버튼
    public TextMeshProUGUI[] giftNameTexts;
    public TextMeshProUGUI[] giftDescTexts;

    [Header("전체 기프트 데이터베이스")]
    public List<GiftInfo> allGifts = new List<GiftInfo>();

    private void Awake()
    {
        if (allGifts.Count == 0)
        {
            //allGifts.Add(new GiftInfo { type = GiftType.Execution, giftName = "처형", description = "체력 20% 이하 적에게 데미지 2배" });
            //allGifts.Add(new GiftInfo { type = GiftType.Combo, giftName = "연격", description = "기본공격 속도 +25%" });
            //allGifts.Add(new GiftInfo { type = GiftType.Critical, giftName = "치명타", description = "기본공격 15% 확률로 데미지 2배" });
            //allGifts.Add(new GiftInfo { type = GiftType.Explosion, giftName = "폭발", description = "특수공격 착탄 시 범위 데미지" });
            //allGifts.Add(new GiftInfo { type = GiftType.RapidFire, giftName = "속사", description = "특수공격 쿨다운 -30%" });
            //allGifts.Add(new GiftInfo { type = GiftType.Endurance, giftName = "지속력", description = "특수공격 데미지 +30%" });
            //allGifts.Add(new GiftInfo { type = GiftType.Awakening, giftName = "각성", description = "대쉬 직후 1초간 다음 공격 2배" });
            //allGifts.Add(new GiftInfo { type = GiftType.Vampirism, giftName = "흡혈", description = "적 처치 시 체력 5회복" });
            //allGifts.Add(new GiftInfo { type = GiftType.Berserk, giftName = "광폭", description = "체력 50% 이하일 때 데미지 +40%" });

            allGifts.Add(new GiftInfo { type = GiftType.Execution, giftName = "Execution", description = "Deal 2x damage to enemies below 20% HP" });
            allGifts.Add(new GiftInfo { type = GiftType.Combo, giftName = "Combo", description = "Basic attack speed +25%" });
            allGifts.Add(new GiftInfo { type = GiftType.Critical, giftName = "Critical", description = "Basic attacks have a 15% chance to deal 2x damage" });
            allGifts.Add(new GiftInfo { type = GiftType.Explosion, giftName = "Explosion", description = "Special attacks deal area damage on impact" });
            allGifts.Add(new GiftInfo { type = GiftType.RapidFire, giftName = "RapidFire", description = "Special attack cooldown -30%" });
            allGifts.Add(new GiftInfo { type = GiftType.Endurance, giftName = "Endurance", description = "Special attack damage +30%" });
            allGifts.Add(new GiftInfo { type = GiftType.Awakening, giftName = "Awakening", description = "For 1 second after dashing, your next attack deals 2x damage" });
            allGifts.Add(new GiftInfo { type = GiftType.Vampirism, giftName = "Vampirism", description = "Restore 5 HP when defeating an enemy" });
            allGifts.Add(new GiftInfo { type = GiftType.Berserk, giftName = "Berserk", description = "Deal 40% more damage when below 50% HP" });
        }
    }

    public void OpenGiftUI()
    {
        // 이미 획득한 스킬 걸러내기
        List<GiftInfo> availableGifts = allGifts.Where(g => !playerData.acquiredGifts.Contains(g.type)).ToList();

        if (availableGifts.Count == 0)
        {
            Debug.Log("더 이상 획득할 기프트가 없습니다!");
            return;
        }

        // 남은 스킬들 순서를 랜덤하게 섞기
        for (int i = 0; i < availableGifts.Count; i++)
        {
            GiftInfo temp = availableGifts[i];
            int randomIndex = Random.Range(i, availableGifts.Count);
            availableGifts[i] = availableGifts[randomIndex];
            availableGifts[randomIndex] = temp;
        }

        // 앞에서부터 최대 3개 뽑기
        int optionsCount = Mathf.Min(3, availableGifts.Count);

        // UI 텍스트 입히고 버튼 기능 연결하기
        for (int i = 0; i < 3; i++)
        {
            if (i < optionsCount)
            {
                giftButtons[i].gameObject.SetActive(true);
                giftNameTexts[i].text = availableGifts[i].giftName;
                giftDescTexts[i].text = availableGifts[i].description;

                // 버튼 이벤트 초기화 후, 새 스킬 부여 이벤트 달아주기
                giftButtons[i].onClick.RemoveAllListeners();
                GiftType selectedType = availableGifts[i].type; // 클로저 이슈 방지
                giftButtons[i].onClick.AddListener(() => SelectGift(selectedType));
            }
            else
            {
                // 남은 스킬이 부족하면 남는 버튼은 숨김
                giftButtons[i].gameObject.SetActive(false);
            }
        }

        // 게임 시간 정지 및 UI 활성화
        Time.timeScale = 0f;
        giftUIPanel.SetActive(true);
    }

    /// <summary> 버튼을 클릭해서 기프트를 선택했을 때 </summary>
    public void SelectGift(GiftType type)
    {
        playerData.acquiredGifts.Add(type);
        giftUIPanel.SetActive(false);
        Time.timeScale = 1f;

        // 3. (선택 사항) 연격(공속)이나 속사(쿨타임) 같은 스탯류는 여기서 바로 스탯을 변경해 주면 좋습니다!
    }
}
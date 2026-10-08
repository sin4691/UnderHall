# 🗡️ UnderHall — 쿼터뷰 액션 로그라이크

> Unity로 제작한 하데스(Hades)류 쿼터뷰 던전 로그라이크 게임입니다.
> 던전을 한 방씩 돌파하며 보상을 선택하고, 마지막 보스를 처치하는 것이 목표입니다.


---

📂 자세한 설명: [포트폴리오 – UnderHall](https://sin4691.github.io/#under-hall)

## 👀 신재윤 작업만 보기

4인 팀 프로젝트라 스크립트 대부분은 팀원 코드입니다. 아래 파일만 보시면 제가 만든 부분입니다. 비중은 현재 코드 기준 `git blame`으로 셌습니다.

| 기능 | 파일 | 비중 |
|---|---|---|
| 방 프리팹 8종 (문·스폰 지점·NavMesh) | [`Assets/_Project/Prefabs/Room`](Assets/_Project/Prefabs/Room) | 폴더 전체 |
| 로비 강화 카드 UI | [`UpgradeCardUI.cs`](Assets/_Project/Scripts/UI/MainMenu/UpgradeCardUI.cs) · [`MataUpgradePanel.cs`](Assets/_Project/Scripts/UI/MainMenu/MataUpgradePanel.cs) · [`ButtonTextColor.cs`](Assets/_Project/Scripts/UI/MainMenu/ButtonTextColor.cs) | 전부 |
| 강화 UI용 이벤트·읽기 전용 Getter | [`LobbyManager.cs`](Assets/_Project/Scripts/Manager/LobbyManager.cs) | 일부 (16줄) |
| 문 상호작용 (근처 등록 + 입력으로 이동) | [`Door.cs`](Assets/_Project/Scripts/Manager/Door.cs) 트리거·`Interact` · [`Player.cs`](Assets/_Project/Scripts/Player/Player.cs) `OnInteract` | 일부 |
| 메인 메뉴·일시정지 | [`MainMenuManager.cs`](Assets/_Project/Scripts/UI/MainMenu/MainMenuManager.cs) · [`PauseMenuManager.cs`](Assets/_Project/Scripts/Manager/PauseMenuManager.cs) | 일부 |
| 보상 카테고리 아이콘·설명 | [`GiftManager.cs`](Assets/_Project/Scripts/Manager/GiftManager.cs) | 일부 |
| 빌드 한글 크래시 해결 (정적 폰트) | [`Assets/_Project/Fonts`](Assets/_Project/Fonts) | 전부 |

[제 커밋만 모아 보기](https://github.com/sin4691/UnderHall/commits?author=sin4691)

## 🙋 내가 맡은 것 (신재윤 · 팀장)

- **팀장·협업**: Git Flow와 커밋 규칙, Git 협업 가이드 공지, 팀원별 개인 작업 씬으로 씬 충돌 예방, Discord–GitHub PR 알림, PR 확인 후 병합
- **방 프리팹 8종**: 방마다 문·스폰 지점·NavMesh를 배치해 게임 루프에 연결
- **문 상호작용**: 근처에 들어오면 등록만 하고 입력으로 이동하는 구조와 안내 UI (팀원이 보상 줍기에 같은 패턴 재사용)
- **UI**: 로비 강화 카드(이벤트 구독으로 카드·골드 동시 갱신), 메인 메뉴·설정·일시정지
- **빌드 한글 크래시 해결**: TMP Dynamic 폰트를 상용 한글 2,350자 Static 아틀라스로 교체
- **데이터**: 몬스터·보스·플레이어·강화 수치를 구글 시트로 관리, 음원 선정

---

## 📖 게임 소개

| 항목 | 내용 |
| --- | --- |
| **장르** | 쿼터뷰 로그라이크 (하데스류) |
| **비주얼** | 로우폴리 (Low Poly) |
| **스테이지** | 1 스테이지 / 방 6~7개 (전투방 → 휴식방 → 보스방) |
| **진행 방식** | 단방향 (이전 방으로 복귀 불가) |
| **클리어 조건** | 각 방의 적 전멸 → 보상 선택 → 다음 방, 최종 보스 처치 시 클리어 |
| **플레이 시간** | 약 5~10분 (1 런 기준) |

### 핵심 루프
적 처치 → 보상 드롭 → 문에 표시된 3가지 보상(최대 체력 / 강화 / 골드) 중 선택 →
다음 방 입장 시 선택한 보상 지급 → 반복 → 보스 처치 → 클리어

게임 내 강화는 한 런(run) 동안만 유지되며, 사망하거나 클리어하면 초기화됩니다.
획득한 골드는 영구 저장되어, 로비의 **메타 강화**(영구 강화)에 사용할 수 있습니다.

---

## 🎮 조작 방법

| 동작 | 키 / 입력 |
| --- | --- |
| **이동** | `W` `A` `S` `D` |
| **기본 공격** | 마우스 좌클릭 |
| **특수 공격 (무기 스킬)** | 마우스 우클릭 |
| **대쉬 (회피)** | `Space` (쿨다운 1초 / 무적 0.2초) |
| **일시정지** | `ESC` |

> 💡 대쉬 중 짧은 무적 시간이 있으니, 적의 공격을 회피하는 데 활용하세요.

---

## ⚙️ 시스템 요소

### 강화 시스템
- **인게임 강화 (런 한정)** — 방 클리어 보상으로 획득. 공격 / 대쉬 / 패시브 카테고리.
  사망 또는 클리어 시 초기화됩니다.
- **메타 강화 (영구)** — 골드로 로비에서 구매하는 영구 강화.
  | 종류 | 설명 | 최대 레벨 |
  | --- | --- | --- |
  | `META_HP` | 최대 체력 증가 | 5 |
  | `META_ATK` | 공격력 증가 | 5 |
  | `META_GOLD` | 골드 획득량 증가 | 5 |
  | `META_REVIVE` | 부활 | 5 |
  | `META_SPEED` | 이동속도 증가 | 5 |
  | `META_DASH` | 대쉬 강화 | 1 |

### 보상
적 처치 후 드롭한 보상을 획득하면 다음 방으로 향하는 문에 **최대 체력 / 강화 / 골드**
중 하나가 무작위로 표시됩니다. 원하는 문을 선택하면 다음 방 입장 시 해당 보상이 지급됩니다.

---

## 🚀 실행 방법

1. 저장소를 클론합니다.
   ```bash
   git clone https://github.com/sin4691/UnderHall.git
   ```
2. **Unity 6.4 (6000.4.5f1)** 버전으로 프로젝트를 엽니다.
3. **시작 씬**을 엽니다.
   ```
   Assets/_Project/Scenes/Main/MainMenu.unity
   ```
4. 상단 ▶ (Play) 버튼을 눌러 실행합니다. (외부 에셋은 저장소에 없으므로 따로 넣어야 화면이 정상으로 보입니다.)
5. 메인 메뉴에서 **시작** 버튼을 누르면 게임이 시작됩니다.

---

## 🛠️ 개발 환경

| 항목 | 내용 |
| --- | --- |
| **엔진** | Unity 6.4 (6000.4.5f1) |
| **렌더 파이프라인** | URP (Universal Render Pipeline) |
| **버전 관리** | Git + GitHub |
| **커뮤니케이션** | Discord |

---

## 👥 팀 구성 및 역할

| 이름 | 메인 역할 | 비고 |
| --- | --- | --- |
| 배창현 | 플레이어 / 시스템 | |
| 서동연 | 적 / AI | |
| 정창우 | 그래픽 / 셰이더 | |
| 신재윤 | 팀장 · 방 프리팹 · UI · 통합 | 밸런싱 · 데이터 총괄 |

## 외부 에셋

사용한 에셋: POLYGON Dungeon·POLYGON Particle FX(Synty), DOTween Pro, All In 1 Sprite Shader, Layer Lab 아이콘, Hun0FX·VFX Klaus·Special Skills 이펙트, polyperfect, 커서 팩, 몬스터 모델(Dragon·Anubis 등).

유료·스토어 에셋은 라이선스상 공개 저장소에 둘 수 없어서 저장소에서 뺐습니다. 그래서 받은 그대로는 씬의 모델·UI가 비어 보입니다. 코드는 모두 그대로 있습니다.

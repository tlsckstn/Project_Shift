# Story 002: 보드 표시와 10진수 값 (BoardView)

> **Epic**: night-shift
> **Status**: Ready
> **Layer**: Presentation
> **Type**: UI
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature 1 (8칸 줄 2~3개를 그리는 보드와 줄별 10진수 표시)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.3.14f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md`, scoped to this story:*

- [ ] 8칸짜리 가로줄이 위에서 아래로 2~3줄 그려진다. 켜진 비트(1)는 발판, 꺼진 비트(0)는 빈칸으로 구분된다.
- [ ] 각 줄 오른쪽에 현재 값이 10진수로 표시된다 (예: `00000110` → 6).
- [ ] `BoardState`가 바뀌면 화면이 다시 그려진다 (`BoardLogic`을 직접 호출하지 않고 상태만 받는다).
- [ ] 로봇, 출구, 현재 선택된 줄이 눈으로 구분된다 (도형과 색만 사용).
- [ ] 1280x720 창에서 8칸 줄과 10진수 표시가 잘리지 않는다.

---

## Implementation Notes

- 그래픽은 단색 사각형(스프라이트)만 쓴다. 칸 오브젝트는 풀로 만들어 재사용하고 런타임 `Instantiate`/`Destroy`를 피한다.
- `transform`은 `Awake`에서 캐싱한다. `Update`에서 `GetComponent`/`Find` 금지.
- 매 프레임 문자열 조합 금지: 10진수 표시는 값이 바뀔 때만 갱신한다.
- 사용 렌더러는 이미 있는 2D URP.

---

## Out of Scope

- Story 003: 로봇 이동 로직
- 애니메이션, 사운드, 타이틀·메뉴

---

## Files

- `Assets/00_Scripts/Board/BoardView.cs`
- `Assets/Scenes/` 의 게임 씬 (씬 파일 수정은 사용자 확인 후)

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: UI
**Required evidence**: 터치한 화면의 스크린샷을 `production/qa/evidence/`에 보존한다 (`qa.level: minimal`에서도 면제되지 않음).

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/night-shift/story-001-shift-logic.md`
- Unlocks: `production/epics/night-shift/story-006-exit-and-stages.md`

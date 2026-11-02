# Story 006: 출구 판정과 스테이지 5개 진행

> **Epic**: night-shift
> **Status**: Done
> **Layer**: Feature
> **Type**: Integration
> **Estimate**: L
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-02

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature 6 (출구 도달 판정과 스테이지 5개 순차 진행, `StageData`)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.3.14f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md`, scoped to this story:*

- [x] 로봇이 출구 좌표에 도달하면 클리어로 판정되고 다음 스테이지가 로드된다.
- [x] `StageData`(ScriptableObject)가 줄 문자열 배열(`"00000110"`), 시작 좌표, 출구 좌표, 시프트 제한을 가진다. 값은 인스펙터에서 입력한다.
- [x] 스테이지 5개가 설계표(브리프 기준 Night Shift 설계 문서)대로 입력되어 있다: 제한 6 / 7 / 6 / 5 / 11.
- [x] 각 스테이지의 의도한 해답을 직접 풀어 검증한다. 더 짧은 해답이 나오면 제한을 줄이거나 배치를 고친다.
- [x] 마지막 스테이지를 클리어하면 더 이상 로드하지 않고 종료 상태가 표시된다.
- [x] 키보드 입력이 연결된다: `←` `→` 걷기, `↓` 내려가기, `W` `S` 줄 선택, `Q` `<<`, `E` `>>`, `Z` 되돌리기, `R` 재시작.
- [x] 추락하면 현재 스테이지가 즉시 재시작된다.

---

## Implementation Notes

- `InputController`: 키 입력 → `BoardLogic` 호출 → `BoardView` 갱신. `StageManager`: 스테이지 로드와 클리어 시 다음 스테이지.
- 스테이지 배치 (줄 0 / 1 / 2, 좌표는 (줄, 칸)):
  1. `11000000`, (0,0)→(0,7), 제한 6
  2. `10000000` / `00000001`, (0,0)→(1,0), 제한 7
  3. `00000001` / `11000000`, (0,7)→(1,7), 제한 6
  4. `11100000` / `00000101`, (0,0)→(1,7), 제한 5
  5. `11000000` / `00000110` / `10000000`, (0,0)→(2,7), 제한 11
- 스테이지 5의 하한은 11회다 (줄 2에 비트가 칸 0에 하나뿐이라 `>>` 7회 필수, 줄 0·1을 만나게 하는 데 최소 4회).
- 레벨 에디터 없이 `StageData`를 인스펙터 문자열로 입력한다.
- 씬 파일(`.unity`) 수정은 사용자 확인 후에만 한다.

---

## Out of Scope

- 별 평가, 저장, 타이틀·메뉴
- 월드 2 이후 규칙 (로테이트, 캐리, 산술 시프트)

---

## Files

- `Assets/00_Scripts/Stage/StageData.cs`
- `Assets/00_Scripts/Stage/StageManager.cs`
- `Assets/00_Scripts/Input/InputController.cs`
- `Assets/Data/Stages/Stage01.asset` … `Stage05.asset`

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: 통합 테스트(`Assets/Tests/EditMode/Stage/StageFlowTests.cs`) 또는 플레이 테스트 기록(5개 스테이지 직접 클리어). 테스트는 `qa.level: minimal`에서 면제.

**Status**: [x] 플레이 모드 검증: StageManager 공개 메서드로 5개 스테이지 해답 실행, 추락 재시작·Z·R 확인. BFS로 각 스테이지 최단 해답 = 제한 (6/7/6/5/11). 스크린샷 `production/qa/evidence/story-006-stage5-mid.png`, `story-006-all-clear.png`. 실제 키 입력은 사용자 확인 필요.

---

## Dependencies

- Depends on: `production/epics/night-shift/story-002-board-view.md`, `story-003-robot-movement.md`, `story-004-shift-limit.md`, `story-005-undo-restart.md`
- Unlocks: `production/epics/night-shift/story-007-webgl-build.md`

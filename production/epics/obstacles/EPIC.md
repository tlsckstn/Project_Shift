# Epic: Obstacles (장애물 3종)

> **Layer**: Core (BoardLogic 규칙) + Feature (스테이지)
> **GDD**: design/game-brief.md ("다음 단계 — 플레이테스트 2026-11-02 반영": 버그 칸, 방화벽, 잠긴 줄)
> **Architecture Module**: Board (BoardState, BoardLogic, BoardView) + Stage (StageData)
> **Status**: Ready
> **Depends on**: onboarding (안내문 필드, par 표시를 소개 스테이지에서 재사용)
> **Stories**: 4 stories (see below)

## Overview

- 플레이테스트 요청(장애물 기믹)에 따라 장애물 3종을 추가한다 (근거: `production/qa/playtests/playtest-2026-11-02-tester1.md`).
- 장애물 튜토리얼은 이 에픽의 소개 스테이지가 맡는다. Onboarding의 튜토리얼 스테이지는 고치지 않는다.
- 장애물은 모두 밟거나 밀리면 작동하는 칸이라 새 조작키가 없다. 조작키 패널은 그대로 둔다.

## Scope

1. 잠긴 줄: 밀 수 없는 줄 + 표시
2. 방화벽: 보드 좌표에 고정. 걷기·내려가기로 진입 불가, 줄에 실려 밀리면 통과, 벽 칸 위에서 멈추면 서 있을 수 있음 + 표시
3. 버그 칸: 보드 좌표에 고정. 걷기·내려가기·줄에 실려 밀리기 중 어떤 방식으로든 진입하면 추락과 같은 실패 + 표시
4. 장애물별 소개 스테이지 1개 + 활용 스테이지 1~2개. 안내문 한 줄과 par를 쓰고 BFS로 검증. `StageData[]` 배열에 끼워 넣는다 (코드 변경 없음)

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | 잠긴 줄 | Logic | Ready | N/A |
| 002 | 방화벽 | Logic | Ready | N/A |
| 003 | 버그 칸 | Logic | Ready | N/A |
| 004 | 장애물 소개·활용 스테이지 | Config/Data | Ready | N/A |

## Definition of Done

- Every story implemented, reviewed and closed with `/story-done`
- Every scoped item above verified in play mode
- 장애물 3종이 화면에서 색이나 모양으로 서로, 그리고 일반 발판과 구분된다 (스크린샷)
- Logic stories have passing tests in the test root (BoardLogic 장애물 규칙)
- UI stories have retained screenshots in `production/qa/evidence/`

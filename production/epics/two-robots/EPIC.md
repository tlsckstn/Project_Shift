# Epic: Two Robots (여러 로봇 2~3대, 교대 조종)

> **Layer**: Core (BoardLogic 규칙, BFS) + Feature (입력·표시·스테이지)
> **GDD**: design/game-brief.md ("다음 단계 — 여러 로봇 (교대 조종)")
> **Architecture Module**: Board (BoardState, BoardLogic, BoardView, BoardHistory) + Stage (StageData, StageManager) + Input
> **Status**: Ready
> **Depends on**: night-shift, onboarding, obstacles (모두 Complete. 장애물·par·안내문을 그대로 재사용)
> **Stories**: 6 stories (see below)

## Overview

- 특정 스테이지에서 로봇을 2대 또는 3대로 늘린다. `Q`로 조종 대상을 차례로 바꾸고, 줄 시프트는 공유한다 (결정: 교대 조종).
- 초반 스테이지(기존 14개: 튜토리얼 3개 + 스테이지 11개, 장애물 소개·활용 포함)는 로봇 1대로 그대로 유지한다. 여러 로봇 스테이지는 장애물이 모두 나온 뒤, `StageData[]` 끝에 덧붙인다 (2대 먼저, 3대는 맨 끝).

## Scope

1. `BoardState`/`BoardLogic`: 로봇 여러 대(위치, 선택), 선택 로봇만 걷기, 줄 시프트는 그 줄 위 모든 로봇을 같이 밂, 겹침 허용
2. `Q` 전환과 선택 표시 (로봇별 색 + 테두리, 3대까지)
3. 클리어 = 모든 로봇이 각자 출구에 동시에 서 있음, 실패 = 한 대라도 추락·버그 칸
4. Undo·재시작에 선택 로봇 포함 (`Q` 자체는 되돌리기 단계가 아님)
5. BFS를 여러 로봇 상태로 확장 (`Q`는 시프트 0회, par는 합산, 3대 성능 확인)
6. 2대 소개 1개 + 2대 활용 2~3개 + 3대 1~2개 (안내문 한 줄, 영어)

## Out of Scope

- 동시 조종, 로봇별 다른 이동 규칙, 로봇끼리 막기·밀기, 4대 이상
- 기존 14개 스테이지 변경

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | 로봇 2대 상태 (구현은 N대 일반) | Logic | In Progress | N/A |
| 002 | Q 전환과 선택 표시 | UI | Ready | N/A |
| 003 | 출구·실패 판정 | Logic | Ready | N/A |
| 004 | Undo·재시작에 선택 로봇 포함 | Logic | Ready | N/A |
| 005 | BFS 여러 로봇 확장 | Logic | Ready | N/A |
| 006 | 여러 로봇 소개·활용 스테이지 (2대·3대) | Config/Data | Ready | N/A |

권장 구현 순서: 001 → 003 → 004 → 005 → 002 → 006.

## Definition of Done

- Every story implemented, reviewed and closed with `/story-done`
- 기존 14개 스테이지의 par와 클리어 동작이 이전과 같다 (회귀 확인)
- 2대·3대 스테이지를 각각 처음부터 끝까지 플레이해 클리어한다 (스크린샷)
- WebGL 빌드에서 `A`/`D`/`Q`가 동작한다
- UI story has a retained screenshot in `production/qa/evidence/`

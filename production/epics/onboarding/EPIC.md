# Epic: Onboarding (튜토리얼과 제한값)

> **Layer**: Feature
> **GDD**: design/game-brief.md ("다음 단계 — 플레이테스트 2026-11-02 반영": 튜토리얼, 제한값)
> **Architecture Module**: Stage (StageData, StageManager) + BoardView
> **Status**: In progress
> **Stories**: 4 stories (see below)

## Overview

- 처음 하는 플레이어가 튜토리얼 스테이지의 안내만으로 모든 스테이지를 풀게 한다 (근거: `production/qa/playtests/playtest-2026-11-02-tester1.md`).
- 시프트 제한에 여유를 주고 최소 횟수(par)를 따로 보여준다.
- 안내문과 par는 모든 스테이지에 쓰는 일반 기능으로 만든다. Obstacles 에픽의 소개 스테이지가 그대로 재사용한다.

## Scope

1. 시프트 제한 = par + 1~2회 여유, par 따로 표시 (기존 5개 스테이지 재조정, par는 BFS 값)
2. 조작키 상시 표시 (화면 하단, 영어)
3. 스테이지별 안내문 한 줄 (`StageData` 선택 필드, 비어 있으면 표시 안 함, 규칙만 설명)
4. 튜토리얼 스테이지 2~3개 (스테이지 1 앞, 기본 규칙만: 걷기, 줄 밀기와 함께 밀림, 내려가기, 추락)

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | 시프트 제한 여유와 par 표시 | UI | Complete | N/A |
| 002 | 조작키 상시 표시 | UI | Ready | N/A |
| 003 | 스테이지별 안내문 한 줄 | UI | Ready | N/A |
| 004 | 튜토리얼 스테이지 2~3개 | Config/Data | Ready | N/A |

## Definition of Done

- Every story implemented, reviewed and closed with `/story-done`
- Every scoped item above verified in play mode
- Logic stories have passing tests in the test root
- UI stories have retained screenshots in `production/qa/evidence/`

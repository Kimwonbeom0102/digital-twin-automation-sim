# Automation Simulation (Unity)

## 📌 프로젝트 개요

Unity 기반으로 설계한 **상태(State) 중심 공정 제어 시뮬레이션 프로젝트**입니다.

정상 공정 흐름과 예외(Fault) 상황을  
상태 전이와 이벤트 기반 구조로 모델링하여  
가상 환경에서 검증할 수 있도록 구현했습니다.

---

## 🎯 핵심 설계 목표

- 상태 기반 제어 아키텍처 설계
- Stop / Resume / Fault 전파 흐름 모델링
- Queue 기반 Buffer 시스템 구현
- Update() 의존 최소화 (이벤트 중심 구조)
- 플랫폼 독립적인 Core Simulation Logic 설계

---

## 🏗 아키텍처 구조

### 🔹 State Layer
- PlantState
- ZoneState
- RobotState

각 단위를 상태 머신처럼 설계하고  
명시적인 이벤트로만 상태를 전이하도록 구현했습니다.

---

### 🔹 Event-Driven Flow

- Item 감지 → Robot 작업 트리거
- Fault 발생 → Spawn 제어 중단
- Resume → Delay 기반 Queue 복구

상태 충돌 및 중복 실행을 방지하는 구조로 설계했습니다.

---

### 🔹 Buffer & Queue

- Max Capacity 제한
- Overflow 시 +N 시각화
- Assign / Release 로직 분리

---

## 🔧 XR 확장 구조

Core Simulation Logic은 유지하고  
입력/카메라 레이어만 XR 모드로 분리했습니다.

---

## 🛠 기술 스택

- Unity 6
- C#
- XR Interaction Toolkit
- JSON 기반 로깅

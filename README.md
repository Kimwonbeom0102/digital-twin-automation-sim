# Digital Twin Automation Simulation (Unity)

## 프로젝트 개요

Unity 기반으로 설계한 **상태(State) 중심 디지털 트윈 공정 제어 시뮬레이션 프로젝트**입니다.

자동화 생산 라인의 흐름을  
단순 시각화가 아닌 **상태(State)와 이벤트(Event) 구조**로 모델링하여  
가상 환경에서 검증 가능하도록 구현했습니다.

---

## 기획 의도

- 로그·데이터로만 존재하던 공정 상태를 시각적으로 표현
- 공정 흐름·병목·예외(Fault)를 구조적으로 이해 가능하게 설계
- 시스템 동작 자체를 코드 구조로 설명하는 시뮬레이션 구현

---

## 핵심 설계 목표

- 상태 기반 제어 아키텍처 설계
- Stop / Resume / Fault 전파 흐름 모델링
- Queue 기반 Buffer 시스템 구현
- Update() 의존 최소화 (이벤트 중심 구조)
- 서버 기반 상태 동기화 구조 확장

---

## 아키텍처 구조

### State Layer

- PlantState
- ZoneState
- RobotState

각 단위를 상태 머신처럼 설계하고  
명시적인 이벤트를 통해서만 상태가 전이되도록 구현했습니다.

---

### Event-Driven Flow

- Sensor 감지 → 이벤트 전달 → Robot 작업 트리거
- Fault 발생 → Spawn 제어 중단
- Resume → Delay 기반 Queue 복구
- 서버 시나리오 응답 → Zone 상태 동기화

---

### Buffer & Queue

- Max Capacity 제한
- Overflow 시 +N 시각화
- Assign / Release 로직 분리
- Queue 기반 비동기 처리

---

## 서버 연동 (Scenario 기반 Fault 시스템)

- Unity → ElapsedTime 전송
- ASP.NET Core Web API → 시나리오 판단
- SQLite → Fault 조건 데이터 관리
- 서버 응답 → Zone 상태 동기화

구조 흐름:

Plant Run  
→ SendElapsedTime()  
→ POST /api/Zone/check-scenario  
→ 서버 시나리오 판단  
→ Zone
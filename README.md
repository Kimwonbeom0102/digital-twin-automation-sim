# AutoSim – State-driven Industrial Process Simulation

Unity 기반 공정 시뮬레이션 시스템으로,  
enum 기반 상태 제어 구조와 Fault 전파 로직을 설계하고  
ASP.NET Core Web API + SQLite 연동을 통해 데이터 중심 제어 아키텍처를 구현했습니다.

---

## Project Overview

AutoSim은 제조 공정을 단순 시각화하는 것이 아니라  
**상태 기반 제어(State-driven control)** 와  
**Fault 전파 구조(Fault propagation logic)** 를 중심으로 설계된 시뮬레이션 시스템입니다.

- Zone 단위 상태 관리
- Plant 전체 상태 재평가 구조
- Server 기반 시나리오 제어
- Unity ↔ ASP.NET Core REST API 연동

---

## System Architecture

<p align="center">
  <img src="docs/system_architecture.png" width="850">
</p>

Unity Client는 공정 상태를 시각화하고,  
ASP.NET Core Web API를 통해 Zone 상태를 서버와 동기화합니다.

서버는 EF Core를 사용해 SQLite DB의 Zone 및 FaultScenario 데이터를 조회하며,  
응답된 ZoneResponse를 기반으로 Unity의 `ApplyZoneStates()`를 호출합니다.

이를 통해 **시뮬레이션 로직과 데이터 제어를 분리**했습니다.

---

## State Transition

<p align="center">
  <img src="docs/state_transition.png" width="750">
</p>

Plant는 `Stopped / Running / Fault` 상태를 enum 기반으로 관리합니다.

- Run() → Running 전환  
- Zone Fault 발생 → Fault 상태 전환  
- ClearFault() → 상태 검증 후 Resume 가능  

상태 전이 로직과 실행 로직을 분리하여  
예측 가능한 흐름을 유지하도록 설계했습니다.

---

## Fault Propagation Logic

<p align="center">
  <img src="docs/fault_propagation.png" width="750">
</p>

특정 Zone에서 Fault가 발생하면  
상위/하위 Zone에 Stop 상태를 전파합니다.

Fault 전파 이후 Plant 상태를 재평가하여  
전체 상태 정합성을 유지하도록 설계했습니다.

이를 통해 단일 Zone 오류가 시스템 전반에 미치는 영향을  
구조적으로 제어할 수 있도록 구성했습니다.

---

## Core Design Concepts

- enum 기반 상태 제어 구조
- bool 남용 제거 및 상태 전이 명확화
- 이벤트 기반 공정 흐름 처리
- Queue 기반 Buffer 관리
- Fault → Stop → Resume 복구 설계
- Client-Server 책임 분리 구조

---

## Tech Stack

### Client
- Unity (C#)
- UnityWebRequest
- Event-driven Architecture

### Server
- ASP.NET Core (.NET 8)
- Entity Framework Core
- REST API

### Database
- SQLite



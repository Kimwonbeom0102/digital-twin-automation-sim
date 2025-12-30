using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// 공장(라인)의 '전역 상태'와 '스폰/속도 게이트'를 관리하는 중심 매니저.
/// - 외부(UI/Feeder/Item)는 이 매니저의 '읽기 전용' 정보(State, CanFeed, SpeedScale)를 보고 판단.
/// - 상태 변경은 오직 이 매니저의 메서드(CmdRun/Stop/... 등)를 통해서만 이루어짐(캡슐화).
/// </summary>
public enum PlantState
{
    Stopped,    // 정지
    Running,    // 작동
    Fault,      // 문제발생
    EStop       // 비상정지 
}

public class PlantManager : MonoBehaviour
{
    [SerializeField] private ZoneManager[] zones;

    [Header("Global Control")]
    [SerializeField] private float globalSpeed = 1.0f;

    // -- 캡슐화 + 디폴트값 -- 읽기전용으로 만들어놓고 로직으로 내부에서 수정
    [Header("Global State")]
    public PlantState State {get; private set;} = PlantState.Stopped; // 시작은 안전하게 Stopped
    
    public bool IsGateOpen =>
        State == PlantState.Running;
        
    private Coroutine stopChainCo;
    public bool HasActiveFault {get; private set;} = false;  // 고장 플래그 
    public bool isEstopActive {get; private set;} = false; // 비상정지 플래그 

    // private ZoneManager lastFaultZone;
    public List<ZoneManager> faultZones = new List<ZoneManager>();

    
    // 코루틴 제어
    // public bool feederOn {get; private set;} = false; // 실행 플래그  
    // private Coroutine feederCo; // 코루틴 핸들 

    // (옵션)상태가 바뀔 때 알림 받고싶다면, 별도의 구독 구조를 직접 추가 가능
    // 필요한 오브젝트들이 매 프레임 참조

    private void Awake()
    {
        // 모든 존의 Fault 이벤트 구독 
        foreach(var z in zones)
        {
            if(z == null) continue;
            z.OnZoneFault += HandleZoneFault;  // 존매니저 문제발생 이벤트 등록 
        }
    }

    /// <summary>
    /// 존에서 Fault 발생 시 이벤트로 호출되는 메서드
    /// 어떤 존에서 Fault 났는지(faultZone.zoneId) 적용
    /// 3번존에서 발생했을때 2->1 순서로 스탑체인 코루틴 발생
    /// 1,2번 존에서 문제가 발생시 코루틴과 상태변화가 적용되지않았음 
    /// </summary>
    private void HandleZoneFault(ZoneManager faultZone)  
    {

        if(!faultZones.Contains(faultZone))
        {
            faultZones.Add(faultZone);  // 문제발생 존 등록 
            Debug.Log($"[Plant] Fault 등록됨 -> Zone {faultZone.zoneId}, 총 Fault 수 : {faultZones.Count}");
        }
        Debug.Log($"[Plant] Zone {faultZone.zoneId} Fault 감지");

        
        // fault가 여러개 들어오면 덮어쓰기돼서 마지막 문제 존만 기억하고, 이전에 문제 발생은 해결되지 않음
        // 단일변수가 아닌 리스트(Colletion)으로 관리해야함
        // 이후에 fault가 발생한 zone을 리스트로 저장

        // 3번 존에서 Fault 나면 2 -> 1 순서로 정지 
        if (faultZone.zoneId == 3)
        {
            // ✅ 단순 버전:
            // 이미 순차 정지 코루틴이 돌고 있으면(중복 방지), 새로 시작하지 않음.
            if (stopChainCo == null)
            {
                stopChainCo = StartCoroutine(StopChainFromTopDown());
            }
        }

    }

    public void ResetFault()
    {
        if(faultZones.Count == 0) return;

        ZoneManager last = faultZones[faultZones.Count -1];  // 문제발생 존을 담아주고
        last.ClearFault();  // 문제 해결 이때의 플랜트랑 존의 상태를 확인해야함 
        faultZones.Remove(last);  // 문제 해결하고나서 모두 비워줌 
    }
    
    public void ResetAllFaults()
    {   
        foreach (var z in faultZones)
        {
            z.ClearFault();
        }

        faultZones.Clear(); // 리스트 초기화
    }

    
    // public ZoneManager GetLastFaultZone() 
    // {
    //     return lastFaultZone;
    // }

    public void SetGlobalSpeed(float value) // 글로벌스피드 수정하는 메서드 
    {
        if (value < 0f) value = 0f;  // 값이 0보다 작으면 0으로 유지
        globalSpeed = value;  // 그 외에는 값으로 치환 
    }

    // 내부 전용 : 상태를 실제로 바꾸는 한 점 (지금은 직접 State 대입하니까 옵션)
    private void SetState(PlantState s)
    {
        if (State == s) return; // 동일한 상태면 종료
        State = s;
        // 여기서 ui/알람/로그/애니메이터 등에 알림을 줄 수 있음 (이벤트/콜백 구조로 확장 가능)
        // Debug.Log("[Plant] State -> " + State);
    }

    

    /// <summary>
    /// 3번 Fault 시 2 → 1 순차 정지 시퀀스.
    /// 한 번 시작되면 끝까지(2,1 모두 Stop) 수행하고 마지막에 stopChainCo를 null로 리셋
    /// </summary>
    private IEnumerator StopChainFromTopDown()
    {
        ZoneManager zone2 = GetZoneById(2);

        if (zone2 != null)
        {
            yield return new WaitForSeconds(2f);
            zone2.ZoneStop();
        }

        ZoneManager zone1 = GetZoneById(1);
        if (zone1 != null)
        {
            yield return new WaitForSeconds(2f);
            zone1.ZoneStop();
        }

        stopChainCo = null;   // 시퀀스 다 끝난 뒤에만 null로 초기화
    }

    /// <summary>
    /// zoneId로 해당 ZoneManager 찾아오는 도우미 메서드.
    /// 없으면 null 반환.
    /// </summary>
    private ZoneManager GetZoneById(int id)
    {
        foreach (var z in zones)
        {
            if(z != null && z.zoneId == id)
                return z;
        }

        return null;
    }
    

    /// <summary>
    /// 현재 플랜트 상태에서 아이템을 흘려보내도 되는지 판정
    /// 플랜트가 Running 인가 만 체크
    /// 추후 EStop/Fault 플래그 도입하면 여기에 조건 추가
    /// </summary>
    public bool GetCanFeed()
    {
        return State == PlantState.Running;
    }

    /// <summary>
    /// 전역 속도 배율: Running+안전 상태일 때만 globalSpeed, 그 외 0(자동 정지)
    /// 외부(아이템 이동)가 이 값을 곱해 실제 이동 속도를 결정.
    /// </summary>
    public float GetSpeedScale()
    {
        if (State != PlantState.Running)  // 정상작동중이고 아무런 문제가 없는 상태를 기준으로 조건 실행 혹은 0(자동 정지)
        {
            return 0f;
            // return (globalSpeed < 0f) ? 0f : globalSpeed; // 음수 방지 -> 조건연산으로 좌 : 우 리턴
        }
        if(globalSpeed < 0f)
            return 0f;

        return globalSpeed;
    }
    
    //public float itemSpawnInterval = 2.0f;
    
    /// <summary>
    /// 전체라인 Run (플랜트 Running + 모든 존 Run)
    /// 해당 상태일 때 Eeventhub 색상 green
    /// <summary>
    public void CmdRunAll()   // 작동 시작 명령  
    {
        if (State == PlantState.Running) return;

        State = PlantState.Running;
        // foreach (var z in zones) // 여기서 활성화시키지않고 따로 권한부여
        // {
        //     if(z != null)
        //         z.Run();
        // }
             

        // 안전 제약 : 비상/고장 중에는 운전 금지
        if (isEstopActive) return;
        if (HasActiveFault) return;

    }

    // public void CmdPause() // 일시정지
    // {
    //     // if (State == PlantState.Running) // 작동 상태면
    //     //     SetState(PlantState.Paused); // 일시정지로 전환 
    // }
    
    /// <summary>
    /// 전체 라인 STOP (플랜트 Stopped + 모든 존 StopAll)
    /// 해당 상태일 때 Eeventhub 색상 없음
    /// </summary>
    public void CmdStopAll()  // 작동 정지 명령
    {
        if(State == PlantState.Stopped) return;

        State = PlantState.Stopped;

        foreach(var z in zones)
        {
            if(z != null)
                z.StopAll();
        }

        // 존3 코루틴이 돌고있으면 (코루틴이 비어있지 않으면 코루틴 돌고있는것) 중단
        if(stopChainCo != null)
        {
            StopCoroutine(stopChainCo);
            stopChainCo = null;
        }
        // 비상 중에는 Stop 의미없음(이미 Estop 상태))
        // if (!isEstopActive)  // 비상정지 플래그가 꺼져있으면 
        //     SetState(PlantState.Stopped);  // 멈춤상태로 전환 
    }

    // public void CmdResume() // 재개, 다시시작
    // {
    //     // if (State == PlantState.Paused) // 일시정지 상태면
    //     //     SetState(PlantState.Running); // 작동으로 전환 
    // }

    // public void CmdEStopOn() // 비상정지
    // {
    //     // isEstopActive = true;   // 비상 플래그 On (조건필요없음))
    //     // SetState(PlantState.EStop); // 즉시 비상 상태로 전환 
    // }

    // public void CmdEStopOff() // 비상정지 해제
    // {
    //     isEstopActive = false;  // 플래그 해제
    //     HasActiveFault = false;  // 비상 해제 

    // }

    // public void RaiseFault() // 문제 발생
    // {
    //     // HasActiveFault = true;  // 고장 플래그 ON 
    //     // SetState(PlantState.Fault);  // 문제 발생으로 전환 
    // }

    // public void ClearFault() // 문제 해결
    // {
    //     // HasActiveFault = false; // 고장 플래그 Off
    //     // SetState(PlantState.Stopped);  // 문제발생에서 정지로 전환 - 사람이 Run 눌러야함 
    // }
    
    
    // ---- 단발 스폰 테스트 ----
    // public void SpawnOnce()
    // {
    //     if (!GetCanFeed()) return;  // 비정상이면 종료 

    //     if (itemPool == null || feederPoint == null || route == null || route.Length == 0)
    //     {
    //         Debug.LogWarning("[Plant] Pool/Feeder/Route 미연결");
    //         return;
    //     }

    //     // 1) 풀에서 꺼내기
    //     Item item = itemPool.GetItem(); // 여기서 바로 Item으로 받기
    //     if (item == null)
    //     {
    //         Debug.LogWarning("[Plant] 아이템을 풀에서 가져오지 못했습니다.");
    //         return;
    //     }

    //     // 2) 스폰 위치/자세 지정
    //     item.transform.position = feederPoint.position;
    //     item.transform.rotation = feederPoint.rotation;
    //     // 3) 아이템 초기화 & 주행 시작
        
    //     float speed = GetSpeedScale(); // 전역 배율
    //     if (speed <= 0f) speed = 0.01f; // 안전: 0이면 MoveTowards가 멈추므로 아주 작은 값으로
    //     item.Init(itemPool, route, speed, "Box", Random.Range(1, 999), this); // Init에서 활성화
    // }


    // public void StartFeeder() 
    // {
    //     // 이미 실행 중이면 중복 방지
    //     if (feederOn || feederCo != null) return;
    //     feederOn = true;
    //     feederCo = StartCoroutine(FeederLoop()); // 반환값 저장
    // }

    // public void StopFeeder()  // 코루틴 정지 
    // {
    //     // 중복 방지
    //     if(feederCo == null || !feederOn)  // 코루틴이 비어있거나 피더가 꺼져있을때 종료 
    //         return; 

    //     if(feederCo != null)
    //     {
    //         StopCoroutine(feederCo);  // 실행중이던 코루틴 StartCoroutine(FeederLoop()) 정지 
    //         feederCo = null;
    //     }
    //     feederOn = false;  // 초기화 
    // }

    // private IEnumerator FeederLoop()
    // {
    //     while (feederOn)  // 주기 반복 루프 
    //     {
    //         if(GetCanFeed())
    //         {
    //             SpawnOnce();
    //             yield return new WaitForSeconds(intervalCoroutine);
    //         }
    //         else
    //         {
    //             yield return new WaitForSeconds(0.1f); // 1초 기다림
    //         }
            
    //     }
    // }
     
    // 자동 계산 확장 메서드 - 코루틴에 적용
    // private float GetIntervalSec()
    // {
    //     float s = GetSpeedScale();
    //     return(s > 0f) ? (intervalCoroutine / s) : 9999f;
    // }
}


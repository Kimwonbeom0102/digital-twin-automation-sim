using UnityEngine;
using System.Collections;
using System;

public enum ZoneRole
{
    Feeder,
    Conveyor,
    Output
}

public enum ZoneState
{
    Stopped,    // 정지
    Running,    // 작동
    Paused,     // 일시정지
    Fault,      // 문제발생
    Estop       // 비상정지 
}

public class ZoneManager : MonoBehaviour
{
    [SerializeField] private PlantManager plant;

    [Header("Feeder Settings")]
    public int zoneId;          // 1,2,3
    public string zoneName;
    [SerializeField] private float firstFeedInterval = 5f; // 존1 피더에서 생성되는 주기 
    [SerializeField] private float transferDelay = 4f;  // 싱크에 들어갔다가 피더에서 다시 생성되는 주기
    [SerializeField] private Sensor[] sensors;

    public ZoneRole Role;

    [Header("References")]
    [SerializeField] private ItemPool itemPool;  // 프리팹 재고 (풀))
    [SerializeField] private Transform feederPoint; // 스폰 위치 
    [SerializeField] private Transform sinkPoint;   // 회수 위치
    [SerializeField] public Transform[] route;  // 아이템 이동 경로

    [SerializeField] private Sink sink;
    [SerializeField] private ZoneManager nextZone;
    

    public bool canSpawn = true;
    public bool isUserStopped = false;

    public ZoneState State { get; private set; } = ZoneState.Stopped;
    public bool HasActiveFault {get; private set;} = false;  // 고장 플래그 
    public bool isEstopActive {get; private set;} = false; // 비상정지 플래그 

    // 플랜트가 구독할 Fault 이벤트
    public event Action<ZoneManager> OnZoneFault;
    
    public bool CanRun => 
        plant != null && State == ZoneState.Stopped && !HasActiveFault;

    // 코루틴 제어
    public bool feederOn {get; private set;} = false; // 실행 플래그  
    private Coroutine feederCo; // 코루틴 핸들 
    private bool isSinkProcessing = false;

    [SerializeField] private bool error; // 확장용(나중에 센서/머신 연결)

    // 코루틴 제어
    // public bool feederOn {get; private set;} = false; // 실행 플래그  
    // private Coroutine feederCo; // 코루틴 핸들 

    // (옵션)상태가 바뀔 때 알림 받고싶다면, 별도의 구독 구조를 직접 추가 가능
    // 필요한 오브젝트들이 매 프레임 참조

    void Awake()
    {
        // if(sensors == null || sensors.Length == 0)
        //     sensors = GetComponentInChild<Sensor>(includeInactive: true);
        foreach(var s in sensors)
        {
            if(s == null) continue;
            s.OnNoPass += OnSensorTimeout;
        }

        if(sink != null)
        {
            if(Role != ZoneRole.Conveyor)
            {
                sink.OnSink += HandleSinkPass;
                Debug.Log($"[Zone {zoneId}] Sink 이벤트 구독완료");
            }
            else
            {
                Debug.Log($"[Zone {zoneId}] Conveyor -> OnSink 미구독");
            }
            
        }
        else
        {
            Debug.LogWarning($"[Zone] {zoneId} Sink 없음");
        }
    }

    public void HandleSinkPass(Sink sink)
    {
        Debug.Log($"[Sink 이벤트 감지] Zone {zoneId} / State = {State} / plant.CanFeed = {plant.GetCanFeed()}");

        Debug.Log($"StartFeeder() called! Zone {zoneId}");

        if(State != ZoneState.Running)
        {
            Debug.Log($"[Zone] {zoneId} Running이 아님 -> Spawn 불가");
            return;
        }

        StartCoroutine(HandleSinkPassRoutine(sink));
    }


    private IEnumerator HandleSinkPassRoutine(Sink sink)
    {
        if(isSinkProcessing) yield break;
        isSinkProcessing = true;
        
        yield return new WaitForSeconds(transferDelay);

        if(nextZone != null) // 조건을 nextZone에게 넘겨서 스폰 호출 
        {
            Debug.Log($"[Zone {zoneId}] 다음 Zone으로 Spawn()");
            nextZone.Spawn();
        }
        else // 따로 스폰안해줌 트리거에서 자동 호출 
        {
            Debug.Log($"[Zone {zoneId}] Output Spawn()");
            // if(Role == ZoneRole.Output)
            // {
                
            // }
        }
        isSinkProcessing = false;
    }

    // 구독해제
    void OnDestroy()
    {
        if(sensors == null) return;
        foreach(var s in sensors)
        {
            if(s == null) continue;
            s.OnNoPass -= OnSensorTimeout;
            Debug.Log($"존매니저 센서 구독해제");
        }

        if(sink == null) return;
        sink.OnSink -= HandleSinkPass;
    }

    //감지 안될 때 문제 발생 메서드 실행 
    public void OnSensorTimeout(Sensor onNoPass)
    {
        Debug.Log($"[Zone {zoneId}] Sensor #{onNoPass.sensorId}  NoPass 감지");

        if (onNoPass.sensorId >= 0)
        {
            RaiseFault();
        }
    }

    public void ToggleUserStop() 
    {
        isUserStopped = !isUserStopped;
        if (isUserStopped)
        {
            ZoneStop();
            Debug.Log($"[Zone {zoneId}] 사용자 STOP");
        }
        else
        {
            ZoneRun();
            Debug.Log($"[Zone {zoneId}] 사용자 RESUME");
        }
    }

    public void ClearFault()
    {
        // if(State != ZoneState.Fault) return;

        HasActiveFault = false; // 문제 해결 
        State = ZoneState.Stopped; // 멈춤상태 
        Debug.Log($"[Zone] {zoneId} Fault 제거! -> {State}"); 

        // StartFeeder(); 
    }
    
    // === 어떤 조건에서 이 메서드 호출해서
    // 존마다 문제를 발생할지 여부를 만들어야함
    public void RaiseFault() 
    {
        if(State == ZoneState.Fault) return;

        HasActiveFault = true;
        State = ZoneState.Fault;
        StopFeeder();

        Debug.Log($"[Zone {zoneId}]Fault 발생!");
        OnZoneFault?.Invoke(this);     // 존매니저의 문제발생 메서드 넘겨줌 
    }

    // === 플랜트에서 전체 Run 시 호출됨 ===
    public void ZoneRun()
    {
        if (!CanRun) 
        {  
            return;
        }
        // if(State != ZoneState.Stopped) return; // CanRun에서 상태확인 

        if(plant == null)
        {
            Debug.LogWarning($"[Zone {zoneId}] PlantManager 미연결");
            return;
        }

        // if(plant.State != PlantState.Running)  // 게이트오픈의 하위이므로 생략
        // {
        //     Debug.LogWarning($"[Zone {zoneId}] Plant가 Running이 아니라서 존 시작 불가");
        //     return;
        // }

        if (!plant.IsGateOpen)
        {
            Debug.LogWarning("허가(Gate)가 닫혀 있어서 Zone 실행 불가");
            return;
        }
        State = ZoneState.Running;
        
        // StartFeeder();
    }

    public void ItemSpawn()
    {
        if(State != ZoneState.Running) 
        {
            Debug.Log("Zone_Run 버튼을 누르지 않았습니다.");
            return;
        }
        if(Role == ZoneRole.Feeder)
        {
            StartFeeder();
        }
    }
    
    public void StopAll() // 플랜트에서 정지하면 모든 존 정지 (모든 작동 정지) 
    {
        if(State != ZoneState.Running) return;

        State = ZoneState.Stopped;
        StopFeeder();
    }

    public void ZoneStop()  // 특정 존만 정지
    {
        if (State == ZoneState.Stopped) return;

        State = ZoneState.Stopped;
        StopFeeder();
    }

    public void StartFeeder() 
    {
        // 이미 실행 중이면 중복 방지
        if (feederOn || feederCo != null) return;

        feederOn = true;

        if(feederCo == null)
            feederCo = StartCoroutine(FeederLoop()); // 반환값 저장
    }

    public void StopFeeder()  // 코루틴 정지 
    {
        // 중복 방지
        if(feederCo == null || !feederOn)  // 코루틴이 비어있거나 피더가 꺼져있을때 종료 
            return; 

        if(feederCo != null)
        {
            StopCoroutine(feederCo);  // 실행중이던 코루틴 StartCoroutine(FeederLoop()) 정지 
            feederCo = null;
        }

        feederOn = false;  // 초기화 
    }

    private IEnumerator FeederLoop() // 코루틴으로 생성 반복
    {
        if (!canSpawn) yield break;

        while (State == ZoneState.Running)  // 주기 반복 루프 
        {
            if(GetCanFeed())
            {
                Spawn(); // 생성 메서드 실행 
                yield return new WaitForSeconds(firstFeedInterval); // 쉬고
            }
            else
            {
                yield return new WaitForSeconds(0.5f); // 1초 기다림
            }
            
        }
    }

    /// <summary>
    /// 이 존이 현재 아이템을 흘려보낼 수 있는지 상태확인 
    /// 존 상태 : Running
    /// 존 개별 Fault/Estop 없어야함
    /// 플랜트도 Running
    /// </summary>
    public bool GetCanFeed()
    {
        if(plant == null) return false; // 기본

        if(HasActiveFault || isEstopActive) return false;

        if (State != ZoneState.Running)
            return false;  
        //if (plant.State != PlantState.Running) return false;
        return plant.GetCanFeed();  
    }
    
    // 단발 스폰 테스트에서
    // 풀링 / 코루틴 사용 -> 연발 스폰
    public void Spawn()
    {
        // int last = route.Length - 1;  // 웨이포인트가 싱크보다 뒷쪽에 위치하지 않으면 풀로 들어감
        
        // if (Vector3.Distance(route[last], sinkPoint.position) < 0.1f)
        // {
        //     Debug.LogWarning("웨이포인트가 싱크보다 앞에 있음!");
        // }
        Debug.Log($"[Zone {zoneId}] Spawn() 호출됨 / CanFeed = {GetCanFeed()}");

        if (!GetCanFeed()) 
        {
            Debug.Log($"[Zone {zoneId}] CanFeed == FALSE → 스폰 중단됨");
            return;  // 비정상이면(피드할 수 없으면) 종료 
        }
        

        if (itemPool == null || feederPoint == null || route == null || route.Length == 0)
        {
            
            Debug.LogWarning($"[Zone] {zoneId} Pool/Feeder/Route 미연결");
            return;
        }

        // 1) 풀에서 꺼내기
        Item item = itemPool.GetItem(); // 여기서 바로 Item으로 받기
        if (item == null)
        {
            Debug.LogWarning($"[Zone] {zoneId} 아이템을 풀에서 가져오지 못했습니다.");
            return;
        }

        // 2) 스폰 위치/자세 지정
        item.transform.position = feederPoint.position;
        item.transform.rotation = feederPoint.rotation;
        // 3) 아이템 초기화 & 주행 시작
        
        float speed = GetSpeedScale(); // 전역 배율
        if (speed <= 0f) speed = 0.01f; // 안전: 0이면 MoveTowards가 멈추므로 아주 작은 값으로
        item.Init(itemPool, route, speed, "Box", UnityEngine.Random.Range(1, 999), this); // Init에서 활성화
        
    }

    public void SpawnOnce()
    {
        if(Role == ZoneRole.Conveyor)
            Spawn();
    }
    
    /// <summary>
    /// 전역 속도 배율: Running+안전 상태일 때만 globalSpeed, 그 외 0(자동 정지)
    /// 외부(아이템 이동)가 이 값을 곱해 실제 이동 속도를 결정.
    /// </summary>
    public float GetSpeedScale()
    {
        if (plant == null) return 0f;  // 정상작동중이고 아무런 문제가 없는 상태를 기준으로 조건 실행 혹은 0(자동 정지)
        
        if(HasActiveFault || isEstopActive) return 0f;

        if(State != ZoneState.Running) return 0f;

        return plant.GetSpeedScale();
    }
    
    void Update()  // 존마다 스폰을 업데이트가 아닌 이벤트로 유지하는게 better.
    {
        
    }

}

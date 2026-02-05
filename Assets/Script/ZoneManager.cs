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
    // Estop       // 비상정지, 나중을 위한 예비책
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
    public RobotArmController robot;

    [Header("References")]
    [SerializeField] private ItemPool itemPool;  // 프리팹 재고 (풀))
    [SerializeField] public Transform feederPoint; // 스폰 위치 (다른 존에서 접근 필요)
    // [SerializeField] private Transform sinkPoint;   // 회수 위치
    [SerializeField] public Transform[] route;  // 아이템 이동 경로

    [SerializeField] private Sink sink;
    [SerializeField] private ZoneManager nextZone;

    [SerializeField] private BufferZone bufferZone;
    [SerializeField] private PickUpSlot pickUpSlot;

    [SerializeField] private ZoneManager zone3Manager;


    // [Header("Zone2 Pickup/Buffer")]
    // [SerializeField] private BufferSlot[] bufferSlots;          // 버퍼존 슬롯들(앞쪽에 배치, 트리거 필요)
    // [SerializeField] private Transform[] routeToPickUpSlot;     // 싱크 이후 -> 픽업슬롯으로 흘러가는 경로
    // [SerializeField] private Transform[] routeToBuffer;         // 싱크 이후 -> 버퍼존으로 흘러가는 경로
    
    // private bool pendingPick = false;
    // private bool pickUpSlotReserved = false;
    public bool canSpawn = true;
    public bool isUserStopped = false;

    public ZoneState State { get; private set; } = ZoneState.Stopped;
    public bool HasActiveFault {get; private set;} = false;  // 고장 플래그 
    public bool isEstopActive {get; private set;} = false; // 비상정지 플래그 

    // 플랜트가 구독할 Fault 이벤트
    public event Action<ZoneManager> OnZoneFault;
    
    // 상태 변경 이벤트 추가
    public event Action<ZoneState> OnStateChanged;
    
    public bool CanRun => 
        plant != null && State == ZoneState.Stopped && !HasActiveFault;

    // 코루틴 제어
    public bool feederOn {get; private set;} = false; // 실행 플래그  
    private Coroutine feederCo; // 코루틴 핸들 
    // private bool isSinkProcessing = false;
    private bool isProcessingQueue = false; // ProcessQueueItems 실행 중 플래그
    private Item pendingItem; 

    // [SerializeField] private bool error; // 확장용(나중에 센서/머신 연결)

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
            sink.OnSink += HandleSinkPass;    // 싱크에 아이템이 들어올 때 이벤트 구독
            Debug.Log($"[Zone {zoneId}] Sink 이벤트 구독완료");
        }
        else
        {
            Debug.LogWarning($"[Zone] {zoneId} Sink 없음");
        }

        if (Role != ZoneRole.Conveyor)  return;

        bufferZone.OnQueueChanged += TryFeedPickUpSlot;
        pickUpSlot.OnBecameEmpty += TryFeedPickUpSlot;
        robot.OnBecameIdle += TryFeedPickUpSlot;

    }
 
    private void TryStartSinkDequeue()  // Zone2에만 해당 
    {
        if (Role != ZoneRole.Conveyor) return;
        if (State != ZoneState.Running) return;
        if (sink == null) return;
        if (isProcessingQueue) return;

        isProcessingQueue = true;
        StartCoroutine(ProcessSinkToZone2());
    }

    private IEnumerator ProcessSinkToZone2()  // Sink2에 들어왔을 때
    {
        while (State == ZoneState.Running)
        {
            yield return new WaitForSeconds(transferDelay);

            Item item = sink.DequeueItem();
            if (item == null) break;

            Debug.Log("[Zone2] Sink → Spawn");

            item.gameObject.SetActive(true);
            item.transform.position = feederPoint.position;
            item.transform.rotation = feederPoint.rotation;

            float speed = GetSpeedScale();
            if (speed <= 0f) speed = 0.01f;

            item.Init(itemPool, route, speed, item.itemName, item.itemID, this);

            
        }                                           
        isProcessingQueue = false;
    }

    public void TryFeedPickUpSlot()
    {
        if (Role != ZoneRole.Conveyor)  return;
        if (plant.State != PlantState.Running) return;
        if (robot.IsBusy) return;
        if (pickUpSlot.HasItem) return;
        if (!bufferZone.HasItem) return;
        if (zone3Manager.State != ZoneState.Running) return;

        var item = bufferZone.Dequeue();
        pickUpSlot.AssignPickUpSlot(item);
        robot.TryStartWork();
    }

    public void HandleSinkPass(Sink sink, Item item)
    {
        // null 체크
        if(sink == null) // 싱크가 null 일 때 
        {
            Debug.LogWarning($"[Zone {zoneId}] HandleSinkPass: sink가 null");
            return;
        }
        
        if(item == null)  // 아이템이 null 일 때 
        {
            Debug.LogWarning($"[Zone {zoneId}] HandleSinkPass: item이 null");
            return;
        }
        
        Debug.Log($"[Sink 이벤트 감지] Zone {zoneId} / State = {State} / plant.CanFeed = {plant?.GetCanFeed()}");

        if(State != ZoneState.Running)  // 존이 작동중이 아니면 스폰불가 
        { 
            Debug.Log($"[Zone] {zoneId} Running이 아님 -> Spawn 불가");
            return;
        }
        if (Role != ZoneRole.Feeder) return;

        // TryStartSinkDequeue();
        Debug.Log($"[Zone {zoneId}] Sink 통과 아이템 수신");
        
        if (!isProcessingQueue)
        {
            StartCoroutine(ProcessQueueItems());
        }
    }


    // private IEnumerator HandleSinkPassRoutine(Sink sink, Item item)  // 싱크에 들어갔을 때 큐에 저장하는 트리거
    // {
    //     if(isSinkProcessing) yield break; 
    //     isSinkProcessing = true;
        
    //     yield return new WaitForSeconds(transferDelay);

    //     // transferDelay 후에는 현재 존 상태를 체크하지 않음
    //     // 이미 싱크를 통과한 아이템은 다음 존 상태만 확인하여 처리
    //     // (현재 존이 스톱되어도 이미 전이 과정이 시작되었으므로 계속 진행)

    //     if(nextZone != null) // 조건을 nextZone에게 넘겨서 스폰 호출
    //     {
    //         Debug.Log($"[Zone {zoneId}] 다음 Zone({nextZone.zoneId})으로 전이 -> 큐에 저장 후 순차 처리");
    //         if(sink != null && item != null)
    //         {
    //             // 아이템 비활성화
    //             if(item.gameObject.activeSelf)
    //             {
    //                 item.gameObject.SetActive(false);
    //             }
    //             sink.EnqueueItem(item);
                
    //             // ProcessQueueItems가 실행 중이 아니면 시작
    //             if(!isProcessingQueue)
    //             {
    //                 ProcessSinkToBuffer();
    //             }
                
    //             Debug.Log($"[Zone {zoneId}] 아이템이 Sink 큐에 저장됨 (순차 처리 대기)");
    //         }
    //         else
    //         {
    //             Debug.LogWarning($"[Zone {zoneId}] sink 또는 item이 null -> 큐 저장 실패");
    //         }
    //     }
    //     else // nextZone이 null인 경우 (예: 존3는 nextZone이 없을 수 있음)
    //     {
    //         Debug.Log($"[Zone {zoneId}] nextZone이 null -> Output 존이거나 마지막 존");
    //         // nextZone이 null이면 이 존이 마지막 존이므로 추가 처리 불필요
    //     }
    //     isSinkProcessing = false;
    // }

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

        if (Role != ZoneRole.Conveyor)
            return;

        bufferZone.OnQueueChanged -= TryFeedPickUpSlot;
        pickUpSlot.OnBecameEmpty -= TryFeedPickUpSlot;
        robot.OnBecameIdle -= TryFeedPickUpSlot;

        // if (sink != null)
        //     sink.OnItemArrivedAtSink -= OnItemArrivedAtSink;

        // if (pickUpSlot != null)
        // {
        //     pickUpSlot.OnBecameEmpty -= HandlePickUpSlotBecameEmpty;
        //     pickUpSlot.OnItemArrived -= HandlePickUpSlotArrived;
        // }

        // if (bufferSlots != null)
        // {
        //     foreach (var bs in bufferSlots)
        //     {
        //         if (bs == null) continue;
        //         bs.OnItemArrived -= HandleBufferSlotArrived;
        //     }
        // }
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
            // 사용자에 의한 정지: 현재 상태에 상관없이 안전하게 Stopped로 전환
            ZoneStop();
            Debug.Log($"[Zone {zoneId}] 사용자 STOP");
        }
        else
        {   
            // 사용자 재개: 큐에 쌓인 아이템을 우선 처리한 뒤 정상 Running 상태로 복귀
            ResumeZone();
            Debug.Log($"[Zone {zoneId}] 사용자 RESUME");
        }
    }

    public void PauseZone()
    {
        if (State == ZoneState.Paused) return;

        Debug.Log($"[Zone {zoneId}] Paused");
        State = ZoneState.Paused;
        // 신규 스폰만 막음
        StopFeeder();
    }

    public void ClearFault()
    {
        // if(State != ZoneState.Fault) return;

        HasActiveFault = false; // 문제 해결 
        State = ZoneState.Stopped; // 멈춤상태 
        Debug.Log($"[Zone] {zoneId} Fault 제거! -> {State}"); 
        // StartFeeder(); 
        OnStateChanged?.Invoke(State);
    }
    
    // === 어떤 조건에서 이 메서드 호출해서
    // 존마다 문제를 발생할지 여부를 만들어야함
    public void RaiseFault() 
    {
        if(State == ZoneState.Fault) return;

        HasActiveFault = true;
        State = ZoneState.Fault;
        OnZoneFault?.Invoke(this);     // 존매니저의 문제발생 메서드 넘겨줌 
        OnStateChanged?.Invoke(State);
        StopFeeder();

        Debug.Log($"[Zone {zoneId}]Fault 발생!");

        DataLogger.Instance.LogEvent("ZoneFault", zoneName, "Fault 발생");
    }

    public void ResumeZone()
    {
        // Stopped 상태에서만 재개 허용
        if (State != ZoneState.Stopped)
        {
            Debug.LogWarning($"[Zone {zoneId}] ResumeZone 호출 시 상태가 Stopped가 아님 -> {State}");
            return;
        }

        if (HasActiveFault || isEstopActive)
        {
            Debug.LogWarning($"[Zone {zoneId}] Fault/EStop 상태에서는 Resume 불가");
            return;
        }

        Debug.Log($"[Zone {zoneId}] Resume 요청");

        // 재개 시 기본 목표 상태는 Running
        State = ZoneState.Running;
        OnStateChanged?.Invoke(State);

        // 1) 싱크 큐에 아이템이 있다면 우선 순차적으로 비워준다.
        if (sink != null && sink.HasItem())
        {
            if (!isProcessingQueue)
            {
                Debug.Log($"[Zone {zoneId}] 싱크 큐에 {sink.QueueCount}개 존재 -> 큐 우선 처리 시작");
                TryStartSinkDequeue();
            }
        }

        // 2) 이 존이 Feeder 역할이면, 재개와 동시에 피더도 다시 켜준다.
        if (Role == ZoneRole.Feeder)
        {
            StartFeeder();
        }

        DataLogger.Instance.LogEvent("ZoneResume", zoneName, "Zone resumed");
    }

    // === 플랜트에서 전체 Run 시 호출됨 ===
    public void ZoneRun()
    {
        if (!CanRun) return;

        if (plant == null)
        {
            Debug.LogWarning($"[Zone {zoneId}] PlantManager 미연결");
            return;
        }

        if (!plant.IsGateOpen)
        {
            Debug.LogWarning("허가(Gate)가 닫혀 있어서 Zone 실행 불가");
            return;
        }

        ZoneState oldState = State;
        State = ZoneState.Running;

        if (oldState != State)
            OnStateChanged?.Invoke(State);

        DataLogger.Instance.LogEvent("ZoneRun", zoneName, "Zone run");

        // if (Role == ZoneRole.Feeder)
        //     StartFeeder();

        // TryStartSinkDequeue();
    }

    // private void TryStartQueueProcess()
    // {
    //     // 이미 큐 처리 중이면 중복 시작 금지
    //     if (isProcessingQueue) 
    //         return;

    //     // 존이 Running 상태가 아니면 큐를 흘려보낼 수 없음
    //     if (State != ZoneState.Running) 
    //         return;

    //     if (sink == null || !sink.HasItem()) 
    //         return;

    //     StartCoroutine(ProcessSinkToBuffer());
    // }

    private IEnumerator ProcessSinkToBuffer()
    {
        isProcessingQueue = true;

        while (State == ZoneState.Running && sink.HasItem())
        {
            Item item = sink.DequeueItem();
            if (item == null) break;
            Debug.Log("[Zone2] Sink → Buffer 이동");

            bufferZone.Enqueue(item);

            yield return new WaitForSeconds(0.2f);
        }

        isProcessingQueue = false;
    }

//     private IEnumerator ProcessQueueItems() 
//     {
//        // 이미 실행 중이면 중복 방지
//        if (isProcessingQueue) 
//            yield break;

//        isProcessingQueue = true;

//        while (true)
//        {
//            // 존이 더 이상 Running이 아니면 즉시 종료
//            if (State != ZoneState.Running)
//                break;

//            // 싱크나 큐가 비어 있으면 종료
//            if (sink == null || !sink.HasItem())
//                break;

//            // 다음 존이 존재하고, 아직 Running이 아니라면 잠시 대기 후 재시도
//            if (nextZone != null && nextZone.State != ZoneState.Running)
//            {
//                yield return new WaitForSeconds(0.1f);
//                continue;
//            }

//         //    Item item = sink.DequeueItem();
//            if (item != null && nextZone != null)
//            {
//                item.gameObject.SetActive(true);
//                item.transform.position = nextZone.feederPoint.position;
//                item.transform.rotation = nextZone.feederPoint.rotation;

//                float speed = nextZone.GetSpeedScale();
//                if (speed <= 0f) speed = 0.01f;

//                item.Init(
//                    itemPool,
//                    nextZone.route,
//                    speed,
//                    item.itemName,
//                    item.itemID,
//                    nextZone
//                );
//            }
//            // 너무 촘촘히 돌지 않도록 약간의 간격을 둔다.
//            yield return new WaitForSeconds(2.5f);
//        }

//        isProcessingQueue = false;
//    }

    // <summary>
    // 큐에 저장된 아이템을 순차적으로 스폰
    // </summary>
    private IEnumerator ProcessQueueItems()
    {
        // 이미 실행 중이면 중복 방지
        if(isProcessingQueue)
        {
            yield break;
        }
        
        isProcessingQueue = true;
        
        while(sink != null && sink.HasItem())
        {
            if (State == ZoneState.Stopped || State == ZoneState.Fault)
                break;

            if (State == ZoneState.Paused)
            {
                yield return null;
                continue;
            }

            // 다음 존 상태 확인 (nextZone이 null이면 마지막 존이므로 큐 처리 계속)
            if(nextZone != null)
            {
                if(nextZone.State != ZoneState.Running)
                {
                    Debug.Log($"[Zone {zoneId}] 다음 존({nextZone.zoneId})이 Stopped -> 큐 처리 중단");
                    yield return null;
                    continue;
                }
            }
            // nextZone이 null이면 마지막 존이므로 계속 처리
            
            int queueCount = sink.QueueCount;

            float delay = 1.0f;              // 기본 텀
            if (queueCount >= 2)
            {
                delay = 2.5f;                // 2개 이상 쌓였을 때 버퍼
            }

            // 큐에서 아이템 가져와서 다음 존에 스폰
            Item queuedItem = sink.DequeueItem();
            if (queuedItem != null)
            {
                Debug.Log($"[Zone {zoneId}] 큐에서 아이템 스폰: {queuedItem.itemName}");

                if (nextZone != null && nextZone.feederPoint != null && nextZone.route != null && nextZone.route.Length > 0)
                {
                    if (itemPool == null)
                    {
                        Debug.LogWarning($"[Zone {zoneId}] itemPool null -> 큐 처리 중단");
                        break;
                    }

                    queuedItem.gameObject.SetActive(true);
                    queuedItem.transform.position = nextZone.feederPoint.position;
                    queuedItem.transform.rotation = nextZone.feederPoint.rotation;

                    float speed = nextZone.GetSpeedScale();
                    if (speed <= 0f) speed = 0.01f;

                    queuedItem.Init(
                        itemPool,
                        nextZone.route,
                        speed,
                        queuedItem.itemName,
                        queuedItem.itemID,
                        nextZone
                    );
                }
            }
            yield return new WaitForSeconds(delay);
        }
        isProcessingQueue = false;
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

        ZoneState oldState = State;
        State = ZoneState.Stopped;
        StopFeeder();
        
        // 상태 변경 이벤트 발생
        if(oldState != State)
        {
            OnStateChanged?.Invoke(State);
        }
    }

    public void ZoneStop()  // 특정 존만 정지
    {
        if (State == ZoneState.Stopped) return;
        
        ZoneState oldState = State;
        
        if (Role == ZoneRole.Feeder || Role == ZoneRole.Conveyor)  // 존 1,2일때만 정지
        {
            State = ZoneState.Stopped;
            StopFeeder();
        }
        else // 존3
        {
            State = ZoneState.Stopped;
        }

        // 상태 변경 이벤트 발생
        if(oldState != State)
        {
            OnStateChanged?.Invoke(State);
        }

        DataLogger.Instance.LogEvent("ZoneStop", zoneName, "Zone stopped");
    }

    public void StartFeeder()
    {
        // Feeder만 허용
        if (Role != ZoneRole.Feeder)
            return;

        if (feederOn || feederCo != null) return;

        feederOn = true;
        feederCo = StartCoroutine(FeederLoop());
    }


    public void StopFeeder()  // 코루틴 정지 
    {
        // 중복 방지
        if(feederCo == null || !feederOn)  // 코루틴이 비어있거나 피더가 꺼져있을때 종료 
            return; 

        if(feederCo != null) // 실행중이던 코루틴 StartCoroutine(FeederLoop()) 정지
        {
            StopCoroutine(feederCo);   
            feederCo = null;
        }

        feederOn = false;  // 초기화 
    }

    private IEnumerator FeederLoop() // 코루틴으로 생성 반복
    {
        if (!canSpawn) yield break;

        while (State == ZoneState.Running)  // 주기 반복 루프 
        {
            if(GetCanFeed()) // 작동중일때 피드 가능하면 스폰
            {
                Spawn(); // 생성 메서드 실행 
                yield return new WaitForSeconds(firstFeedInterval); // 쉬고
            }
            else  // 아니면 기다림
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

    // public void SpawnOnce()
    // {
    //     if(Role == ZoneRole.Conveyor)
    //         Spawn();
    // }
    
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


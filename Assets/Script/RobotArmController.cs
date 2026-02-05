using UnityEngine;
using System;
using System.Collections;
using TMPro;

public enum RobotState
{
    Idle,
    Running
}


public class RobotArmController : MonoBehaviour
{
    // 필요한것 
    // 관절
    // 그리퍼
    // 관절움직임 포지션
    // 그리퍼 포지션
    // ====== ROOT ======
    [SerializeField] private Sink pickSink;
    [SerializeField] private PlantManager plant;
    [SerializeField] private ItemPool itemPool;
    [SerializeField] private ZoneManager zone3Manager;

    [Header("=== Idle Pose (Captured at Awake) ===")]
    private Quaternion baseIdleRot;
    private Quaternion chestIdleRot;
    private Quaternion armIdleRot;

    [Header("---Base---")]
    public Transform baseRoot;
    public float baseRotateSpeed = 10f;
    public Transform targetRot;

    [Header("---Chest---")]
    public Transform chest;
    public float chestRotateSpeed = 10f;

    [Header("---Arm---")]
    public Transform armJoint;  // 관절
    public float armRotateSpeed = 10f;

    [Header("Reference")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private string zoneLabel = "Z3";

    // [Header("---End Effctor---")]
    // public Transform effector;           // 필요 없으면 Inspector에서 비워둬도 됨
    // public float effectRotateSpeed = 2f; // 현재는 사용 안 함 (그립 회전만 사용)

    [Header("---Gripper---")]
    public Transform leftHand;
    public Transform rightHand;
    public float gripOpenX = 0.025f;
    public float gripCLoseX = -0.025f;
    public float gripSpeed = 2f;

    [Header("---Pick / Drop Points---")]
    // public Transform homePoint;  // 원점
    public Transform pickPoint;  // 존2의 싱크포인트
    public Transform dropPoint;  // 존3의 피더포인트
    public PickUpSlot pickUpSlot;

    // ====== ITEM ====== 
    [Header("---Item---")]
    public Item heldItem;  // 아이템 받아오기 
    public Transform itemHoldPoint;

    [Header("=== Robot State ===")]
    public RobotState State { get; private set; } = RobotState.Idle;
    private bool isBusy;
    public bool IsBusy => isBusy;

    public event Action OnBecameIdle;

    // ====== MOTHODS ======

    void Awake() 
    {   
        if(baseRoot == null || chest == null || armJoint == null)
        {
            Debug.LogError("[RobotArmController] Joint 비어있음");
            enabled = false;
            return;
        }

        baseIdleRot = baseRoot.localRotation; 
        chestIdleRot = chest.localRotation; 
        armIdleRot = armJoint.localRotation; 

        if (statusText == null)
        {
            Debug.LogWarning("[RobotZoneStatusUI] statusText 미할당");
        }
    }

    private void OnEnable()
    {
        if(pickUpSlot != null)
        {
            pickUpSlot.OnItemArrived += HandleItemArrive;
            pickUpSlot.OnBecameEmpty += HandleSlotEmpty;
        }
        
        // 존3 상태 변경 이벤트 구독
        if(zone3Manager != null)
        {
            zone3Manager.OnStateChanged += OnZone3StateChanged;
        }
    }

    private void OnDisable()
    {
        if(pickUpSlot != null)
        {
            pickUpSlot.OnItemArrived -= HandleItemArrive;
            pickUpSlot.OnBecameEmpty -= HandleSlotEmpty;
        }
        
        // 존3 상태 변경 이벤트 구독 해제
        if(zone3Manager != null)
        {
            zone3Manager.OnStateChanged -= OnZone3StateChanged;
        }
    }
    
    // public void TryPick()
    // {
    //     if (isBusy) return;
    //     if (State != RobotState.Idle) return;

    //     if (zone3Manager == null || zone3Manager.State != ZoneState.Running) return;

    //     Item item = pickUpSlot.Release();
    //     if (item == null) return;

    //     StartPickAndPlace(item);
    // }

    /// <summary>
    /// 존3 상태 변경 시 호출
    /// </summary>
    private void OnZone3StateChanged(ZoneState newState)
    {
        Debug.Log($"[RobotArmController] 존3 상태 변경: {newState}");
        
        UpdateUI(newState);
        // Stopped일 때는 현재 처리 중인 아이템만 완료하고 중단 (PickAndPlaceRoutine에서 처리)
    }

    private void UpdateUI(ZoneState state)
    {
        if (statusText == null) return;

        switch (state)
        {
            case ZoneState.Running:
                statusText.text = $"RUN ({zoneLabel})";
                statusText.color = Color.green;
                break;

            case ZoneState.Stopped:
            case ZoneState.Paused:
                statusText.text = $"WAIT ({zoneLabel})";
                statusText.color = Color.yellow;
                break;

            case ZoneState.Fault:
                statusText.text = $"FAULT ({zoneLabel})";
                statusText.color = Color.red;
                break;

            default:
                statusText.text = $"UNKNOWN ({zoneLabel})";
                statusText.color = Color.white;
                break;
        }
    }

    // private void HandleItemArrived(Item item)  
    // {
    //     if(item == null) 
    //     {
    //         Debug.LogWarning("[RobotArmController] 아이템이 null");
    //         return;
    //     }

    //     if(plant == null || plant.State != PlantState.Running) 
    //     {
    //         Debug.Log("[RobotArmController] 플랜트가 Running이 아님");
    //         return;
    //     }
        
    //     if(zone3Manager == null || zone3Manager.State != ZoneState.Running) 
    //     {
    //         Debug.Log("[RobotArmController] 존3가 Running이 아님 -> 처리 안 함");
    //         return;
    //     }
        
    //     // 로봇이 Idle 상태이고 Busy가 아니면 처리 시작
    //     if(State == RobotState.Idle)
    //     {
    //         StartPickAndPlace(item);
    //     }
    // }

    private void HandleSlotEmpty()
    {
        Debug.Log("[Robot] PickUpSlot 비어짐");
    }

    private void HandleItemArrive(Item item)
    {
        TryStartWork();
    }   


    public void TryStartWork()
    {
        if (isBusy) return;
        if (plant.State != PlantState.Running) return;
        if (!pickUpSlot.HasItem) return;

        StartCoroutine(PickAndPlaceRoutine());
    }


    private IEnumerator PickAndPlaceRoutine()
    {
        if(State != RobotState.Idle)
            yield break;

        State = RobotState.Running;
        isBusy = true;

        heldItem = pickUpSlot.Release();
        if (heldItem == null)
        {
            State = RobotState.Idle;
            yield break;
        }

        // 처리 시작 전 존3 상태 재확인
        if(zone3Manager == null || zone3Manager.State != ZoneState.Running)
        {
            Debug.Log("[RobotArmController] 처리 시작 전 존3 상태 확인 -> Stopped, 처리 중단");
            
            State = RobotState.Idle;
            yield break;
        }

        // 1. 픽업 포인트로 회전(베이스 + 관절만 제자리에서 회전)
        yield return StartCoroutine(RotateBaseTo(pickPoint));
        yield return StartCoroutine(RotateChestTo(pickPoint));
        yield return StartCoroutine(RotateArmTo(pickPoint));

        // 처리 중간에도 존3 상태 확인
        if(zone3Manager == null || zone3Manager.State != ZoneState.Running)
        {
            Debug.Log("[RobotArmController] 픽업 중 존3 상태 변경 -> Stopped, 현재 아이템만 처리 후 중단");
            // 현재 아이템은 처리 완료
        }

        // 2. 그립 닫기 + 픽업
        CloseGrip();
        yield return new WaitForSeconds(0.2f);
        PickUpItem();

        // 3. 드랍 포인트로 회전(베이스 + 관절만 제자리에서 회전)
        yield return StartCoroutine(RotateBaseTo(dropPoint));
        yield return StartCoroutine(RotateArmTo(dropPoint));
        
        // 드롭 전 존3 상태 최종 확인
        bool zone3WasRunning = (zone3Manager != null && zone3Manager.State == ZoneState.Running);
        
        // 4. 그립 열기 + 드랍
        OpenGrip();
        yield return new WaitForSeconds(0.2f);
        DropItem(zone3WasRunning);  // 존3 상태 전달

        yield return ReturnToIdle();
        
        isBusy = false;  // 작동 끝났으면 상태변경
        State = RobotState.Idle;
        
        // 존3가 Running이면 다음 아이템 처리, 아니면 Idle 유지 -> 싱크기반에서 슬롯기반으로 변경 
        // if(zone3Manager != null && zone3Manager.State == ZoneState.Running)
        // {
        //     TryProcessNextItem();
        // }

        OnBecameIdle?.Invoke();
    }

    // private void TryProcessNextItem() 
    // {
    //     if(State != RobotState.Idle) 
    //     {
    //         Debug.Log("[RobotArmController] 로봇이 Idle 상태가 아님");
    //         return;
    //     }
        
    //     if(!pickUpSlot.HasItem()) 
    //     {
    //         Debug.Log("[RobotArmController] 큐에 아이템 없음");
    //         return;
    //     }
        
    //     // 존3 상태 확인
    //     if(zone3Manager == null || zone3Manager.State != ZoneState.Running)
    //     {
    //         Debug.Log("[RobotArmController] 존3가 Running이 아님 -> 처리 안 함");
    //         return;
    //     }

    //     Item next = pickSink.DequeueItem();
    //     if(next == null)
    //     {
    //         Debug.LogWarning("[RobotArmController] 큐에서 가져온 아이템이 null");
    //         return;
    //     }

    //     // StartCoroutine(PickAndPlaceRoutine(next));
    //     StartPickAndPlace(next);
        
    // }

    private IEnumerator ReturnToIdle()
    {
        yield return RotateArmToIdle();
        yield return RotateChestToIdle();
        yield return RotateBaseToIdle();
    }

    private IEnumerator RotateBaseToIdle()
    {
        const float maxTime = 1.2f;
        float elapsed = 0f;

        while (Quaternion.Angle(baseRoot.localRotation, baseIdleRot) > 1f)
        {
            baseRoot.localRotation = Quaternion.RotateTowards(
                baseRoot.localRotation,
                baseIdleRot,
                baseRotateSpeed * Time.deltaTime
            );

            elapsed += Time.deltaTime;
            if (elapsed > maxTime)
                break;

            yield return null;
        }

        baseRoot.localRotation = baseIdleRot;
    }

    private IEnumerator RotateChestToIdle()
    {
        const float maxTime = 0.9f;
        float elapsed = 0f;

        while (Quaternion.Angle(chest.localRotation, chestIdleRot) > 1f)
        {
            chest.localRotation = Quaternion.RotateTowards(
                chest.localRotation,
                chestIdleRot,
                chestRotateSpeed * Time.deltaTime
            );

            elapsed += Time.deltaTime;
            if (elapsed > maxTime)
                break;

            yield return null;
        }

        chest.localRotation = chestIdleRot;
    }

    private IEnumerator RotateArmToIdle()
    {
        const float maxTime = 0.4f;
        float elapsed = 0f;

        while (Quaternion.Angle(armJoint.localRotation, armIdleRot) > 1f)
        {
            armJoint.localRotation = Quaternion.RotateTowards(
                armJoint.localRotation,
                armIdleRot,
                armRotateSpeed * Time.deltaTime
            );

            elapsed += Time.deltaTime;
            if (elapsed > maxTime)
                break;

            yield return null;
        }

        armJoint.localRotation = armIdleRot;
    }

    // public IEnumerator MoveEffectorToRoutine(Transform target)
    // {
    //     if(effector == null || target == null) yield break;

    //     Vector3 targetPos = target.position;
    //     while(Vector3.Distance(effector.position, targetPos) > 0.01f)
    //     {
    //         effector.position = Vector3.MoveTowards(effector.position, targetPos, effectMoveSpeed * Time.deltaTime);
    //     }
    //     yield break;
    // }

    private IEnumerator RotateChestTo(Transform target)
    {
        if(chest == null || target == null) yield break;

        Vector3 targetPos = target.position;

        const float maxTime = 0.5f;   // 최대 2초까지만 회전
        float elapsed = 0f;

        while(true)
        {
            Vector3 localTarget = chest.TransformPoint(targetPos);
            localTarget.x = 0f;

            if(localTarget.sqrMagnitude < 0.0001f)
                yield break;

            Vector3 targetDir = localTarget.normalized;
            // if(targetDir.y < 0f)
            // {
            //     targetDir.y = targetDir.y;
            // }
            targetDir.Normalize();

            Vector3 currentDir = Vector3.down;

            float angle = Vector3.SignedAngle(currentDir, targetDir, Vector3.right);

            Quaternion targetLocalRot = Quaternion.AngleAxis(angle, Vector3.right);

            chest.localRotation = Quaternion.RotateTowards(
                chest.localRotation,
                targetLocalRot,
                chestRotateSpeed * Time.deltaTime
            );

            //  멈추는 조건 1: 각도 차이가 충분히 작으면 종료 (1도 정도)
            if (Quaternion.Angle(chest.localRotation, targetLocalRot) < 1f)
                yield break;

            //  멈추는 조건 2: 최대 시간 초과 → 그냥 여기서 종료
            elapsed += Time.deltaTime;
            if (elapsed > maxTime)
                yield break;

            yield return null;
        }
    }
    
    public IEnumerator RotateArmTo(Transform target)
    {
        if (armJoint == null || target == null) yield break;

        // 타겟 위치는 시작 시점에 한 번만 스냅샷
        Vector3 worldTargetPos = target.position;

        const float maxTime = 0.5f;   // 최대 2초까지만 회전
        float elapsed = 0f;

        while (true)
        {
            Vector3 localTarget = armJoint.InverseTransformDirection(worldTargetPos);
            localTarget.y = 0f; // Z축 힌지이기 때문에 XY 평면에 투영

            if (localTarget.sqrMagnitude < 0.0001f)
                yield break;

            // 항상 "아래쪽" 반공간(로컬 Y- 영역)만 보도록 강제
            Vector3 targetDir = localTarget.normalized;
            if (targetDir.z > 0f)
            {
                targetDir.z = - targetDir.z; // 위쪽에 있으면 Y 성분을 반전시켜 아래쪽으로 보정
            }
            targetDir.Normalize();

            // 현재 기준 방향은 로컬 아래쪽(Y-)
            Vector3 currentDir = Vector3.up;

            // Z축 기준 각도
            float angle = Vector3.SignedAngle(currentDir, targetDir, Vector3.forward);

            Quaternion targetLocalRot = Quaternion.AngleAxis(angle, Vector3.forward);

            armJoint.localRotation = Quaternion.RotateTowards(
                armJoint.localRotation,
                targetLocalRot,
                armRotateSpeed * Time.deltaTime
            );

            // 멈추는 조건 1: 각도 차이가 충분히 작으면 종료 (1도 정도)
            if (Quaternion.Angle(armJoint.localRotation, targetLocalRot) < 1f)
                yield break;

            // ★ 멈추는 조건 2: 최대 시간 초과 → 그냥 여기서 종료
            elapsed += Time.deltaTime;
            if (elapsed > maxTime)
                yield break;

            yield return null;
        }
}
    
    // HIGH - LEVEL 
    public IEnumerator RotateBaseTo(Transform target)
    {
        // 루트(바디자체)
        // target 방향으로 몸체를 회전시키니까
        // 필요한건 베이스루트 포지션 -> 회전시킬방향 target 혹은 좌표가 필요하고 
        // 방향과 속도를 계산해서 회전을 시켜줌
        while (true)
        {
            Vector3 dir = target.position - baseRoot.position;
            dir.y = 0f;

            if (dir.sqrMagnitude < 0.001f)
                yield break;

            Quaternion targetRot = Quaternion.LookRotation(dir);

            baseRoot.rotation = Quaternion.RotateTowards(
                baseRoot.rotation, 
                targetRot, 
                baseRotateSpeed * Time.deltaTime);

            // 회전 완료 감지
            if (Quaternion.Angle(baseRoot.rotation, targetRot) < 0.5f)
                yield break;

            yield return null;
        }
    }

    // End Effector를 별도로 회전시키고 싶으면 아래 코드를 사용하면 됨
    // 현재 요구사항은 관절(Gripper)만 제자리에서 회전하는 것이기 때문에 사용하지 않음.
    /*
    public IEnumerator RotateEffectTo(Transform target)
    {
        if (effector == null || target == null)
            yield break;

        while (true)
        {
            Vector3 dir = target.position - effector.position;
            Quaternion targetRot = Quaternion.LookRotation(dir);

            effector.rotation = Quaternion.RotateTowards(
                effector.rotation,
                targetRot,
                effectRotateSpeed * Time.deltaTime
            );

            if (Quaternion.Angle(effector.rotation, targetRot) < 0.5f)
                yield break;

            yield return null;
        }
    }
    */
    // 로봇 팔 움직임(집기)
    public void CloseGrip()
    {
        // 레프트 그리퍼 x축 -0.025
        // 라이트 그리퍼 x축 +0.025 움직임
        // 아이템 맞으면 아이템 집기
        // 유지

        // 현재 그립 포지션값 
        // 속도
        // 원하는 위치
        // 양손 다 적용

        if(leftHand == null || rightHand == null) return;

        Vector3 leftPos = leftHand.localPosition;
        Vector3 rightPos = rightHand.localPosition;

        leftPos.x = gripCLoseX;
        rightPos.x = -gripCLoseX;

        leftHand.localPosition = leftPos;
        rightHand.localPosition = rightPos;
    }

    public void OpenGrip()
    {
        // 레프트 그리퍼 x축 +0.025
        // 라이트 그리퍼 x축 -0.025 움직임

        // 나중에 연결 존3 피더포인트 포지션 지점에 도착해서 오픈그립을 호출하면
        // 아이템은 아래로 낙하(어떻게 낙하?)
        // 조건 : 오픈그립을 하면
        // DropItem 호출

        if(leftHand == null || rightHand == null) return;

        Vector3 leftPos = leftHand.localPosition;
        Vector3 rightPos = rightHand.localPosition;

        leftPos.x = -gripCLoseX;
        rightPos.x = gripCLoseX;

        leftHand.localPosition = leftPos;
        rightHand.localPosition = rightPos;   
    }

    public void PickUpItem()
    {
        // 싱크 포지션 지점 맞고
        // 아이템 != null 아니면 
        // 아이템은 pickuppoint로 이동(부모의 자식으로 옮겨줌)
        if (heldItem == null || itemHoldPoint == null) return;
        if (pickUpSlot == null) return;

        Transform itemTr = heldItem.transform;
        itemTr.SetParent(itemHoldPoint);

        itemTr.localPosition = Vector3.zero;
        itemTr.localRotation = Quaternion.identity;
        // itemTr.localScale = Vector3.one;

        Rigidbody rb = heldItem.GetComponent<Rigidbody>();
        if(rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        Debug.Log($"{itemHoldPoint} 아이템 픽업");
    }

    public void DropItem(bool zone3IsRunning = true)
    {
        // 피더 포인트 지점이 맞고
        // // 아이템 != Null 아니면
        // 아이템은 존3 싱크 포인트로 이동

        if (heldItem == null) 
        {
            Debug.LogWarning("[DropItem] heldItem is NULL");
            return;
        }

        if (dropPoint == null)
        {
            Debug.LogError("[DropItem] dropPoint is NULL");
            return;
        }

        Transform t = heldItem.transform;
        Item itemComp = heldItem.GetComponent<Item>();

        // 존3 상태 확인
        bool zone3Running = (zone3Manager != null && zone3Manager.State == ZoneState.Running);
        
        if(!zone3Running || !zone3IsRunning)
        {
            // 존3가 Stopped면 아이템을 비활성화하고 큐에 저장 (또는 별도 저장 공간)
            Debug.Log("[DropItem] 존3가 Stopped -> 아이템 비활성화 및 큐 저장");
            
            // 아이템 비활성화
            t.SetParent(null);
            t.gameObject.SetActive(false);
            
            // 필요시 별도 큐에 저장하거나, 존3 재가동 시 처리할 수 있도록 관리
            // 현재는 비활성화만 하고, 나중에 존3 재가동 시 다시 활성화할 수 있도록 설계
            // (추가 구현 필요: 존3 전용 큐 또는 저장소)
            
            heldItem = null;
            return;
        }

        // 존3가 Running이면 정상 드롭
        // 1. 드롭 위치 배치
        t.SetParent(null);  // 풀어주고 드롭포인트에 넣어줘야함 
        t.position = dropPoint.position;
        t.localScale = Vector3.one;

        // 2) 물리 다시 켜서 떨어지게
        Rigidbody rb = heldItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = false;
        }

        if (itemComp != null)
        {
            if(zone3Manager != null && zone3Manager.route != null && zone3Manager.route.Length > 0)
            {
                 itemComp.SetupRoute(zone3Manager.route);
            }
            else
            {
                Debug.LogWarning("[DropItem] zone3Manager 또는 route가 Null");
                // route가 없어도 OnDropped는 호출 (아이템이 이미 경로를 가지고 있을 수 있음)
            }
           
            itemComp.OnDropped(1);  // ★★ 여기만 호출하면 됨
            Debug.Log("[DropItem] 아이템 정상 드롭 완료");
        }

        heldItem = null;
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            // 1번 키: 픽업 포인트 방향으로 베이스 + 관절만 회전
            StartCoroutine(RotateBaseTo(pickPoint));
            StartCoroutine(RotateArmTo(pickPoint));
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            // 2번 키: 그립 닫기 + 픽업
            CloseGrip();
            PickUpItem();
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            // 3번 키: 드랍 포인트 방향으로 베이스 + 관절만 회전
            StartCoroutine(RotateBaseTo(dropPoint));
            StartCoroutine(RotateArmTo(dropPoint));
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            // 4번 키: 그립 열기 + 드랍
            OpenGrip();
            DropItem();
        }
    }

}

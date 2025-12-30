using UnityEngine;
using System;
using System.Collections;

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

    // ====== ARM ======
    [Header("---Chest---")]
    public Transform chest;
    public float chestRotateSpeed = 10f;

    [Header("---Arm Joint---")]
    public Transform armJoint;  // 관절
    public float armRotateSpeed = 10f;


    // [Header("---End Effctor---")]
    // public Transform effector;           // 필요 없으면 Inspector에서 비워둬도 됨
    // public float effectRotateSpeed = 2f; // 현재는 사용 안 함 (그립 회전만 사용)

    // ====== GRIPPER ======
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

    // ====== ITEM ====== 
    [Header("---Item---")]
    public GameObject heldItem;  // 아이템 받아오기 
    public Transform itemHoldPoint;

    [Header("=== Robot State ===")]
    public RobotState State { get; private set; } = RobotState.Idle;
    private bool isBusy = false;

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
    }

    private void OnEnable()
    {
        if(pickSink != null)
        {
            pickSink.OnItemArrivedForRobot += HandleItemArrived;
            pickSink.OnQueueUpdated += TryProcessNextItem;  // 이벤트 구독 
        } 
    }

    private void OnDisable()
    {
        if(pickSink != null)
        {
            pickSink.OnItemArrivedForRobot -= HandleItemArrived;
            pickSink.OnQueueUpdated -= TryProcessNextItem;
        }    
    }

    private void HandleItemArrived(Item item)  // 필드에서 받아오지 않음 
    {
        if(plant == null || plant.State != PlantState.Running) return;
        if(item == null) return;
        
        // StartPickAndPlace(item); Sink에서 Pick 관리
    }

    public void StartPickAndPlace(Item item)  // 필드에서 받아오지 않음 
    {
        if(isBusy || item == null) return;

        heldItem = item.gameObject;

        StartCoroutine(PickAndPlaceRoutine(item));
    }

    private IEnumerator PickAndPlaceRoutine(Item item)
    {
        if(State != RobotState.Idle)
            yield break;

        State = RobotState.Running;
        isBusy = true;

        // 1. 픽업 포인트로 회전(베이스 + 관절만 제자리에서 회전)
        yield return StartCoroutine(RotateBaseTo(pickPoint));
        yield return StartCoroutine(RotateChestTo(pickPoint));
        yield return StartCoroutine(RotateArmTo(pickPoint));
        // yield return RotateBaseToRoutine(pickPoint);
        // yield return RotateArmToRoutine(pickPoint);

        // 2. 그립 닫기 + 픽업
        CloseGrip();
        yield return new WaitForSeconds(0.2f);
        PickUpItem();

        // State = RobotState.Running;
        // 3. 드랍 포인트로 회전(베이스 + 관절만 제자리에서 회전)
        yield return StartCoroutine(RotateBaseTo(dropPoint));
        yield return StartCoroutine(RotateArmTo(dropPoint));
        // 4. 그립 열기 + 드랍
        OpenGrip();
        yield return new WaitForSeconds(0.2f);
        DropItem();

        yield return ReturnToIdle();
        
        isBusy = false;  // 작동 끝났으면 상태변경
        State = RobotState.Idle;
        TryProcessNextItem();
        // itemPool.ReturnItem(item);

    }

    private void TryProcessNextItem()
    {
        if(State != RobotState.Idle) return;
        if(!pickSink.HasItem()) return;

        Item next = pickSink.DequeueItem();
        heldItem = next.gameObject;
        StartCoroutine(PickAndPlaceRoutine(next));
    }

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
            if(targetDir.y < 0f)
            {
                targetDir.y = targetDir.y;
            }
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
        if(heldItem == null || itemHoldPoint == null) return;

        Transform itemTr = heldItem.transform;
        itemTr.SetParent(itemHoldPoint);
        Debug.Log($"{itemHoldPoint} 아이템 픽업");

        itemTr.localPosition = Vector3.zero;
        itemTr.localRotation = Quaternion.identity;
        // itemTr.localScale = Vector3.one;

        Rigidbody rb = heldItem.GetComponent<Rigidbody>();
        if(rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    public void DropItem()
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

        Item itemComp = heldItem.GetComponent<Item>();
        if (itemComp != null)
        {
            if(zone3Manager != null)
            {
                 itemComp.SetupRoute(zone3Manager.route);
            }
            else
            {
                Debug.LogWarning("[DropItem] zone3Manager is Null");
            }
           
            itemComp.OnDropped(1);  // ★★ 여기만 호출하면 됨
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

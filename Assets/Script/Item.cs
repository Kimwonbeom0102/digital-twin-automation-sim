using UnityEngine;
using System.Collections;

public class Item : MonoBehaviour
{
    private PlantManager plant;
    private ZoneManager zone;

    [Header("이동 설정")]
    public float moveSpeed = 5.0f;                 // 아이템 기본 속도 (PlantManager의 전역 배율과 곱 가능)
    public bool isMoving = false;                // 풀에서 Init 시 true로 켬 // ★ 유지

    [Header("경로 설정")]
    private Transform[] waypoints;                // 경로는 ItemPool/PlantManager에서 주입 // 현재는 아이템에서 경로 설정
    private int currentWaypointIndex = 0;
    private Vector3 targetPosition;              // 현재 목표 위치의 Vector3 // ★ 유지
    
    [Header("물체 정보")]
    public string itemName = "Item";
    public int itemID = 0;

    private ItemPool itemPool;                   // 자신을 만든 풀 (회수시 사용) // 아이템 풀링 후 여기에 저장

    // --- 품질 상태: 외부는 읽기만 가능, 설정은 내부 전용 ---
    public bool IsNG { get; private set; } = false; // 읽기전용 프로퍼티 

    // (선택) 중복 판정 방지용 플래그
    public bool HasEvaluated { get; private set; } = false;

    
    void Update()
    {
        // 1) 이동을 안 하기로 되어 있거나, 경로가 없으면 아무 것도 하지 않음
        if (!isMoving || waypoints == null || waypoints.Length == 0) 
            return;

        // 2) 플랜트의 전역 속도 배율(게이트)을 읽어옴
        //    - Running & 안전 상태면 0보다 큰 값(예: 1.0f)
        //    - Paused/Stopped/EStop/Fault면 0 → 이동 “멈춤”
        float scale = (plant != null) ? plant.GetSpeedScale() : 1f;
        if (scale <= 0f) 
            return; // 전역이 멈춤이면 이 프레임은 이동하지 않음

        // 3) 이번 프레임에 실제로 적용할 속도 계산
        //    - moveSpeed(자기 고유 속도) × scale(전역 배율)
        //    - deltaTime(프레임 보정)으로 초당 속도가 일정하게 유지됨
        float effectiveSpeed = moveSpeed * scale;

        float fixedY = 4.0f; // 씬에 맞춰 숫자만 조절하면 됨
        Vector3 target = new Vector3(targetPosition.x, fixedY, targetPosition.z);

        // 4) 현재 위치 -> 목표 지점(target)으로 한 발자국 “보간 이동”
        //    - MoveTowards는 '초과 이동'을 자동으로 막아줌(부드럽게 이동))
        transform.position = Vector3.MoveTowards(
            transform.position,        // 시작점(현재 아이템 위치)
            target,            // 도착점(이번 웨이포인트 위치)
            effectiveSpeed * Time.deltaTime // 이번 프레임에 움직일 거리(미터)
        );

        // 5) "목표 지점에 도착했는지" 확인
        //    - 0에 딱 맞추면 플로팅 오차로 떨릴 수 있으니 소량의 여유(0.1f)로 체크
        if (Vector3.Distance(transform.position, target) < 0.1f)
        {
            // 다음 웨이포인트로 인덱스 진급
            currentWaypointIndex++;

            // 6) 모든 웨이포인트를 소비했다면 "회수" (싱크가 아닌, 경로 끝으로 회수하는 설계라면)
            if (currentWaypointIndex >= waypoints.Length)
            {
                Debug.Log($"[Item] 웨이포인트 종료, 회수는 Sink에서 담당합니다.");
                isMoving = false;
                // ReturnToPool 메서드 더 이상 호출하지않음 
                // ReturnToPool();  // 풀로 반환(비활성+큐 적재). 여기서 isMoving=false, 상태 초기화 등 수행
                return;          // 더 이상 이동 로직 없음
            }

            // 7) 다음 목표 지점(targetPosition)을 갱신
            //    - waypoints는 Transform[], targetPosition은 Vector3로 저장
            if (waypoints[currentWaypointIndex] != null)
                targetPosition = waypoints[currentWaypointIndex].position;
        }
    }

    public void SetupRoute(Transform[] newRoute)
    {
        if(newRoute == null || newRoute.Length == 0)
        {
            Debug.LogError("[Item] SetupRoute: newRoute가 비어있음");
            return;
        }

        waypoints = newRoute;
    }


    public void OnDropped(int startIndex)
    {
        if(waypoints == null || waypoints.Length == 0)
        {
            Debug.LogError("[Item] OnDropped : waypoints가 비어있음");
            return;
        }

        if(startIndex < 0 || startIndex >= waypoints.Length)
        {
            Debug.LogError("[Item] OnDropped : startIndex 범위 초과");
            return;
        }

        currentWaypointIndex = startIndex;
        targetPosition = waypoints[startIndex].position;

        // 3) 이동 재시작
        isMoving = true;
        // 드롭 직후에는 잠깐 멈춰서 떨어지는 모션 보여주기
        // StartCoroutine(RestartMoveRoutine(startIndex));
        Debug.Log($"[Item] Dropped -> waypoints {startIndex}부터 이동 시작");

    }

    // private IEnumerator RestartMoveRoutine(int startIndex)
    // {
    //     // ★ 0.1~0.2초 정도 기다렸다가 이동 재개해야 자연스러움
    //     yield return new WaitForSeconds(0.15f);

    //     // 1) 이동 재시작
    //     isMoving = true;

    //     // 2) Zone3으로 가는 웨이포인트 인덱스 초기화
    //     currentWaypointIndex = startIndex;

    //     // 3) 이동 목표 갱신
    //     if (waypoints != null && waypoints.Length > currentWaypointIndex)
    //         targetPosition = waypoints[currentWaypointIndex].position;

    //     Debug.Log($"[Item] Drop 이후 Zone3 웨이포인트 {startIndex}번부터 이동 재시작");
    // }

    // (선택) 품질 점수나 측정값을 저장하고 싶으면 여기에 추가 가능
    // public float measuredValue;

    /// <summary>
    /// 품질 판정의 “유일한 진입점”.
    /// threshold(0~1): NG 확률. 0.1f면 대략 10%가 NG.
    /// </summary>
    public void EvaluateQuality(float threshold)
    {
        if (HasEvaluated) return;         // 🔒 같은 아이템의 재평가 방지(선택)
        if (threshold < 0f) threshold = 0f;
        if (threshold > 1f) threshold = 1f;

        // 임시 로직: Random.value(0~1) 가 threshold보다 크면 NG
        IsNG = Random.value < threshold;  // 예: threshold 0.1 → 약 90% OK / 10% NG
        HasEvaluated = true;
    }

    /// <summary>
    /// 외부에서 직접 결과를 지정해야 하는 경우(예: 외부 검사기) 위해 준비.
    /// 내부 무결성은 유지하면서 공개 API로만 수정 가능.
    /// </summary>
    public void SetQuality(bool isNg)
    {
        IsNG = isNg;
        HasEvaluated = true;
    }

    // ... (이동/웨이포인트/풀 반환 로직은 기존 그대로)

    /// <summary>
    /// 풀에서 꺼낼 때 1회 초기화. (생성자가 아님)
    /// </summary>
    public void Init(ItemPool pool, Transform[] assignedWaypoints, float speed, string name, int id, ZoneManager zoneRef = null)
    {
        itemPool = pool;                         // ★ 추가: 나를 만든 풀 기억
        waypoints = assignedWaypoints;           // ★ 유지: 경로 주입
        moveSpeed = speed;                       // ★ 추가: 속도 주입
        itemName = name;
        itemID = id;
        zone = zoneRef;

        IsNG = false;
        HasEvaluated = false;

        currentWaypointIndex = 0;

        if(waypoints != null && waypoints.Length > 0 && waypoints[0] != null)
        {
            targetPosition = waypoints[0].position;
        }

        // targetPosition = waypoints[0].position;
        // transform.position = targetPosition;


        isMoving = true;
        gameObject.SetActive(true);
    }

    // public void Init(ItemPool pool, Transform[] assignedWaypoints, float speed, string name, int id, PlantManager plantRef = null)
    // {
    //     itemPool = pool;                         // ★ 추가: 나를 만든 풀 기억
    //     waypoints = assignedWaypoints;           // ★ 유지: 경로 주입
    //     moveSpeed = speed;                       // ★ 추가: 속도 주입
    //     itemName = name;
    //     itemID = id;

    //     // ✅ 풀 재사용 초기화
    //     IsNG = false;
    //     HasEvaluated = false;

    //     // (선택) 전역 속도 게이트를 읽기 위한 참조
    //     plant = plantRef;

    //     currentWaypointIndex = 0;                // ★ 유지
    //     if (waypoints != null && waypoints.Length > 0 && waypoints[0] != null)
    //     {
    //         targetPosition = waypoints[0].position; // ★ 유지
    //         transform.position = targetPosition;     // ★ 추가: 스폰 지점으로 스냅
    //     }

    //     isMoving = true;                         // 이동 시작 // ★ 유지
    //     gameObject.SetActive(true);              // 풀에서 활성화 // ★ 유지
    // }


    public void SetMoving(bool move) => isMoving = move;            // ★ 유지
    public void SetMoveSpeed(float speed) => moveSpeed = speed;     // ★ 유지

    /// <summary>
    /// 풀로 반환
    /// </summary>
    void ReturnToPool()
    {
        isMoving = false;                                           // ★ 유지
        if (itemPool != null)
            itemPool.ReturnItem(this);                              // ★ 수정: 자기 파괴 X → 풀로 반환
        else
            gameObject.SetActive(false);

        Debug.Log($"[Item] 회수됨: {itemName} (ID:{itemID})");
    }

    /// <summary>
    /// 목표 웨이포인트로 이동
    /// 나중에 사용할수도 있는 메서드 생성
    /// 해당 메서드는 호출하지 않음.
    /// </summary>
    void MoveToNextWaypoint() //
    {
        float fixedY = 4.0f;
        // ★ 수정: targetPosition은 Vector3이므로 .position 사용 금지
        Vector3 target = new Vector3(targetPosition.x, fixedY, targetPosition.z);
        
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);

        // 도달 판정
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            currentWaypointIndex++;

            if (currentWaypointIndex >= waypoints.Length)
            {
                ReturnToPool(); // 마지막 포인트 도달 → 회수
                return;
            }

            // ★ 유지: 다음 타겟 갱신
            if (waypoints[currentWaypointIndex] != null)
                targetPosition = waypoints[currentWaypointIndex].position;
        }
    }
}

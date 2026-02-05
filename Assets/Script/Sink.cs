using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public enum ZoneType
{
    Zone1,
    Zone2,
    Zone3
}

public enum SinkRole
{
    Zone1,
    Zone2_Buffer,
    Zone3
}

/// <summary>
/// Sink
/// - 아이템 회수 지점
/// - 큐 스택 관리
/// - 로봇은 이벤트로만 연결
/// </summary>
public class Sink : MonoBehaviour
{
    [Header("Sink Info")]
    public ZoneType zoneType;

    [Header("Sink Role")]
    [SerializeField] private SinkRole role;
    [SerializeField] private BufferZone bufferZone;
    [SerializeField] private StorageRack storageRack;
    [SerializeField] private NgRack ngRack;

    // 더 이상 로봇시퀀스에 관여하지않음
    // [Header("Flags")] 
    // public bool holdForRobot = false;   // Zone2에서 로봇 공정 대상 여부 

    [Header("References")]
    [SerializeField] private ItemPool itemPool;   // Zone3 전용 풀

    // ===== Queue =====
    private Queue<Item> itemQueue = new Queue<Item>();
    private bool isProcessing = false;

    // ===== Count (Zone3) =====
    [SerializeField] private int totalCount = 0;
    private int okCount = 0;
    private int ngCount = 0;
    private float ngThreshold = 0.1f;

    // ===== Events =====
    public event Action<Sink, Item> OnSink;          // ZoneManager 구독
    public event Action OnQueueUpdated;               // Robot / ZoneManager
    // public event Action<Item> OnItemArrivedForRobot;  // Robot 전용
    public event Action<int, int, int> OnCountChanged;     // UI

    public int QueueCount => itemQueue.Count;
    public int TotalCount => totalCount;
    public int NgCount => ngCount;
    public int OkCount => okCount; 

    // ===============================
    // Queue API (유일한 Enqueue 경로)
    // ===============================
    public void EnqueueItem(Item item)
    {
        if (item == null) return;

        item.gameObject.SetActive(false);
        itemQueue.Enqueue(item);
        OnQueueUpdated?.Invoke();

        
        Debug.Log($"[Sink-{zoneType}] Enqueue : {item.itemName}, Count={itemQueue.Count}");
    }

    public bool HasItem()
    {
        return itemQueue.Count > 0;
    }

    public Item DequeueItem()
    {
        if (itemQueue.Count == 0) return null;
        return itemQueue.Dequeue();
    }

    // ===============================
    // Trigger
    // ===============================
    private void OnTriggerEnter(Collider other)
    {
        if (isProcessing) return;

        Item item = other.GetComponent<Item>();
        if (item == null) return;
        // item.EvaluateQuality(threshold);
        // if (item.IsNG) Debug.Log("[Sink] 불량품 감지!");

        isProcessing = true;
        
        Debug.Log($"[Sink-{zoneType}] Trigger Enter : {item.name}");

        // ===========================
        // Zone3 : 최종 회수
        // ===========================
        if (zoneType == ZoneType.Zone3)
        {
            item.EvaluateQuality(ngThreshold);

            totalCount++;
            if (item.IsNG)
            {
                ngCount++;
                // itemPool.ReturnItem(item);
                ngRack.StoreNg(item);

                AccumulatedStatsManager.Instance.AddNG();
                DataLogger.Instance.LogInspectionResult("Zone3", true);
            } 
            else
            {
                okCount++;
                storageRack.Store(item);

                AccumulatedStatsManager.Instance.AddOK();
                DataLogger.Instance.LogInspectionResult("Zone3", false);

            }
            Debug.Log($"[Sink3] 검사 완료 → {item.itemName} " + $"Total={totalCount}, OK={okCount}, NG={ngCount}");

            
            OnCountChanged?.Invoke(totalCount, ngCount, okCount);

            StartCoroutine(ResetFlag());
            return;
        }
        
        if (zoneType == ZoneType.Zone1)
        {
            EnqueueItem(item);
            // item.gameObject.SetActive(false);
            Debug.Log($"[Sink-{zoneType}] 인큐 + 비활성화");
        }

        // ===========================
        // Zone2 + 로봇 대상
        // ===========================

        switch (role)
        {
            case SinkRole.Zone2_Buffer:
                bufferZone.Enqueue(item);
                break;
        }

        // if (zoneType == ZoneType.Zone2)  // 로봇트리거를 비활성화해서 픽업슬롯으로 로봇시퀀스 작동
        // {
        //     // EnqueueItem(item);

        //     if (holdForRobot)
        //     {
        //         Debug.Log("[Sink-Zone2] 로봇 공정 대상 → 이벤트 전달");

        //         // FreezeItem(item);
        //         // OnQueueUpdated?.Invoke();

        //         OnItemArrivedForRobot?.Invoke(item);
        //     }
        // }

        OnSink?.Invoke(this, item);
        StartCoroutine(ResetFlag());
        
    }

    private void FreezeItem(Item item)
    {
        if (item == null) return;

        item.isMoving = false;
        
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private IEnumerator ResetFlag()
    {
        yield return new WaitForSeconds(0.05f);
        isProcessing = false;
    }
}

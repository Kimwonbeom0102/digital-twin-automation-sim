using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;


public enum ZoneType
{
    Zone1,
    Zone2,
    Zone3
}

/// <summary>
/// 아이템 회수 지점 컨트롤하는 객체     
/// /// </summary>
public class Sink : MonoBehaviour
{
    public ZoneType zoneType;
    public bool holdForRobot = false;
    [SerializeField] public Item currentItem;
    
    [SerializeField] private ItemPool itemPool;     // ★ 추가: 회수할 풀
    [SerializeField] private int totalCount = 0;    // ★ 추가: 누적 카운트(옵션)

    private Queue<Item> itemQueue = new Queue<Item>();
    private int ngCount = 0;

    [SerializeField, Range(0f, 1f)] private float ngThreshold = 0.1f; // 임시 NG 확률(예시) 인스펙터에서 설정 

    public event Action<Sink> OnSink;
    public event Action<Item> OnItemArrivedForRobot;
    public event Action OnQueueUpdated;

    private bool isProcessing = false;

    public event Action<int, int> OnCountChanged;

    // 필요하면 외부에서 읽을 수 있게 프로퍼티도 제공
    public int TotalCount => totalCount;
    public int NgCount => ngCount;

    // public bool isSinkPassed = false;

    // public void OnItemArrived(Item item)
    // {
    //     itemQueue.Enqueue(item);
    //     OnQueueUpdated?.Invoke();
    // }

    public bool HasItem()
    {
        return itemQueue.Count > 0;
    }
    public Item DequeueItem()
    {
        return itemQueue.Dequeue();
    }

    /// <summary>
    /// 싱크에 충돌시 
    /// 업데이트에서 1번 호출 (아이템이 할당되어있고 싱크에 닿으면 풀로 보내고)
    /// (아이템이 비어있으면 풀로 보내지않고 종료한다.)
    /// <summary>
    private void OnTriggerEnter(Collider other)  
    {
        Item item = other.GetComponent<Item>();
        if(item == null) return;
        
        if(isProcessing) return;
        isProcessing = true;

        Debug.Log("[Sink Trigger Enter] " + other.name + " at " + Time.frameCount);

        // itemQueue.Enqueue(item);
        // Debug.Log($"[Sink] Queue Count = {itemQueue.Count}");
        // OnQueueUpdated?.Invoke();  // 누가 등록? -> 머신이 구독해서 큐에 스택이 쌓인지 체크
        
        // GetComponent<Item>() 은 Unity에서
        // “이 GameObject에 Item 컴포넌트가 붙어있다면 그걸 반환하고,
        // 없다면 null을 반환한다.”
        
        // 아이템인지 확인(충돌체의 GameObject에 Item 컴포넌트가 있는가?)
        if (item == null)
        {
            // 아이템이 아니면 무시
            Debug.Log("[Sink] Item 컴포넌트가 아닌 충돌체입니다.");
            return;
        }
            
        // Debug.Log($"[Sink] {zoneType} 아이템 존재함");

        // 회수 직전 품질 판정(데모: 확률 기반). 중복 판정을 막고 싶으면 Item.HasEvaluated로 가드
        item.EvaluateQuality(ngThreshold);
        if (item.IsNG) Debug.Log("[Sink] 불량품 감지!");
        
        if (zoneType == ZoneType.Zone3)
        {
            totalCount++;                   // ★ 추가: 카운팅 (풀로 반환할때만 카운트가 됨)
            if(item.IsNG) 
                ngCount++;
            
            Debug.Log($"[Sink3] 생산 카운트 증가 → Total:{totalCount}, NG:{ngCount}");
            Debug.Log($"[Sink3] 회수: {item.itemName}, NG:{item.IsNG} (누적:{totalCount})");

            OnCountChanged?.Invoke(totalCount, ngCount);
            itemPool.ReturnItem(item);
        }
        else if(zoneType == ZoneType.Zone1)    
        {
            Debug.Log($"[Sink1] Zone {zoneType} -> 일반존이면 회수, 로봇존이면 로봇 처리");
            itemPool.ReturnItem(item);
        }

        if(holdForRobot)
        {
            itemQueue.Enqueue(item);
            Debug.Log($"[Sink2] Queue Count = {itemQueue.Count}");
            OnQueueUpdated?.Invoke();  // 누가 등록? -> 머신이 구독해서 큐에 스택이 쌓인지 체크

            Debug.Log($"[Sink2] Zone {zoneType} -> 로봇 픽업용 싱크, 카운트/회수 X");
            currentItem = item;

            var rb = item.GetComponent<Rigidbody>();
            if(rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            item.transform.position = transform.position;
            item.transform.rotation = transform.rotation;

            OnItemArrivedForRobot?.Invoke(item);  // 로봇에게 이벤트 넘겨줌. 아이템이 작업대상임
            
            StartCoroutine(ResetFlag());
            return;
        }

        // 회수 (혹은 임시 비활성화)
        // if(itemPool != null && zoneType != ZoneType.Zone2)
        // {
        //     itemPool.ReturnItem(item);
        // }
        // if (itemPool != null) // Sink에 itemPool 인스턴스 연결되어있음? 
            // itemPool.ReturnItem(item);      // ★ 추가: 풀로 반환 (풀로 보내버림) 풀에 있는 메서드를 활용  
        // else // Sink가 풀을 모르면 (인스펙터 연결 안되어있으면) 아이템 그냥 꺼버림
        //     item.gameObject.SetActive(false);
        


        OnSink?.Invoke(this);

        StartCoroutine(ResetFlag());

        // item.gameObject.SetActive(false);
    }
    

    private IEnumerator ResetFlag()
    {
        yield return new WaitForSeconds(0.05f);

        isProcessing = false;
    }
}

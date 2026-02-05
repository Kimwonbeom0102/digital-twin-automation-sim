using UnityEngine;
using System;
using System.Collections.Generic;

public class PickUpSlot : MonoBehaviour
{
    public Item currentItem;

    public event Action OnBecameEmpty;
    public event Action<Item> OnItemArrived; // 슬롯에 실제로 도착했을 때(트리거 기반)
    [SerializeField] private RobotArmController robot;


    public bool HasItem  // 슬롯상태 확인
    {
        get {return currentItem != null;}
    }

    public bool AssignPickUpSlot(Item item)
    {
        if (item == null)
            return false;

        if (currentItem != null)
            return false; // 이미 점유 중

        currentItem = item;

        // 이동/물리 제어는 슬롯이 책임짐
        SnapAndHold(item);
        
        OnItemArrived?.Invoke(item);

        return true;
    } 

    public Item Release() // 아이템 제거 메서드 (로봇이 집었을때 슬롯을 비움)
    {
        if (currentItem == null) return null;

        Item releasedItem = currentItem; // 픽업 대상 아이템을 꺼내고 

        currentItem = null;  // 빈 슬롯으로 만들고 

        OnBecameEmpty?.Invoke();
        return releasedItem;  // 다음 아이템으로 확정 
    }

    /// <summary>
    /// 픽업 슬롯 위치에 고정
    /// </summary>
    private void SnapAndHold(Item item)
    {
        item.isMoving = false;

        if (item.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        item.transform.position = transform.position;
        item.transform.rotation = transform.rotation;
    }


    // public void Reserve(Item item)  // 아이템을 픽업 대상으로 예약
    // {
    //     if (currentItem != null)  // 다른 아이템이 있으면 종료
    //         return;

    //     currentItem = item;  // 아무것도 없으면 아이템을 픽업 대상으로 확정 
    // }
    
    

    // private void Awake()
    // {
    //     var col = GetComponent<Collider>();
    //     if (col != null)
    //         col.isTrigger = true;
    // }

    // private void OnTriggerEnter(Collider other)
    // {
    //     var item = other.GetComponent<Item>();

    //     if (currentItem != null) return;
    //     if (item == null) return;

    //     Debug.Log($"[PickUpSlot Trigger] {item.name} / frame={Time.frameCount}");

    //     item.SetHeadingToPickUpSlot(false);

    //     // 슬롯에 도착했으니 여기서 "정지 + 고정 + 예약" 처리
    //     Reserve(item);
    //     SnapAndStop(item);

    //     // 로봇에게 상태 확인 요청 
    //     // robot.TryPick();
    //     OnItemArrived?.Invoke(item);
    // }

    // private void SnapAndStop(Item item)
    // {
    //     if (item == null) return;

    //     item.SetMoving(false);

    //     var rb = item.GetComponent<Rigidbody>();
    //     if (rb != null)
    //     {
    //         rb.linearVelocity = Vector3.zero;
    //         rb.angularVelocity = Vector3.zero;
    //         rb.isKinematic = true;
    //     }

    //     item.transform.position = transform.position;
    //     item.transform.rotation = transform.rotation;
    // }
}

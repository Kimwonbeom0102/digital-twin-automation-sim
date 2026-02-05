using System;
using UnityEngine;


public class BufferSlot : MonoBehaviour
{
    public Item currentItem;

    public bool IsEmpty => currentItem == null;

    public bool HasItem => currentItem != null;

    public void Assign(Item item)
    {
        currentItem = item;
        Snap(item);
    }

    public Item Clear()
    {
        var item = currentItem;
        currentItem = null;
        return item;
    }

    public void Snap(Item item)
    {
        currentItem = item;

        item.isMoving = false;
        item.transform.position = transform.position;
        item.transform.rotation = transform.rotation;
    }
}



// /// <summary>
// /// 버퍼존에 아이템을 "도착 시" 고정/대기시키는 슬롯.
// /// 로봇은 버퍼 슬롯 아이템을 직접 집지 않고, 픽업 슬롯이 비면 Zone2가 Release하여 픽업 슬롯으로 흘려보낸다.
// /// </summary>
// [RequireComponent(typeof(Collider))]
// public class BufferSlot : MonoBehaviour
// {
//     public Item currentItem;

//     public event Action<Item> OnItemArrived; // 슬롯에 아이템이 도착해 고정되었을 때

//     private void Awake()
//     {
//         var col = GetComponent<Collider>();
//         col.isTrigger = true;
//     }

//     private void OnTriggerEnter(Collider other)
//     {
//         if (currentItem != null) return;

//         var item = other.GetComponent<Item>();
//         if (item == null) return;

//         if (!item.IsHeadingToBufferSlot(slotIndex))
//         {
//             Debug.Log($"[BufferSlot {slotIndex}] 무시 - 목표 슬롯 아님");
//             return;
//         }
        
//         Reserve(item);
//         SnapAndStop(item);
//         OnItemArrived?.Invoke(item);
//     }

//     public bool Reserve(Item item)
//     {
//         if (currentItem != null) return false;
//         if (item == null) return false;
//         currentItem = item;
//         return true;
//     }

//     public Item Release()
//     {
//         var released = currentItem;
//         currentItem = null;
//         return released;
//     }

//     private static void SnapAndStop(Item item)
//     {
//         item.SetMoving(false);

//         var rb = item.GetComponent<Rigidbody>();
//         if (rb != null)
//         {
//             rb.linearVelocity = Vector3.zero;
//             rb.angularVelocity = Vector3.zero;
//             rb.isKinematic = true;
//         }
//     }
// }





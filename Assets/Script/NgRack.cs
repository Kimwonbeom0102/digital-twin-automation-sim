using UnityEngine;
using System;
using System.Collections.Generic;

public class NgRack : MonoBehaviour
{
    [SerializeField] private List<Transform> slots = new List<Transform>();
    public int currentIndex = 0;
    [SerializeField] private ItemPool itemPool;

    /// <summary>
    /// ng 아이템을 스토리지 슬롯에 적재
    /// </summary>
    public void StoreNg(Item item)
    {
        if (item == null)
        {
            Debug.LogWarning("[StoreNg] Item is null");
            return;
        }

        if (currentIndex >= slots.Count)
        {
            Debug.LogWarning("[StoreNg] 슬롯 가득 참");
            return;
        }

        Transform slot = slots[currentIndex];

        // 아이템 이동
        item.transform.SetParent(slot);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;
        
        // 물리 / 이동 완전 차단

        StopMoving(item);
        

        currentIndex++;

        Debug.Log($"[StoreNg] 아이템 적재 완료 ({currentIndex}/{slots.Count})");

    }

     private void StopMoving(Item item)
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

    /// <summary>
    /// 스토리지 초기화 (선택)
    /// </summary>
    public void ClearNg()
    {
        for (int i = 0; i < currentIndex; i++)
        {
            Transform slot = slots[i];
            if (slot.childCount == 0) continue;

            for (int c = slot.childCount - 1; c >= 0; c--)
            {
                Transform child = slot.GetChild(c);
                Item item = child.GetComponent<Item>();
                if (item != null)
                {
                    // item.transform.SetParent(null);
                    itemPool.ReturnItem(item);
                }
            }
        }

        currentIndex = 0;
        Debug.Log("[StoreNg] 초기화 완료");
    }

   
}

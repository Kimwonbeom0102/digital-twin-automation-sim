using UnityEngine;
using System;
using System.Collections.Generic;

public class StorageRack : MonoBehaviour
{
    [SerializeField] private List<Transform> slots = new List<Transform>();

    public event Action<int, string> OnStorageUpdated;
    [SerializeField] private int totalCount = 0;
    private string lastInTime = "_";

    public int currentIndex = 0;

    [SerializeField] private ItemPool itemPool;

    public int maxCapacity => slots.Count;
    /// <summary>
    /// OK 아이템을 스토리지 슬롯에 적재
    /// </summary>
    public void Store(Item item)
    {
        if (item == null)
        {
            Debug.LogWarning("[StorageRack] Item is null");
            return;
        }

        if (currentIndex >= slots.Count)
        {
            Debug.LogWarning("[StorageRack] 슬롯 가득 참");
            return;
        }

        Transform slot = slots[currentIndex];

        // 아이템 이동
        item.transform.SetParent(slot);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;
        
        // 물리 / 이동 완전 차단

        StopMovement(item);
        

        currentIndex++;

        totalCount++;
        lastInTime = DateTime.Now.ToString("HH:mm:ss");

        OnStorageUpdated?.Invoke(totalCount, lastInTime);

        Debug.Log($"[StorageRack] 아이템 적재 완료 ({currentIndex}/{slots.Count})");

    }

    /// <summary>
    /// 스토리지 초기화 (선택)
    /// </summary>
    public void ClearOk()
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

        if (totalCount != 0)
        {
            totalCount = 0;
            lastInTime = "-";

            OnStorageUpdated?.Invoke(totalCount, lastInTime);
        }
        Debug.Log("[StorageRack] 초기화 완료");
    }

    private void StopMovement(Item item)
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

}

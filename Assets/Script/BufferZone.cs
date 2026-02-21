using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro;

public enum BufferState
{
    Empty,  // 0개
    Processing,  // 1개 (슬롯)
    Backlog,  // 2개이상 (인큐로보냄)
    Blocked   // 인큐도 맥스일 때
}

public class BufferZone : MonoBehaviour
{
    // [SerializeField] private BufferSlot[] slots; // 0 = 로봇에 가장 가까움
    private Queue<Item> queue = new Queue<Item>();
    public event Action<int> OnQueueChanged;
    public event Action<BufferState> OnBufferStateChanged;
    
    private BufferState currentState;
    [SerializeField] private List<BufferSlot> slots;
    [SerializeField] private int maxCapacity = 10;

    

    public void Enqueue(Item item)
    {
        if (queue.Count >= maxCapacity)
        {
            UpdateBufferState(queue.Count);
            return;
        }

        queue.Enqueue(item);
        UpdateVisuals();
        OnQueueChanged?.Invoke(queue.Count);
    }

    public Item Dequeue()
    {
        if (queue.Count == 0)
            return null;

        var item = queue.Dequeue();
        UpdateVisuals();
        OnQueueChanged?.Invoke(queue.Count);
        return item;
    }

    public bool HasItem => queue.Count > 0;

    private void UpdateVisuals()
    {
        Item[] items = queue.ToArray();

        // 1️⃣ 슬롯 비우기
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].Clear();
        }

        // 2️⃣ 첫 번째 아이템만 표시 (슬롯 1개)
        if (items.Length > 0)
        {
            Item item = items[0];
            item.gameObject.SetActive(true);
            slots[0].Snap(item);
        }

        // 3️⃣ 나머지는 비활성화
        for (int i = 1; i < items.Length; i++)
        {
            items[i].gameObject.SetActive(false);
        }

        UpdateBufferState(items.Length);
    }

    private void UpdateBufferState(int count)
    {
        BufferState newState;

        if (count == 0)
            newState = BufferState.Empty;
        else if (count == 1)
            newState = BufferState.Processing;
        else
            newState = BufferState.Backlog;

        if (newState != currentState)
        {
            currentState = newState;
            OnBufferStateChanged?.Invoke(currentState);
        }
    }

}

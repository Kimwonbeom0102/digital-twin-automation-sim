using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro;

public class BufferZone : MonoBehaviour
{
    // [SerializeField] private BufferSlot[] slots; // 0 = 로봇에 가장 가까움
    private Queue<Item> queue = new Queue<Item>();
    public event Action OnQueueChanged;
    
    [SerializeField] private List<BufferSlot> slots;
    [SerializeField] private GameObject overflowIndicator;
    [SerializeField] private TMP_Text overflowText;


    private void Awake()
    {
        if (overflowIndicator != null)
            overflowIndicator.SetActive(false);
    }

    public void Enqueue(Item item)
    {
        queue.Enqueue(item);
        UpdateVisuals();
        OnQueueChanged?.Invoke();
    }

    public Item Dequeue()
    {
        if (queue.Count == 0)
            return null;

        var item = queue.Dequeue();
        UpdateVisuals();
        return item;
    }

    public bool HasItem => queue.Count > 0;

    private void UpdateVisuals()
    {
        Item[] items = queue.ToArray();

        // 1️⃣ 모든 슬롯 비우기 (핵심)
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].Clear();
        }

        // 2️⃣ 큐 순서대로 다시 채우기
        int visibleCount = Mathf.Min(items.Length, slots.Count);

        for (int i = 0; i < visibleCount; i++)
        {
            Item item = items[i];

            item.gameObject.SetActive(true);
            slots[i].Snap(items[i]);
        }

        for (int i = visibleCount; i < items.Length; i++)
        {
            items[i].gameObject.SetActive(false);  // 6번부터 
        }

        // 3️⃣ +N 표시
        int overflow = items.Length - slots.Count;
        overflowIndicator.SetActive(overflow > 0);
        if (overflow > 0)
            overflowText.text = $"+{overflow}";
    }


}

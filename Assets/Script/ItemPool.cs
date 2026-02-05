using UnityEngine;
using System.Collections.Generic;

public class ItemPool : MonoBehaviour
{
    [Header("아이템 풀 설정")]
    [SerializeField] private GameObject itemPrefab;      // ★ 유지
    [SerializeField] private int initialSize = 30;       // ★ 유지

    private readonly Queue<Item> poolQueue = new Queue<Item>(); // ★ 유지/정리

    void Awake()
    {
        // 초기 재고 확보
        for (int i = 0; i < initialSize; i++)
        {
            GameObject go = Instantiate(itemPrefab, transform);
            go.SetActive(false);
            poolQueue.Enqueue(go.GetComponent<Item>());
        }
    }

    /// <summary>
    /// 비활성 아이템 하나 꺼내 활성화(Init은 외부에서 호출)
    /// </summary>
    public Item GetItem()
    {
        if (poolQueue.Count == 0)
        {
            GameObject goNew = Instantiate(itemPrefab, transform); // 추가: 부족 시 추가 생성
            goNew.SetActive(false);
            poolQueue.Enqueue(goNew.GetComponent<Item>());
        }
        return poolQueue.Dequeue(); // 아직 비활성화 상태 

        // Item item = poolQueue.Dequeue();
        // item.gameObject.SetActive(true);
        // return item.gameObject; // ★ 유지: GameObject 반환
    }

    /// <summary>
    /// 사용 완료된 아이템을 풀로 반납
    /// </summary>
    public void ReturnItem(Item item)
    {
        if (item == null) return;
        item.transform.SetParent(null);
        item.gameObject.SetActive(false);   // 비활성
        poolQueue.Enqueue(item);            // 재고로 복귀
    }

    // ** 인터페이스로 정의하여 여러 클래스를 정의하고 사용시 생성후 호출 


}

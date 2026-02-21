using System;                 // Action<T> 사용
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]     // 콜라이더 필수
public class Sensor : MonoBehaviour
{
    // 식별
    public int sensorId;          // 센서 번호
    public int zoneId;         // 선택 사항

    [SerializeField] private Renderer indicatorRenderer;

    [SerializeField] private Color idleColor = Color.gray;
    [SerializeField] private Color passColor = Color.green;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color faultColor = Color.red;


    // 시간 관리
    public float timeoutSec = 5.0f;       // T초 동안 PASS가 없으면 경고
    public float checkInterval = 1.0f;  // 확인 주기(초)

    // 감지 필터
    public string itemTag = "Item";     // 통과 물체 태그

    // 알림 이벤트(매니저가 구독)
    public event Action<Sensor> OnNoPass;

    public ZoneManager zone;
    // 내부 상태
    private float lastPassTime;

    private bool faultCheckEnabled = false;

    void Start()
    {
        // 트리거로 쓰려면 Collider.isTrigger = true 필요
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        lastPassTime = Time.time;

        if (zone.State == ZoneState.Running)
        {
            if(zoneId == 1)
            {
                StartCoroutine(CheckNoPass());

            }
        }

    }

    private void SetIndicatorColor(Color color)
    {
        if (indicatorRenderer == null) return;
        indicatorRenderer.material.color = color;
    }

    void OnTriggerEnter(Collider other)
    {
        // 아이템만 인정
        if (!other.CompareTag(itemTag)) return;

        lastPassTime = Time.time;

        if (zone.State == ZoneState.Warning)
            zone.ReturnToRunning();
            
        Debug.Log($"[Sensor#{sensorId}] Item 지나감!");

        SetIndicatorColor(passColor);

        StartCoroutine(ReturnToIdle());
    }

    private IEnumerator ReturnToIdle()
    {
        yield return new WaitForSeconds(0.2f);
        SetIndicatorColor(idleColor);
    }

    private IEnumerator CheckNoPass()
    {
        yield return new WaitForSeconds(2f);
        faultCheckEnabled = true;

        var wait = new WaitForSeconds(checkInterval);
        while (true)
        {

            if (!faultCheckEnabled)
            {
                yield return null;
                continue;
            }

            if(zone.State != ZoneState.Running)
            {
                yield return wait;
                continue;
            }

            // 마지막 PASS 이후 timeoutSec 초가 지났는지 체크
            if (Time.time - lastPassTime > timeoutSec)
            {
                Debug.LogWarning($"[Sensor#{sensorId}] 안 지나감(Timeout)");
                SetIndicatorColor(warningColor);

                OnNoPass?.Invoke(this);                 // 매니저가 구독하여 처리
                lastPassTime = Time.time;               // 같은 경고 연속 발행 방지
                // if(Time.time - lastPassTime > 30f)  // 여기에 존매니저 스탑상태라면 wait
                // {
                    
                // }
            }
            yield return wait;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // 센서 영역 시각화(박스 중심/크기 추정)
        var col = GetComponent<Collider>();
        if (!col) return;
        Gizmos.color = Color.cyan;
        Gizmos.matrix = transform.localToWorldMatrix;
        if (col is BoxCollider box)
            Gizmos.DrawWireCube(box.center, box.size);
    }
#endif
}

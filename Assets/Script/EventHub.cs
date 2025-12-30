using UnityEngine;

/// <summary>
/// 관제탑(옵저버 역할)
/// 공정 상태를 추적하고 알람(색상,경보 등) 알림
/// <summary>
public class EventHub : MonoBehaviour
{
    [Header("센서 설정")]
    [SerializeField] private GameObject noticeBoard; // 색상변경을 위한 보드 

    // public GameObject noticeBoardColor = GetComponent<noticeBoard>();

    [SerializeField] private bool stuckLine = false;
    [SerializeField] private float stuckTimer;



    // 예시: 카운트 변경 방송
    // public event Action<int,int> OnCountsChanged;
    // public void EmitCounts(int total, int ng) => OnCountsChanged?.Invoke(total, ng);

    
}

using System.IO;
using UnityEngine;

public class AccumulatedStatsManager : MonoBehaviour
{
    public static AccumulatedStatsManager Instance;

    private string savePath;
    public AccumulatedStats stats;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitPath();
        LoadStats();
    }

    private void InitPath()
    {
        savePath = Path.Combine(
            Application.persistentDataPath,
            "Stats",
            "AccumulatedStats.json"
        );

        string dir = Path.GetDirectoryName(savePath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    // 🔹 시작 시 누적 데이터 로드
    public void LoadStats()
    {
        if (!File.Exists(savePath))
        {
            stats = new AccumulatedStats();
            Debug.Log("[AccumulatedStats] No file. Start from zero.");
            return;
        }

        string json = File.ReadAllText(savePath);
        stats = JsonUtility.FromJson<AccumulatedStats>(json);

        Debug.Log($"[AccumulatedStats] Loaded → Total:{stats.total}, OK:{stats.ok}, NG:{stats.ng}, Fault:{stats.faultCount}");
    }

    // 🔹 종료 시 누적 데이터 저장 (덮어쓰기)
    public void SaveStats()
    {
        string json = JsonUtility.ToJson(stats, true);
        File.WriteAllText(savePath, json);

        Debug.Log("[AccumulatedStats] Saved.");
        Debug.Log($"[AccumulatedStats] Save 호출됨: total={stats.total}");
    }
    
    // 🔹 누적 카운트 증가 메서드들
    public void AddOK()
    {
        stats.total++;
        stats.ok++;
    }

    public void AddNG()
    {
        stats.total++;
        stats.ng++;
    }

    public void AddFault()
    {
        stats.faultCount++;
    }

    private void OnApplicationQuit()
    {
        if (stats == null) return;

        SaveStats();
        Debug.Log("[AccumulatedStats] Application Quit → Stats Saved");
    }
}

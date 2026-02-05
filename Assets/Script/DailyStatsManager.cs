using System;
using System.IO;
using UnityEngine;

public class DailyStatsManager : MonoBehaviour
{
    public static DailyStatsManager Instance;

    private string today;
    private string savePath;
    public DailyStats stats;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Init();
        LoadTodayStats();
    }

    private void Init()
    {
        today = DateTime.Now.ToString("yyyy-MM-dd");

        savePath = Path.Combine(
            Application.persistentDataPath,
            "Stats",
            "Daily",
            $"Daily_{today}.json"
        );

        Directory.CreateDirectory(Path.GetDirectoryName(savePath));
    }

    private void LoadTodayStats()
    {
        if (!File.Exists(savePath))
        {
            stats = new DailyStats
            {
                date = today
            };

            Debug.Log($"[DailyStats] New day started: {today}");
            return;
        }

        string json = File.ReadAllText(savePath);
        stats = JsonUtility.FromJson<DailyStats>(json);

        Debug.Log($"[DailyStats] Loaded: {today}");
    }

    // ===== 누적 메서드 =====

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

    // ===== 저장 =====

    public void Save()
    {
        string json = JsonUtility.ToJson(stats, true);
        File.WriteAllText(savePath, json);

        Debug.Log($"[DailyStats] Saved: {savePath}");
    }
}

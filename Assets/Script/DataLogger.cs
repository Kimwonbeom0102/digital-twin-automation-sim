using System.IO;
using UnityEngine;
using System;

public class DataLogger : MonoBehaviour
{
    public static DataLogger Instance;

    private string logFolderPath;

    private SessionData currentSession;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitLogPath();
    }

    private void InitLogPath()
    {
        logFolderPath = Path.Combine(
            Application.persistentDataPath,
            "Logs"
        );

        if (!Directory.Exists(logFolderPath))
            Directory.CreateDirectory(logFolderPath);

        Debug.Log($"[DataLogger] Log Path: {logFolderPath}");
    }

    public void StartSession()
    {
        string timeStamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

        currentSession = new SessionData();

        currentSession.sessionInfo = new SessionInfo
        {
            sessionId = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"),
            startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            // endTime = null; 
        };

        LogEvent("RunStarted", "Plant", "Plant Run");
    }

    public void LogEvent(string type, string zone, string message)
    {
        currentSession.events.Add(new EventLog
        {
            time = DateTime.Now.ToString("HH:mm:ss.fff"),
            type = type,
            zone = zone,
            message = message
        });
    }

    public void LogInspectionResult(string zoneId, bool isNG)
    {
        currentSession.events.Add(new EventLog
        {
            time = DateTime.Now.ToString("HH:mm:ss.fff"),
            type = "InspectionResult",
            zone = zoneId,
            result = isNG ? "NG" : "OK"
        });
    }

    public void EndSession(SummaryData summary)
    {
        if (currentSession == null)
        {
            Debug.LogWarning("[DataLogger] Session is null. EndSession skipped.");
            return;
        }
        currentSession.sessionInfo.endTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        currentSession.summary = summary;

        SaveToJson();
    }

    private void SaveToJson()
    {
        string json = JsonUtility.ToJson(currentSession, true);
        string path = Path.Combine(
            logFolderPath,
            $"Session_{currentSession.sessionInfo.sessionId}.json"
        );

        File.WriteAllText(path, json);
        Debug.Log($"[DataLogger] Saved: {path}");
    }
}

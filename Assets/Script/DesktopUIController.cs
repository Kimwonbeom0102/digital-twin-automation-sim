using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class DesktopUIController : MonoBehaviour
{
    [Header("매니저 연결")]
    [SerializeField] private PlantManager plantManager;  // ★ 플랜트 매니저 참조 (인스펙터 연결)
    [SerializeField] private ZoneManager[] zones;
    [SerializeField] private StorageRack storageRack;
    [SerializeField] private BufferZone buffer;
    [SerializeField] private RobotArmController robot;
    [SerializeField] private NgRack ngRack;

    [Header("UI 버튼")]
    [SerializeField] private Button runButton;  // 
    [SerializeField] private Button stopButton;
    // [SerializeField] private Button pauseButton;
    // [SerializeField] private Button eStopOnButton;
    // [SerializeField] private Button eStopOffButton;
    [SerializeField] private Button spawnOnceButton;  // 스폰 
    // [SerializeField] private Button clearFaultButton;
    [SerializeField] private Button zoneRunButton;  // 존 준비
    [SerializeField] private Button zoneStopButton1;  // 존1 스톱
    [SerializeField] private Button zoneStopButton2;
    [SerializeField] private Button zoneStopButton3;
    [SerializeField] private Button zoneFaultButton;  // 존 폴트 발생
    [SerializeField] private Button zoneClearButton;  // 존 폴트 초기화
    [SerializeField] private Button okResetButton;
    [SerializeField] private Button ngResetButton;
    [SerializeField] private Sink zone3Sink;      // 존3 Sink drag
    [SerializeField] private TMP_Text totalText;  // 전체 카운트 UI
    [SerializeField] private TMP_Text ngText;     // NG 카운트 UI (있으면)
    [SerializeField] private TMP_Text okText;     // Ok 카운트 UI (있으면)
    [SerializeField] private TMP_Text robotFlowText;
    [SerializeField] private TMP_Text bufferQueueText;
    [SerializeField] private TMP_Text storageTotalText;
    [SerializeField] private TMP_Text storageLastInText;
    [SerializeField] private TMP_Text ngTotalText;
    [SerializeField] private TMP_Text ngLastInText;
    // [SerializeField] private Button resumeButton;

    void Start()
    {
        if (plantManager == null)
        Debug.LogError("[UIManager] PlantManager 미연결!");

        if (zones == null)
        Debug.LogError("[UIManager] ZoneManager 미연결!");

        // --- 각 버튼에 PlantManager의 메서드 연결 ---
        runButton.onClick.AddListener(OnRunClicked);
        stopButton.onClick.AddListener(OnStopClicked);
        zoneRunButton.onClick.AddListener(OnZoneRunClicked);
        spawnOnceButton.onClick.AddListener(OnSpawnOnceClicked);
        zoneStopButton1.onClick.AddListener(StopZone1);
        zoneStopButton2.onClick.AddListener(StopZone2);
        zoneStopButton3.onClick.AddListener(StopZone3);
        zoneFaultButton.onClick.AddListener(ZoneFaultClicked);
        zoneClearButton.onClick.AddListener(ZoneClearClicked);
        okResetButton.onClick.AddListener(OkResetClicked);
        ngResetButton.onClick.AddListener(NgResetClicked);

        // eStopOnButton.onClick.AddListener(OnEStopOnClicked);
        // eStopOffButton.onClick.AddListener(OnEStopOffClicked);
        // clearFaultButton.onClick.AddListener(OnClearFaultClicked);
        // pauseButton.onClick.AddListener(OnPauseClicked);
        // resumeButton.onClick.AddListener(OnResumeClicked);
    }

    private void OnEnable()
    {
        if (zone3Sink != null)
            zone3Sink.OnCountChanged += HandleCountChanged;

        if (robot != null)
            robot.OnRobotStateChanged += HandleRobotStateChanged;

        if(buffer != null)
        {
            buffer.OnQueueChanged += UpdateQueueText;
        }

        if (storageRack != null)
            storageRack.OnStorageUpdated+= UpdateStorageUI;
        
        if (ngRack != null)
            ngRack.OnNgUpdated += UpdateNgUI;
    }

    private void OnDisable()
    {
        if (zone3Sink != null)
            zone3Sink.OnCountChanged -= HandleCountChanged;

        if(buffer != null)
        {
            buffer.OnQueueChanged -= UpdateQueueText;
        }

        if (robot != null)
            robot.OnRobotStateChanged -= HandleRobotStateChanged;

        if (storageRack != null)
            storageRack.OnStorageUpdated -= UpdateStorageUI;
        if (ngRack != null)
            ngRack.OnNgUpdated -= UpdateNgUI;
    }

    private void UpdateStorageUI(int total, string time)
    {
        storageTotalText.text = $"Total : {total}";
        storageLastInText.text = $"Last In : {time}";

        Color c = Color.green;

        if (storageRack != null && total >= storageRack.maxCapacity)
            c = Color.red;

        storageTotalText.color = c;

    }
    private void UpdateNgUI(int ngtotal, string time)
    {
        ngTotalText.text = $"Total : {ngtotal}";
        ngLastInText.text = $"Last In : {time}";
    }

    private void UpdateQueueText(int count)
    {
        bufferQueueText.text = $"Queue : {count}";
    }

    private void HandleCountChanged(int total, int ng, int ok)
    {
        if (totalText != null)
            totalText.text = $"Total : {total}";

        if (ngText != null)
            ngText.text = $"NG : {ng}";
        
        if (okText != null)
            okText.text = $"OK : {ok}";
    }

    private void HandleRobotStateChanged(RobotState state)
    {
        Color c = Color.green;
        string text = "";

        switch (state)
        {
            case RobotState.Running:
                text = "RUNNING";
                c = Color.green;
                break;

            case RobotState.Idle:
                text = "WAIT";
                c = Color.yellow;
                break;
        }

        if (robotFlowText != null)
        {
            robotFlowText.text = $"Robot : {text}";
        }
        if (robotFlowText != null)
        {
            robotFlowText.color = c;
        }
    }

    private void ZoneClearClicked()
    {  
        if (zones == null) return;

        plantManager.ResetFault();
        // var z = plantManager.GetLastFaultZone();
        // if( z != null)  // 문제 발생한 존을 가져와서
        // {
        //     z.ClearFault();  // 클리어해줌 
        // }
    }

    private void OkResetClicked()
    {
        if (storageRack == null)
        {
            Debug.LogWarning("[UIManager] StorageRack 미연결");
            return;
        }

        storageRack.ClearOk();
    }

    private void NgResetClicked()
    {
        if (ngRack == null)
        {
            Debug.LogWarning("[UIManager] NgRack 미연결");
            return;
        }
        
        ngRack.ClearNg();  
    }

    private void ZoneFaultClicked() 
    {
        int index = UnityEngine.Random.Range(0, zones.Length);

        ZoneManager targetZone = zones[index];

        Debug.Log($"강제 Fault 발생 -> Zone {targetZone.zoneId}");
        
        targetZone.RaiseFault();
    }

    private void OnRunClicked()  // 전원 on 스위치 Plant
    {
        plantManager.CmdRunAll();
        Debug.Log("[Check] Plant State = " + plantManager.State);
    }

    private void OnStopClicked() // 전원 off 스위치 Plnat
    {
        plantManager.CmdStopAll();
    }

    private void StopZone1()
    {
        zones[0].ToggleUserStop();
    }

    private void StopZone2()
    {
        zones[1].ToggleUserStop();
    }

    private void StopZone3()
    {
        zones[2].ToggleUserStop();
    }

    // private void OnEStopOnClicked()
    // {
    //     plantManager.CmdEStopOn();
    // }

    // private void OnEStopOffClicked()
    // {
    //     plantManager.CmdEStopOff();
    // }

    private void OnZoneRunClicked()  
    {
        foreach(var z in zones)
        {
            z.ZoneRun();
        }
    }

    private void OnSpawnOnceClicked() // 아이템 스폰 버튼 (작동스위치가 On일때만)
    {
        if (plantManager == null)
        {
            Debug.Log("PlantManager가 할당되어 있지 않습니다.");
            return;
        }
        if( plantManager.State != PlantState.Running)
        {
            Debug.Log("Run버튼을 눌러 전원을 켜주세요.");
            return;
        }
        if(zones == null)
        {
            Debug.Log("zoneManager가 할당되어 있지 않습니다.");
            return;
        }

        zones[0].ItemSpawn();
        zones[1].ItemSpawn();
        zones[2].ItemSpawn();
    }

    // private void OnPauseClicked()
    // {
    //     plantManager.CmdPause();
    // }

    // private void OnResumeClicked()
    // {
    //     plantManager.CmdResume();
    // }

    

    // private void OnClearFaultClicked() // 문제 초기화
    // {
    //     plantManager.ClearFault(); 
    // }
}
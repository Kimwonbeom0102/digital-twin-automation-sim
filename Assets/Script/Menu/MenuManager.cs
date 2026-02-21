using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Dropdown scenarioDropdown;
    public TMP_Dropdown speedDropdown;
    public TMP_Text faultText;

    private bool autoFault = true;

    void Start()
    {
        InitializeDefaults();
    }

    void InitializeDefaults()
    {
        // 기본값
        SimulationConfig.CurrentScenario = ScenarioType.Normal;
        SimulationConfig.SimulationSpeed = 1f;
        SimulationConfig.AutoFault = true;

        autoFault = true;

        if (scenarioDropdown != null)
            scenarioDropdown.value = 0;

        if (speedDropdown != null)
            speedDropdown.value = 0;

        UpdateFaultText();
    }

    // =========================
    // 버튼
    // =========================

    public void StartSimulation()
    {
        SceneManager.LoadScene("AutoSim");
    }

    public void ExitApplication()
    {
        Debug.Log("프로그램 종료");
        Application.Quit();
    }

    public void ToggleFault()
    {
        autoFault = !autoFault;
        SimulationConfig.AutoFault = autoFault;

        UpdateFaultText();
    }

    private void UpdateFaultText()
    {
        if (faultText != null)
            faultText.text = autoFault 
                ? "Fault Mode : ON" 
                : "Fault Mode : OFF";
    }

    // =========================
    // Dropdown 이벤트
    // =========================

    public void OnScenarioChanged(int index)
    {
        SimulationConfig.CurrentScenario = (ScenarioType)index;
    }

    public void OnSpeedChanged(int index)
    {
        switch (index)
        {
            case 0:
                SimulationConfig.SimulationSpeed = 1f;
                break;
            case 1:
                SimulationConfig.SimulationSpeed = 2f;
                break;
            case 2:
                SimulationConfig.SimulationSpeed = 3f;
                break;
        }
    }
}

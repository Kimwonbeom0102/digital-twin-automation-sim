using UnityEngine;

public class AutoSimInitializer : MonoBehaviour
{
    [Header("Zone References")]
    public ZoneManager[] zones;

    [Header("Scenario Settings")]
    public float normalSpawnInterval = 4f;
    public float hardSpawnInterval = 2f;
    public float faultTestSpawnInterval = 2f;

    public float normalFaultProbability = 0f;
    public float hardFaultProbability = 0f;
    public float faultTestProbability = 0.5f;

    private float spawnInterval;
    private float faultProbability;

    void Start()
    {
        ApplyScenario();
    }

    private void ApplyScenario()
    {
        // == 시나리오 == 
        switch (SimulationConfig.CurrentScenario)
        {
            case ScenarioType.Normal: // Normal
                spawnInterval = normalSpawnInterval;
                faultProbability = normalFaultProbability;
                break;

            case ScenarioType.HighLoad: // Hard
                spawnInterval = hardSpawnInterval;
                faultProbability = hardFaultProbability;
                break;
        }

        // == Zone에 적용 ==
        foreach (var zone in zones)
        {
            zone.SetSpawnInterval(spawnInterval);
            zone.SetFaultProbability(faultProbability);
        }

        // == 배속 적용 ==
        Time.timeScale = SimulationConfig.SimulationSpeed;

        Debug.Log($"Scenario: {SimulationConfig.CurrentScenario}");
        Debug.Log($"Speed: {SimulationConfig.SimulationSpeed}");
        Debug.Log($"AutoFault: {SimulationConfig.AutoFault}");
        Debug.Log($"SpawnInterval: {spawnInterval}");
        Debug.Log($"FaultProbability: {faultProbability}");
    }
}

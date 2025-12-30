using UnityEngine;

public class StatusBoard : MonoBehaviour
{
    public enum BoardLevel { Plant, Zone }         // 무엇을 표시할지
    public enum BlinkMode  { None, Slow, Fast }    // 깜빡임 종류
    public enum Lamp       { None, Red, Yellow, Blue } // 램프 색상

    [Header("Target")]
    public BoardLevel level = BoardLevel.Plant;
    public PlantManager plant;     // plant.State 사용
    public ZoneManager zone;      // zone.State  사용

    [Header("Lamp Renderers")]
    public Renderer lampRed;
    public Renderer lampYellow;
    public Renderer lampBlue;

    [Header("Material/Emission")]
    public bool   useEmission      = true;
    public string emissionProperty = "_EmissionColor"; // 파이프라인에 맞게 필요시 수정
    public Color  colRed    = new(1f, 0.2f, 0.1f);
    public Color  colYellow = new(1f, 0.8f, 0.1f);
    public Color  colBlue   = new(0.2f, 0.6f, 1f);
    public float  intensity = 10f;

    [Header("Blink")]
    public float blinkSlowHz = 1.5f;
    public float blinkFastHz = 5.0f;
    [Range(0f, 1f)] public float blinkDuty = 0.5f;

    [Header("Stability")]
    public float gracePeriod = 1.5f;   // 시작 직후 가짜 경보 방지

    // 내부 상태
    Lamp _currLamp = Lamp.Blue;
    BlinkMode _currBlink = BlinkMode.None;
    bool _blinkOn = true;
    float _startTime;

    void Start()
    {
        _startTime = Time.time;
        ApplyPattern(DecidePattern());
    }

    void Update()
    {
        var next = DecidePattern();

        // 패턴(램프/깜빡임)이 바뀌면 재적용
        if (next.lamp != _currLamp || next.blink != _currBlink)
            ApplyPattern(next);

        // 깜빡임 처리
        if (_currBlink != BlinkMode.None)
            TickBlink();
    }

    // 상태를 읽어 "램프/깜빡임" 패턴 결정
    (Lamp lamp, BlinkMode blink) DecidePattern()
    {
        // 시작 그레이스: 잠깐 정지 취급
        if (Time.time - _startTime < gracePeriod)
            return (Lamp.Yellow, BlinkMode.None);

        // null-safe 기본값
        var p = plant ? plant.State : PlantState.Stopped; // enum이 Stop이면 여기만 Stop으로
        var z = zone  ? zone.State  : ZoneState.Stopped;

        // 1) 플랜트 우선(게이트): 파랑(Running)일 때만 존 세부 표시
        // 1) 플랜트 보드일 경우: 상태 그대로 표시 (Zone 로직으로 가지 않음)
        if (level == BoardLevel.Plant)
        {
            switch (p)
            {
                case PlantState.EStop:   return (Lamp.Red,    BlinkMode.Fast);
                case PlantState.Fault:   return (Lamp.Red,    BlinkMode.None);
                case PlantState.Stopped: return (Lamp.Yellow, BlinkMode.None);
                case PlantState.Running: return (Lamp.Blue,   BlinkMode.None); // ★ 파란불!
            }
        }

        // 2) 플랜트가 Running일 때의 존 상태 매핑
        switch (z)
        {
            case ZoneState.Fault:   return (Lamp.Red,    BlinkMode.None);
            case ZoneState.Paused:  return (Lamp.Yellow, BlinkMode.Slow);
            case ZoneState.Stopped: return (Lamp.Yellow, BlinkMode.None);
            case ZoneState.Running: return (Lamp.Blue,   BlinkMode.None);
            default:                return (Lamp.Yellow, BlinkMode.None);
        }
    }

    void ApplyPattern((Lamp lamp, BlinkMode blink) p)
    {
        _currLamp  = p.lamp;
        _currBlink = p.blink;
        _blinkOn   = true;      // 깜빡임 시작은 켜진 상태부터

        SetAllOff();
        if (_currLamp != Lamp.None)
            SetLamp(_currLamp, true);
    }

    void TickBlink()
    {
        float hz = (_currBlink == BlinkMode.Fast) ? blinkFastHz : blinkSlowHz;
        if (hz <= 0f) return;

        float period = 1f / hz;
        float t      = (Time.time % period) / period;
        bool on      = t < blinkDuty;

        if (on != _blinkOn)
        {
            _blinkOn = on;
            SetLamp(_currLamp, _blinkOn);
        }
    }

    void SetAllOff()
    {
        SetRenderer(lampRed,    Color.black, false);
        SetRenderer(lampYellow, Color.black, false);
        SetRenderer(lampBlue,   Color.black, false);
    }

    void SetLamp(Lamp lamp, bool on)
    {
        switch (lamp)
        {
            case Lamp.Red:    SetRenderer(lampRed,    colRed,    on); break;
            case Lamp.Yellow: SetRenderer(lampYellow, colYellow, on); break;
            case Lamp.Blue:   SetRenderer(lampBlue,   colBlue,   on); break;
        }
    }

    void SetRenderer(Renderer r, Color c, bool on)
    {
        if (!r) return;

        var mat = r.material; // 인스턴스화된 머티리얼 사용(공유 머티리얼 X)
        if (useEmission)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor(emissionProperty, on ? c * intensity : Color.black);
        }
        else
        {
            // 빌트인/URP의 Lit 계열은 보통 _BaseColor 사용(프로젝트에 맞춰 조정)
            mat.SetColor("_BaseColor", on ? c : Color.black);
        }
    }
}

// DTO(Datat Transfer Object)
// Unity<->Sever 사이에서 데이터를 주고받기 위한 껍데기 

[System.Serializable]
public class FaultRequest
{
    public int zoneId;
    public string faultType;
    public int faultCountInLast60Sec;
}
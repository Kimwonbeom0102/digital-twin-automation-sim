// DTO(Datat Transfer Object)
// Unity<->Sever 사이에서 데이터를 주고받기 위한 껍데기 

[System.Serializable]
public class ZoneResponse
{
    public int id;
    public string zoneName;
    public int status;
}

[System.Serializable]
public class ZoneResponseArray
{
    public ZoneResponse[] zones;
}
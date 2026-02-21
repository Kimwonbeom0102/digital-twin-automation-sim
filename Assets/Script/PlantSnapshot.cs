using UnityEngine;
using System.Collections.Generic;

public class PlantSnapshot
{
    public string time;
    public PlantState plantState;

    public List<ZoneSanpshot> zones;

    public int total;
    public int ok;
    public int ng;
}

public class ZoneSanpshot
{
    public int zoneId;
    public ZoneState state;
    public int queueCount;
}

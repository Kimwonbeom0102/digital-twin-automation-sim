using System;

[Serializable]
public class DailyStats
{
    public string date;   // yyyy-MM-dd
    public int total;
    public int ok;
    public int ng;
    public int faultCount;
}

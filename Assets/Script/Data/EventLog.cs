using System;

[Serializable]
public class EventLog
{
    public string time;
    public string type;
    public string zone;
    public string message;
    public string result; // "ok, ng"
}

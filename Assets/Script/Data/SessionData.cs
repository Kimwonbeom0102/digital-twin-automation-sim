using System;
using System.Collections.Generic;

[Serializable]
public class SessionData
{
    public SessionInfo sessionInfo;
    public List<EventLog> events = new List<EventLog>();
    public SummaryData summary;
}

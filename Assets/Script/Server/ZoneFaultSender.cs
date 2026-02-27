using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Linq;

/// 통신 담당 스크립트
// Unity에서 서버 API 호출 담당
// HTTP POST 보냄
// 응답 JSON 받음
// 파싱해서 Unity 로직에 전달

public class ZoneFaultSender : MonoBehaviour
{
    [SerializeField] private PlantManager plantManager;

    private string scenarioUrl = "http://localhost:5079/api/Zone/check-scenario";
    private string faultUrl = "http://localhost:5079/api/Zone/fault";

    void Start()
    {

    }

    public void SendElapsedTime(float elapsedTime)
    {
        ElapsedTimeRequest request = new ElapsedTimeRequest
        {
            elapsedTime = elapsedTime
        };

        string json = JsonUtility.ToJson(request);
        StartCoroutine(PostScenarioCheck(json));
    }

    IEnumerator PostScenarioCheck(string json)
    {
        UnityWebRequest www = new UnityWebRequest(scenarioUrl, "POST");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(www.error);
        }
        else
        {
            ZoneResponse[] zones =
                JsonUtility.FromJson<ZoneResponseArray>(www.downloadHandler.text).zones;

            plantManager.ApplyZoneStates(zones.ToList());
        }

        Debug.Log("서버 응답: " + www.downloadHandler.text);
    }

    private ZoneManager FindZoneById(int id)
    {
        ZoneManager[] zones = FindObjectsOfType<ZoneManager>();

        foreach (var z in zones)
        {
            if (z.zoneId == id)
                return z;
        }

        return null;
    }

    IEnumerator PostRequest(string json)
    {
        UnityWebRequest www = new UnityWebRequest(faultUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error: " + www.error);
        }
        else
        {
            Debug.Log("Server Response: " + www.downloadHandler.text);

            ZoneResponse response =
                JsonUtility.FromJson<ZoneResponse>(www.downloadHandler.text);


            Debug.Log("Zone ID :" + response.id);         
            Debug.Log("Zone Name :" + response.zoneName);   
            Debug.Log("Zone Status :" + response.status);   

            ZoneManager targetZone = FindZoneById(response.id);

            if (targetZone != null)
            {
                if (response.status == 2)   // 2 = Fault
                {
                    Debug.Log("서버 승인 → Fault 적용");
                    targetZone.RaiseFault();
                }
            }
            else
            {
                Debug.LogWarning("해당 Zone을 찾지 못함");
            }
        }
    }

}
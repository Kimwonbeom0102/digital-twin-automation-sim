using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Linq;
using System.Collections.Generic;

/// 통신 담당 스크립트
// Unity에서 서버 API 호출 담당
// HTTP POST 보냄
// 응답 JSON 받음
// 파싱해서 Unity 로직에 전달

public class ZoneCommandSender : MonoBehaviour
{
    [SerializeField] private PlantManager plantManager;
    
    // private string scenarioUrl = "http://localhost:5079/api/Zone/check-scenario";
    // private string faultUrl = "http://localhost:5079/api/Zone/fault";
    private string baseUrl = "http://localhost:5079/api/Zone";


    public void RequestReset()
    {
        StartCoroutine(PostReset());
    }

    private IEnumerator PostReset()
    {
        UnityWebRequest www =
            new UnityWebRequest(baseUrl + "/reset", "POST");

        www.uploadHandler = new UploadHandlerRaw(new byte[0]);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("서버 Reset 완료");

            // 🔥 여기서 상태 다시 불러오기
            StartCoroutine(GetZones());
        }
        else
        {
            Debug.LogError("Reset 실패: " + www.error);
        }
    }

    public void RequestDirectFault(int zoneId)
    {
        StartCoroutine(PostDirectFault(zoneId));
    }

    private IEnumerator PostDirectFault(int zoneId)
    {
        var request = new ZoneRunRequest { ZoneId = zoneId };
        string json = JsonUtility.ToJson(request);

        UnityWebRequest www =
            new UnityWebRequest(baseUrl + "/force-fault", "POST");

        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            ZoneResponse response =
                JsonUtility.FromJson<ZoneResponse>(www.downloadHandler.text);

            plantManager.ApplyZoneStates(
                new List<ZoneResponse> { response }
            );
        }
        else
        {
            Debug.LogError("Direct Fault 실패: " + www.error);
        }
    }

    private IEnumerator GetZones()
    {
        UnityWebRequest www = UnityWebRequest.Get(baseUrl);

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Zone 상태 동기화 완료");

            ZoneResponseArray response =
                JsonUtility.FromJson<ZoneResponseArray>(www.downloadHandler.text);

            plantManager.ApplyZoneStates(response.zones.ToList());
        }
        else
        {
            Debug.LogError("Zone 상태 불러오기 실패: " + www.error);
        }
    }

    public void RequestZoneRun(int zoneId)
    {
        StartCoroutine(PostZoneRun(zoneId));
    }

    private IEnumerator PostZoneRun(int zoneId)
    {
        var request = new ZoneRunRequest{ ZoneId = zoneId };
        string json = JsonUtility.ToJson(request);

        using (UnityWebRequest www = new UnityWebRequest(baseUrl + "/run", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                var response =
                    JsonUtility.FromJson<ZoneResponseArray>(www.downloadHandler.text);

                plantManager.ApplyZoneStates(response.zones.ToList());
            }
            else
            {
                Debug.LogError("ZoneRun 서버 요청 실패");
                Debug.LogError($"ZoneRun 실패: {www.responseCode} / {www.error}");
                Debug.LogError("Response Body: " + www.downloadHandler.text);
            }
        }
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
        UnityWebRequest www = new UnityWebRequest(baseUrl + "/check-scenario", "POST");

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
        UnityWebRequest www = new UnityWebRequest(baseUrl + "/fault", "POST");
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
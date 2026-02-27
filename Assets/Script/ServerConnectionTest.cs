using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class ServerConnectionTest : MonoBehaviour
{
    IEnumerator Start()
    {
        UnityWebRequest request =
            UnityWebRequest.Get("http://localhost:5079/api/test");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("서버 응답: " + request.downloadHandler.text);
        }
        else
        {
            Debug.Log("에러: " + request.error);
        }
    }
}
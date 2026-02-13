using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;
using System.Text;

namespace KnowledgeStack.Networking
{
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        // Replace with your Ubuntu Server IP or Domain
        private const string BASE_URL = "http://YOUR_UBUNTU_SERVER_IP/api"; 

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        [Serializable]
        public class UserData
        {
            public string userId;
            public int level;
            public int totalScore;
            // Add list of served question IDs here
        }

        public void GetUserData(string userId, Action<UserData> onSuccess, Action<string> onError)
        {
            StartCoroutine(GetUserDataRoutine(userId, onSuccess, onError));
        }

        private IEnumerator GetUserDataRoutine(string userId, Action<UserData> onSuccess, Action<string> onError)
        {
            string url = $"{BASE_URL}/user/{userId}";
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                // Add Auth Header here if needed (e.g. Bearer Token from Play Store)
                // request.SetRequestHeader("Authorization", "Bearer " + token);

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        UserData data = JsonUtility.FromJson<UserData>(request.downloadHandler.text);
                        onSuccess?.Invoke(data);
                    }
                    catch (Exception e)
                    {
                        onError?.Invoke("Parse Error: " + e.Message);
                    }
                }
                else
                {
                    onError?.Invoke(request.error);
                }
            }
        }

        // Example method to sync data to server
        public void SyncUserData(UserData data, Action onSuccess, Action<string> onError)
        {
            StartCoroutine(SyncUserDataRoutine(data, onSuccess, onError));
        }

        private IEnumerator SyncUserDataRoutine(UserData data, Action onSuccess, Action<string> onError)
        {
            string url = $"{BASE_URL}/user/sync";
            string json = JsonUtility.ToJson(data);
            
            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    onSuccess?.Invoke();
                }
                else
                {
                    onError?.Invoke(request.error);
                }
            }
        }
    }
}

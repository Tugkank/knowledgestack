using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;
using System.Text;
using System.Collections.Generic;

namespace KnowledgeStack.Networking
{
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        [Header("Server Configuration")]
        public string serverIpAddress = "YOUR_SERVER_IP"; // User will set this in Inspector
        private string BaseUrl => $"http://{serverIpAddress}:3000/api";

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

        #region Data Models
        [Serializable]
        public class UserData
        {
            public string userId;
            public int level;
            public int totalScore;
            public List<int> servedQuestions;
        }

        [Serializable]
        public class QuestionWrapper
        {
            public List<QuestionData> questions;
        }
        #endregion

        #region API Calls

        // 1. Login / Register (Syncs user data on start)
        public void LoginOrRegister(string userId, Action<UserData> onSuccess, Action<string> onError)
        {
            StartCoroutine(PostRequest<UserData>("/auth/login", new { userId = userId }, onSuccess, onError));
        }

        // 2. Get All Questions
        public void GetQuestions(Action<List<QuestionData>> onSuccess, Action<string> onError)
        {
            StartCoroutine(GetRequest<QuestionWrapper>("/game/questions", (wrapper) => 
            {
                onSuccess?.Invoke(wrapper.questions);
            }, onError));
        }

        // 3. Sync Progress
        public void SyncProgress(string userId, int level, int addedScore, int solvedQuestionId, Action onSuccess, Action<string> onError)
        {
            var payload = new 
            { 
                userId = userId, 
                level = level, 
                score = addedScore, // This might need to be 'totalScore' depending on server logic, assuming server adds it or we send delta
                solvedQuestionId = solvedQuestionId 
            };
            StartCoroutine(PostRequest<object>("/game/sync", payload, (obj) => onSuccess?.Invoke(), onError));
        }

        #endregion

        #region Helper Coroutines

        private IEnumerator GetRequest<T>(string endpoint, Action<T> onSuccess, Action<string> onError)
        {
            string url = BaseUrl + endpoint;
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        T data = JsonUtility.FromJson<T>(request.downloadHandler.text);
                        onSuccess?.Invoke(data);
                    }
                    catch (Exception e)
                    {
                        onError?.Invoke($"Parse Error on {endpoint}: {e.Message}");
                    }
                }
                else
                {
                    onError?.Invoke($"Network Error on {endpoint}: {request.error}");
                }
            }
        }

        private IEnumerator PostRequest<T>(string endpoint, object payload, Action<T> onSuccess, Action<string> onError)
        {
            string url = BaseUrl + endpoint;
            string json = JsonUtility.ToJson(payload);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        // If T is object, we validly ignore response body for simple success
                        if (request.downloadHandler.text.Length > 0)
                        {
                            T data = JsonUtility.FromJson<T>(request.downloadHandler.text);
                            onSuccess?.Invoke(data);
                        }
                        else
                        {
                            onSuccess?.Invoke(default);
                        }
                    }
                    catch (Exception e)
                    {
                        onError?.Invoke($"Parse Error on {endpoint}: {e.Message}");
                    }
                }
                else
                {
                    onError?.Invoke($"Network Error on {endpoint}: {request.error}");
                }
            }
        }

        #endregion
    }
}

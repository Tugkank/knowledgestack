using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class QuestionManager : MonoBehaviour
{
    public static QuestionManager Instance { get; private set; }
    
    // Categorized questions by difficulty (1, 2, 3, 4)
    private Dictionary<int, List<QuestionData>> questionsByDifficulty;
    // Track used question IDs to prevent repetition
    private HashSet<int> servedQuestionIds;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadServedQuestions();
            // Don't load local immediately, try server first in Start
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Event to notify when questions are ready
    public event System.Action OnQuestionsLoaded;
    public bool IsDataLoaded { get; private set; } = false;

    private void Start()
    {
        InitializeQuestions();
    }

    private void InitializeQuestions()
    {
        questionsByDifficulty = new Dictionary<int, List<QuestionData>>();
        for(int i=1; i<=4; i++) questionsByDifficulty[i] = new List<QuestionData>();

        // Try Network Load
        if (KnowledgeStack.Networking.NetworkManager.Instance != null)
        {
            Debug.Log("Attempting to load questions from Server...");
            KnowledgeStack.Networking.NetworkManager.Instance.GetQuestions(
                (serverQuestions) => 
                {
                    Debug.Log($"Successfully loaded {serverQuestions.Count} questions from Server!");
                    ProcessLoadedQuestions(serverQuestions);
                },
                (error) => 
                {
                    Debug.LogError($"Server Load Failed: {error}. Falling back to Local Resource.");
                    LoadQuestionsFromLocal();
                }
            );
        }
        else
        {
            Debug.LogWarning("NetworkManager not found. Loading Local.");
            LoadQuestionsFromLocal();
        }
    }

    private void ProcessLoadedQuestions(List<QuestionData> questions)
    {
        foreach(var q in questions)
        {
            if(questionsByDifficulty.ContainsKey(q.difficulty))
            {
                questionsByDifficulty[q.difficulty].Add(q);
            }
        }
        Debug.Log($"Processed Questions - D1:{questionsByDifficulty[1].Count}, D2:{questionsByDifficulty[2].Count}, D3:{questionsByDifficulty[3].Count}, D4:{questionsByDifficulty[4].Count}");
        
        IsDataLoaded = true;
        OnQuestionsLoaded?.Invoke();
    }

    private void LoadQuestionsFromLocal()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("questions");
        if (jsonFile != null)
        {
            QuestionList allData = JsonUtility.FromJson<QuestionList>(jsonFile.text);
            ProcessLoadedQuestions(new List<QuestionData>(allData.questions));
        }
        else
        {
            Debug.LogError("questions.json file not found in Resources!");
        }
    }
    
    // --- Distribution Logic ---

    public List<QuestionData> GetQuestionsForLevel(int level)
    {
        List<QuestionData> levelQuestions = new List<QuestionData>();
        Dictionary<int, int> distribution = GetDistributionForLevel(level);

        foreach (var kvp in distribution)
        {
            int difficulty = kvp.Key;
            int countNeeded = kvp.Value;
            
            AddQuestionsByDifficulty(levelQuestions, difficulty, countNeeded);
        }
        
        // Final fallback: If we still don't have enough questions (e.g. 10), fill with ANY available unique question
        int targetTotal = 10;
        if (levelQuestions.Count < targetTotal)
        {
            Debug.LogWarning("Not enough questions matching distribution. Filling with random available...");
            AddQuestionsAny(levelQuestions, targetTotal - levelQuestions.Count);
        }

        // If STILL not enough, it means we ran out of unique questions globally. Reset usage.
        if (levelQuestions.Count < targetTotal)
        {
            Debug.LogWarning("Ran out of ALL unique questions! Resetting history.");
            ResetServedQuestions();
            // Try filling again recursively or just grab from reset pool
            AddQuestionsAny(levelQuestions, targetTotal - levelQuestions.Count);
        }

        return levelQuestions;
    }

    private Dictionary<int, int> GetDistributionForLevel(int level)
    {
        if (level <= 20)      return new Dictionary<int, int> { {1,5}, {2,2}, {3,2}, {4,1} };
        else if (level <= 40) return new Dictionary<int, int> { {1,3}, {2,3}, {3,2}, {4,2} };
        else if (level <= 80) return new Dictionary<int, int> { {1,1}, {2,2}, {3,3}, {4,4} };
        else                  return new Dictionary<int, int> { {1,0}, {2,1}, {3,4}, {4,5} };
    }

    private void AddQuestionsByDifficulty(List<QuestionData> targetList, int difficulty, int count)
    {
        if (count <= 0) return;
        
        // Recursive fallback: If diff 5 requested (invalid), try wrapping or stop
        if (difficulty > 4) difficulty = 4; // Cap at 4 or implement other logic

        List<QuestionData> candidates = questionsByDifficulty[difficulty]
            .Where(q => !servedQuestionIds.Contains(q.id))
            .OrderBy(x => Random.value) // Shuffle
            .ToList();

        if (candidates.Count >= count)
        {
            for (int i = 0; i < count; i++)
            {
                targetList.Add(candidates[i]);
                MarkAsServed(candidates[i].id);
            }
        }
        else
        {
            // Take all available from this tier
            foreach (var q in candidates)
            {
                targetList.Add(q);
                MarkAsServed(q.id);
            }
            
            // Fallback: Request remaining count from next higher difficulty
            int remaining = count - candidates.Count;
            // Ensure we don't loop forever if max difficulty
            if (difficulty < 4)
            {
                AddQuestionsByDifficulty(targetList, difficulty + 1, remaining);
            }
            else
            {
                // If difficulty is 4 and ran out, try fetching from Lower difficulty as last resort? 
                // Or handled by generic filler.
            }
        }
    }

    // Fills list with ANY served question regardless of difficulty
    private void AddQuestionsAny(List<QuestionData> targetList, int count)
    {
        // Gather ALL available questions from all difficulties
        List<QuestionData> allCandidates = new List<QuestionData>();
        foreach (var list in questionsByDifficulty.Values)
        {
            allCandidates.AddRange(list.Where(q => !servedQuestionIds.Contains(q.id)));
        }

        var shuffled = allCandidates.OrderBy(x => Random.value).Take(count).ToList();
        foreach (var q in shuffled)
        {
            targetList.Add(q);
            MarkAsServed(q.id);
        }
    }

    // --- History Management ---

    private void MarkAsServed(int id)
    {
        if (!servedQuestionIds.Contains(id))
        {
            servedQuestionIds.Add(id);
            SaveServedQuestions();
        }
    }

    private void LoadServedQuestions()
    {
        servedQuestionIds = new HashSet<int>();
        if (PlayerPrefs.HasKey("ServedQuestions"))
        {
            string data = PlayerPrefs.GetString("ServedQuestions");
            string[] ids = data.Split(',');
            foreach (var id in ids)
            {
                if (int.TryParse(id, out int result)) servedQuestionIds.Add(result);
            }
        }
    }

    private void SaveServedQuestions()
    {
        string data = string.Join(",", servedQuestionIds);
        PlayerPrefs.SetString("ServedQuestions", data);
        PlayerPrefs.Save();
    }

    private void ResetServedQuestions()
    {
        servedQuestionIds.Clear();
        PlayerPrefs.DeleteKey("ServedQuestions");
        PlayerPrefs.Save();
    }

    // Helper for answer shuffling
    public List<string> GetShuffledAnswers(QuestionData q)
    {
        List<string> answers = new List<string>();
        answers.Add(q.answer);
        answers.AddRange(q.wrong);
        
        for (int i = 0; i < answers.Count; i++) {
            string temp = answers[i];
            int randomIndex = Random.Range(i, answers.Count);
            answers[i] = answers[randomIndex];
            answers[randomIndex] = temp;
        }
        
        return answers;
    }
}

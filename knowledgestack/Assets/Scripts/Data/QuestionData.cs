using System;

[Serializable]
public class QuestionData
{
    public int id;
    public string category;
    public string text_tr;
    public string text_en;
    public string answer; // Correct answer
    public string[] wrong; // Wrong answers
    public int difficulty;
    public int time;
}

[Serializable]
public class QuestionList
{
    public QuestionData[] questions;
}

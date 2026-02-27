using System;

[Serializable]
public class QuestionData
{
    public int id;
    public string category;
    public string text_tr;
    public string text_en;
    public string answer_tr;
    public string[] wrong_tr;
    public string answer_eng;
    public string[] wrong_eng;
    public int difficulty;
    public int time;
}

[Serializable]
public class QuestionList
{
    public QuestionData[] questions;
}

using UnityEngine;

namespace KnowledgeStack.Game.Hangman
{
    [CreateAssetMenu(fileName = "NewHangmanSkin", menuName = "KnowledgeStack/Hangman Skin", order = 1)]
    public class HangmanCharacterSkin : ScriptableObject
    {
        [Header("Skin Information")]
        public string skinName;

        [Header("Body Parts (All must be same canvas size)")]
        public Sprite head;
        public Sprite body;
        public Sprite rightArm;
        public Sprite leftArm;
        public Sprite rightLeg;
        public Sprite leftLeg;     
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KnowledgeStack.Game.Hangman
{
    public enum BodyPartType
    {
        Head,
        Body,
        RightArm,
        LeftArm,
        RightLeg,
        LeftLeg
    }

    [System.Serializable]
    public class BodyPartGroup
    {
        public List<BodyPartType> parts = new List<BodyPartType>();
        
        public BodyPartGroup(params BodyPartType[] p)
        {
            parts.AddRange(p);
        }
    }

    [System.Serializable]
    public class LevelBracket
    {
        public int startLevel;
        public int endLevel;
        public List<BodyPartGroup> appearanceOrder;
    }

    public class HangmanManager : MonoBehaviour
    {
        [Header("References")]
        public HangmanCharacterSkin currentSkin;
        
        [Header("UI Image Layers (Bottom to Top)")]
        public Image imgHead;
        public Image imgBody;
        public Image imgRightArm;
        public Image imgLeftArm;
        public Image imgRightLeg;
        public Image imgLeftLeg;

        [Header("Level Configurations")]
        public List<LevelBracket> brackets = new List<LevelBracket>();

        private int currentMistakes = 0;
        private LevelBracket currentBracket;

        private void Start()
        {
            if (brackets.Count == 0)
            {
                SetupDefaultBrackets();
            }
        }

        private void SetupDefaultBrackets()
        {
            brackets.Add(new LevelBracket
            {
                startLevel = 1,
                endLevel = 20, // 3 Mistakes Max
                appearanceOrder = new List<BodyPartGroup> 
                { 
                    new BodyPartGroup(BodyPartType.Head), 
                    new BodyPartGroup(BodyPartType.Body, BodyPartType.RightArm, BodyPartType.LeftArm), 
                    new BodyPartGroup(BodyPartType.RightLeg, BodyPartType.LeftLeg) 
                }
            });

            brackets.Add(new LevelBracket
            {
                startLevel = 21,
                endLevel = 80, // 4 Mistakes Max
                appearanceOrder = new List<BodyPartGroup> 
                { 
                    new BodyPartGroup(BodyPartType.Head), 
                    new BodyPartGroup(BodyPartType.Body, BodyPartType.RightArm, BodyPartType.LeftArm), 
                    new BodyPartGroup(BodyPartType.RightLeg), 
                    new BodyPartGroup(BodyPartType.LeftLeg) 
                }
            });

            brackets.Add(new LevelBracket
            {
                startLevel = 81,
                endLevel = 9999, // 6 Mistakes Max
                appearanceOrder = new List<BodyPartGroup> 
                { 
                    new BodyPartGroup(BodyPartType.Head), 
                    new BodyPartGroup(BodyPartType.Body), 
                    new BodyPartGroup(BodyPartType.RightArm), 
                    new BodyPartGroup(BodyPartType.LeftArm), 
                    new BodyPartGroup(BodyPartType.RightLeg), 
                    new BodyPartGroup(BodyPartType.LeftLeg) 
                }
            });
        }

        public void InitializeForLevel(int level)
        {
            currentMistakes = 0;
            currentBracket = GetBracketForLevel(level);
            
            HideAllParts();

            Debug.Log($"Hangman Initialized for Level {level}. Max Mistakes Allowed: {currentBracket.appearanceOrder.Count}");
        }

        private LevelBracket GetBracketForLevel(int level)
        {
            foreach (var bracket in brackets)
            {
                if (level >= bracket.startLevel && level <= bracket.endLevel)
                {
                    return bracket;
                }
            }
            return brackets[brackets.Count - 1];
        }

        public bool HandleMistake()
        {
            if (currentBracket == null) return false;

            if (currentMistakes < currentBracket.appearanceOrder.Count)
            {
                BodyPartGroup groupToShow = currentBracket.appearanceOrder[currentMistakes];
                foreach (var part in groupToShow.parts)
                {
                    ShowPart(part);
                }
                
                currentMistakes++;
                
                if (currentMistakes >= currentBracket.appearanceOrder.Count)
                {
                    return true; // Game Over
                }
            }
            return false; 
        }

        private void ShowPart(BodyPartType part)
        {
            if (currentSkin == null)
            {
                Debug.LogWarning("No Hangman Skin assigned!");
                return;
            }

            switch (part)
            {
                case BodyPartType.Head:
                    if (imgHead) { imgHead.sprite = currentSkin.head; imgHead.gameObject.SetActive(true); }
                    break;
                case BodyPartType.Body:
                    if (imgBody) { imgBody.sprite = currentSkin.body; imgBody.gameObject.SetActive(true); }
                    break;
                case BodyPartType.RightArm:
                    if (imgRightArm) { imgRightArm.sprite = currentSkin.rightArm; imgRightArm.gameObject.SetActive(true); }
                    break;
                case BodyPartType.LeftArm:
                    if (imgLeftArm) { imgLeftArm.sprite = currentSkin.leftArm; imgLeftArm.gameObject.SetActive(true); }
                    break;
                case BodyPartType.RightLeg:
                    if (imgRightLeg) { imgRightLeg.sprite = currentSkin.rightLeg; imgRightLeg.gameObject.SetActive(true); }
                    break;
                case BodyPartType.LeftLeg:
                    if (imgLeftLeg) { imgLeftLeg.sprite = currentSkin.leftLeg; imgLeftLeg.gameObject.SetActive(true); }
                    break;
            }
        }

        private void HideAllParts()
        {
            if (imgHead) imgHead.gameObject.SetActive(false);
            if (imgBody) imgBody.gameObject.SetActive(false);
            if (imgRightArm) imgRightArm.gameObject.SetActive(false);
            if (imgLeftArm) imgLeftArm.gameObject.SetActive(false);
            if (imgRightLeg) imgRightLeg.gameObject.SetActive(false);
            if (imgLeftLeg) imgLeftLeg.gameObject.SetActive(false);
        }
    }
}

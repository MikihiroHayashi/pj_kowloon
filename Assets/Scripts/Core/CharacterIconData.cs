using UnityEngine;

namespace KowloonBreak.Core
{
    /// <summary>
    /// キャラクターアイコンのデータ
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterIconData", menuName = "KowloonBreak/Character Icon Data")]
    public class CharacterIconData : ScriptableObject
    {
        [Header("Character Info")]
        public string characterId;
        public string characterName;

        [Header("Icon")]
        public Sprite iconSprite;

        [Header("Default Icon Colors")]
        public Color healthBarColor = Color.green;
        public Color infectionBarColor = new Color(0.5f, 0f, 0.5f); // Purple
    }
}

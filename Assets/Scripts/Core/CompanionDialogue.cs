using System;
using System.Collections.Generic;
using UnityEngine;
using KowloonBreak.Characters;

namespace KowloonBreak.Core
{
    /// <summary>
    /// コンパニオンのセリフデータを管理するScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "CompanionDialogue", menuName = "Kowloon Break/Companion Dialogue")]
    public class CompanionDialogue : ScriptableObject
    {
        [Header("状態変更時のセリフ")]
        [SerializeField] private StateDialogueGroup[] stateDialogues;
        
        [Header("命令受領時のセリフ")]
        [SerializeField] private CommandDialogueGroup[] commandDialogues;
        
        [Header("戦闘関連のセリフ")]
        [SerializeField] private CombatDialogueGroup[] combatDialogues;
        
        [Header("一般的なセリフ")]
        [SerializeField] private GeneralDialogueGroup[] generalDialogues;

        [Header("感染関連のセリフ")]
        [SerializeField] private InfectionDialogueGroup[] infectionDialogues;

        /// <summary>
        /// 指定されたAIステートに対応するセリフをランダムで取得
        /// </summary>
        public string GetStateDialogue(AIState state)
        {
            foreach (var group in stateDialogues)
            {
                if (group.state == state && group.dialogues.Length > 0)
                {
                    return group.dialogues[UnityEngine.Random.Range(0, group.dialogues.Length)];
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// 指定されたコマンドに対応するセリフをランダムで取得
        /// </summary>
        public string GetCommandDialogue(CompanionCommand command)
        {
            foreach (var group in commandDialogues)
            {
                if (group.command == command && group.dialogues.Length > 0)
                {
                    return group.dialogues[UnityEngine.Random.Range(0, group.dialogues.Length)];
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// 戦闘関連のセリフを取得
        /// </summary>
        public string GetCombatDialogue(CombatDialogueType type)
        {
            foreach (var group in combatDialogues)
            {
                if (group.type == type && group.dialogues.Length > 0)
                {
                    return group.dialogues[UnityEngine.Random.Range(0, group.dialogues.Length)];
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// 一般的なセリフを取得
        /// </summary>
        public string GetGeneralDialogue(GeneralDialogueType type)
        {
            foreach (var group in generalDialogues)
            {
                if (group.type == type && group.dialogues.Length > 0)
                {
                    return group.dialogues[UnityEngine.Random.Range(0, group.dialogues.Length)];
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// 感染関連のセリフを取得
        /// </summary>
        public string GetInfectionDialogue(InfectionDialogueType type)
        {
            if (infectionDialogues == null)
                return string.Empty;

            foreach (var group in infectionDialogues)
            {
                if (group.type == type && group.dialogues != null && group.dialogues.Length > 0)
                {
                    return group.dialogues[UnityEngine.Random.Range(0, group.dialogues.Length)];
                }
            }

            return string.Empty;
        }
    }

    [Serializable]
    public class StateDialogueGroup
    {
        [Header("対象のAIステート")]
        public AIState state;
        
        [Header("セリフリスト")]
        [TextArea(2, 4)]
        public string[] dialogues;
    }

    [Serializable]
    public class CommandDialogueGroup
    {
        [Header("対象のコマンド")]
        public CompanionCommand command;
        
        [Header("セリフリスト")]
        [TextArea(2, 4)]
        public string[] dialogues;
    }

    [Serializable]
    public class CombatDialogueGroup
    {
        [Header("戦闘状況")]
        public CombatDialogueType type;
        
        [Header("セリフリスト")]
        [TextArea(2, 4)]
        public string[] dialogues;
    }

    [Serializable]
    public class GeneralDialogueGroup
    {
        [Header("一般状況")]
        public GeneralDialogueType type;

        [Header("セリフリスト")]
        [TextArea(2, 4)]
        public string[] dialogues;
    }

    [Serializable]
    public class InfectionDialogueGroup
    {
        [Header("感染状況")]
        public InfectionDialogueType type;

        [Header("セリフリスト")]
        [TextArea(2, 4)]
        public string[] dialogues;
    }

    public enum CombatDialogueType
    {
        EnemySpotted,      // 敵を発見
        AttackStart,       // 攻撃開始
        TakingDamage,      // ダメージを受けた
        LowHealth,         // 体力低下
        EnemyDefeated,     // 敵を倒した
        PlayerInDanger,    // プレイヤーが危険
        Death              // 死亡時
    }

    public enum GeneralDialogueType
    {
        Greeting,          // 挨拶
        Waiting,           // 待機中
        Following,         // 追従中
        ItemFound,         // アイテム発見
        RestComplete,      // 休憩完了
        LevelUp            // レベルアップ
    }

    public enum InfectionDialogueType
    {
        JustInfected,      // 感染直後
        InfectionPain,     // 感染による痛み
        FeelingSick,       // 体調不良
        FearOfInfection,   // 感染への恐怖
        PleaForHelp,       // 助けを求める
        BeforeAmputation,  // 切断前
        AfterAmputation,   // 切断後
        AfterVaccine,      // ワクチン治療後
        InfectionSpread,   // 感染拡大への懸念
        LastWords,         // 最後の言葉（重篤時）
        RecoveryHope,      // 回復への希望

        // ボタンフォーカス時のセリフ
        VaccineFocused,    // ワクチンボタンにフォーカス時
        CarryFocused,      // 運搬ボタンにフォーカス時
        AmputateFocused,   // 切断ボタンにフォーカス時

        // ボタン決定時のセリフ
        VaccineSelected,   // ワクチン選択時
        CarrySelected,     // 運搬選択時
        AmputateSelected   // 切断選択時
    }
}
using System;
using UnityEngine;

namespace KowloonBreak.Core
{
    [Serializable]
    public class ConsumableEffect
    {
        [Header("回復効果")]
        public ConsumableType effectType = ConsumableType.HealthRestore;

        [Header("体力回復")]
        public float healthRestore = 0f;

        [Header("感染治療")]
        public float infectionTreatment = 0f;

        [Header("スタミナ回復")]
        public float staminaRestore = 0f;

        [Header("使用時効果")]
        public bool consumeOnUse = true;
        public string useMessage = "アイテムを使用しました";

        public bool HasHealthEffect => healthRestore > 0f;
        public bool HasInfectionEffect => infectionTreatment > 0f;
        public bool HasStaminaEffect => staminaRestore > 0f;

        public bool HasAnyEffect => HasHealthEffect || HasInfectionEffect || HasStaminaEffect;
    }
}
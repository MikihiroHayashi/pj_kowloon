using System;
using UnityEngine;

namespace KowloonBreak.Core
{
    [Serializable]
    public class InfectionDamageSettings
    {
        [Header("Attack Type Infection Damage")]
        [SerializeField] private float punchInfectionDamage = 10f;
        [SerializeField] private float clawInfectionDamage = 20f;
        [SerializeField] private float biteInfectionDamage = 30f;
        [SerializeField] private float tackleInfectionDamage = 15f;
        [SerializeField] private float slamInfectionDamage = 25f;
        [SerializeField] private float chargeInfectionDamage = 20f;
        [SerializeField] private float projectileInfectionDamage = 12f;
        [SerializeField] private float specialInfectionDamage = 35f;

        [Header("Settings")]
        [SerializeField] private bool useGlobalMultiplier = true;
        [SerializeField] private float globalMultiplier = 1f;

        /// <summary>
        /// 指定された攻撃タイプの感染ダメージを取得
        /// </summary>
        /// <param name="attackType">攻撃タイプ</param>
        /// <param name="baseInfectionDamage">ベース感染ダメージ（フォールバック用）</param>
        /// <returns>感染ダメージ値</returns>
        public float GetInfectionDamage(EnemyAttackType attackType, float baseInfectionDamage = 15f)
        {
            float damage = attackType switch
            {
                EnemyAttackType.Punch => punchInfectionDamage,
                EnemyAttackType.Claw => clawInfectionDamage,
                EnemyAttackType.Bite => biteInfectionDamage,
                EnemyAttackType.Tackle => tackleInfectionDamage,
                EnemyAttackType.Slam => slamInfectionDamage,
                EnemyAttackType.Charge => chargeInfectionDamage,
                EnemyAttackType.Projectile => projectileInfectionDamage,
                EnemyAttackType.Special => specialInfectionDamage,
                _ => baseInfectionDamage
            };

            // グローバル倍率を適用
            if (useGlobalMultiplier)
            {
                damage *= globalMultiplier;
            }

            return damage;
        }

        /// <summary>
        /// 指定された攻撃タイプの感染ダメージを設定
        /// </summary>
        /// <param name="attackType">攻撃タイプ</param>
        /// <param name="damage">感染ダメージ値</param>
        public void SetInfectionDamage(EnemyAttackType attackType, float damage)
        {
            switch (attackType)
            {
                case EnemyAttackType.Punch:
                    punchInfectionDamage = damage;
                    break;
                case EnemyAttackType.Claw:
                    clawInfectionDamage = damage;
                    break;
                case EnemyAttackType.Bite:
                    biteInfectionDamage = damage;
                    break;
                case EnemyAttackType.Tackle:
                    tackleInfectionDamage = damage;
                    break;
                case EnemyAttackType.Slam:
                    slamInfectionDamage = damage;
                    break;
                case EnemyAttackType.Charge:
                    chargeInfectionDamage = damage;
                    break;
                case EnemyAttackType.Projectile:
                    projectileInfectionDamage = damage;
                    break;
                case EnemyAttackType.Special:
                    specialInfectionDamage = damage;
                    break;
            }
        }

        /// <summary>
        /// グローバル倍率を設定
        /// </summary>
        /// <param name="multiplier">倍率</param>
        public void SetGlobalMultiplier(float multiplier)
        {
            globalMultiplier = Mathf.Max(0f, multiplier);
        }

        /// <summary>
        /// グローバル倍率の使用を切り替え
        /// </summary>
        /// <param name="useMultiplier">倍率を使用するかどうか</param>
        public void SetUseGlobalMultiplier(bool useMultiplier)
        {
            useGlobalMultiplier = useMultiplier;
        }

        /// <summary>
        /// デフォルト値でリセット
        /// </summary>
        public void ResetToDefaults()
        {
            punchInfectionDamage = 10f;
            clawInfectionDamage = 20f;
            biteInfectionDamage = 30f;
            tackleInfectionDamage = 15f;
            slamInfectionDamage = 25f;
            chargeInfectionDamage = 20f;
            projectileInfectionDamage = 12f;
            specialInfectionDamage = 35f;
            globalMultiplier = 1f;
            useGlobalMultiplier = true;
        }

        /// <summary>
        /// 全攻撃タイプの感染ダメージを取得（デバッグ用）
        /// </summary>
        /// <returns>攻撃タイプと感染ダメージのペア配列</returns>
        public (EnemyAttackType type, float damage)[] GetAllInfectionDamages()
        {
            return new[]
            {
                (EnemyAttackType.Punch, GetInfectionDamage(EnemyAttackType.Punch, 0f)),
                (EnemyAttackType.Claw, GetInfectionDamage(EnemyAttackType.Claw, 0f)),
                (EnemyAttackType.Bite, GetInfectionDamage(EnemyAttackType.Bite, 0f)),
                (EnemyAttackType.Tackle, GetInfectionDamage(EnemyAttackType.Tackle, 0f)),
                (EnemyAttackType.Slam, GetInfectionDamage(EnemyAttackType.Slam, 0f)),
                (EnemyAttackType.Charge, GetInfectionDamage(EnemyAttackType.Charge, 0f)),
                (EnemyAttackType.Projectile, GetInfectionDamage(EnemyAttackType.Projectile, 0f)),
                (EnemyAttackType.Special, GetInfectionDamage(EnemyAttackType.Special, 0f))
            };
        }
    }
}
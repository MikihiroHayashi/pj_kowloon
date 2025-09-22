using System.Collections;
using UnityEngine;

namespace KowloonBreak.Core
{
    /// <summary>
    /// ノックバック機能の共通実装
    /// </summary>
    [System.Serializable]
    public class KnockbackSystem
    {
        [Header("Knockback Settings")]
        [SerializeField] private float knockbackForce = 5f;
        [SerializeField] private float knockbackDuration = 0.8f;
        [SerializeField] private bool enableKnockback = true;

        // ノックバック関連の状態
        private bool isKnockedBack = false;
        private float knockbackEndTime = 0f;
        private MonoBehaviour owner;
        private Rigidbody targetRigidbody;
        private Animator targetAnimator;

        // アニメーションパラメータ名
        private const string ANIM_DAMAGE = "Damage";
        private const string ANIM_RESET = "Reset";

        public bool IsKnockedBack => isKnockedBack;

        /// <summary>
        /// ノックバックシステムを初期化
        /// </summary>
        /// <param name="owner">オーナーのMonoBehaviour</param>
        /// <param name="rigidbody">対象のRigidbody</param>
        /// <param name="animator">対象のAnimator</param>
        public void Initialize(MonoBehaviour owner, Rigidbody rigidbody, Animator animator)
        {
            this.owner = owner;
            this.targetRigidbody = rigidbody;
            this.targetAnimator = animator;

            // Rigidbodyの初期設定
            if (targetRigidbody != null)
            {
                targetRigidbody.freezeRotation = true;
            }
        }

        /// <summary>
        /// ノックバック状態の更新（Updateで呼び出す）
        /// </summary>
        public void UpdateKnockbackState()
        {
            if (isKnockedBack && Time.time >= knockbackEndTime)
            {
                EndKnockback();
            }
        }

        /// <summary>
        /// ノックバックを開始
        /// </summary>
        /// <param name="attackerPosition">攻撃者の位置</param>
        /// <param name="toolType">使用された武器のタイプ</param>
        /// <param name="onKnockbackStart">ノックバック開始時のコールバック</param>
        /// <param name="onKnockbackEnd">ノックバック終了時のコールバック</param>
        /// <param name="isEnemyAttack">敵からの攻撃かどうか（武器乗算値を無効化）</param>
        public void StartKnockback(Vector3 attackerPosition, ToolType toolType = ToolType.IronPipe,
            System.Action onKnockbackStart = null, System.Action onKnockbackEnd = null, bool isEnemyAttack = false)
        {
            if (!enableKnockback || owner == null) return;

            isKnockedBack = true;
            knockbackEndTime = Time.time + knockbackDuration;

            // ノックバック開始時のコールバック実行
            onKnockbackStart?.Invoke();

            // ダメージアニメーションを再生
            if (targetAnimator != null)
            {
                targetAnimator.SetTrigger(ANIM_DAMAGE);
            }

            // ノックバック方向の計算
            Vector3 knockbackDirection = (owner.transform.position - attackerPosition).normalized;
            knockbackDirection.y = 0f; // Y軸方向の力を除去

            // 武器による乗算値を取得（敵攻撃の場合は1.0固定）
            float weaponKnockbackMultiplier = isEnemyAttack ? 1.0f : GetWeaponKnockbackMultiplier(toolType);
            float finalKnockbackForce = knockbackForce * weaponKnockbackMultiplier;

            // Rigidbodyを使用してノックバック
            if (targetRigidbody != null)
            {
                targetRigidbody.isKinematic = false;
                targetRigidbody.freezeRotation = true;
                targetRigidbody.AddForce(knockbackDirection * finalKnockbackForce, ForceMode.Impulse);
            }

            // 終了処理をコルーチンで実行
            if (owner != null)
            {
                owner.StartCoroutine(KnockbackEndCoroutine(onKnockbackEnd));
            }
        }

        /// <summary>
        /// ノックバック終了処理のコルーチン
        /// </summary>
        private IEnumerator KnockbackEndCoroutine(System.Action onKnockbackEnd)
        {
            yield return new WaitUntil(() => Time.time >= knockbackEndTime);
            EndKnockback();
            onKnockbackEnd?.Invoke();
        }

        /// <summary>
        /// ノックバックを終了
        /// </summary>
        private void EndKnockback()
        {
            isKnockedBack = false;

            // Rigidbodyを停止
            if (targetRigidbody != null)
            {
                // kinematicに設定する前にvelocityをリセット
                if (!targetRigidbody.isKinematic)
                {
                    targetRigidbody.velocity = Vector3.zero;
                    targetRigidbody.angularVelocity = Vector3.zero;
                }
                targetRigidbody.isKinematic = true;
                targetRigidbody.freezeRotation = true;
            }

            // リセットアニメーションを再生
            if (targetAnimator != null)
            {
                targetAnimator.SetTrigger(ANIM_RESET);
            }
        }

        /// <summary>
        /// ノックバック設定を変更
        /// </summary>
        /// <param name="force">ノックバック力</param>
        /// <param name="duration">ノックバック持続時間</param>
        public void SetKnockbackSettings(float force, float duration)
        {
            knockbackForce = force;
            knockbackDuration = duration;
        }

        /// <summary>
        /// ノックバック機能の有効/無効を切り替え
        /// </summary>
        /// <param name="enabled">有効かどうか</param>
        public void SetKnockbackEnabled(bool enabled)
        {
            enableKnockback = enabled;
        }

        /// <summary>
        /// 強制的にノックバック状態を終了
        /// </summary>
        public void ForceEndKnockback()
        {
            if (isKnockedBack)
            {
                EndKnockback();
            }
        }

        /// <summary>
        /// 武器タイプに基づいてノックバック乗算値を取得
        /// </summary>
        /// <param name="toolType">武器タイプ</param>
        /// <returns>ノックバック乗算値</returns>
        private float GetWeaponKnockbackMultiplier(ToolType toolType)
        {
            // ItemDataが存在する場合はそちらを優先
            ItemData weaponData = GetWeaponItemData(toolType);
            if (weaponData != null)
            {
                return weaponData.knockbackMultiplier;
            }

            // フォールバック：デフォルトの武器別ノックバック乗算値
            switch (toolType)
            {
                case ToolType.Pickaxe:
                    return 1.5f;  // つるはしは強力なノックバック
                case ToolType.IronPipe:
                    return 1.0f;  // 鉄パイプは通常のノックバック
                default:
                    return 1.0f;  // デフォルト
            }
        }

        /// <summary>
        /// 指定された武器タイプのItemDataを取得
        /// </summary>
        /// <param name="toolType">武器タイプ</param>
        /// <returns>対応するItemData、見つからない場合はnull</returns>
        private ItemData GetWeaponItemData(ToolType toolType)
        {
            // すべてのItemDataアセットを検索
            ItemData[] allItems = Resources.FindObjectsOfTypeAll<ItemData>();
            foreach (var item in allItems)
            {
                if (item.IsTool() && item.toolType == toolType)
                {
                    return item;
                }
            }
            return null;
        }

        /// <summary>
        /// 設定値のゲッター
        /// </summary>
        public float KnockbackForce => knockbackForce;
        public float KnockbackDuration => knockbackDuration;
        public bool EnableKnockback => enableKnockback;
    }
}
using UnityEngine;

namespace KowloonBreak.Core
{
    /// <summary>
    /// ノックバック可能なオブジェクトのインターフェース
    /// </summary>
    public interface IKnockbackable
    {
        /// <summary>
        /// ノックバックを開始
        /// </summary>
        /// <param name="attackerPosition">攻撃者の位置</param>
        /// <param name="toolType">使用された武器のタイプ</param>
        void StartKnockback(Vector3 attackerPosition, ToolType toolType = ToolType.IronPipe);

        /// <summary>
        /// ノックバック中かどうか
        /// </summary>
        bool IsKnockedBack { get; }

        /// <summary>
        /// ノックバック設定を変更
        /// </summary>
        /// <param name="force">ノックバック力</param>
        /// <param name="duration">ノックバック持続時間</param>
        void SetKnockbackSettings(float force, float duration);

        /// <summary>
        /// ノックバック機能の有効/無効を切り替え
        /// </summary>
        /// <param name="enabled">有効かどうか</param>
        void SetKnockbackEnabled(bool enabled);
    }
}
using UnityEngine;

namespace KowloonBreak.Core
{
    /// <summary>
    /// 入力ハンドラーのインターフェース
    /// コンテキストごとに異なる入力処理を実装する
    /// </summary>
    public interface IInputHandler
    {
        /// <summary>
        /// 入力処理を更新
        /// </summary>
        void HandleInput();

        /// <summary>
        /// このハンドラーが有効かどうか
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// ハンドラーを有効化
        /// </summary>
        void Activate();

        /// <summary>
        /// ハンドラーを無効化
        /// </summary>
        void Deactivate();
    }
}

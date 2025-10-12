namespace KowloonBreak.UI
{
    /// <summary>
    /// フォーカス管理可能なUIインターフェース
    /// UIManagerによって管理されるすべてのUIはこのインターフェースを実装する
    /// </summary>
    public interface IFocusableUI
    {
        /// <summary>
        /// UI入力の有効/無効を設定
        /// </summary>
        /// <param name="enabled">trueで入力有効、falseで入力無効</param>
        void SetInputEnabled(bool enabled);

        /// <summary>
        /// UIが現在表示されているか
        /// </summary>
        bool IsVisible { get; }

        /// <summary>
        /// UIの優先度（数値が小さいほど高優先度）
        /// 0: TargetSelectionDialog
        /// 1: GameOverPanel
        /// 2: InfectedInteractionPanel
        /// 3: CompanionCommandPanel
        /// 4: InventoryDialog
        /// 5: ToolSelectionHUD
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// UIの名前（デバッグ用）
        /// </summary>
        string UIName { get; }
    }
}

namespace KowloonBreak.Core
{
    /// <summary>
    /// UIコンテキスト - 現在のUI状態を表す
    /// </summary>
    public enum UIContext
    {
        /// <summary>通常のゲームプレイ中</summary>
        Gameplay,

        /// <summary>インベントリ表示中</summary>
        Inventory,

        /// <summary>ターゲット選択ダイアログ表示中</summary>
        TargetSelection,

        /// <summary>会話/ダイアログ表示中</summary>
        Dialogue,

        /// <summary>仲間コマンドUI表示中</summary>
        CompanionCommand,

        /// <summary>拠点管理UI表示中</summary>
        BaseManagement,

        /// <summary>マップ表示中</summary>
        Map,

        /// <summary>クラフトUI表示中</summary>
        Crafting,

        /// <summary>タクティカルポーズ中</summary>
        TacticalPause,

        /// <summary>ポーズメニュー表示中</summary>
        PauseMenu,

        /// <summary>ゲームオーバー画面</summary>
        GameOver
    }
}

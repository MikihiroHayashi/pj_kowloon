using UnityEngine;

namespace KowloonBreak.Core
{
    /// <summary>
    /// ゲームプレイ中の入力ハンドラー
    /// プレイヤーの移動・攻撃・アイテム使用などの入力を処理
    /// </summary>
    public class GameplayInputHandler : IInputHandler
    {
        private bool isActive = false;

        public bool IsActive => isActive;

        public void Activate()
        {
            isActive = true;
            Debug.Log("[GameplayInputHandler] Activated");
        }

        public void Deactivate()
        {
            isActive = false;
            Debug.Log("[GameplayInputHandler] Deactivated");
        }

        public void HandleInput()
        {
            if (!isActive) return;

            var inputManager = InputManager.Instance;
            if (inputManager == null) return;

            var uiManager = UI.UIManager.Instance;
            if (uiManager == null) return;

            var contextManager = UIContextManager.Instance;

            // インベントリ表示切り替え
            // ※ゲームプレイ中のみ開く処理を行う（UI開閉の競合を防ぐため先に処理）
            if (inputManager.IsInventoryPressed())
            {
                if (contextManager == null || contextManager.IsInGameplay)
                {
                    uiManager.TogglePanel("Inventory");
                    return; // 他の処理をスキップ
                }
            }

            // メニュー表示切り替え
            // ※ゲームプレイ中のみ開く処理を行う
            if (inputManager.IsMenuPressed())
            {
                if (contextManager == null || contextManager.IsInGameplay)
                {
                    uiManager.TogglePanel("Menu");
                    return; // 他の処理をスキップ
                }
            }

            // キャンセル入力（ESCキーまたはダッジロールボタン）
            // UIが開いている場合は最上位のUIを閉じる
            if (inputManager.IsCancelPressed())
            {
                if (contextManager != null && !contextManager.IsInGameplay)
                {
                    // TargetSelectionは独自のキャンセル処理を持つため、ここでは介入しない
                    if (contextManager.CurrentContext != UIContext.TargetSelection)
                    {
                        uiManager.CloseTopPanel();
                        return; // 他の処理をスキップ
                    }
                }
            }

            // その他のゲームプレイ入力はEnhancedPlayerControllerで直接処理
            // （移動、攻撃、道具使用など）
        }
    }
}

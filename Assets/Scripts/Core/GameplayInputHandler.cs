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

            // インベントリ表示切り替え
            if (inputManager.IsInventoryPressed())
            {
                // UIManagerを通してインベントリを開く
                if (UI.UIManager.Instance != null)
                {
                    UI.UIManager.Instance.TogglePanel("Inventory");
                }
            }

            // メニュー表示切り替え
            if (inputManager.IsMenuPressed())
            {
                // UIManagerを通してメニューを開く
                if (UI.UIManager.Instance != null)
                {
                    UI.UIManager.Instance.TogglePanel("Menu");
                }
            }

            // その他のゲームプレイ入力はEnhancedPlayerControllerで直接処理
            // （移動、攻撃、道具使用など）
        }
    }
}

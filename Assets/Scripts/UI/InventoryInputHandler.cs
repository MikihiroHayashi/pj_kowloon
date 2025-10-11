using UnityEngine;

namespace KowloonBreak.UI
{
    /// <summary>
    /// インベントリUI表示中の入力ハンドラー
    /// インベントリの操作（選択、使用、破棄、閉じるなど）を処理
    /// </summary>
    public class InventoryInputHandler : Core.IInputHandler
    {
        private bool isActive = false;
        private InventoryDialogController inventoryController;

        public bool IsActive => isActive;

        public InventoryInputHandler(InventoryDialogController controller)
        {
            inventoryController = controller;
        }

        public void Activate()
        {
            isActive = true;
            Debug.Log("[InventoryInputHandler] Activated");
        }

        public void Deactivate()
        {
            isActive = false;
            Debug.Log("[InventoryInputHandler] Deactivated");
        }

        public void HandleInput()
        {
            if (!isActive) return;
            if (inventoryController == null) return;

            var inputManager = Core.InputManager.Instance;
            if (inputManager == null) return;

            // インベントリ表示中はゲームプレイ入力を無効化（念のため）
            // EnhancedPlayerControllerのHandleInput()でも既にブロックされているが、
            // ここでも明示的に入力を消費して確実にブロックする

            // インタラクトボタンを無視（消費）
            if (inputManager.IsInteractionPressed())
            {
                // 何もしない - 入力を消費してプレイヤーに届かないようにする
                return;
            }

            // 道具使用ボタンを無視（消費）
            if (inputManager.IsUseToolPressed())
            {
                // 何もしない - 入力を消費してプレイヤーに届かないようにする
                return;
            }

            // Bボタン / Escape / インベントリボタンで閉じる
            if (inputManager.IsCancelPressed() || inputManager.IsInventoryPressed())
            {
                inventoryController.CloseInventory();
            }

            // アイテムの使用・破棄などはItemDetailPopupが直接処理するため、
            // ここでは基本的な閉じる操作のみを扱う
        }
    }
}

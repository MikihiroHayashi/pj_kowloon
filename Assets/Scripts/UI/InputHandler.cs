using UnityEngine;
using KowloonBreak.Core;

namespace KowloonBreak.UI
{
    public class InputHandler : MonoBehaviour
    {
        [Header("Inventory Settings")]
        [SerializeField] private InventoryDialogController inventoryController;

        private void Update()
        {
            HandleInventoryInput();
        }

        private void HandleInventoryInput()
        {
            // InputManagerを使用してインベントリ入力を検出
            var inputManager = InputManager.Instance;
            if (inputManager != null && inputManager.IsInventoryPressed())
            {
                Debug.Log("[InputHandler] Inventory input detected");

                if (inventoryController != null)
                {
                    inventoryController.ToggleInventory();
                    Debug.Log("[InputHandler] Inventory toggle triggered");
                }
                else
                {
                    Debug.LogWarning("[InputHandler] InventoryController is not assigned!");

                    // UIManagerを使用
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.TogglePanel("Inventory");
                        Debug.Log("[InputHandler] Inventory toggle triggered via UIManager");
                    }
                }
            }
        }

        public void SetInventoryController(InventoryDialogController controller)
        {
            inventoryController = controller;
            Debug.Log($"[InputHandler] InventoryController set: {controller != null}");
        }
    }
}
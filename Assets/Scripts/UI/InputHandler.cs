using UnityEngine;

namespace KowloonBreak.UI
{
    public class InputHandler : MonoBehaviour
    {
        [Header("Inventory Settings")]
        [SerializeField] private InventoryDialogController inventoryController;
        [SerializeField] private KeyCode inventoryKey = KeyCode.I;
        
        private void Update()
        {
            HandleInventoryInput();
        }
        
        private void HandleInventoryInput()
        {
            if (Input.GetKeyDown(inventoryKey))
            {
                if (inventoryController != null)
                {
                    inventoryController.ToggleInventory();
                    Debug.Log("Inventory toggle triggered by InputHandler");
                }
                else
                {
                    // UIManagerを使用
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.TogglePanel("Inventory");
                        Debug.Log("Inventory toggle triggered via UIManager");
                    }
                }
            }
        }
        
        public void SetInventoryController(InventoryDialogController controller)
        {
            inventoryController = controller;
        }
    }
}
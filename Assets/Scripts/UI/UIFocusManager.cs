using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KowloonBreak.UI
{
    /// <summary>
    /// UI間のフォーカス管理を行うマネージャークラス
    /// 優先度に基づいてUI入力の有効/無効を自動制御する
    /// </summary>
    public class UIFocusManager : MonoBehaviour
    {
        public static UIFocusManager Instance { get; private set; }

        // 登録されたすべてのUI
        private List<IFocusableUI> registeredUIs = new List<IFocusableUI>();

        // 現在アクティブなUIスタック（優先度順）
        private Stack<IFocusableUI> activeUIStack = new Stack<IFocusableUI>();

        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugLog = true;

        private void Awake()
        {
            // シングルトン設定
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        /// <summary>
        /// UIをシステムに登録
        /// </summary>
        public void RegisterUI(IFocusableUI ui)
        {
            if (ui == null)
            {
                Debug.LogWarning("[UIFocusManager] Attempted to register null UI");
                return;
            }

            if (!registeredUIs.Contains(ui))
            {
                registeredUIs.Add(ui);
                registeredUIs = registeredUIs.OrderBy(u => u.Priority).ToList();

                if (enableDebugLog)
                    Debug.Log($"[UIFocusManager] Registered UI: {ui.UIName} (Priority: {ui.Priority})");
            }
        }

        /// <summary>
        /// UIをシステムから登録解除
        /// </summary>
        public void UnregisterUI(IFocusableUI ui)
        {
            if (ui == null) return;

            if (registeredUIs.Contains(ui))
            {
                registeredUIs.Remove(ui);

                // スタックにある場合は削除
                if (activeUIStack.Contains(ui))
                {
                    var tempStack = new Stack<IFocusableUI>();
                    while (activeUIStack.Count > 0)
                    {
                        var current = activeUIStack.Pop();
                        if (current != ui)
                            tempStack.Push(current);
                    }

                    while (tempStack.Count > 0)
                        activeUIStack.Push(tempStack.Pop());
                }

                if (enableDebugLog)
                    Debug.Log($"[UIFocusManager] Unregistered UI: {ui.UIName}");
            }
        }

        /// <summary>
        /// UIを開く（フォーカススタックにプッシュ）
        /// </summary>
        public void PushUI(IFocusableUI ui)
        {
            if (ui == null)
            {
                Debug.LogWarning("[UIFocusManager] Attempted to push null UI");
                return;
            }

            // 既にスタックにある場合は何もしない
            if (activeUIStack.Contains(ui))
            {
                if (enableDebugLog)
                    Debug.LogWarning($"[UIFocusManager] UI already in stack: {ui.UIName}");
                return;
            }

            // 現在のトップUIの入力を無効化
            if (activeUIStack.Count > 0)
            {
                var currentTop = activeUIStack.Peek();
                currentTop.SetInputEnabled(false);

                if (enableDebugLog)
                    Debug.Log($"[UIFocusManager] Disabled input for: {currentTop.UIName}");
            }

            // 新しいUIをスタックにプッシュ
            activeUIStack.Push(ui);
            ui.SetInputEnabled(true);

            if (enableDebugLog)
                Debug.Log($"[UIFocusManager] Pushed UI: {ui.UIName} (Stack size: {activeUIStack.Count})");

            // 低優先度のUIを無効化
            RefreshFocus();
        }

        /// <summary>
        /// UIを閉じる（フォーカススタックからポップ）
        /// </summary>
        public void PopUI(IFocusableUI ui)
        {
            if (ui == null)
            {
                Debug.LogWarning("[UIFocusManager] Attempted to pop null UI");
                return;
            }

            // スタックにない場合は何もしない
            if (!activeUIStack.Contains(ui))
            {
                if (enableDebugLog)
                    Debug.LogWarning($"[UIFocusManager] UI not in stack: {ui.UIName}");
                return;
            }

            // スタックから削除
            var tempStack = new Stack<IFocusableUI>();
            IFocusableUI removedUI = null;

            while (activeUIStack.Count > 0)
            {
                var current = activeUIStack.Pop();
                if (current == ui)
                {
                    removedUI = current;
                    removedUI.SetInputEnabled(false);
                    break;
                }
                tempStack.Push(current);
            }

            // 残りを戻す
            while (tempStack.Count > 0)
                activeUIStack.Push(tempStack.Pop());

            if (enableDebugLog && removedUI != null)
                Debug.Log($"[UIFocusManager] Popped UI: {removedUI.UIName} (Stack size: {activeUIStack.Count})");

            // 次のトップUIの入力を有効化
            RefreshFocus();
        }

        /// <summary>
        /// フォーカスを再計算（スタックの最上位UIのみ有効化）
        /// </summary>
        public void RefreshFocus()
        {
            // すべての登録UIを無効化
            foreach (var ui in registeredUIs)
            {
                if (!activeUIStack.Contains(ui))
                {
                    ui.SetInputEnabled(false);
                }
            }

            // スタックの最上位UIのみ有効化
            if (activeUIStack.Count > 0)
            {
                var topUI = activeUIStack.Peek();
                topUI.SetInputEnabled(true);

                if (enableDebugLog)
                    Debug.Log($"[UIFocusManager] Active UI: {topUI.UIName} (Priority: {topUI.Priority})");

                // スタック内の他のUIは無効化
                foreach (var ui in activeUIStack)
                {
                    if (ui != topUI)
                    {
                        ui.SetInputEnabled(false);
                    }
                }
            }
            else
            {
                // スタックが空の場合、最低優先度のUIを有効化
                var lowestPriorityUI = registeredUIs.LastOrDefault();
                if (lowestPriorityUI != null && lowestPriorityUI.IsVisible)
                {
                    lowestPriorityUI.SetInputEnabled(true);

                    if (enableDebugLog)
                        Debug.Log($"[UIFocusManager] No active UI, enabled lowest priority: {lowestPriorityUI.UIName}");
                }
            }
        }

        /// <summary>
        /// 現在アクティブなUIを取得
        /// </summary>
        public IFocusableUI GetActiveUI()
        {
            return activeUIStack.Count > 0 ? activeUIStack.Peek() : null;
        }

        /// <summary>
        /// スタックの状態をデバッグ出力
        /// </summary>
        public void DebugPrintStack()
        {
            Debug.Log("=== UIFocusManager Stack ===");
            Debug.Log($"Stack Size: {activeUIStack.Count}");

            int index = 0;
            foreach (var ui in activeUIStack)
            {
                Debug.Log($"[{index}] {ui.UIName} (Priority: {ui.Priority}, Visible: {ui.IsVisible})");
                index++;
            }

            Debug.Log("===========================");
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}

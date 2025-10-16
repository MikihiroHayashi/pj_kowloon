using System;
using System.Collections.Generic;
using UnityEngine;

namespace KowloonBreak.Core
{
    /// <summary>
    /// UI状態を統括管理するマネージャー
    /// すべてのUI表示状態を一元管理し、入力のブロック/許可を制御
    /// </summary>
    public class UIContextManager : MonoBehaviour
    {
        public static UIContextManager Instance { get; private set; }

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;

        // 現在のUIコンテキスト
        private UIContext currentContext = UIContext.Gameplay;

        // コンテキストスタック（複数UIが重なった場合の管理）
        private Stack<UIContext> contextStack = new Stack<UIContext>();

        // コンテキスト変更直後フラグ（入力競合を防ぐ）
        private int contextChangedFrameCount = 0;

        // イベント
        public event Action<UIContext, UIContext> OnContextChanged; // (previous, current)

        public UIContext CurrentContext => currentContext;
        public bool IsInGameplay => currentContext == UIContext.Gameplay;
        public bool IsInUI => currentContext != UIContext.Gameplay;
        public bool ContextJustChanged => contextChangedFrameCount > 0;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                if (transform.parent != null)
                {
                    transform.SetParent(null);
                }
                DontDestroyOnLoad(gameObject);

                Debug.Log("[UIContextManager] Initialized");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            // コンテキスト変更フラグを減少
            if (contextChangedFrameCount > 0)
            {
                contextChangedFrameCount--;
            }
        }

        /// <summary>
        /// UIコンテキストを変更（スタック管理）
        /// </summary>
        public void PushContext(UIContext newContext)
        {
            if (currentContext != UIContext.Gameplay)
            {
                // 現在のコンテキストをスタックに保存
                contextStack.Push(currentContext);
            }

            UIContext previousContext = currentContext;
            currentContext = newContext;

            NotifyContextChange(previousContext, newContext);

            if (showDebugInfo)
            {
                Debug.Log($"[UIContextManager] PushContext: {previousContext} -> {newContext} (Stack depth: {contextStack.Count})");
            }
        }

        /// <summary>
        /// UIコンテキストを元に戻す
        /// </summary>
        public void PopContext()
        {
            UIContext previousContext = currentContext;

            if (contextStack.Count > 0)
            {
                currentContext = contextStack.Pop();
            }
            else
            {
                currentContext = UIContext.Gameplay;
            }

            // コンテキスト変更直後フラグを設定（3フレーム間）
            contextChangedFrameCount = 3;

            NotifyContextChange(previousContext, currentContext);

            if (showDebugInfo)
            {
                Debug.Log($"[UIContextManager] PopContext: {previousContext} -> {currentContext} (Stack depth: {contextStack.Count})");
            }
        }

        /// <summary>
        /// UIコンテキストを直接設定（スタックをクリア）
        /// </summary>
        public void SetContext(UIContext newContext)
        {
            contextStack.Clear();

            UIContext previousContext = currentContext;
            currentContext = newContext;

            NotifyContextChange(previousContext, newContext);

            if (showDebugInfo)
            {
                Debug.Log($"[UIContextManager] SetContext: {previousContext} -> {newContext}");
            }
        }

        /// <summary>
        /// ゲームプレイに戻る（すべてのUIを閉じる）
        /// </summary>
        public void ReturnToGameplay()
        {
            contextStack.Clear();
            SetContext(UIContext.Gameplay);
        }

        private void NotifyContextChange(UIContext previous, UIContext current)
        {
            OnContextChanged?.Invoke(previous, current);

            // InputManagerに通知
            var inputManager = InputManager.Instance;
            if (inputManager != null)
            {
                inputManager.SetUIContext(current);
            }
        }

        // === 入力許可判定メソッド ===

        /// <summary>
        /// ゲームプレイ入力が許可されているか
        /// </summary>
        public bool IsGameplayInputAllowed()
        {
            return currentContext == UIContext.Gameplay;
        }

        /// <summary>
        /// インタラクト入力が許可されているか
        /// </summary>
        public bool IsInteractionAllowed()
        {
            switch (currentContext)
            {
                case UIContext.Gameplay:
                    return true;
                case UIContext.Dialogue:
                    return true; // ダイアログ進行用
                default:
                    return false;
            }
        }

        /// <summary>
        /// ツール使用が許可されているか
        /// </summary>
        public bool IsToolUseAllowed()
        {
            return currentContext == UIContext.Gameplay;
        }

        /// <summary>
        /// ツール切替が許可されているか
        /// </summary>
        public bool IsToolSwitchAllowed()
        {
            return currentContext == UIContext.Gameplay;
        }

        /// <summary>
        /// 移動入力が許可されているか
        /// </summary>
        public bool IsMovementAllowed()
        {
            switch (currentContext)
            {
                case UIContext.Gameplay:
                    return true;
                case UIContext.Dialogue:
                case UIContext.Inventory:
                case UIContext.PauseMenu:
                case UIContext.GameOver:
                    return false;
                default:
                    return false;
            }
        }

        /// <summary>
        /// UI入力が許可されているか（インベントリ内操作など）
        /// </summary>
        public bool IsUIInputAllowed()
        {
            return currentContext != UIContext.Gameplay;
        }

        /// <summary>
        /// 特定のUIコンテキストが現在アクティブか
        /// </summary>
        public bool IsInContext(UIContext context)
        {
            return currentContext == context;
        }

        /// <summary>
        /// デバッグ情報表示
        /// </summary>
        private void OnGUI()
        {
            if (!showDebugInfo) return;

            GUILayout.BeginArea(new Rect(10, 150, 300, 200));
            GUILayout.Label($"<b>UI Context Manager</b>", new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Label($"Current Context: {currentContext}");
            GUILayout.Label($"Stack Depth: {contextStack.Count}");
            GUILayout.Label($"Is In Gameplay: {IsInGameplay}");
            GUILayout.Label($"Gameplay Input Allowed: {IsGameplayInputAllowed()}");
            GUILayout.Label($"Interaction Allowed: {IsInteractionAllowed()}");
            GUILayout.Label($"Tool Use Allowed: {IsToolUseAllowed()}");
            GUILayout.EndArea();
        }
    }
}

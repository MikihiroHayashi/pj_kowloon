using UnityEngine;
using UnityEngine.UI;

namespace KowloonBreak.UI
{
    /// <summary>
    /// CompanionCommandPanelのフォーカス管理用ラッパークラス
    /// UIManager経由で管理されるCompanionCommandPanelをIFocusableUI対応にする
    /// </summary>
    public class CompanionCommandPanelController : MonoBehaviour, IFocusableUI
    {
        [Header("UI References")]
        [SerializeField] private GameObject panelObject;

        private bool isInputEnabled = true;
        private Button[] buttons;

        // IFocusableUI実装
        public bool IsVisible => panelObject != null && panelObject.activeSelf;
        public int Priority => 3; // コンパニオンコマンドは中優先度
        public string UIName => "CompanionCommandPanel";

        private void Awake()
        {
            // パネルオブジェクトが設定されていない場合、自身のGameObjectを使用
            if (panelObject == null)
            {
                panelObject = gameObject;
            }

            // すべてのボタンを取得
            buttons = GetComponentsInChildren<Button>(true);
        }

        private void Start()
        {
            // UIFocusManagerに登録
            if (UIFocusManager.Instance != null)
            {
                UIFocusManager.Instance.RegisterUI(this);
            }
        }

        private void OnEnable()
        {
            // パネルが有効化されたらUIFocusManagerにプッシュ
            if (UIFocusManager.Instance != null && IsVisible)
            {
                UIFocusManager.Instance.PushUI(this);
            }
        }

        private void OnDisable()
        {
            // パネルが無効化されたらUIFocusManagerからポップ
            if (UIFocusManager.Instance != null)
            {
                UIFocusManager.Instance.PopUI(this);
            }
        }

        private void OnDestroy()
        {
            // UIFocusManagerから登録解除
            if (UIFocusManager.Instance != null)
            {
                UIFocusManager.Instance.UnregisterUI(this);
            }
        }

        /// <summary>
        /// IFocusableUI実装: 入力の有効/無効を設定
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            isInputEnabled = enabled;

            // すべてのボタンの有効/無効を制御
            if (buttons != null)
            {
                foreach (var button in buttons)
                {
                    if (button != null)
                    {
                        button.interactable = enabled;
                    }
                }
            }
        }
    }
}

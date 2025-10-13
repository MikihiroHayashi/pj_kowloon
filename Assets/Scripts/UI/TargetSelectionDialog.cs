using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

namespace KowloonBreak.UI
{
    /// <summary>
    /// アイテム使用時のターゲット選択ダイアログ
    /// </summary>
    public class TargetSelectionDialog : MonoBehaviour, IFocusableUI
    {
        [Header("UI References")]
        [SerializeField] private GameObject dialogPanel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Transform targetSlotsContainer;
        [SerializeField] private GameObject targetSlotPrefab;
        [SerializeField] private Button cancelButton;

        [Header("Settings")]
        [SerializeField] private float detectionRange = 10f;
        [SerializeField] private Core.CharacterIconData playerIconData;
        [SerializeField] private List<CompanionIconMapping> companionIconMappings = new List<CompanionIconMapping>();

        private List<TargetSelectionSlot> activeSlots = new List<TargetSelectionSlot>();
        private Core.InventorySlot currentItem;
        private bool isInputEnabled = true;

        public event Action<object> OnTargetSelected;
        public event Action OnCancelled;

        // IFocusableUI実装
        public bool IsVisible => dialogPanel != null && dialogPanel.activeSelf;
        public int Priority => 0; // 最高優先度
        public string UIName => "TargetSelectionDialog";

        private void Awake()
        {
            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(OnCancelClicked);
            }

            // 初期状態は非表示
            if (dialogPanel != null)
            {
                dialogPanel.SetActive(false);
            }
        }

        private void Start()
        {
            // UIFocusManagerに登録
            if (UIFocusManager.Instance != null)
            {
                UIFocusManager.Instance.RegisterUI(this);
            }
        }

        /// <summary>
        /// ターゲット選択ダイアログを表示
        /// </summary>
        public void Show(Core.InventorySlot item)
        {
            currentItem = item;

            if (dialogPanel != null)
            {
                dialogPanel.SetActive(true);
            }

            // タイトル設定
            if (titleText != null && item != null)
            {
                titleText.text = $"{item.ItemData.itemName}を使用 - 対象を選択";
            }

            // ターゲットリストを作成
            PopulateTargets();

            // UIContextManagerにプッシュ（TargetSelectionコンテキストに変更）
            var contextManager = Core.UIContextManager.Instance;
            if (contextManager != null)
            {
                contextManager.PushContext(Core.UIContext.TargetSelection);
            }

            // UIFocusManagerにプッシュ（他のUIを自動的に無効化）
            if (UIFocusManager.Instance != null)
            {
                UIFocusManager.Instance.PushUI(this);
            }

            // 最初のスロットにフォーカスを移動
            StartCoroutine(SelectFirstSlot());
        }

        /// <summary>
        /// ダイアログを閉じる
        /// </summary>
        public void Hide()
        {
            Debug.Log("[TargetSelectionDialog] Hide called");

            if (dialogPanel != null)
            {
                dialogPanel.SetActive(false);
            }

            ClearTargetSlots();

            // UIContextManagerからポップ（前のコンテキストに戻る）
            var contextManager = Core.UIContextManager.Instance;
            if (contextManager != null)
            {
                Debug.Log($"[TargetSelectionDialog] PopContext before: {contextManager.CurrentContext}");
                contextManager.PopContext();
                Debug.Log($"[TargetSelectionDialog] PopContext after: {contextManager.CurrentContext}");
            }

            // UIFocusManagerからポップ（他のUIを自動的に有効化）
            if (UIFocusManager.Instance != null)
            {
                UIFocusManager.Instance.PopUI(this);
            }
        }

        /// <summary>
        /// ターゲットリストを生成
        /// </summary>
        private void PopulateTargets()
        {
            ClearTargetSlots();

            // プレイヤーを追加
            var player = Player.EnhancedPlayerController.FindObjectOfType<Player.EnhancedPlayerController>();
            if (player != null)
            {
                CreatePlayerSlot(player);
            }

            // 近くのコンパニオンを検出して追加
            var companions = FindNearbyCompanions();
            foreach (var companion in companions)
            {
                CreateCompanionSlot(companion);
            }
        }

        /// <summary>
        /// プレイヤースロットを作成
        /// </summary>
        private void CreatePlayerSlot(Player.EnhancedPlayerController player)
        {
            GameObject slotObj = CreateSlotObject();
            if (slotObj == null) return;

            TargetSelectionSlot slot = slotObj.GetComponent<TargetSelectionSlot>();
            if (slot != null)
            {
                slot.SetTarget(player, playerIconData);
                slot.OnTargetSelected += OnSlotSelected;
                activeSlots.Add(slot);
            }
        }

        /// <summary>
        /// コンパニオンスロットを作成
        /// </summary>
        private void CreateCompanionSlot(Characters.CompanionAI companion)
        {
            GameObject slotObj = CreateSlotObject();
            if (slotObj == null) return;

            TargetSelectionSlot slot = slotObj.GetComponent<TargetSelectionSlot>();
            if (slot != null)
            {
                // アイコンデータを取得
                Core.CharacterIconData iconData = GetCompanionIconData(companion);
                slot.SetTarget(companion, iconData);
                slot.OnTargetSelected += OnSlotSelected;
                activeSlots.Add(slot);
            }
        }

        /// <summary>
        /// スロットオブジェクトを作成
        /// </summary>
        private GameObject CreateSlotObject()
        {
            if (targetSlotPrefab != null && targetSlotsContainer != null)
            {
                GameObject slotObj = Instantiate(targetSlotPrefab, targetSlotsContainer);
                return slotObj;
            }
            else
            {
                Debug.LogWarning("[TargetSelectionDialog] Target slot prefab or container is not assigned!");
                return null;
            }
        }

        /// <summary>
        /// 近くのコンパニオンを検出
        /// </summary>
        private List<Characters.CompanionAI> FindNearbyCompanions()
        {
            List<Characters.CompanionAI> nearbyCompanions = new List<Characters.CompanionAI>();

            var player = Player.EnhancedPlayerController.FindObjectOfType<Player.EnhancedPlayerController>();
            if (player == null) return nearbyCompanions;

            var allCompanions = FindObjectsOfType<Characters.CompanionAI>();
            foreach (var companion in allCompanions)
            {
                if (companion == null) continue;

                float distance = Vector3.Distance(player.transform.position, companion.transform.position);
                if (distance <= detectionRange)
                {
                    nearbyCompanions.Add(companion);
                }
            }

            return nearbyCompanions;
        }

        /// <summary>
        /// コンパニオンのアイコンデータを取得
        /// </summary>
        private Core.CharacterIconData GetCompanionIconData(Characters.CompanionAI companion)
        {
            // CompanionのGameObject名やIDでマッピングを探す
            foreach (var mapping in companionIconMappings)
            {
                if (mapping.companion == companion || mapping.companionName == companion.name)
                {
                    return mapping.iconData;
                }
            }

            return null; // アイコンデータが見つからない場合
        }

        /// <summary>
        /// スロットが選択された時の処理
        /// </summary>
        private void OnSlotSelected(object target)
        {
            OnTargetSelected?.Invoke(target);
            Hide();
        }

        /// <summary>
        /// キャンセルボタンが押された時の処理
        /// </summary>
        private void OnCancelClicked()
        {
            OnCancelled?.Invoke();
            Hide();
        }

        /// <summary>
        /// すべてのターゲットスロットをクリア
        /// </summary>
        private void ClearTargetSlots()
        {
            foreach (var slot in activeSlots)
            {
                if (slot != null)
                {
                    slot.OnTargetSelected -= OnSlotSelected;
                    Destroy(slot.gameObject);
                }
            }
            activeSlots.Clear();
        }

        private void OnDestroy()
        {
            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(OnCancelClicked);
            }

            ClearTargetSlots();

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

            // キャンセルボタンの有効/無効を制御
            if (cancelButton != null)
            {
                cancelButton.interactable = enabled;
            }

            // すべてのターゲットスロットの有効/無効を制御
            foreach (var slot in activeSlots)
            {
                if (slot != null)
                {
                    var button = slot.GetComponent<Button>();
                    if (button != null)
                    {
                        button.interactable = enabled;
                    }
                }
            }
        }

        /// <summary>
        /// 最初のスロットを選択（1フレーム遅延させる）
        /// </summary>
        private IEnumerator SelectFirstSlot()
        {
            // UIが完全に構築されるまで1フレーム待つ
            yield return null;

            if (activeSlots.Count > 0 && activeSlots[0] != null)
            {
                EventSystem eventSystem = EventSystem.current;
                if (eventSystem != null)
                {
                    GameObject firstSlot = activeSlots[0].gameObject;
                    eventSystem.SetSelectedGameObject(firstSlot);
                }
            }
        }

        /// <summary>
        /// コンパニオンとアイコンのマッピング
        /// </summary>
        [System.Serializable]
        public class CompanionIconMapping
        {
            public Characters.CompanionAI companion;
            public string companionName;
            public Core.CharacterIconData iconData;
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

namespace KowloonBreak.UI
{
    /// <summary>
    /// ターゲット選択UIの1スロット（キャラクター1人分）
    /// </summary>
    public class TargetSelectionSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [Header("UI References")]
        [SerializeField] private Image characterIcon;
        [SerializeField] private TextMeshProUGUI characterNameText;
        [SerializeField] private Slider healthBar;
        [SerializeField] private Slider infectionBar;
        [SerializeField] private Button selectButton;
        [SerializeField] private TextMeshProUGUI healthPercentText;
        [SerializeField] private TextMeshProUGUI infectionPercentText;
        [SerializeField] private Animator animator;

        private object targetCharacter; // PlayerController or CompanionAI
        public event Action<object> OnTargetSelected;

        private static readonly int FocusHash = Animator.StringToHash("Focus");

        private void Awake()
        {
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(OnSelectButtonClicked);
            }
        }

        /// <summary>
        /// プレイヤーをターゲットとして設定
        /// </summary>
        public void SetTarget(Player.EnhancedPlayerController player, Core.CharacterIconData iconData)
        {
            if (player == null) return;

            targetCharacter = player;

            // キャラクター名
            if (characterNameText != null)
            {
                characterNameText.text = iconData != null ? iconData.characterName : "プレイヤー";
            }

            // アイコン
            if (characterIcon != null && iconData != null && iconData.iconSprite != null)
            {
                characterIcon.sprite = iconData.iconSprite;
                characterIcon.enabled = true;
            }
            else if (characterIcon != null)
            {
                characterIcon.enabled = false;
            }

            // HPバー
            if (healthBar != null)
            {
                healthBar.value = player.HealthPercentage;
                if (iconData != null && healthBar.fillRect != null)
                {
                    healthBar.fillRect.GetComponent<Image>().color = iconData.healthBarColor;
                }
            }

            if (healthPercentText != null)
            {
                healthPercentText.text = $"{Mathf.RoundToInt(player.HealthPercentage * 100)}%";
            }

            // 感染バー
            if (infectionBar != null)
            {
                infectionBar.value = player.InfectionLevel / 100f;
                if (iconData != null && infectionBar.fillRect != null)
                {
                    infectionBar.fillRect.GetComponent<Image>().color = iconData.infectionBarColor;
                }
            }

            if (infectionPercentText != null)
            {
                infectionPercentText.text = $"{Mathf.RoundToInt(player.InfectionLevel)}%";
            }
        }

        /// <summary>
        /// コンパニオンをターゲットとして設定
        /// </summary>
        public void SetTarget(Characters.CompanionAI companion, Core.CharacterIconData iconData)
        {
            if (companion == null) return;

            targetCharacter = companion;

            // キャラクター名
            if (characterNameText != null)
            {
                characterNameText.text = iconData != null ? iconData.characterName : companion.name;
            }

            // アイコン
            if (characterIcon != null && iconData != null && iconData.iconSprite != null)
            {
                characterIcon.sprite = iconData.iconSprite;
                characterIcon.enabled = true;
            }
            else if (characterIcon != null)
            {
                characterIcon.enabled = false;
            }

            // HPバー（CompanionAIから直接取得）
            float healthPercentage = companion.CurrentHealth / companion.MaxHealth;
            if (healthBar != null)
            {
                healthBar.value = healthPercentage;
                if (iconData != null && healthBar.fillRect != null)
                {
                    healthBar.fillRect.GetComponent<Image>().color = iconData.healthBarColor;
                }
            }

            if (healthPercentText != null)
            {
                healthPercentText.text = $"{Mathf.RoundToInt(healthPercentage * 100)}%";
            }

            // 感染バー（CompanionCharacterから取得）
            float infectionLevel = 0f;
            var companionCharacter = companion.GetComponent<Characters.CompanionCharacter>();
            if (companionCharacter != null && companionCharacter.Infection != null)
            {
                infectionLevel = companionCharacter.Infection.CurrentInfection;
            }

            if (infectionBar != null)
            {
                infectionBar.value = infectionLevel / 100f;
                if (iconData != null && infectionBar.fillRect != null)
                {
                    infectionBar.fillRect.GetComponent<Image>().color = iconData.infectionBarColor;
                }
            }

            if (infectionPercentText != null)
            {
                infectionPercentText.text = $"{Mathf.RoundToInt(infectionLevel)}%";
            }
        }

        private void OnSelectButtonClicked()
        {
            OnTargetSelected?.Invoke(targetCharacter);
        }

        private void OnDestroy()
        {
            if (selectButton != null)
            {
                selectButton.onClick.RemoveListener(OnSelectButtonClicked);
            }
        }

        /// <summary>
        /// マウスホバー時の処理
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            SetFocus(true);
        }

        /// <summary>
        /// マウスホバー解除時の処理
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            SetFocus(false);
        }

        /// <summary>
        /// ナビゲーション選択時の処理
        /// </summary>
        public void OnSelect(BaseEventData eventData)
        {
            SetFocus(true);
        }

        /// <summary>
        /// ナビゲーション選択解除時の処理
        /// </summary>
        public void OnDeselect(BaseEventData eventData)
        {
            SetFocus(false);
        }

        /// <summary>
        /// フォーカス状態を設定
        /// </summary>
        private void SetFocus(bool isFocused)
        {
            if (animator != null)
            {
                animator.SetBool(FocusHash, isFocused);
            }
        }
    }
}

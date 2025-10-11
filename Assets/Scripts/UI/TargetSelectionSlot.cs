using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace KowloonBreak.UI
{
    /// <summary>
    /// ターゲット選択UIの1スロット（キャラクター1人分）
    /// </summary>
    public class TargetSelectionSlot : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image characterIcon;
        [SerializeField] private TextMeshProUGUI characterNameText;
        [SerializeField] private Image healthBar;
        [SerializeField] private Image infectionBar;
        [SerializeField] private Button selectButton;
        [SerializeField] private TextMeshProUGUI healthPercentText;
        [SerializeField] private TextMeshProUGUI infectionPercentText;

        private object targetCharacter; // PlayerController or CompanionAI
        public event Action<object> OnTargetSelected;

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
                healthBar.fillAmount = player.HealthPercentage;
                if (iconData != null)
                {
                    healthBar.color = iconData.healthBarColor;
                }
            }

            if (healthPercentText != null)
            {
                healthPercentText.text = $"{Mathf.RoundToInt(player.HealthPercentage * 100)}%";
            }

            // 感染バー
            if (infectionBar != null)
            {
                infectionBar.fillAmount = player.InfectionLevel / 100f;
                if (iconData != null)
                {
                    infectionBar.color = iconData.infectionBarColor;
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
                healthBar.fillAmount = healthPercentage;
                if (iconData != null)
                {
                    healthBar.color = iconData.healthBarColor;
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
                infectionBar.fillAmount = infectionLevel / 100f;
                if (iconData != null)
                {
                    infectionBar.color = iconData.infectionBarColor;
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
    }
}

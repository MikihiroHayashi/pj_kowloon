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

        [Header("Tween Settings")]
        [SerializeField] private float barTweenDuration = 0.25f; // 悪化方向
        [SerializeField] private float barRecoveryTweenDuration = 1.0f; // 回復方向

        private Coroutine healthTweenCo;
        private Coroutine infectionTweenCo;

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

            // HPバー（ダイアログ表示時は即時反映）
            if (healthBar != null)
            {
                if (iconData != null && healthBar.fillRect != null)
                {
                    healthBar.fillRect.GetComponent<Image>().color = iconData.healthBarColor;
                }
                float hp = player.HealthPercentage;
                healthBar.value = hp;
                if (healthPercentText != null) healthPercentText.text = $"{Mathf.RoundToInt(hp * 100)}%";
            }

            // 感染バー（ダイアログ表示時は即時反映）
            if (infectionBar != null)
            {
                if (iconData != null && infectionBar.fillRect != null)
                {
                    infectionBar.fillRect.GetComponent<Image>().color = iconData.infectionBarColor;
                }
                float inf = player.InfectionLevel / 100f;
                infectionBar.value = inf;
                if (infectionPercentText != null) infectionPercentText.text = $"{Mathf.RoundToInt(inf * 100)}%";
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
                if (iconData != null && healthBar.fillRect != null)
                {
                    healthBar.fillRect.GetComponent<Image>().color = iconData.healthBarColor;
                }
                healthBar.value = healthPercentage;
                if (healthPercentText != null) healthPercentText.text = $"{Mathf.RoundToInt(healthPercentage * 100)}%";
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
                if (iconData != null && infectionBar.fillRect != null)
                {
                    infectionBar.fillRect.GetComponent<Image>().color = iconData.infectionBarColor;
                }
                float inf = infectionLevel / 100f;
                infectionBar.value = inf;
                if (infectionPercentText != null) infectionPercentText.text = $"{Mathf.RoundToInt(inf * 100)}%";
            }
        }

        private void OnSelectButtonClicked()
        {
            OnTargetSelected?.Invoke(targetCharacter);
        }

        public void SetInteractable(bool interactable)
        {
            if (selectButton != null)
            {
                selectButton.interactable = interactable;
            }
            if (!interactable && animator != null)
            {
                animator.SetTrigger("Disable");
            }
        }

        private void OnDestroy()
        {
            if (selectButton != null)
            {
                selectButton.onClick.RemoveListener(OnSelectButtonClicked);
            }
        }

        public void RefreshFromTarget()
        {
            if (targetCharacter is Player.EnhancedPlayerController player)
            {
                TweenHealth(player.HealthPercentage);
                TweenInfection(player.InfectionLevel / 100f);
            }
            else if (targetCharacter is Characters.CompanionAI companion)
            {
                float healthPercentage = companion.CurrentHealth / companion.MaxHealth;
                TweenHealth(healthPercentage);

                float infectionLevel = 0f;
                var compChar = companion.GetComponent<Characters.CompanionCharacter>();
                if (compChar != null && compChar.Infection != null)
                {
                    infectionLevel = compChar.Infection.CurrentInfection;
                }
                TweenInfection(infectionLevel / 100f);
            }
        }

        private void TweenHealth(float target)
        {
            if (healthBar == null)
            {
                if (healthPercentText != null)
                    healthPercentText.text = $"{Mathf.RoundToInt(target * 100)}%";
                return;
            }
            float current = healthBar.value;
            float duration = target > current ? barRecoveryTweenDuration : barTweenDuration;
            if (healthTweenCo != null) StopCoroutine(healthTweenCo);
            healthTweenCo = StartCoroutine(TweenSlider(healthBar, target, duration, (v) =>
            {
                if (healthPercentText != null) healthPercentText.text = $"{Mathf.RoundToInt(v * 100)}%";
            }));
        }

        private void TweenInfection(float target)
        {
            if (infectionBar == null)
            {
                if (infectionPercentText != null)
                    infectionPercentText.text = $"{Mathf.RoundToInt(target * 100)}%";
                return;
            }
            float current = infectionBar.value;
            float duration = target < current ? barRecoveryTweenDuration : barTweenDuration; // 減少=回復
            if (infectionTweenCo != null) StopCoroutine(infectionTweenCo);
            infectionTweenCo = StartCoroutine(TweenSlider(infectionBar, target, duration, (v) =>
            {
                if (infectionPercentText != null) infectionPercentText.text = $"{Mathf.RoundToInt(v * 100)}%";
            }));
        }

        private System.Collections.IEnumerator TweenSlider(Slider slider, float target, float duration, Action<float> onProgress)
        {
            float start = slider.value;
            if (Mathf.Approximately(start, target) || duration <= 0f)
            {
                slider.value = target;
                onProgress?.Invoke(target);
                yield break;
            }
            float t = 0f;
            while (t < duration)
            {
                // ポーズ中でもUIは進行させる
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                float v = Mathf.Lerp(start, target, p);
                slider.value = v;
                onProgress?.Invoke(v);
                yield return null;
            }
            slider.value = target;
            onProgress?.Invoke(target);
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

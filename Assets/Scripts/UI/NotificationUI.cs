using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace KowloonBreak.UI
{
    public class NotificationUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Animation Settings")]
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.5f;
        [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [Header("Type Colors")]
        [SerializeField] private Color infoColor = Color.blue;
        [SerializeField] private Color warningColor = Color.yellow;
        [SerializeField] private Color errorColor = Color.red;
        [SerializeField] private Color successColor = Color.green;

        [Header("Type Icons")]
        [SerializeField] private Sprite infoIcon;
        [SerializeField] private Sprite warningIcon;
        [SerializeField] private Sprite errorIcon;
        [SerializeField] private Sprite successIcon;

        private NotificationType currentType;
        private float displayDuration;
        private Coroutine displayCoroutine;

        public void Setup(string message, NotificationType type, float duration)
        {
            currentType = type;
            displayDuration = duration;
            
            SetupMessage(message);
            SetupAppearance(type);
            
            if (displayCoroutine != null)
            {
                StopCoroutine(displayCoroutine);
            }
            
            displayCoroutine = StartCoroutine(DisplaySequence());
        }

        private void SetupMessage(string message)
        {
            if (messageText != null)
            {
                messageText.text = message;
            }
        }

        private void SetupAppearance(NotificationType type)
        {
            Color typeColor = GetTypeColor(type);
            Sprite typeIcon = GetTypeIcon(type);
            
            if (backgroundImage != null)
            {
                backgroundImage.color = new Color(typeColor.r, typeColor.g, typeColor.b, 0.3f);
            }
            
            if (iconImage != null && typeIcon != null)
            {
                iconImage.sprite = typeIcon;
                iconImage.color = typeColor;
            }
            
            if (messageText != null)
            {
                messageText.color = Color.white;
            }
        }

        private Color GetTypeColor(NotificationType type)
        {
            return type switch
            {
                NotificationType.Info => infoColor,
                NotificationType.Warning => warningColor,
                NotificationType.Error => errorColor,
                NotificationType.Success => successColor,
                _ => infoColor
            };
        }

        private Sprite GetTypeIcon(NotificationType type)
        {
            return type switch
            {
                NotificationType.Info => infoIcon,
                NotificationType.Warning => warningIcon,
                NotificationType.Error => errorIcon,
                NotificationType.Success => successIcon,
                _ => infoIcon
            };
        }

        private IEnumerator DisplaySequence()
        {
            yield return StartCoroutine(FadeIn());
            yield return new WaitForSeconds(displayDuration);
            yield return StartCoroutine(FadeOut());
            
            Destroy(gameObject);
        }

        private IEnumerator FadeIn()
        {
            if (canvasGroup == null) yield break;
            
            float elapsedTime = 0f;
            canvasGroup.alpha = 0f;
            
            while (elapsedTime < fadeInDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / fadeInDuration;
                canvasGroup.alpha = fadeInCurve.Evaluate(progress);
                yield return null;
            }
            
            canvasGroup.alpha = 1f;
        }

        private IEnumerator FadeOut()
        {
            if (canvasGroup == null) yield break;
            
            float elapsedTime = 0f;
            canvasGroup.alpha = 1f;
            
            while (elapsedTime < fadeOutDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / fadeOutDuration;
                canvasGroup.alpha = fadeOutCurve.Evaluate(progress);
                yield return null;
            }
            
            canvasGroup.alpha = 0f;
        }

        public void ForceClose()
        {
            if (displayCoroutine != null)
            {
                StopCoroutine(displayCoroutine);
            }
            
            StartCoroutine(FadeOut());
        }

        private void OnDestroy()
        {
            if (displayCoroutine != null)
            {
                StopCoroutine(displayCoroutine);
            }
        }
    }
}
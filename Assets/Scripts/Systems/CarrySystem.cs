using System;
using UnityEngine;
using KowloonBreak.Characters;
using KowloonBreak.Player;
using KowloonBreak.UI;

namespace KowloonBreak.Systems
{
    public class CarrySystem : MonoBehaviour
    {
        public static CarrySystem Instance { get; private set; }

        [Header("Carry Settings")]
        [SerializeField] private Transform carryPoint; // プレイヤーの背負いポイント
        [SerializeField] private float dropDistance = 2f; // 降ろす時の距離
        [SerializeField] private LayerMask groundLayerMask = 1; // 地面判定用

        private bool isCarrying = false;
        private CompanionCharacter carriedCharacter;
        private EnhancedPlayerController playerController;
        private Vector3 originalCarriedPosition;
        private bool originalCarriedActiveState;

        public bool IsCarrying => isCarrying;
        public CompanionCharacter CarriedCharacter => carriedCharacter;

        public event Action<CompanionCharacter> OnStartCarrying;
        public event Action<CompanionCharacter> OnStopCarrying;

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
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            InitializeCarrySystem();
        }

        private void InitializeCarrySystem()
        {
            // プレイヤーコントローラーを取得
            playerController = FindObjectOfType<EnhancedPlayerController>();
            if (playerController == null)
            {
                Debug.LogError("CarrySystem: EnhancedPlayerController not found!");
                return;
            }

            // キャリーポイントが設定されていない場合はプレイヤーの子オブジェクトとして作成
            if (carryPoint == null)
            {
                GameObject carryPointObj = new GameObject("CarryPoint");
                carryPointObj.transform.SetParent(playerController.transform);
                carryPointObj.transform.localPosition = new Vector3(0f, 1.5f, -0.5f); // プレイヤーの背中
                carryPoint = carryPointObj.transform;
            }
        }

        private void Update()
        {
            HandleCarryInput();
            UpdateCarriedCharacter();
        }

        private void HandleCarryInput()
        {
            // Qキーで降ろす（インベントリ開時は無効）
            var inputManager = KowloonBreak.Core.InputManager.Instance;
            bool inventoryOpen = inputManager != null && inputManager.IsInventoryOpen();

            if (!inventoryOpen && Input.GetKeyDown(KeyCode.Q) && isCarrying)
            {
                StopCarrying();
            }
        }

        private void UpdateCarriedCharacter()
        {
            if (isCarrying && carriedCharacter != null && carryPoint != null)
            {
                // 背負っているキャラクターの位置をプレイヤーに追従させる
                carriedCharacter.transform.position = carryPoint.position;
                carriedCharacter.transform.rotation = carryPoint.rotation;
            }
        }

        public bool StartCarrying(CompanionCharacter character)
        {
            if (isCarrying || character == null || !character.Infection.IsInfected)
            {
                Debug.LogWarning("Cannot start carrying: already carrying someone or character is not infected");
                return false;
            }

            if (playerController == null)
            {
                Debug.LogError("PlayerController not found!");
                return false;
            }

            // キャラクターの状態を保存
            originalCarriedPosition = character.transform.position;
            originalCarriedActiveState = character.gameObject.activeSelf;

            // キャラクターを非表示にして位置を設定
            character.gameObject.SetActive(false);
            carriedCharacter = character;
            isCarrying = true;

            // プレイヤーのCarry状態を有効化
            SetPlayerCarryState(true);

            OnStartCarrying?.Invoke(character);

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowNotification($"{character.Name}を背負いました (Qキーで降ろす)", NotificationType.Info);
            }

            Debug.Log($"Started carrying {character.Name}");
            return true;
        }

        public void StopCarrying()
        {
            if (!isCarrying || carriedCharacter == null)
            {
                Debug.LogWarning("Cannot stop carrying: not currently carrying anyone");
                return;
            }

            // 降ろす位置を計算
            Vector3 dropPosition = CalculateDropPosition();

            // キャラクターを表示して位置を設定
            carriedCharacter.transform.position = dropPosition;
            carriedCharacter.gameObject.SetActive(true);

            // イベント通知
            CompanionCharacter droppedCharacter = carriedCharacter;
            OnStopCarrying?.Invoke(droppedCharacter);

            // 状態をリセット
            carriedCharacter = null;
            isCarrying = false;

            // プレイヤーのCarry状態を無効化
            SetPlayerCarryState(false);

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowNotification($"{droppedCharacter.Name}を降ろしました", NotificationType.Info);
            }

            Debug.Log($"Stopped carrying {droppedCharacter.Name}");
        }

        private Vector3 CalculateDropPosition()
        {
            if (playerController == null) return transform.position;

            // プレイヤーの前方に降ろす
            Vector3 forwardDirection = playerController.transform.forward;
            Vector3 dropPosition = playerController.transform.position + forwardDirection * dropDistance;

            // 地面との当たり判定
            RaycastHit hit;
            if (Physics.Raycast(dropPosition + Vector3.up * 2f, Vector3.down, out hit, 5f, groundLayerMask))
            {
                dropPosition.y = hit.point.y;
            }
            else
            {
                dropPosition.y = playerController.transform.position.y;
            }

            return dropPosition;
        }

        private void SetPlayerCarryState(bool carrying)
        {
            if (playerController == null) return;

            // プレイヤーのAnimatorでCarryパラメータを設定
            var animator = playerController.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetBool("Carry", carrying);
            }

            // プレイヤーの行動制限を設定
            SetPlayerActionRestrictions(carrying);
        }

        private void SetPlayerActionRestrictions(bool restricted)
        {
            if (playerController == null) return;

            // プレイヤーコントローラーにキャリー状態を設定
            playerController.SetCarryingState(restricted);

            Debug.Log($"Player action restrictions set to: {restricted}");
        }

        public bool CanCarryCharacter(CompanionCharacter character)
        {
            if (isCarrying) return false;
            if (character == null) return false;
            if (!character.Infection.IsInfected) return false;

            return true;
        }

        public void ForceStopCarrying()
        {
            if (isCarrying)
            {
                StopCarrying();
            }
        }

        private void OnDestroy()
        {
            if (isCarrying && carriedCharacter != null)
            {
                // システム破棄時にキャラクターを元の状態に戻す
                carriedCharacter.transform.position = originalCarriedPosition;
                carriedCharacter.gameObject.SetActive(originalCarriedActiveState);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (playerController != null)
            {
                // 降ろす位置の可視化
                Vector3 dropPos = playerController.transform.position + playerController.transform.forward * dropDistance;
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(dropPos, 0.5f);

                // キャリーポイントの可視化
                if (carryPoint != null)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(carryPoint.position, 0.3f);
                }
            }
        }
    }
}
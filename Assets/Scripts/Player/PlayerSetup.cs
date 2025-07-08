using UnityEngine;

namespace KowloonBreak.Player
{
    [System.Serializable]
    public class PlayerSetupData
    {
        [Header("Player Configuration")]
        public string playerName = "Player";
        public Vector3 spawnPosition = Vector3.zero;
        public Vector3 spawnRotation = Vector3.zero;

        [Header("Controller Settings")]
        public float walkSpeed = 3f;
        public float runSpeed = 6f;
        public float crouchSpeed = 1.5f;
        public float maxStamina = 100f;
        public float staminaRegenRate = 20f;
        public float runStaminaCost = 30f;

        [Header("Camera Settings")]
        public Vector3 cameraOffset = new Vector3(0f, 8f, -10f);
        public float cameraRotationX = 30f;
        public float followDamping = 1f;

        [Header("Interaction Settings")]
        public float interactionRange = 3f;
        public KeyCode interactionKey = KeyCode.E;
        public KeyCode flashlightKey = KeyCode.F;

        [Header("Audio Settings")]
        public float footstepInterval = 0.5f;
    }

    public class PlayerSetup : MonoBehaviour
    {
        [Header("Setup Configuration")]
        [SerializeField] private PlayerSetupData setupData = new PlayerSetupData();
        [SerializeField] private bool autoSetupOnStart = true;
        [SerializeField] private bool createCinemachineCamera = true;
        [SerializeField] private bool useBillboard = true;
        [SerializeField] private bool showDirectionIndicator = true;

        private GameObject playerObject;
        private EnhancedPlayerController playerController;
        private CharacterController characterController;
        private AudioSource audioSource;

        private void Start()
        {
            if (autoSetupOnStart)
            {
                SetupPlayer();
            }
        }

        [ContextMenu("Setup Player")]
        public void SetupPlayer()
        {
            CreatePlayerObject();
            SetupPlayerComponents();
            SetupCameraSystem();
            ApplySettings();
            
            Debug.Log("Player setup completed!");
        }

        private void CreatePlayerObject()
        {
            // 既存のPlayerオブジェクトを探す
            playerObject = GameObject.FindGameObjectWithTag("Player");
            
            if (playerObject == null)
            {
                // 新しいPlayerオブジェクトを作成
                playerObject = new GameObject(setupData.playerName);
                playerObject.tag = "Player";
                playerObject.layer = 0; // Default layer
            }

            // 位置と回転を設定
            playerObject.transform.position = setupData.spawnPosition;
            playerObject.transform.rotation = Quaternion.Euler(setupData.spawnRotation);
        }

        private void SetupPlayerComponents()
        {
            // CharacterController
            characterController = playerObject.GetComponent<CharacterController>();
            if (characterController == null)
            {
                characterController = playerObject.AddComponent<CharacterController>();
                characterController.radius = 0.5f;
                characterController.height = 2f;
                characterController.center = new Vector3(0, 1f, 0);
            }

            // AudioSource
            audioSource = playerObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = playerObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f; // 3D sound
            }

            // EnhancedPlayerController
            playerController = playerObject.GetComponent<EnhancedPlayerController>();
            if (playerController == null)
            {
                playerController = playerObject.AddComponent<EnhancedPlayerController>();
            }

            // PlayerDirectionIndicator
            if (showDirectionIndicator)
            {
                var directionIndicator = playerObject.GetComponent<PlayerDirectionIndicator>();
                if (directionIndicator == null)
                {
                    directionIndicator = playerObject.AddComponent<PlayerDirectionIndicator>();
                }
            }

            // PlayerAnimatorController
            var animatorController = playerObject.GetComponent<PlayerAnimatorController>();
            if (animatorController == null)
            {
                animatorController = playerObject.AddComponent<PlayerAnimatorController>();
            }

            // Capsule mesh for visualization (optional)
            SetupPlayerVisuals();
        }

        private void SetupPlayerVisuals()
        {
            // プレイヤーの見た目用のオブジェクトを作成
            Transform visualsTransform = playerObject.transform.Find("PlayerVisuals");
            if (visualsTransform == null)
            {
                GameObject visualsGO;
                
                if (useBillboard)
                {
                    // 板ポリ（Quad）を作成
                    visualsGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    visualsGO.name = "PlayerVisuals";
                    visualsGO.transform.SetParent(playerObject.transform);
                    visualsGO.transform.localPosition = new Vector3(0, 1f, 0);
                    visualsGO.transform.localScale = new Vector3(1f, 2f, 1f); // 人型に近いサイズ
                    
                    // Billboardコンポーネントを追加
                    var billboard = visualsGO.AddComponent<Billboard>();
                    billboard.SetLockY(true); // Y軸をロックして上下に傾かないようにする
                }
                else
                {
                    // 通常のCapsuleを作成
                    visualsGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    visualsGO.name = "PlayerVisuals";
                    visualsGO.transform.SetParent(playerObject.transform);
                    visualsGO.transform.localPosition = new Vector3(0, 1f, 0);
                    visualsGO.transform.localScale = Vector3.one;
                }

                // Colliderを削除（CharacterControllerを使用するため）
                if (Application.isPlaying)
                {
                    Destroy(visualsGO.GetComponent<Collider>());
                }
                else
                {
                    DestroyImmediate(visualsGO.GetComponent<Collider>());
                }

                // マテリアルを設定
                var renderer = visualsGO.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = useBillboard ? Color.red : Color.blue;
                }
            }
        }

        private void SetupCameraSystem()
        {
            if (!createCinemachineCamera) return;

            // Camera Follow Target作成
            Transform cameraTarget = playerObject.transform.Find("CameraFollowTarget");
            if (cameraTarget == null)
            {
                GameObject targetGO = new GameObject("CameraFollowTarget");
                targetGO.transform.SetParent(playerObject.transform);
                targetGO.transform.localPosition = Vector3.zero;
                cameraTarget = targetGO.transform;

                // Cinemachine Setup
                var cinemachineSetup = targetGO.AddComponent<KowloonBreak.Camera.CinemachineSetup>();
                cinemachineSetup.FollowTarget = playerObject.transform;
            }
        }

        private void ApplySettings()
        {
            if (playerController == null) return;

            // プライベートフィールドにアクセスするためのReflectionは避けて、
            // 設定可能なプロパティを使用
            
            // プレイヤーコントローラーの設定をInspectorで確認・設定する必要があります
            Debug.Log($"Player settings applied. Please configure the following in Inspector:");
            Debug.Log($"- Walk Speed: {setupData.walkSpeed}");
            Debug.Log($"- Run Speed: {setupData.runSpeed}");
            Debug.Log($"- Crouch Speed: {setupData.crouchSpeed}");
            Debug.Log($"- Max Stamina: {setupData.maxStamina}");
            Debug.Log($"- Interaction Range: {setupData.interactionRange}");
        }

        public void SetPlayerPosition(Vector3 position)
        {
            setupData.spawnPosition = position;
            if (playerObject != null)
            {
                playerObject.transform.position = position;
            }
        }

        public void SetCameraOffset(Vector3 offset)
        {
            setupData.cameraOffset = offset;
            
            var cameraSetup = GetComponentInChildren<KowloonBreak.Camera.CinemachineSetup>();
            if (cameraSetup != null)
            {
                cameraSetup.SetCameraOffset(offset);
            }
        }

        public GameObject GetPlayerObject()
        {
            return playerObject;
        }

        public EnhancedPlayerController GetPlayerController()
        {
            return playerController;
        }

        [ContextMenu("Remove Player")]
        public void RemovePlayer()
        {
            if (playerObject != null)
            {
                DestroyImmediate(playerObject);
                playerObject = null;
                playerController = null;
                Debug.Log("Player removed");
            }
        }
    }
}
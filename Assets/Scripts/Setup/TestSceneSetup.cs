using UnityEngine;
using KowloonBreak.Environment;
using KowloonBreak.Player;
using KowloonBreak.Managers;
using KowloonBreak.Core;

namespace KowloonBreak.Setup
{
    public class TestSceneSetup : MonoBehaviour
    {
        [Header("Test Scene Setup")]
        [SerializeField] private bool autoSetupOnStart = true;
        [SerializeField] private Vector3 playerSpawnPosition = Vector3.zero;
        [SerializeField] private int numberOfTestBoxes = 5;
        [SerializeField] private float boxSpacing = 3f;
        
        private void Start()
        {
            if (autoSetupOnStart)
            {
                SetupTestScene();
            }
        }
        
        [ContextMenu("Setup Test Scene")]
        public void SetupTestScene()
        {
            Debug.Log("Setting up test scene for item drop system...");
            
            // プレイヤーの設定
            SetupPlayer();
            
            // 地面の作成
            CreateGround();
            
            // テスト用の破壊可能オブジェクトを配置
            CreateTestBoxes();
            
            // ライティング設定
            SetupLighting();
            
            // カメラ設定
            SetupCamera();
            
            // ゲームマネージャーの確認
            SetupGameManager();
            
            Debug.Log("Test scene setup completed!");
        }
        
        private void SetupPlayer()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                Debug.Log("[TestSceneSetup] Creating player object...");
                player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.name = "Player";
                player.tag = "Player";
                player.transform.position = playerSpawnPosition;
                
                Debug.Log($"[TestSceneSetup] Player created with tag: {player.tag}");
                
                // プレイヤーコントローラーを追加
                EnhancedPlayerController playerController = player.AddComponent<EnhancedPlayerController>();
                
                // 必要なコンポーネントを追加
                Rigidbody playerRb = player.GetComponent<Rigidbody>();
                if (playerRb == null)
                {
                    playerRb = player.AddComponent<Rigidbody>();
                }
                playerRb.constraints = RigidbodyConstraints.FreezeRotation;
                
                // プレイヤーのマテリアル
                Renderer playerRenderer = player.GetComponent<Renderer>();
                Material playerMat = new Material(Shader.Find("Standard"));
                playerMat.color = Color.blue;
                playerRenderer.material = playerMat;
                
                Debug.Log($"[TestSceneSetup] Player setup complete. Position: {player.transform.position}");
            }
            else
            {
                player.transform.position = playerSpawnPosition;
                Debug.Log($"[TestSceneSetup] Player found and repositioned. Tag: {player.tag}");
            }
        }
        
        private void CreateGround()
        {
            GameObject ground = GameObject.Find("Ground");
            if (ground == null)
            {
                Debug.Log("Creating ground...");
                ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Ground";
                ground.transform.position = Vector3.zero;
                ground.transform.localScale = new Vector3(5, 1, 5);
                
                // 地面のマテリアル
                Renderer groundRenderer = ground.GetComponent<Renderer>();
                Material groundMat = new Material(Shader.Find("Standard"));
                groundMat.color = Color.gray;
                groundRenderer.material = groundMat;
                
                // 地面を Ground レイヤーに設定
                ground.layer = LayerMask.NameToLayer("Default");
            }
        }
        
        private void CreateTestBoxes()
        {
            Debug.Log($"Creating {numberOfTestBoxes} test boxes...");
            
            // 既存のテストボックスを削除
            TestDestructibleBox[] existingBoxes = FindObjectsOfType<TestDestructibleBox>();
            for (int i = 0; i < existingBoxes.Length; i++)
            {
                if (Application.isPlaying)
                {
                    Destroy(existingBoxes[i].gameObject);
                }
                else
                {
                    DestroyImmediate(existingBoxes[i].gameObject);
                }
            }
            
            // 新しいテストボックスを作成
            for (int i = 0; i < numberOfTestBoxes; i++)
            {
                GameObject testBox = new GameObject($"TestBox_{i + 1}");
                testBox.transform.position = new Vector3(
                    (i - (numberOfTestBoxes - 1) * 0.5f) * boxSpacing,
                    1f,
                    5f
                );
                
                TestDestructibleBox boxComponent = testBox.AddComponent<TestDestructibleBox>();
                
                Debug.Log($"Created {testBox.name} at {testBox.transform.position}");
            }
        }
        
        private void SetupLighting()
        {
            GameObject sun = GameObject.Find("Directional Light");
            if (sun == null)
            {
                Debug.Log("Creating directional light...");
                sun = new GameObject("Directional Light");
                Light light = sun.AddComponent<Light>();
                light.type = LightType.Directional;
                light.color = Color.white;
                light.intensity = 1f;
                sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }
        }
        
        private void SetupCamera()
        {
            UnityEngine.Camera mainCamera = UnityEngine.Camera.main;
            if (mainCamera == null)
            {
                Debug.Log("Creating main camera...");
                GameObject cameraObj = new GameObject("Main Camera");
                mainCamera = cameraObj.AddComponent<UnityEngine.Camera>();
                cameraObj.tag = "MainCamera";
            }
            
            // カメラ位置の設定
            mainCamera.transform.position = new Vector3(0f, 8f, -10f);
            mainCamera.transform.rotation = Quaternion.Euler(30f, 0f, 0f);
        }
        
        private void SetupGameManager()
        {
            // GameManagerの確認
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager == null)
            {
                Debug.Log("Creating GameManager...");
                GameObject gameManagerObj = new GameObject("GameManager");
                gameManager = gameManagerObj.AddComponent<GameManager>();
            }
            
            // EnhancedResourceManagerの確認
            EnhancedResourceManager resourceManager = FindObjectOfType<EnhancedResourceManager>();
            if (resourceManager == null)
            {
                Debug.Log("Creating EnhancedResourceManager...");
                GameObject resourceManagerObj = new GameObject("ResourceManager");
                resourceManager = resourceManagerObj.AddComponent<EnhancedResourceManager>();
            }
        }
        
        [ContextMenu("Clear Test Scene")]
        public void ClearTestScene()
        {
            Debug.Log("Clearing test scene...");
            
            // テストボックスを削除
            TestDestructibleBox[] testBoxes = FindObjectsOfType<TestDestructibleBox>();
            for (int i = 0; i < testBoxes.Length; i++)
            {
                if (Application.isPlaying)
                {
                    Destroy(testBoxes[i].gameObject);
                }
                else
                {
                    DestroyImmediate(testBoxes[i].gameObject);
                }
            }
            
            // ドロップされたアイテムを削除
            DroppedItem[] droppedItems = FindObjectsOfType<DroppedItem>();
            for (int i = 0; i < droppedItems.Length; i++)
            {
                if (Application.isPlaying)
                {
                    Destroy(droppedItems[i].gameObject);
                }
                else
                {
                    DestroyImmediate(droppedItems[i].gameObject);
                }
            }
            
            Debug.Log("Test scene cleared!");
        }
    }
}
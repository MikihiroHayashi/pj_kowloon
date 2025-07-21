using UnityEngine;

namespace KowloonBreak.Player
{
    public class PlayerSetup : MonoBehaviour
    {
        [Header("Player Configuration")]
        [SerializeField] private Vector3 spawnPosition = Vector3.zero;
        [SerializeField] private Vector3 spawnRotation = Vector3.zero;

        private GameObject playerObject;

        public GameObject GetPlayerObject()
        {
            return playerObject;
        }

        public void CreatePlayer()
        {
            // 既存のPlayerオブジェクトを探す
            playerObject = GameObject.FindGameObjectWithTag("Player");
            
            if (playerObject == null)
            {
                // 新しいPlayerオブジェクトを作成
                playerObject = new GameObject("Player");
                playerObject.tag = "Player";
                
                // 基本コンポーネントを追加
                var characterController = playerObject.AddComponent<CharacterController>();
                characterController.radius = 0.5f;
                characterController.height = 2f;
                characterController.center = new Vector3(0, 1f, 0);
                
                var audioSource = playerObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f;
                
                playerObject.AddComponent<EnhancedPlayerController>();
                
                // シンプルな見た目
                var visualsGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                visualsGO.name = "PlayerVisuals";
                visualsGO.transform.SetParent(playerObject.transform);
                visualsGO.transform.localPosition = new Vector3(0, 1f, 0);
                DestroyImmediate(visualsGO.GetComponent<Collider>());
                
                var renderer = visualsGO.GetComponent<Renderer>();
                renderer.material.color = Color.blue;
            }

            // 位置と回転を設定
            playerObject.transform.position = spawnPosition;
            playerObject.transform.rotation = Quaternion.Euler(spawnRotation);
        }

        public void SetPlayerPosition(Vector3 position)
        {
            spawnPosition = position;
            if (playerObject != null)
            {
                playerObject.transform.position = position;
            }
        }
    }
}
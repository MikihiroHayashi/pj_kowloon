using System;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

namespace KowloonBreak.Managers
{
    public class CameraManager : MonoBehaviour
    {
        public static CameraManager Instance { get; private set; }

        [Header("Camera Configuration")]
        [SerializeField] private CameraData[] cameraConfigurations;
        [SerializeField] private GameCameraType defaultCameraType = GameCameraType.Player;
        [SerializeField] private float transitionDuration = 1f;

        [Header("Priority Settings")]
        [SerializeField] private int activePriority = 10;
        [SerializeField] private int inactivePriority = 0;

        private Dictionary<GameCameraType, CameraData> cameraDictionary;
        private GameCameraType currentCameraType;
        private GameCameraType previousCameraType;

        public GameCameraType CurrentCameraType => currentCameraType;
        public GameCameraType PreviousCameraType => previousCameraType;

        public event Action<GameCameraType, GameCameraType> OnCameraSwitched;
        public event Action<GameCameraType> OnCameraActivated;
        public event Action<GameCameraType> OnCameraDeactivated;

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
                InitializeCameraManager();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            SwitchToCamera(defaultCameraType);
        }

        private void InitializeCameraManager()
        {
            cameraDictionary = new Dictionary<GameCameraType, CameraData>();

            if (cameraConfigurations == null || cameraConfigurations.Length == 0)
            {
                Debug.LogWarning("[CameraManager] No camera configurations found. Please assign camera configurations in the Inspector.");
                return;
            }

            foreach (var cameraData in cameraConfigurations)
            {
                if (cameraData.virtualCamera != null)
                {
                    cameraDictionary[cameraData.cameraType] = cameraData;
                    cameraData.virtualCamera.Priority = inactivePriority;
                }
            }

            Debug.Log($"[CameraManager] Initialized with {cameraDictionary.Count} cameras");
        }

        public void SwitchToCamera(GameCameraType targetCameraType)
        {
            if (!cameraDictionary.ContainsKey(targetCameraType))
            {
                Debug.LogError($"[CameraManager] Camera type {targetCameraType} not found in configuration!");
                return;
            }

            if (currentCameraType == targetCameraType)
            {
                Debug.Log($"[CameraManager] Already using camera {targetCameraType}");
                return;
            }

            previousCameraType = currentCameraType;
            currentCameraType = targetCameraType;

            DeactivateAllCameras();
            ActivateCamera(targetCameraType);

            OnCameraSwitched?.Invoke(previousCameraType, currentCameraType);

            Debug.Log($"[CameraManager] Switched from {previousCameraType} to {currentCameraType}");
        }

        public void SwitchToInfectedCamera(Transform infectedTarget)
        {
            if (!cameraDictionary.ContainsKey(GameCameraType.Infected))
            {
                Debug.LogError("[CameraManager] Infected camera not configured!");
                return;
            }

            var infectedCameraData = cameraDictionary[GameCameraType.Infected];
            if (infectedCameraData.virtualCamera != null && infectedTarget != null)
            {
                // 感染者カメラのターゲットを設定
                infectedCameraData.virtualCamera.Follow = infectedTarget;
                infectedCameraData.virtualCamera.LookAt = infectedTarget;
            }

            SwitchToCamera(GameCameraType.Infected);
        }

        public void ReturnToPreviousCamera()
        {
            if (previousCameraType != GameCameraType.None)
            {
                SwitchToCamera(previousCameraType);
            }
            else
            {
                SwitchToCamera(defaultCameraType);
            }
        }

        private void ActivateCamera(GameCameraType cameraType)
        {
            if (cameraDictionary.TryGetValue(cameraType, out CameraData cameraData))
            {
                if (cameraData.virtualCamera != null)
                {
                    cameraData.virtualCamera.Priority = activePriority;
                    OnCameraActivated?.Invoke(cameraType);
                }
            }
        }

        private void DeactivateAllCameras()
        {
            foreach (var kvp in cameraDictionary)
            {
                if (kvp.Value.virtualCamera != null)
                {
                    kvp.Value.virtualCamera.Priority = inactivePriority;
                    OnCameraDeactivated?.Invoke(kvp.Key);
                }
            }
        }

        public CameraData GetCameraData(GameCameraType cameraType)
        {
            return cameraDictionary.TryGetValue(cameraType, out CameraData data) ? data : null;
        }

        public CinemachineVirtualCamera GetVirtualCamera(GameCameraType cameraType)
        {
            var data = GetCameraData(cameraType);
            return data?.virtualCamera;
        }

        public void SetCameraTarget(GameCameraType cameraType, Transform target)
        {
            var data = GetCameraData(cameraType);
            if (data?.virtualCamera != null)
            {
                data.virtualCamera.Follow = target;
                data.virtualCamera.LookAt = target;
            }
        }

        public void RegisterCamera(GameCameraType cameraType, CinemachineVirtualCamera virtualCamera, string description = "")
        {
            var cameraData = new CameraData
            {
                cameraType = cameraType,
                virtualCamera = virtualCamera,
                description = description
            };

            cameraDictionary[cameraType] = cameraData;
            virtualCamera.Priority = inactivePriority;

            Debug.Log($"[CameraManager] Registered camera: {cameraType}");
        }

        public void UnregisterCamera(GameCameraType cameraType)
        {
            if (cameraDictionary.ContainsKey(cameraType))
            {
                cameraDictionary.Remove(cameraType);
                Debug.Log($"[CameraManager] Unregistered camera: {cameraType}");
            }
        }

        // Inspector上でカメラ設定を表示するためのヘルパーメソッド
        [ContextMenu("Debug Camera States")]
        private void DebugCameraStates()
        {
            Debug.Log($"[CameraManager] Current Camera: {currentCameraType}");
            Debug.Log($"[CameraManager] Previous Camera: {previousCameraType}");

            foreach (var kvp in cameraDictionary)
            {
                var priority = kvp.Value.virtualCamera?.Priority ?? -1;
                Debug.Log($"[CameraManager] {kvp.Key}: Priority {priority}");
            }
        }
    }

    [Serializable]
    public class CameraData
    {
        public GameCameraType cameraType;
        public CinemachineVirtualCamera virtualCamera;
        public string description;
        [Tooltip("このカメラ固有の設定があれば記述")]
        public string notes;
    }

    public enum GameCameraType
    {
        None,
        Player,        // プレイヤー追従カメラ
        Infected,      // 感染者専用カメラ
        Exploration,   // 探索カメラ
        Combat,        // 戦闘カメラ
        Cinematic,     // シネマティックカメラ
        Base,          // 拠点管理カメラ
        Overview       // 俯瞰カメラ
    }
}
using UnityEngine;
using Cinemachine;

namespace KowloonBreak.Camera
{
    public class CinemachineSetup : MonoBehaviour
    {
        [Header("Virtual Camera Settings")]
        [SerializeField] private CinemachineVirtualCamera virtualCamera;

        private void Start()
        {
            ConfigureVirtualCamera();
        }

        private void ConfigureVirtualCamera()
        {
            if (virtualCamera == null)
            {
                Debug.LogWarning("[CinemachineSetup] Virtual Camera is not assigned. Please assign it manually in the Inspector.");
                return;
            }
        }

        public void SetFollowTarget(Transform target)
        {
            if (virtualCamera != null)
            {
                virtualCamera.Follow = target;
                virtualCamera.LookAt = target;
            }
        }

        public void SetVirtualCamera(CinemachineVirtualCamera vcam)
        {
            virtualCamera = vcam;
            ConfigureVirtualCamera();
        }
    }
}
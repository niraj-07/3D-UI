using UnityEngine;
using UnityEngine.XR.Management;

namespace TaskBubble.VR
{
    // Started explicitly after choosing VR mode. Desktop instances never initialize XR.
    public sealed class PcVrRig : MonoBehaviour
    {
        [SerializeField] private GameObject trackedObjects;
        private XRManagerSettings manager;
        private bool ownsLoader;

        public bool IsRunning { get; private set; }
        public string Status { get; private set; } = "VR not started";

        public bool StartVr()
        {
            if (!Application.isPlaying || !isActiveAndEnabled || trackedObjects == null) return false;
            if (IsRunning) return true;
            manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
            if (manager == null) { Status = "XR settings are missing"; return false; }
            if (manager.activeLoader != null || manager.isInitializationComplete)
            {
                Status = "XR is already initialized by another rig";
                return false;
            }

            // Called after mode selection, never from Awake. No coroutine can outlive this rig.
            manager.InitializeLoaderSync();
            ownsLoader = manager.isInitializationComplete;
            if (manager.activeLoader == null)
            {
                StopVr();
                Status = "Connect Quest Link/Air Link and check the PC OpenXR runtime, then retry";
                return false;
            }
            manager.StartSubsystems();
            trackedObjects.SetActive(true);
            IsRunning = true;
            Status = "XR started; headset and controller tracking depend on the connected devices";
            return true;
        }

        public void StopVr()
        {
            if (trackedObjects != null) trackedObjects.SetActive(false);
            if (ownsLoader && manager != null && manager.isInitializationComplete)
                manager.DeinitializeLoader(); // Also stops its subsystems.
            ownsLoader = false;
            manager = null;
            IsRunning = false;
            Status = "VR not started";
        }

        private void OnDisable() => StopVr();
    }
}

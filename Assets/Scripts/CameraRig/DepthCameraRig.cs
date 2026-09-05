using JellyRush.Core;
using UnityEngine;

namespace JellyRush.CameraRig
{
    /// <summary>
    /// Perspective camera that looks straight down the play corridor into the depth
    /// of the screen (CAMERA_AND_DEPTH_SPEC section 2). It stays put in Z, tilts down
    /// a little, and only softly copies a fraction of the player's sideways position
    /// so the 3 lanes and the far distance always stay readable. It never rotates
    /// per-swipe and never makes the player look like they are launching skyward.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class DepthCameraRig : MonoBehaviour
    {
        Camera _cam;
        Transform _target;
        PrototypeConfig _cfg;
        float _currentX;

        public void Configure(PrototypeConfig cfg, Transform playerTarget)
        {
            _cfg = cfg;
            _target = playerTarget;
            _cam = GetComponent<Camera>();

            _cam.orthographic = false;
            _cam.fieldOfView = cfg.cameraFieldOfView;
            _cam.nearClipPlane = cfg.cameraNear;
            _cam.farClipPlane = cfg.cameraFar;
            _cam.backgroundColor = new Color(0.62f, 0.79f, 0.93f); // toy-workshop sky placeholder
            _cam.clearFlags = CameraClearFlags.SolidColor;

            transform.position = new Vector3(cfg.cameraOffset.x, cfg.cameraOffset.y,
                                             cfg.playerZ + cfg.cameraOffset.z);
            transform.rotation = Quaternion.Euler(cfg.cameraPitch, 0f, 0f);
            _currentX = transform.position.x;
        }

        void LateUpdate()
        {
            if (_target == null || _cfg == null) return;

            float desiredX = _cfg.cameraOffset.x + _target.position.x * _cfg.cameraLateralAmount;
            _currentX = Mathf.Lerp(_currentX, desiredX, 1f - Mathf.Exp(-_cfg.cameraLateralFollow * Time.deltaTime));

            var p = transform.position;
            p.x = _currentX;
            transform.position = p;
        }
    }
}

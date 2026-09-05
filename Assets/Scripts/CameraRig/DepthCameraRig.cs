using JellyRush.Core;
using UnityEngine;

namespace JellyRush.CameraRig
{
    /// <summary>
    /// Perspective camera looking down the play corridor into the depth of the
    /// screen (CAMERA_AND_DEPTH_SPEC section 2). It stays put in Z, tilts down a
    /// little, and only softly copies a fraction of the player's sideways AND
    /// vertical position - the latter just enough that climbing to the High tier
    /// does not push the pair off the top of the screen. It never rotates per-swipe
    /// and never makes the player look like they are launching skyward.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class DepthCameraRig : MonoBehaviour
    {
        Camera _cam;
        Transform _target;
        PrototypeConfig _cfg;
        float _currentX;
        float _currentY;
        float _baseY;

        public void Configure(PrototypeConfig cfg, Transform playerTarget, Color skyColor)
        {
            _cfg = cfg;
            _target = playerTarget;
            _cam = GetComponent<Camera>();

            _cam.orthographic = false;
            _cam.fieldOfView = cfg.cameraFieldOfView;
            _cam.nearClipPlane = cfg.cameraNear;
            _cam.farClipPlane = cfg.cameraFar;
            _cam.backgroundColor = skyColor;
            _cam.clearFlags = CameraClearFlags.SolidColor;

            _baseY = cfg.startHeight + cfg.cameraOffset.y;
            transform.position = new Vector3(cfg.cameraOffset.x, _baseY,
                                             cfg.playerZ + cfg.cameraOffset.z);
            transform.rotation = Quaternion.Euler(cfg.cameraPitch, 0f, 0f);
            _currentX = transform.position.x;
            _currentY = _baseY;
        }

        void LateUpdate()
        {
            if (_target == null || _cfg == null) return;

            float dt = Time.deltaTime;

            float desiredX = _cfg.cameraOffset.x + _target.position.x * _cfg.cameraLateralAmount;
            _currentX = Mathf.Lerp(_currentX, desiredX, 1f - Mathf.Exp(-_cfg.cameraLateralFollow * dt));

            float desiredY = _baseY + (_target.position.y - _cfg.startHeight) * _cfg.cameraVerticalAmount;
            _currentY = Mathf.Lerp(_currentY, desiredY, 1f - Mathf.Exp(-_cfg.cameraVerticalFollow * dt));

            var p = transform.position;
            p.x = _currentX;
            p.y = _currentY;
            transform.position = p;
        }
    }
}

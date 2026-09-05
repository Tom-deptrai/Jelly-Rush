# Jelly-Rush

Prototype game arcade 3D cuộn dọc, điều khiển một chạm, làm bằng Unity.

## Prototype V1 — Depth camera + 3 lane

Unity **6000.5.10f1**, mobile portrait.

### Chạy thử
1. Mở project bằng Unity 6000.5.10f1 (Unity Hub → Add → chọn thư mục repo).
2. Mở scene `Assets/Scenes/Prototype.unity`.
3. Bấm **Play**. Game view nên đặt tỉ lệ dọc (vd 1080x1920).
4. Điều khiển:
   - **Tap / click** = nhảy thẳng trong lane hiện tại.
   - **Swipe / kéo chuột trái** = nhảy sang lane trái.
   - **Swipe / kéo chuột phải** = nhảy sang lane phải.
   - Nút `II` góc dưới phải = pause. Khi fail có nút **RETRY**.

### Kiến trúc
Scene chỉ chứa 1 GameObject `Prototype` với `PrototypeBootstrap` — toàn bộ scene
(camera, lane, player, world scroller, spawner, HUD, ánh sáng) được dựng bằng code
để dễ thay asset / chỉnh số sau này. Mọi thông số nằm trong `PrototypeConfig`
(inspector của `PrototypeBootstrap`).

| Module | File |
| --- | --- |
| Bootstrap / lắp ráp scene | `Assets/Scripts/Core/PrototypeBootstrap.cs` |
| Thông số tuning | `Assets/Scripts/Core/PrototypeConfig.cs` |
| State / distance / coin / combo / pause / retry | `Assets/Scripts/Core/GameManager.cs` |
| Camera perspective chiều sâu | `Assets/Scripts/CameraRig/DepthCameraRig.cs` |
| 3 lane | `Assets/Scripts/Lanes/LaneSystem.cs` |
| Input tap / swipe | `Assets/Scripts/InputSystem/SwipeTapInput.cs` |
| Nhảy + đổi lane mượt | `Assets/Scripts/Player/PlayerController.cs` |
| Placeholder Jelly + carrier + phản ứng | `Assets/Scripts/Player/PlayerVisuals.cs` |
| Va chạm (coin / obstacle / bounce / gate) | `Assets/Scripts/Player/PlayerCollisions.cs` |
| World trôi về phía camera | `Assets/Scripts/World/WorldScroller.cs` |
| Sàn cuộn tạo chiều sâu | `Assets/Scripts/World/ScrollingFloor.cs` |
| Spawn thử nghiệm | `Assets/Scripts/Spawning/Spawner.cs`, `SpawnableFactory.cs` |
| Hành vi vật thể | `Assets/Scripts/Spawnables/SpawnableBehaviours.cs` |
| HUD tối thiểu | `Assets/Scripts/UI/HudController.cs` |

### Kiểm thử / build
- Smoke test Play Mode headless: `Unity -batchmode -projectPath . -executeMethod JellyRush.EditorTools.ProtoSmokeTest.Run`
- Build: `Unity -batchmode -quit -projectPath . -buildTarget Android -executeMethod JellyRush.EditorTools.BuildScript.BuildAndroid`

Xem `GAMEPLAY_SPEC_V1.md` và `CAMERA_AND_DEPTH_SPEC_V1.md` cho định hướng thiết kế.

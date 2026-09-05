# CAMERA_AND_DEPTH_SPEC_V1

> Trạng thái: V1 – tài liệu sống, có thể tiếp tục chỉnh sửa sau khi chơi thử prototype.
>
> Mục tiêu của tài liệu này là khóa **góc camera, cảm giác chiều sâu, 3 lane và cách vật thể xuất hiện từ xa rồi tiến dần ra gần** dựa trên hình reference đã được chọn. Đây chưa phải luật bất biến; nếu khi chơi thử thấy chưa tốt thì được phép điều chỉnh.

## 1. Mục tiêu hình ảnh

Game phải tạo cảm giác người chơi đang nhìn vào **một không gian 3D có chiều sâu rõ ràng**.

Các platform, coin, chướng ngại vật và điểm đáp phải:

- xuất hiện ở **phía xa, sâu trong màn hình**;
- lúc mới xuất hiện có kích thước nhỏ;
- tiến dần về phía camera/người chơi;
- lớn dần theo perspective (phối cảnh);
- tạo cảm giác như toàn bộ đường chơi đang **trôi từ trong ra ngoài**, không phải đơn giản rơi từ đỉnh màn hình xuống dưới.

Phần xa nhất của màn hình phải luôn còn nhìn thấy được để người chơi có thời gian quan sát và chuẩn bị phản xạ.

## 2. Góc camera

- Game chơi ở màn hình dọc (portrait).
- Camera dùng perspective 3D, không dùng góc nhìn phẳng 2D.
- Camera nhìn theo trục đường chơi hướng vào chiều sâu.
- Góc camera phải đủ thấp để người chơi thấy rõ đường chơi kéo dài vào phía xa, nhưng vẫn đủ cao để nhìn được các platform/điểm đáp phía trước.
- Không tạo cảm giác nhân vật đang nhảy thẳng lên trời rất cao.
- Phần trên của màn hình có thể là bầu trời, trần nhà, kiến trúc xa hoặc không gian world tùy bối cảnh.
- Vị trí camera phải ưu tiên: **nhìn xa được, đọc chướng ngại sớm được, nhân vật vẫn nổi bật và đẹp mắt**.

## 3. Vị trí nhân vật trên màn hình

- Cặp nhân vật gồm Jelly chính + thú nhỏ cõng Jelly.
- Cặp nhân vật nằm chủ yếu ở vùng **dưới – giữa màn hình**, nhưng không quá sát mép dưới.
- Jelly chính luôn phải nhìn rõ mặt và biểu cảm.
- Thú nhỏ quay theo hướng di chuyển vào chiều sâu của world, thể hiện vai trò là nhân vật thực hiện cú nhảy.
- Khi chuyển lane, cả cặp nghiêng và dịch chuyển mềm, không teleport cứng.
- Khi rapid tap (bấm liên tục), Jelly có thể tròn hơn, co người, vui/phấn khích hơn và có hiệu ứng trail/particle.

## 4. Hệ thống 3 lane

Prototype V1 dùng **3 lane cố định**:

- Lane trái
- Lane giữa
- Lane phải

Các lane là các quỹ đạo logic trong không gian 3D, không nhất thiết phải vẽ đường lane lên màn hình.

Yêu cầu:

- khoảng cách giữa lane đủ rõ để người chơi nhận biết vị trí đích;
- lane hội tụ theo perspective khi nhìn về phía xa;
- các platform có thể xuất hiện ở bất kỳ lane nào;
- một obstacle có thể chiếm 1 lane, 2 lane hoặc tạo khe an toàn ở 1 lane;
- không sinh tình huống không thể vượt qua.

## 5. Điều khiển liên quan camera/lane

- Chạm (tap) = nhảy thẳng trong lane hiện tại.
- Vuốt trái = nhảy/chuyển sang lane bên trái.
- Vuốt phải = nhảy/chuyển sang lane bên phải.
- Không dùng joystick.
- Không giới hạn tốc độ tap tối thiểu giữa các lần bấm.
- Người chơi được phép tap nhanh liên tục để phản xạ với obstacle.
- Camera không xoay theo từng swipe; camera giữ ổn định để người chơi luôn đọc được 3 lane và chiều sâu.

## 6. Cách platform và obstacle xuất hiện

Mọi phần tử gameplay phải được spawn (sinh ra) từ vùng xa của đường chơi.

Quy tắc thị giác:

1. Vật thể xuất hiện nhỏ ở phía xa.
2. Sau đó tiến dần về phía người chơi.
3. Kích thước nhìn thấy tăng dần tự nhiên theo perspective.
4. Người chơi phải có đủ thời gian để nhận biết đó là platform, coin hay obstacle trước khi tới gần.
5. Không spawn vật thể đột ngột ngay sát nhân vật.

Các nhóm chính trong V1:

- platform đáp thường;
- platform lệch trái/giữa/phải;
- moving platform;
- rotating bar;
- closing gate;
- obstacle chắn lane;
- coin;
- perfect marker / perfect landing target;
- bounce pad.

## 7. Cảm giác chuyển động của world

Mục tiêu không phải là “mọi thứ rơi từ trên xuống”.

Cảm giác đúng phải là:

> vật thể ở xa trong không gian → tiến dần ra trước mắt → đi qua vùng nhân vật → tiếp tục ra ngoài khung hình.

Có thể dùng một trong hai cách kỹ thuật:

- world/obstacle thực sự di chuyển về phía camera;
- hoặc camera/player tiến về phía trước trong world nhưng giữ bố cục màn hình tương tự.

Claude được phép chọn cách kỹ thuật tốt hơn, miễn **kết quả hình ảnh và cảm giác chơi đúng theo mô tả trên**.

## 8. Chiều sâu và lớp cảnh

World nên có ít nhất 3 lớp để tạo chiều sâu:

### Lớp gameplay gần
- nhân vật;
- platform;
- coin;
- obstacle;
- VFX gameplay.

### Lớp trung cảnh
- máy móc;
- cầu;
- cột;
- dây chuyền;
- hộp, đồ chơi, kiến trúc phụ.

### Lớp hậu cảnh xa
- bầu trời / trần nhà;
- nhà máy xa;
- đảo bay / tháp / cấu trúc lớn;
- ánh sáng môi trường.

Các lớp có thể dùng parallax (thị sai: lớp gần di chuyển nhanh hơn lớp xa) để tăng cảm giác 3D.

## 9. Khả năng đọc gameplay

Ưu tiên hàng đầu là người chơi phải nhìn được thử thách phía trước.

Do đó:

- không để background quá rối;
- obstacle phải có silhouette rõ;
- lane an toàn phải có thể nhận ra;
- màu obstacle phải đủ tương phản với background;
- coin và perfect target phải nổi bật;
- không để vật trang trí che platform quan trọng.

## 10. Mục tiêu prototype V1

Prototype camera/depth được xem là đạt khi:

- người chơi nhìn thấy rõ đường chơi kéo dài vào phía xa;
- platform/obstacle xuất hiện nhỏ ở xa và lớn dần khi tiến gần;
- 3 lane dễ hiểu dù không có vạch lane;
- người chơi có thể nhìn trước 2–4 thử thách kế tiếp;
- cảm giác không phải “nhảy thẳng lên trời”, mà là tiến xuyên qua một hành lang 3D;
- nhân vật vẫn là trung tâm thị giác;
- camera không gây chóng mặt;
- chạy ổn định trên mobile portrait.

## 11. Lưu ý quan trọng cho AI/Claude

- Hình reference camera/depth đã được người dùng chọn là chuẩn thị giác chính.
- Không tự đổi sang camera top-down hoặc side-view.
- Không biến world thành các vật thể rơi từ mép trên màn hình xuống dưới.
- Không khóa cứng các con số camera/FOV/lane distance nếu chưa test gameplay.
- Có thể điều chỉnh thông số sau khi build thử lên điện thoại.
- Đây là tài liệu sống; gameplay thực tế được ưu tiên hơn việc giữ nguyên thông số V1.

---
phase: 3
title: "Map authoring và validation"
status: pending
priority: P1
dependencies: [2]
---

# Phase 3: Map authoring và validation

## Tổng quan

Cho phép đặt player spawn, enemy/item spawn, Environmental Bomb và Turret trực tiếp trong scene; validator phát hiện dữ liệu map sai trước khi gameplay chạy.

## Yêu cầu

### Chức năng

- Có đúng một player spawn.
- Enemy/item spawn nằm trên Ground hợp lệ.
- Environmental Bomb nằm trên Ground.
- Turret nằm trên Wall hoặc cell non-walkable.
- Actor spawn không chồng nhau.
- Validator báo rõ object, tọa độ và nguyên nhân lỗi.
- Marker allowance, required marker count, duplicate policy và reachability target được cấu hình bằng `MapAuthoringConfig`.

### Phi chức năng

- Marker chỉ chứa dữ liệu authoring tối thiểu.
- Không tự sửa hoặc bỏ qua marker sai.
- Thông số chưa chốt được serialized, không hard-code.
- Config sai phải fail-fast bằng lỗi validator, không silently fallback sang default runtime.

## Kiến trúc

```text
Scene markers
→ MapAuthoring thu thập với MapAuthoringConfig
→ MapValidator kiểm tra với MapGrid + config
→ dữ liệu hợp lệ mới tạo runtime entities
```

Marker và runtime prefab là hai vai trò khác nhau. Marker mô tả vị trí; factory/spawner tạo đối tượng gameplay khi bắt đầu run.

`MapAuthoringConfig` định nghĩa loại marker nào được đặt trên terrain nào, marker nào bắt buộc nằm trong reachable component từ player spawn, ID/prefab mặc định nào được phép dùng và giới hạn số lượng MVP. Marker scene chỉ override các field thật sự cần khác default.

## File liên quan

- Tạo: `countdown-game/Assets/_Game/Scripts/Gameplay/Map/Authoring/MapAuthoring.cs`
- Tạo: `countdown-game/Assets/_Game/Scripts/Gameplay/Map/Authoring/MapValidator.cs`
- Tạo: `countdown-game/Assets/_Game/Scripts/Gameplay/Map/Authoring/PlayerSpawnMarker.cs`
- Tạo: `countdown-game/Assets/_Game/Scripts/Gameplay/Map/Authoring/EnemySpawnMarker.cs`
- Tạo: `countdown-game/Assets/_Game/Scripts/Gameplay/Map/Authoring/ItemSpawnMarker.cs`
- Tạo: `countdown-game/Assets/_Game/Scripts/Gameplay/Map/Authoring/HazardMarker.cs`
- Tạo: `countdown-game/Assets/_Game/Scripts/Gameplay/Map/Config/MapAuthoringConfig.cs`
- Tạo: `countdown-game/Assets/_Game/Prefabs/Map/Markers/`
- Tạo: `countdown-game/Assets/_Game/Data/Map/MapAuthoringConfig.asset`
- Sửa: `countdown-game/Assets/_Game/Prefabs/Map/MapRuntime.prefab`
- Có thể sửa: `countdown-game/Assets/_Game/Scenes/MapPrefabTest.unity` để test authoring prefab

## Các bước triển khai

1. Tạo component marker và gizmo phân biệt theo loại.
2. Chuẩn hóa marker position về tâm cell.
3. Tạo `MapAuthoring` thu thập marker theo hierarchy rõ ràng.
4. Tạo `MapAuthoringConfig` cho terrain allowance, required count, duplicate policy, reachability và marker ID.
5. Tạo validator cho bounds, terrain, duplicate và occupancy theo config.
6. Kiểm tra khả năng tiếp cận từ player spawn tới các marker/cell được config đánh dấu `MustBeReachable`, không tính actor runtime.
7. Quy định lỗi nghiêm trọng chặn Play; warning dành cho vấn đề không phá gameplay.
8. Chạy validator khi scene khởi tạo và cung cấp nút kiểm tra trong Editor nếu cần.

## Tiêu chí hoàn thành

- [ ] Map hợp lệ khởi tạo được toàn bộ marker.
- [ ] Thiếu hoặc trùng player spawn bị báo lỗi.
- [ ] Bomb/Turret đặt sai terrain bị báo lỗi kèm tọa độ.
- [ ] Spawn chồng nhau hoặc ngoài bounds bị chặn.
- [ ] Marker nhìn thấy rõ bằng Gizmos nhưng không xuất hiện trong build.
- [ ] Không có giá trị cân bằng `TBD` bị biến thành luật cố định.
- [ ] Đổi terrain allowance hoặc required marker count trong config làm validator đổi kết quả đúng.

## Rủi ro

- Quá nhiều loại marker: dùng enum/config chung khi hành vi authoring giống nhau.
- Connectivity validation tốn công: MVP chỉ cần flood-fill bốn hướng trên terrain tĩnh.
- Scene reference dễ gãy: marker không giữ tham chiếu trực tiếp tới runtime object nếu có thể dùng ID/config.
- Config fallback che lỗi content: thiếu config hoặc config invalid phải là error chặn khởi tạo map.

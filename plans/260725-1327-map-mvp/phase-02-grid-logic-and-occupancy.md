---
phase: 2
title: "Grid logic và occupancy"
status: in-progress
priority: P1
dependencies: [1]
---

# Phase 2: Grid logic và occupancy

## Tổng quan

Tạo mô hình grid runtime độc lập với Tilemap hiển thị. Đây là nguồn sự thật cho walkability, occupancy và các truy vấn gameplay.

## Yêu cầu

### Chức năng

- Chuyển đổi ổn định giữa world position và grid position.
- Phân biệt Ground, Wall, Obstacle và ngoài map.
- Theo dõi actor occupant độc lập với item/hazard.
- Hỗ trợ item và Environmental Bomb cùng tồn tại với actor trên một ô.
- Cung cấp truy vấn bốn ô lân cận và vùng theo range.
- Terrain precedence, max bounds, range query cap và comparator mặc định được truyền từ config đã validate.

### Phi chức năng

- Không để movement, spawn hoặc hazard đọc trực tiếp Tilemap.
- API không phụ thuộc MonoBehaviour khi không cần thiết.
- Không cấp phát collection mới liên tục trong đường chạy mỗi nhịp.
- Không hard-code tunable value trong `MapGrid`; constructor nhận settings/snapshot rõ ràng.

## Kiến trúc dữ liệu

```text
MapCell
├── Terrain: Ground | Wall | Obstacle
├── Occupant: Player | Enemy | Empty
├── Interactable: SkillItem | Empty
└── Hazard: EnvironmentalBomb | BombSkill | Empty
```

Turret gắn với cell non-walkable nhưng runtime entity của Turret vẫn được quản lý riêng.

Config không đi thẳng vào state mutable. `MapController` chuyển `MapGridConfig` thành `MapGridSettings` immutable, validate toàn bộ field, rồi mới tạo `MapGrid`.

`MapController` sống trong `MapRuntime.prefab`, không nằm trực tiếp trong scene chung. Scene test và scene chung gắn trực tiếp prefab instance để giảm conflict khi merge.

## File liên quan

- Tạo: `countdown-game/Assets/_Game/Scripts/Gameplay/Map/GridPosition.cs`
- Tạo: `countdown-game/Assets/_Game/Scripts/Gameplay/Map/CellTerrain.cs`
- Tạo: `countdown-game/Assets/_Game/Scripts/Gameplay/Map/MapCell.cs`
- Tạo: `countdown-game/Assets/_Game/Scripts/Gameplay/Map/MapGrid.cs`
- Tạo: `countdown-game/Assets/_Game/Scripts/Gameplay/Map/MapController.cs`
- Tạo: `countdown-game/Assets/_Game/Scripts/Gameplay/Map/MapCellFacts.cs`
- Tạo: `countdown-game/Assets/_Game/Scripts/Gameplay/Map/MapGridSmokeHarness.cs`
- Tạo: `countdown-game/Assets/_Game/Scripts/Gameplay/Map/Config/MapGridConfig.cs`
- Tạo: `countdown-game/Assets/_Game/Scripts/Gameplay/Map/Config/MapGridSettings.cs`
- Tạo: `countdown-game/Assets/_Game/Data/Map/MapGridConfig.asset`

## API tối thiểu

- `WorldToGrid(Vector3 worldPosition)`
- `GridToWorld(GridPosition position)`
- `IsInsideMap(GridPosition position)`
- `IsWalkable(GridPosition position)`
- `IsOccupied(GridPosition position)`
- `CanEnter(GridPosition position)`
- `GetCellFacts(GridPosition position)`
- `GetOccupant(GridPosition position)`
- `GetFourNeighbors(GridPosition position)`
- `TryPlaceOccupant(...)`
- `TryMoveOccupant(...)`
- `RemoveOccupant(...)`
- Truy vấn item/hazard và cell trong range

## Các bước triển khai

1. Tạo `GridPosition` immutable, equality/hash rõ ràng.
2. Tạo terrain/cell model không phụ thuộc Unity scene.
3. Tạo `MapGridConfig` với min/max bounds, terrain precedence, range cap và stable coordinate comparator.
4. Tạo `MapGrid` quản lý bounds và cell data từ `MapGridSettings` immutable.
5. Tạo `MapController` đọc Ground/Wall/Obstacle Tilemap để build grid theo config.
6. Tạo API đặt, di chuyển và xóa occupant nguyên khối.
7. Reject candidate không hợp lệ mà không thay đổi state.
8. Thêm debug query hoặc gizmo để kiểm tra cell data và config đang dùng.

## Tiêu chí hoàn thành

- [ ] World/grid conversion round-trip đúng.
- [ ] Ground walkable; Wall/Obstacle/out-of-bounds không walkable.
- [ ] Hai actor không thể chiếm cùng một ô.
- [ ] Failed placement/movement không làm thay đổi state.
- [ ] Actor có thể cùng ô với item hoặc Environmental Bomb.
- [ ] Map logic không phụ thuộc sprite hoặc final art.
- [ ] Cùng một map chạy được với ít nhất hai `MapGridConfig` hợp lệ trong test/harness để bắt hard-code.

## Rủi ro

- Dùng `Vector3` làm khóa gây sai số: chỉ dùng `GridPosition` integer.
- Occupancy và Transform lệch nhau: MapGrid đổi state trước, presentation cập nhật sau khi resolve thành công.
- Grid quá gắn Tilemap: giữ `MapGrid` là plain C# và cô lập Unity API trong `MapController`.
- Quá nhiều setting làm rule mơ hồ: config chỉ chứa tunable runtime/authoring value; gameplay invariant vẫn được test như code contract.

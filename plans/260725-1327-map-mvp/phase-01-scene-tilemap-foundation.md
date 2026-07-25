---
phase: 1
title: "Scene và Tilemap foundation"
status: in-progress
priority: P1
dependencies: []
---

# Phase 1: Scene và Tilemap foundation

## Tổng quan

Dựng scene gameplay và các Tilemap layer tối thiểu để level designer vẽ terrain, còn runtime có một hệ tọa độ grid thống nhất.

Map hierarchy phải được đóng gói vào prefab riêng để merge tốt với scene chung của team. Scene test chỉ giữ camera/light và một prefab instance trực tiếp của map.

## Yêu cầu

### Chức năng

- Có `Grid` orthogonal và các layer Ground, Wall, Obstacle, Overlay.
- Camera hiển thị map đúng pixel/aspect mục tiêu.
- Sorting order dành chỗ cho actor, item, hazard và telegraph.
- Dùng placeholder tile; không phụ thuộc final art.
- Camera framing, sorting order và placeholder tile reference nằm trong serialized config/scene reference, không hard-code.

### Phi chức năng

- Không đưa game-owned asset ra ngoài `Assets/_Game/`.
- Không sửa hoặc thay GUID của asset Unity hiện có.
- Cấu trúc scene dễ đọc và không có GameObject thừa.

## Kiến trúc

```text
MapRuntime.prefab
├── Map
│   └── Grid
│       ├── GroundTilemap
│       ├── WallTilemap
│       ├── ObstacleTilemap
│       └── OverlayTilemap
├── Actors
├── Interactables
├── Hazards
└── Systems
    ├── MapSceneConfigurator
    └── MapController

MapPrefabTest.unity hoặc scene chung
└── MapRuntime prefab instance
```

`OverlayTilemap` chỉ hiển thị preview/telegraph, không được dùng làm dữ liệu collision hoặc walkability.

Các thông số trình bày thuộc authoring layer: camera size/offset, sorting layer/order, placeholder tile reference và overlay tile mặc định phải chỉnh được trong Inspector hoặc config asset. Runtime map logic không đọc trực tiếp các giá trị này.

## File liên quan

- Tạo: `countdown-game/Assets/_Game/Scenes/MapPrefabTest.unity`
- Tạo: `countdown-game/Assets/_Game/Prefabs/Map/MapRuntime.prefab`
- Tạo: `countdown-game/Assets/_Game/Art/Tilemaps/Placeholder/`
- Tạo: `countdown-game/Assets/_Game/Data/Map/`
- Có thể tạo: `countdown-game/Assets/_Game/Scripts/Gameplay/Map/Config/MapSceneConfig.cs`
- Có thể sửa: `countdown-game/ProjectSettings/TagManager.asset` nếu cần sorting layer

## Các bước triển khai

1. Tạo `MapPrefabTest.unity` từ URP 2D scene hiện có để test map prefab.
2. Tạo `MapRuntime.prefab` chứa hierarchy và bốn Tilemap layer; scene test gắn prefab instance trực tiếp.
3. Cấu hình renderer, sorting layer, pivot và cell size thống nhất.
4. Tạo placeholder tile dễ phân biệt ground/wall/obstacle/overlay.
5. Tạo `MapSceneConfig` hoặc scene component chứa camera framing, sorting order, default overlay tile và reference Tilemap.
6. Vẽ một map test nhỏ có hành lang, góc kín và vùng mở.
7. Cấu hình camera bằng serialized value; chưa khóa kích thước map.
8. Lưu scene và kiểm tra không có missing reference.

## Tiêu chí hoàn thành

- [ ] Scene mở được bằng Unity `6000.5.5f1`.
- [ ] Tọa độ cell không lệch giữa các Tilemap.
- [ ] Terrain và overlay render đúng thứ tự.
- [ ] Map test đủ trường hợp để kiểm tra walkability ở phase sau.
- [ ] Đổi camera/overlay/sorting config hợp lệ trong Inspector phản ánh đúng trong scene.
- [ ] Scene chung có thể tích hợp map bằng một `MapRuntime.prefab` instance trực tiếp mà không copy toàn bộ hierarchy Tilemap.
- [ ] Không có lỗi hoặc warning mới trong Unity Console.

## Rủi ro

- Pixel art bị mờ hoặc rung: khóa Pixels Per Unit, filtering và camera setup trước khi làm final art.
- Sorting chồng chéo: định nghĩa sorting layer ngay trong phase này.
- Camera bị khóa quá sớm: giữ size/offset dưới dạng serialized configuration.
- Config presentation bị lẫn logic: đặt tên và folder rõ `Config`, chỉ dùng cho scene setup/overlay, không dùng làm gameplay authority.

---
title: "Kế hoạch triển khai Map MVP"
description: "Xây dựng nền tảng map grid 2D cho movement, spawn, item, hazard và telegraph của Countdown."
status: in-progress
priority: P1
branch: main
tags: [feature, unity, map, gameplay]
blockedBy: []
blocks: []
created: 2026-07-25
---

# Kế hoạch triển khai Map MVP

## Tổng quan

Tạo một map thủ công bằng Unity Tilemap, tách dữ liệu grid logic khỏi phần hiển thị. Map cung cấp API chung cho Move/Dash, occupancy, spawn, item, Environmental Bomb, Turret và telegraph.

Thiết kế mặc định theo hướng data-driven: designer có thể chỉnh map size cap, camera framing, terrain precedence, marker rule, range/overlay style và debug option bằng serialized asset trong `Assets/_Game/Data/Map/` mà không sửa code. Code chỉ giữ invariant gameplay đã chốt trong `docs/gameplay/`.

Phạm vi chỉ gồm map foundation và các điểm tích hợp. Không triển khai toàn bộ combat, enemy AI, WC, cooldown hoặc UI flow.

## Giả định MVP

- Một map thủ công, grid vuông orthogonal, di chuyển bốn hướng.
- Không procedural generation trong game jam.
- Kích thước map, camera, rule authoring và thông số cân bằng MVP được serialized để thay đổi.
- Ground, actor, item và hazard được lưu ở các lớp dữ liệu riêng.
- Không tự chốt các gameplay rule đang được ghi `TBD`.
- Mỗi giá trị tune được phải có default asset, min/max hoặc validator rõ ràng; không dùng literal "magic number" trong runtime logic.

## Kiến trúc

```text
MapRuntime.prefab chứa Tilemap, marker và map systems
→ Scene test và scene chung gắn trực tiếp MapRuntime prefab instance
→ MapController đọc MapConfig + dữ liệu authoring trong prefab instance
→ MapGrid tạo trạng thái runtime
→ Movement / Spawn / Hazard truy vấn MapGrid qua policy/config
→ OverlayTilemap hiển thị preview và telegraph
```

## Nguyên tắc dễ điều chỉnh

- Tạo `MapGridConfig`, `MapAuthoringConfig`, `MapOverlayConfig` và các config nhỏ liên quan dưới `countdown-game/Assets/_Game/Data/Map/`.
- Đóng gói hierarchy map vào `countdown-game/Assets/_Game/Prefabs/Map/MapRuntime.prefab`; scene chung không trực tiếp sở hữu Ground/Wall/Obstacle/Overlay Tilemap để giảm conflict merge.
- `MapGrid` là plain C# và nhận snapshot/config đã validate, không tự đọc ScriptableObject hoặc Tilemap.
- `MapController` là biên dịch Unity authoring thành runtime snapshot: Tilemap + marker + config phải validate xong rồi mới publish grid ready.
- Những thứ được tune: bounds cap, camera framing, terrain precedence, marker allowance, range/query cap, overlay color/tile, debug visibility và default scene content.
- Những thứ không được tune trong map config: rule đã chốt của beat, cooldown, WC, victory, damage và các gameplay `TBD` chưa được quyết định.
- Mỗi config asset phải có validation lỗi rõ ràng: thiếu reference, out-of-range, duplicate ID, terrain rule mâu thuẫn, hoặc giá trị làm map không thể chơi.

## Các phase

| Phase | Tên | Trạng thái | Phụ thuộc |
| --- | --- | --- | --- |
| 1 | [Scene và Tilemap foundation](./phase-01-scene-tilemap-foundation.md) | Implemented; Unity verify pending | Không |
| 2 | [Grid logic và occupancy](./phase-02-grid-logic-and-occupancy.md) | Implemented; Unity verify pending | Phase 1 |
| 3 | [Map authoring và validation](./phase-03-map-authoring-and-validation.md) | Pending | Phase 2 |
| 4 | [Tích hợp gameplay](./phase-04-gameplay-integration.md) | Pending | Phase 3 |
| 5 | [Kiểm thử và hoàn thiện](./phase-05-testing-and-polish.md) | Pending | Phase 4 |

## Ngoài phạm vi

- Procedural generation, nhiều biome hoặc hệ thống level progression.
- Final art, animation và âm thanh.
- Full combat, enemy AI, WC, cooldown và toàn bộ UI.
- Save/load trạng thái map.
- NavMesh hoặc pathfinding phức tạp.

## Quyết định cần chốt

- Kích thước map và camera cố định hay di chuyển.
- Một map hay nhiều map trong bản game jam.
- Dash endpoint, landing occupancy và intermediate interaction.
- Comparator tọa độ dùng cho thứ tự hazard ổn định.
- Spawn point cố định hay chọn trong vùng.
- Turret line-of-sight, projectile blocking và target priority.
- Danh sách thông số nào designer được tune tự do và thông số nào cần gameplay approval trước khi đổi.

## Definition of Done

- Map hiển thị và chuyển đổi world/grid chính xác.
- Walkability và occupancy là nguồn sự thật duy nhất cho movement/spawn.
- Marker sai được validator báo rõ tọa độ và nguyên nhân.
- Move, Dash, spawn, item và hazard có API tích hợp rõ ràng.
- Overlay hiển thị được path/range/telegraph mà không đổi gameplay state.
- Scene không có missing reference; Unity Console không có compile error.
- Scene chung có thể dùng map bằng một `MapRuntime.prefab` instance trực tiếp, không cần copy toàn bộ map hierarchy.
- Thay đổi config hợp lệ trong Inspector làm runtime đổi tương ứng mà không cần sửa code.

## Tài liệu nguồn

`map-ui-and-game-flow.md`, `beat-and-action-system.md`, `enemies-and-spawning.md`, `environmental-hazards.md`, `player-and-combat.md` và `skills-and-items.md` trong `docs/gameplay/`.

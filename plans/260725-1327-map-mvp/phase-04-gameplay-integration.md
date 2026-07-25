---
phase: 4
title: "Tích hợp gameplay"
status: pending
priority: P1
dependencies: [3]
---

# Phase 4: Tích hợp gameplay

## Tổng quan

Kết nối MapGrid với movement, spawn, item, hazard và telegraph qua API rõ ràng nhưng không triển khai toàn bộ các hệ thống đó.

## Yêu cầu

### Movement

- Standard Move validate destination qua MapGrid.
- Dash validate toàn path/endpoint trước resolve.
- Invalid input không đổi state, không consume movement/cooldown/resource.
- Mỗi entity có tối đa một self-directed movement hợp lệ mỗi beat.
- Move/Dash prototype đọc policy/config thay vì hard-code range, path color hoặc query limit.

### Spawn và item

- Spawn chỉ chọn cell/marker hợp lệ, walkable và không có actor.
- Skill item có thể tồn tại dưới actor và được nhặt khi landing hợp lệ.
- Map không quyết định enemy weight, cap hoặc spawn pressure.
- Map chỉ nhận spawn query config/marker pool; weight/cap/pressure thuộc spawn system về sau.

### Hazard

- Environmental Bomb có thể cùng cell với actor và kích hoạt khi player landing.
- Turret gắn cell non-walkable và truy vấn target trong range.
- Map hỗ trợ thứ tự tọa độ ổn định cho hazard resolution.
- Hazard không tự reposition player/enemy.

### Telegraph

- Overlay hiển thị Move/Dash path, range và impact area.
- Preview không thay đổi MapGrid.
- Cancel/resolve phải xóa overlay tương ứng.
- Overlay color/tile/priority và debug visibility chỉnh qua `MapOverlayConfig`.

## Kiến trúc tích hợp

```text
Action tạo candidate với policy/config
→ MapGrid trả spatial facts/query result
→ gameplay rule xác nhận cooldown/phase
→ MapGrid commit thay đổi nguyên khối
→ presentation/Transform animate tới state mới
```

Map chỉ trả lời câu hỏi không gian. Beat, cooldown, WC, damage và phase thuộc hệ thống gameplay tương ứng. Các policy chưa chốt dùng config có tên rõ và test harness, nhưng không được âm thầm biến `TBD` trong gameplay docs thành luật production.

## File liên quan

- Tạo: `countdown-game/Assets/_Game/Scripts/Gameplay/Map/MapQueryService.cs`
- Tạo: `countdown-game/Assets/_Game/Scripts/Gameplay/Map/MapOverlayController.cs`
- Có thể tạo: `countdown-game/Assets/_Game/Scripts/Gameplay/Map/GridRangeUtility.cs`
- Tạo: `countdown-game/Assets/_Game/Scripts/Gameplay/Map/Config/MapOverlayConfig.cs`
- Có thể tạo: `countdown-game/Assets/_Game/Scripts/Gameplay/Map/Config/MapQueryPolicyConfig.cs`
- Tạo: `countdown-game/Assets/_Game/Data/Map/MapOverlayConfig.asset`
- Sửa về sau: movement, spawn, item và hazard components khi các hệ thống đó được triển khai
- Sửa: `countdown-game/Assets/_Game/Prefabs/Map/MapRuntime.prefab`
- Có thể sửa: `countdown-game/Assets/_Game/Scenes/MapPrefabTest.unity` để test integration prefab

## Các bước triển khai

1. Định nghĩa query contract cho destination, path, range và policy/config input.
2. Tạo movement candidate không thay đổi state.
3. Chỉ commit MapGrid sau khi toàn bộ guard hợp lệ.
4. Tạo query spawn cell/marker và cơ chế reserve cell nếu spawn resolve theo batch.
5. Tạo item/hazard lookup độc lập với occupant.
6. Tạo range query phục vụ Bomb, Turret, Attack và telegraph.
7. Tạo comparator deterministic; nếu comparator ảnh hưởng gameplay, cập nhật đặc tả trước khi khóa.
8. Tạo overlay API dùng cùng query result với gameplay để preview không lệch resolve.
9. Tạo overlay/config sanity check để thiếu tile/color/policy invalid bị báo lỗi ngay khi scene start.

## Tiêu chí hoàn thành

- [ ] Move và Dash prototype dùng MapGrid thay vì Tilemap trực tiếp.
- [ ] Invalid candidate không để lại partial state.
- [ ] Spawn không chọn cell ngoài map, non-walkable hoặc occupied.
- [ ] Player landing có thể kích hoạt Bomb/nhặt item mà occupancy vẫn đúng.
- [ ] Telegraph và resolve dùng chung kết quả query.
- [ ] Không vô tình triển khai rule đang `TBD`.
- [ ] Đổi overlay style hoặc query policy hợp lệ trong config làm prototype đổi hành vi/hiển thị mà không sửa code.

## Rủi ro

- Preview và resolve dùng hai thuật toán khác nhau: trả về một query result dùng chung.
- Dash rule chưa chốt: tạo interface/query seam, không hard-code intermediate interaction.
- Hazard order ảnh hưởng kết quả: chốt comparator với gameplay docs trước khi production content phụ thuộc nó.
- Spawn race trong cùng beat: reserve cell trước khi instantiate.
- Config bị dùng như nơi giấu gameplay rule: đặt owner rõ cho từng config field và yêu cầu field ảnh hưởng gameplay phải có doc/source-of-truth tương ứng.

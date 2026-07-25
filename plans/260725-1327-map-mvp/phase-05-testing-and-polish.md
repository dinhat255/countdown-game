---
phase: 5
title: "Kiểm thử và hoàn thiện"
status: pending
priority: P1
dependencies: [4]
---

# Phase 5: Kiểm thử và hoàn thiện

## Tổng quan

Kiểm tra map foundation trong Editor và Play Mode, sửa lỗi dữ liệu/hiển thị, rồi khóa một scene MVP ổn định cho các hệ thống gameplay tiếp theo.

## Yêu cầu

- Không có compile error hoặc missing serialized reference.
- Các invariants của grid/occupancy được giữ sau mọi action thử nghiệm.
- Map authoring sai được phát hiện sớm.
- Debug overlay đủ để xác định lỗi tọa độ, path và range.
- Không tạo GC spike đáng kể từ query chạy mỗi nhịp.
- Config hợp lệ có thể thay đổi hành vi/hiển thị dự kiến; config sai bị validator chặn trước gameplay.

## Ma trận kiểm thử

| Nhóm | Tình huống | Kết quả mong đợi |
| --- | --- | --- |
| Tọa độ | World → grid → world | Trả đúng tâm cell |
| Bounds | Candidate ngoài map | Reject, state giữ nguyên |
| Terrain | Đi vào Wall/Obstacle | Reject |
| Occupancy | Hai actor cùng cell | Actor thứ hai bị reject |
| Layer data | Actor trên item/Bomb | Được phép |
| Movement | Invalid Move/Dash | Không partial movement |
| Spawn | Cell occupied/non-walkable | Không được chọn |
| Hazard | Bomb/Turret marker sai terrain | Validator báo lỗi |
| Telegraph | Preview rồi cancel | Overlay được xóa |
| Config | Đổi bounds/range/overlay/marker rule | Runtime hoặc validator phản ánh đúng |
| Config invalid | Thiếu reference/out-of-range/duplicate ID | Fail-fast với lỗi rõ ràng |
| Scene | Mở/Play/thoát Play Mode | Không mất reference |

## File liên quan

- Có thể tạo: `countdown-game/Assets/_Game/Scenes/MapSandbox.unity`
- Có thể tạo: `countdown-game/Assets/_Game/Scripts/Gameplay/Map/Debug/MapDebugOverlay.cs`
- Có thể tạo: `countdown-game/Assets/_Game/Scripts/Gameplay/Map/Tests/`
- Sửa: các file Map trong phase 1–4 khi phát hiện lỗi
- Sửa: `countdown-game/Assets/_Game/Prefabs/Map/MapRuntime.prefab`
- Có thể sửa: `countdown-game/Assets/_Game/Scenes/MapPrefabTest.unity`

## Các bước triển khai

1. Chạy validator trên map hợp lệ và các bản cố tình đặt marker sai.
2. Kiểm tra world/grid conversion ở biên map và cell âm.
3. Chạy prototype Move/Dash qua hành lang, góc và vùng occupied.
4. Kiểm tra item/Bomb cùng cell với actor.
5. Kiểm tra spawn reserve nhiều candidate trong cùng end-of-beat.
6. Kiểm tra overlay sau valid, invalid, cancel và scene reload.
7. Chạy config audit: đổi từng config field MVP ít nhất một lần và xác nhận không cần sửa code.
8. Thử config invalid: missing tile/reference, duplicate marker ID, bounds quá lớn/nhỏ, range cap invalid.
9. Dùng Unity Profiler kiểm tra allocation từ neighbor/range query.
10. Kiểm tra Console, missing script, missing reference và scene dirty state.
11. Ghi lại rule gameplay chưa chốt; không sửa docs bằng giả định.

## Tiêu chí hoàn thành

- [ ] Tất cả tình huống trong ma trận kiểm thử đạt.
- [ ] Unity Console sạch compile error.
- [ ] `MapPrefabTest.unity` và `MapRuntime.prefab` không có missing script/reference.
- [ ] Map Validator báo đúng các case sai.
- [ ] Query thường dùng không tạo allocation lặp lại đáng kể.
- [ ] Test/harness dùng ít nhất hai config hợp lệ và một config invalid cho validator.
- [ ] Một thành viên khác có thể mở scene và hiểu hierarchy mà không cần giải thích miệng.

## Rủi ro

- Test Framework không còn là direct package: ưu tiên test harness/Play Mode checklist; chỉ thêm package khi team thống nhất.
- Final art thay đổi kích thước sprite: giữ logic theo cell, không theo sprite bounds.
- Scope creep sang AI/combat: chỉ tạo integration seam và mock/prototype tối thiểu.
- "Mọi thứ đều chỉnh được" làm mất invariant: checklist phân loại rõ Tunable, Fixed-by-spec và TBD trước khi bàn giao.

## Bàn giao

- Scene MVP đã validate.
- Danh sách API MapGrid/MapQueryService.
- Danh sách config asset, owner, default value, min/max và field nào cần gameplay approval trước khi đổi.
- Danh sách quyết định gameplay còn mở.
- Kết quả Unity Console, Play Mode và Profiler được ghi trong session memory.

# Session Memory: Review Map Plan

Date: 2026-07-25 13:51  
Status: `COMPLETED`

## User request

Review kế hoạch làm map MVP hiện có; sau đó cập nhật plan để map dễ điều chỉnh bằng config.

## Context loaded

- `memory_bank/README.md`, template và session tạo plan map gần nhất.
- Toàn bộ plan trong `plans/260725-1327-map-mvp/`.
- Gameplay spec liên quan map, movement, spawn, item, hazard, telegraph và beat ordering.
- Unity project inventory, package manifest/lock, build settings và Unity version `6000.5.5f1`.

## Plan

1. Fact-check plan với gameplay spec và project hiện tại.
2. Chạy red-team review theo các lens bắt buộc, lọc findings bằng bằng chứng `file:line`.
3. Tổng hợp findings đã adjudicate và đề xuất phạm vi sửa.
4. Khi user yêu cầu dễ điều chỉnh, cập nhật plan theo hướng data-driven config và chạy consistency checks.

## TODO

- [x] Thu kết quả từ các reviewer và loại finding trùng/không đủ bằng chứng.
- [x] Hoàn tất review theo severity và ghi trạng thái verification.
- [x] Cập nhật plan với thiết kế config/tunability.
- [x] Validate Markdown structure sau khi sửa.

## What was done

- Xác định plan map MVP gồm năm phase, hiện chưa triển khai code/asset gameplay.
- Xác nhận project vẫn build từ `Assets/Scenes/SampleScene.unity`.
- Xác nhận Unity Test Framework có mặt trong `packages-lock.json` dưới dạng dependency gián tiếp.
- Red-team bằng ba lens và adjudicate còn tám finding: một Critical, sáu High, một Medium.
- Cập nhật plan để đưa `MapGridConfig`, `MapAuthoringConfig`, `MapOverlayConfig`, config validation và config audit vào phase 1-5.

## Files touched

- `memory_bank/2026-07-25-1351-review-map-plan.md` — handoff cho investigation review plan.
- `plans/260725-1327-map-mvp/plan.md` — thêm nguyên tắc data-driven tunability.
- `plans/260725-1327-map-mvp/phase-01-scene-tilemap-foundation.md` — thêm scene/presentation config.
- `plans/260725-1327-map-mvp/phase-02-grid-logic-and-occupancy.md` — thêm grid config/settings immutable.
- `plans/260725-1327-map-mvp/phase-03-map-authoring-and-validation.md` — thêm authoring config và validator theo config.
- `plans/260725-1327-map-mvp/phase-04-gameplay-integration.md` — thêm query/policy/overlay config.
- `plans/260725-1327-map-mvp/phase-05-testing-and-polish.md` — thêm config audit và invalid config checks.

## Key decisions

- Review ban đầu là read-only; sau yêu cầu mới của user, plan được chỉnh trong phạm vi tunability/config.
- Finding không có bằng chứng `file:line` sẽ không được đưa vào kết luận.
- Loại cảnh báo animation recovery và static-state leak vì vượt phạm vi hoặc chỉ là giả định khi plan chưa đề xuất static state.
- Dễ điều chỉnh không đồng nghĩa mọi invariant thành setting; plan phân loại Tunable, Fixed-by-spec và TBD.

## Verification

- Documentation checks: `PASS` — sáu file plan có đúng một H1, code fence cân bằng và link Markdown nội bộ hợp lệ sau chỉnh tunability.
- Unity compilation: `NOT RUN`
- Unity tests: `NOT RUN`
- Play Mode: `NOT RUN`
- Other: `PASS` — đối chiếu Unity `6000.5.5f1`, build settings, package lock, project inventory và gameplay spec; `ck plan status` không chạy được vì `ck` không có trong PATH.

## Blockers and next steps

- Yêu cầu tunability đã được áp dụng; vẫn nên xử lý các finding review còn lại trước khi implement toàn bộ plan.
- Quyết định phạm vi Phase 4: vertical slice chạy thật hoặc contract tests với fake consumers.

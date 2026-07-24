# Enemy và hệ thống spawn

## Tổng quan

Enemy pool hiện hành có Runner, Jumper và Thrower. Thrower là Enemy Lv3.

## Enemy Phase

- Chạy một lần sau Player Phase.
- Mỗi enemy resolve finite action sequence.
- Mỗi enemy resolve tối đa một self-directed valid movement.
- AI candidate invalid bị reject trước resolve.
- Freeze làm toàn bộ enemy bỏ phase; pending Throw telegraph giữ Paused.

## Validate movement AI

Trước Move/Jump:

- Ô đích/đáp trong map.
- Không phải tường/vật cản.
- Thỏa occupancy/range/path rule.
- Enemy chưa `SelfMovedThisBeat`.

Candidate invalid:

- Không resolve.
- Không tạo failure state.
- Không tiêu cooldown/resource.
- AI có thể chọn candidate hoặc stationary action khác.

Valid self-movement đổi vị trí enemy và đặt `SelfMovedThisBeat = true`.

## Action không dùng self-movement

Ví dụ:

- Runner Attack.
- Jumper telegraph.
- Status/ability không đổi vị trí.

Các action này không dùng cơ hội self-movement của actor và phải có cooldown/condition/per-beat guard. Thrower dùng finite action sequence riêng; Throw relocation là non-self-movement exception duy nhất có thể đổi vị trí một enemy target.

## Enemy HP và hit

- Enemy có HP và nhận outgoing damage.
- Enemy hit lên player cộng WC penalty và có thể áp status được định nghĩa.
- Hit/status không đổi vị trí actor hoặc player target.
- Ngoại lệ duy nhất: Thrower reposition Runner/Jumper target khi Throw resolve.

Environmental Bomb explosion và Turret shot hiện chỉ gây damage lên enemy hoặc cộng WC lên player, không áp status. Đây là neutral friendly fire; hazard không reposition entity.

## Spawn cooldown

```text
SpawnCooldown -= 1 mỗi nhịp
```

Nếu player resolve valid standard Move:

```text
SpawnCooldown -= MovePressure
```

Nếu player resolve valid Dash:

```text
SpawnCooldown -= DashSpawnPressure
```

`DashSpawnPressure` mạnh hơn `MovePressure`; amount/formula là `TBD`. Không cộng cả hai và có tối đa một pressure event/beat. Mọi enemy action đều không tạo pressure.

## Quy trình spawn cuối nhịp

1. Resolve due effects và cập nhật Lowest/Highest WC cùng phase.
2. Nếu `WC ≤ 0`, Victory short-circuit; không tick/spawn.
3. Nếu chưa Victory, base tick.
4. Thêm Move Pressure nếu standard Move, hoặc Dash Spawn Pressure nếu Dash.
5. Nếu cooldown `≤ 0`, kiểm tra cap.
6. Dùng phase mới để chọn điểm và loại hợp lệ.
7. Spawn và reset.

Hệ thống xử lý tối đa một đợt spawn/nhịp. Overflow là `TBD`.
Move Pressure và Dash Spawn Pressure là active mechanics; chỉ amount/formula là `TBD`.

## Trọng số phase hiện tại

| Phase | Enemy pool |
| --- | --- |
| Phase 1 | Runner weight cao nhất; Jumper/Thrower vẫn có thể spawn |
| Phase 2 | Jumper weight tăng; Runner/Thrower vẫn có thể spawn |
| Phase 3 | Thrower weight tăng; Runner/Jumper vẫn có thể spawn |

Thrower cap và weight cụ thể là `TBD`.

## Runner

- Valid Move một ô về player.
- Attack tại chỗ nếu trong range và cooldown Ready.
- Có thể Attack và Move cùng Enemy Phase theo priority `TBD`.

## Jumper

Jump là self-directed movement của Jumper:

```text
J . X
```

- Validate ô đáp trước resolve.
- Invalid Jump bị reject; không có failed Jump.
- Valid Jump đổi vị trí, đặt `SelfMovedThisBeat = true`.
- Nếu ô đáp hit player, cộng WC penalty.
- Telegraph là stationary action.

Đề xuất hai nhịp: telegraph rồi Jump ở Enemy Phase kế tiếp.

## Thrower

Thrower là Enemy Lv3:

- Có thể tự valid AI Move tối đa một lần/beat.
- Self-Move đặt `SelfMovedThisBeat = true`.
- Throw là non-self-movement action có cooldown/per-beat guard.

### Nhịp chuẩn bị

1. Tìm Runner/Jumper hợp lệ trong pickup range.
2. Chọn và khóa target.
3. Xác định đường ném và ô/vùng đáp hướng về player.
4. Hiển thị target, trajectory và landing telegraph.

### Nhịp resolve

1. Revalidate đúng locked target, path và landing.
2. Nếu bất kỳ phần nào invalid, cancel Throw; không retarget âm thầm.
3. Nếu valid, reposition target tới landing.
4. Nếu player trong impact area, tạo hit và cộng WC penalty.
5. Player không đổi vị trí.

Throw relocation:

- Không set, clear, reset hoặc consume `SelfMovedThisBeat` của target.
- Nếu flag false và lượt AI của target chưa qua, target vẫn có thể self-Move; nếu true thì không được Move lại.
- Eligibility đã chốt; chỉ timing/AI ordering sau landing là `TBD`.
- Không khôi phục push/pull/throw player hoặc forced reposition khác.

AI ordering, pickup/throw range, cooldown, impact AOE và cap là `TBD`.

## Freeze

Freeze chặn toàn Enemy Phase:

- Không Move/Jump.
- Không Attack.
- Không telegraph.
- Không Throw.
- Không stationary action khác.

- Pending Throw telegraph giữ trạng thái Paused; không đổi lock hoặc tiến sang resolve.
- Locked target/path/landing được revalidate ở Enemy Phase đầu tiên hết Freeze.
- Environmental Bomb/Turret vẫn tick/resolve/fire theo end-of-beat rule; Freeze không ảnh hưởng hazard.
- Nếu chưa Victory, spawn vẫn tick; enemy mới spawn nhận Freeze duration còn lại.

## Tài liệu liên quan

- [Hệ thống nhịp](./beat-and-action-system.md)
- [WC và phase](./win-condition-and-progression.md)
- [Map và UI](./map-ui-and-game-flow.md)
- [Environmental Hazards](./environmental-hazards.md)

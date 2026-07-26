# Hệ thống nhịp và hành động

## Loop

```text
Player Phase → Enemy Phase → End-of-beat Update
```

Đầu beat, `SelfMovedThisBeat` của mọi actor reset về false. Với player, `PlayerMovedThisBeat` là alias của flag này.

Player resolve tối đa một valid action trong beat: standard Move, Dash, Attack hoặc một Active skill. Enemy vẫn resolve tối đa một self-directed valid movement theo rule riêng. Runner Move, Jumper Move/Jump và Thrower Move dùng cap của từng enemy. Throw relocation không phải self-movement và không đổi movement flag của target.

Player Phase không có input End Beat. Timer tự kết thúc phase theo WC hiện tại:

| WC hiện tại | Thời lượng Player Phase |
| --- | ---: |
| `WC > 10` | `2,4s` |
| `5 < WC ≤ 10` | `1,8s` |
| `0 < WC ≤ 5` | `1,6s` |

Mốc `WC = 10` thuộc tier `1,8s`; `WC = 5` thuộc tier `1,6s`. Nếu WC đổi trong Player Phase, duration áp dụng tier mới ngay mà không reset elapsed time. Timer chỉ chạy trong Player Phase gameplay đang active, pause khi dialog/replacement modal chặn gameplay, và reset ở đầu beat mới. `WC ≤ 0` được xử lý bởi Victory ở end-of-beat, không mở beat gameplay mới.

## Validate trước resolve

Candidate được kiểm tra đầy đủ trước mutation:

- Đúng phase và action còn hợp lệ.
- Slot, Mana, target/range và per-beat guard hợp lệ.
- Player chưa dùng valid action khác trong beat; enemy chưa self-move nếu candidate là movement.
- Toàn path/endpoint trong map, walkable và occupancy hợp lệ.

Candidate invalid bị reject, không tạo failure state, không trừ Mana, không xóa skill, không cộng WC/pressure, không partial movement và không consume player action.

## Player Phase

Action đầu tiên resolve hợp lệ consume toàn bộ player action của beat. Sau valid standard Move, Dash, Attack hoặc Active skill, mọi skill/Attack/Move/Dash khác bị reject tới beat sau. Active hợp lệ trừ Mana và bị xóa ngay. Chỉ standard Move hoặc Dash đổi vị trí và đặt `PlayerMovedThisBeat = true`.

Dash đi ba ô theo facing trong một resolve, dùng một consumable slot, cộng `2 WC` đúng một lần và tạo Dash Pressure. Standard Move tạo Move Pressure. Chỉ landing của Move/Dash nhặt ground item.

## Enemy Phase

Nếu Freeze armed, toàn Enemy Phase bị skip. Pending telegraph hiển thị Paused nhưng original lock giữ nguyên.

Nếu không Freeze, enemy chạy theo stable spawn order. Runner, Jumper và Thrower validate rồi resolve finite action sequence. Thrower có thể relocate Runner/Jumper mà không set, clear hoặc reset movement flag của target.

Enemy hit gọi chung player-hit resolver: WC Dampener và Ward chỉ sửa phần WC penalty; status không reposition player.

## End-of-beat Update

Thứ tự canonical:

1. Hoàn tất hoặc skip Enemy Phase; chốt `PlayerMovedThisBeat`.
2. Nếu no-move: giảm WC, tăng Standing Streak rồi hồi Mana `2` hoặc `3` với Meditation, clamp `6`. Nếu moved: reset streak và không hồi.
3. Tick Bomb skill fuse và status duration bất kể movement.
4. Resolve due effects: Bomb skill theo placement order → Environmental Bomb theo stable map order → Turret theo stable map order. Mỗi damage/WC effect áp ngay.
5. Cập nhật Lowest/Highest WC, phase và threshold.
6. Nếu `WC ≤ 0`, Victory short-circuit spawn, skill drop và pending UI.
7. Nếu chưa Victory, tick/spawn enemy bằng pressure hiện tại.
8. Ở completed beat `3, 6, 9…`, thử drop một skill bằng RNG channel riêng.
9. UI priority: high-WC dialog → pickup replacement → phase panel → beat mới.

Attack cooldown và environmental hazard timers vẫn theo rule riêng đã mô tả ở tài liệu tương ứng. Active skill không còn cooldown; consumable slot đã dùng trở thành empty.

## Freeze, Ward và timer

- Freeze chỉ skip Enemy Phase; hazards và Bomb fuse vẫn chạy.
- Ward còn armed hết hạn trước Player Phase kế tiếp nếu chưa chặn hit.
- Stationary skill consume player action nhưng không làm mất no-move WC/Mana update.
- Valid Move/Dash ngăn Mana restoration; rejected movement không ngăn.
- Bomb fuse luôn tick, kể cả beat có Move/Dash.

## Input buffer

Chỉ buffer action kế tiếp và luôn validate lại trước resolve. Invalid input bị bỏ nhưng player có thể nhập candidate khác. Sau bất kỳ valid player action nào, mọi input skill/Attack/Move/Dash khác bị khóa tới beat sau.

Beat timeout không phải player action, không đổi `PlayerMovedThisBeat` và không hoàn tác action đã resolve. Khi timer hết, phase đi qua cùng canonical Enemy Phase và end-of-beat update.

## Tài liệu liên quan

- [WC và phase](./win-condition-and-progression.md)
- [Player và combat](./player-and-combat.md)
- [Enemy và spawn](./enemies-and-spawning.md)
- [Skill và item](./skills-and-items.md)
- [Environmental Hazards](./environmental-hazards.md)

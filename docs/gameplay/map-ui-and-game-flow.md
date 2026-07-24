# Map, UI và game flow

## Tổng quan

UI phải truyền đạt phase, self-movement availability, Dash path/cooldown, WC không giới hạn và Thrower telegraph. Không có HP hoặc dual movement flag.

## Map

- Grid với ground, wall và obstacle.
- Player/enemy/item spawn.
- Environmental Bomb đặt sẵn trên ground.
- Turret đặt sẵn trên wall/non-walkable.
- Bomb skill do player đặt và phải có icon/name khác Environmental Bomb.
- Player tự di chuyển bằng standard Move/Dash; ngoài self-directed movement, chỉ Thrower được reposition Runner/Jumper target.

## UI Player Phase

- Timer.
- End Beat.
- `PlayerMovedThisBeat: No/Yes`.
- Move input hợp lệ/không hợp lệ.
- Attack/skill cooldown, gồm Dash.
- Dash facing path, intermediate tiles, endpoint và landing validity.
- Preview Dash `+WC` và Dash Spawn Pressure.
- Action result.

Ví dụ:

```text
PLAYER PHASE  1.2s
Moved This Beat: No
[Move] [Dash] [Attack] [Bomb skill] [Refresh] [End Beat]
```

Invalid Move bị reject và UI giải thích lý do; player có thể chọn hướng khác.
Invalid Dash reject toàn bộ path và hiển thị lý do; không preview partial movement/cooldown/WC/pressure.

Valid standard Move hoặc Dash landing vào Environmental Bomb phải hiện ngay Dormant → Active, Countdown và no tick trong activation beat. Intermediate Dash Bomb/item interaction hiển thị `TBD`, không tự suy diễn.

## UI Enemy Phase

- Enemy đang resolve.
- `SelfMovedThisBeat` của enemy khi cần debug.
- Runner/Jumper/Thrower action.
- Jumper telegraph/ô đáp.
- Thrower locked target, trajectory và ô/vùng đáp.
- Freeze skip phase.
- Pending Throw telegraph hiển thị Paused khi Freeze; giữ original lock tới Enemy Phase đầu tiên hết Freeze rồi mới revalidate.

Khi Throw bị cancel sau revalidation, UI phải xóa telegraph và báo cancel; không hiển thị target mới âm thầm.

## Preview cuối nhịp

- WC reduction.
- Standing Streak.
- Active skill cooldown tick/pause; skill vừa dùng vẫn tick ở no-Move beat.
- Attack cooldown chỉ tick ở no-Move beat nếu không vừa được bắt đầu.
- Environmental Bomb Countdown/Turret Reload `> 0` tick hoặc pause.
- Bomb skill fuse/status tick bất kể self-movement.
- Turret Ready `0` fire-check kể cả standard Move/Dash beat; không target vẫn giữ Ready.
- Move Pressure cho standard Move hoặc Dash Spawn Pressure mạnh hơn; không cộng cả hai.
- Movement-used state khóa cả Move và Dash sau valid self-movement, kể cả khi Refresh reset Dash CD.

Sau valid standard Move/Dash, preview no-movement chuyển off và giữ nguyên đến hết nhịp.

## WC động

- Hiện WC current và Initial baseline.
- Hỗ trợ WC vượt baseline.
- Marker phase đi xuống và high-WC threshold đi lên.
- Phân biệt threshold đã handled/chưa chạm.
- Hiện `-WC` và `+WC`.

Không hiển thị HP/Loss/Game Over.

## Telegraph

- Jumper ô đáp.
- Thrower target, đường ném và impact area.
- Dash facing path/landing, enemy traverse, invalid segment và endpoint.
- Bomb skill area.
- Environmental Bomb Dormant/Active, activation no-tick, Countdown và blast area.
- Turret Reload, range, Ready, aim và shot.
- Freeze.
- Player Attack area.

Telegraph không đổi vị trí. Thrower chỉ reposition locked Runner/Jumper khi Throw resolve hợp lệ.

## High-WC dialog

Ví dụ:

> Bạn vẫn muốn chơi tiếp chứ?

- Continue: xử lý UI pending.
- Exit: `VoluntaryExit/Quit`.
- Pause phase/timer/spawn/cooldown.
- Environmental hazard Countdown/Reload cũng pause.

## Phase và skill panel

- Phase panel dùng `LowestWCReached`.
- High-WC dialog dùng `HighestWCReached`.
- Skill replacement mở sau high-WC dialog nếu pending.

## Luồng

```text
Reset SelfMovedThisBeat
→ Player Phase
   → stationary action hợp lệ
   → tối đa một valid standard Move hoặc Dash
→ Enemy Phase
   → mỗi enemy finite action sequence
   → tối đa một valid Move/Jump
   → Thrower có thể resolve locked Throw
→ End-of-beat update
   → chốt PlayerMovedThisBeat
   → no-Move WC/streak/slot CD/eligible Attack CD/hazard timer
   → Bomb fuse/status tick mọi beat
   → Bomb placement order → Env Bomb stable map order → Turret stable map order
   → áp damage/WC ngay sau từng effect
   → Lowest/Highest WC + phase/threshold
   → Victory: short-circuit spawn và UI pending
   → nếu chưa Victory: base spawn + một Move/Dash pressure event + cap/type theo phase mới
   → high-WC dialog → skill replacement → phase panel → nhịp mới
```

## Feedback quan trọng

- Invalid candidate bị reject.
- Valid standard Move/Dash, shared movement flag và movement-used lock.
- Hit/WC penalty, status nếu được định nghĩa; không status nào reposition player.
- Outgoing damage.
- Cooldown/resource.
- Environmental Bomb/Turret state và distinct icon.
- Move Pressure/Dash Spawn Pressure.
- Threshold/phase.

## Tài liệu liên quan

- [Tóm tắt gameplay](./gameplay-summary.md)
- [Hệ thống nhịp](./beat-and-action-system.md)
- [Skill và item](./skills-and-items.md)
- [Environmental Hazards](./environmental-hazards.md)

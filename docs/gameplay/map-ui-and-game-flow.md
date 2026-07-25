# Map, UI và game flow

## Map

Map là grid walkable/non-walkable với actor, spawn cell và non-blocking overlays.

- Skill item chỉ spawn trên empty walkable non-spawn cell không có actor/item/bomb/hazard.
- Tối đa hai skill item trên ground.
- Enemy có thể đứng trên item nhưng không nhặt.
- Chỉ valid player Move/Dash landing thu item; intermediate Dash cell không thu.
- Click trực tiếp ground item chọn chính xác logical cell của item. Nếu item chưa kề player, input chọn một standard Move hợp lệ tiến về item; mỗi beat vẫn chỉ resolve tối đa một self-movement.
- Bomb skill và Environmental Bomb phải có presentation khác nhau.

## Player HUD

HUD luôn hiển thị:

- Phase, timer, End Beat và `PlayerMovedThisBeat`.
- WC current/baseline và phase thresholds.
- Mana `current/6` cùng predicted `+2` hoặc `+3` nếu no-move.
- Ba Active consumable slot: icon, cost, targeting state và invalid reason.
- Một Passive slot.
- Dash facing path, landing, `+2 WC` và pressure.
- Ward/Freeze armed state, Bomb fuse và enemy HP.

Ví dụ:

```text
PLAYER PHASE     WC 12     Mana 3/6 (+2 no-move)
Moved: No
[Dash 1] [Bomb 2] [Empty]     Passive: Meditation
```

Invalid action hiển thị failure reason mà không preview resource/slot mutation.

## Ground item và replacement UI

Ground item presentation hiển thị category/level/icon. Pickup vào empty matching slot xảy ra ngay.

Nếu matching slot type đầy, ground item biến mất và replacement panel được queue sau beat. Pending Active tự điền nếu Active slot trống trước panel. Nếu không, panel chỉ cho:

- Replace một Active slot hoặc discard Active mới.
- Replace Passive slot hoặc discard Passive mới.

High-WC dialog có priority trước replacement; Victory hủy pending UI.

## Enemy/hazard telegraph

UI hiển thị Jumper landing, Thrower locked target/path/impact, Freeze Paused state, Bomb areas/fuse, Environmental Bomb lifecycle và Turret reload/ready. Telegraph không reposition actor.

## Canonical flow

```text
Reset movement flags
→ Player Phase
   → stationary consumable skills nếu hợp lệ
   → tối đa một valid Move hoặc Dash
→ Enemy Phase hoặc Freeze skip
→ End-of-beat
   → no-move WC/streak rồi Mana restore
   → Bomb fuse/status tick
   → Bomb → Environmental Bomb → Turret due effects
   → threshold/phase
   → Victory short-circuit
   → enemy spawn
   → mỗi beat thứ ba thử skill drop
   → high-WC → replacement → phase panel → beat mới
```

Pause/dialog phải pause phase, timer, spawn, drop và hazard progression.

## Tài liệu liên quan

- [Tóm tắt gameplay](./gameplay-summary.md)
- [Hệ thống nhịp](./beat-and-action-system.md)
- [Skill và item](./skills-and-items.md)
- [Environmental Hazards](./environmental-hazards.md)

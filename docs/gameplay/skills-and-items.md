# Skill và item

## Tổng quan

Player có ba slot Active dùng một lần, một slot Passive tồn tại lâu dài và Mana.

- Mana tối đa `6`, bắt đầu run với `3`.
- Active hợp lệ trừ Mana rồi biến mất khỏi slot ngay.
- Active không hợp lệ không trừ Mana, không xóa skill và không tạo side effect.
- Passive không tốn Mana, không stack và giữ nguyên tới khi bị thay.
- Chỉ Dash đổi vị trí player; mọi skill khác là stationary action.
- Refresh và cooldown của Active không còn trong gameplay.

## Mana

Cuối beat, nếu `PlayerMovedThisBeat = false`, player hồi `2` Mana sau cập nhật WC/Standing Streak và trước due hazards. Meditation đổi lượng hồi thành `3`. Mana luôn clamp ở `6`.

Valid standard Move hoặc Dash ngăn hồi Mana. Movement bị reject không đổi `PlayerMovedThisBeat`, nên player vẫn đủ điều kiện hồi. Dùng một stationary skill consume player action nhưng không ngăn hồi.

## Slot và dùng skill

```text
[Active 1] [Active 2] [Active 3] [Passive]
```

Một Active chỉ resolve khi phase, player action, slot, Mana, target, path, movement cap và guard hiệu lực đều hợp lệ. Toàn action được validate trước khi trừ Mana hoặc xóa slot. Mỗi beat chỉ một Active, Attack, Move hoặc Dash có thể resolve thành công; invalid candidate không consume action.

Active trùng nhau được phép. Passive chỉ có một slot nên không stack.

Player Phase tự timeout theo tier WC hiện tại. Targeting không tạo action và không reset timer; chỉ valid resolve mới consume action. Replacement/dialog modal pause timer theo game-flow rule.

## Starter Active

| Level | Skill | Mana | Hiệu ứng |
| --- | --- | ---: | --- |
| 1 | Dash | 1 | Đi ba ô theo facing; `+2 WC`, Dash Pressure và shared self-movement cap |
| 1 | Snipe | 2 | Gây `3` damage lên enemy đầu tiên trong bốn ô facing; wall chặn |
| 1 | Ward | 2 | Chặn WC tăng từ player hit kế tiếp trước Player Phase sau; status vẫn áp |
| 2 | Bomb | 2 | Đặt bomb fuse hai beat trong Manhattan range `2` |
| 2 | Shockwave | 3 | Gây `2` damage lên mọi enemy trong tám ô quanh player; cần ít nhất một target |
| 3 | Freeze | 4 | Skip Enemy Phase ngay sau đó; hazards vẫn resolve |

Damage Up cộng `1` vào Snipe, Shockwave và Bomb. Player Attack là instant kill nên không nhận modifier.

### Dash

- Validate cả path ba ô và endpoint trước resolve.
- Không xuyên wall/obstacle; có thể đi qua enemy nhưng endpoint không được occupied.
- Dùng chung một self-movement opportunity với standard Move.
- Invalid Dash không partial move, không trừ Mana, không xóa skill, không cộng WC và không tạo pressure.
- Valid Dash đặt `PlayerMovedThisBeat = true`, cộng `2 WC` đúng một lần và chỉ nhặt item tại landing; intermediate cells không tương tác item.

### Snipe

Snipe quét tối đa bốn ô theo facing. Wall kết thúc ray. Enemy đầu tiên trên ray nhận damage; không có target hợp lệ thì action bị reject.

### Ward

Ward chỉ chặn phần WC tăng của hit kế tiếp; status đi kèm hit vẫn áp. Ward còn hiệu lực nhưng chưa bị consume sẽ hết trước Player Phase kế tiếp. Dùng Ward khi Ward đã armed bị reject.

### Bomb

- Ô đặt phải walkable, không có actor, item, bomb hoặc environmental hazard và cách player tối đa Manhattan `2`.
- Bomb không đổi vị trí actor.
- Fuse bắt đầu ở `2`, giảm mỗi end-of-beat bất kể self-movement.
- Fuse về `0` nổ ngay; nhiều Bomb resolve theo placement order.
- Blast `3×3` gây `2` player-originated damage lên enemy và không friendly fire player.

### Shockwave

Shockwave đánh tám ô kề theo Chebyshev distance `1`. Tất cả enemy hợp lệ nhận damage theo stable spawn order. Không có enemy trong vùng thì action bị reject.

### Freeze

Freeze skip toàn bộ Enemy Phase ngay sau Player Phase hiện tại. Pending Jumper/Thrower telegraph giữ Paused và được revalidate ở Enemy Phase đầu tiên không bị Freeze. Environmental Bomb, Turret và Bomb fuse vẫn tick/resolve. Mọi action khác trong cùng beat bị reject bởi one-action guard.

## Starter Passive

| Passive | Hiệu ứng |
| --- | --- |
| WC Dampener | Giảm mỗi hit-based WC penalty `1`, tối thiểu `0`; không giảm WC của Dash |
| Damage Up | Cộng `1` offensive-skill damage; không tăng player instant-kill Attack hoặc neutral hazard |
| Meditation | Tăng no-move Mana restoration từ `2` lên `3` |

## Ground drop

Sau enemy spawn ở beat hoàn tất `3, 6, 9…`, nếu chưa Victory, game thử tạo một skill item.

- Tối đa hai skill item tồn tại trên map; drop bị skip không được queue.
- Drop dùng deterministic RNG channel riêng enemy spawn.
- Trọng số level mặc định: Phase 1 `60/30/10`, Phase 2 `30/50/20`, Phase 3 `20/35/45`.
- Sau khi chọn level, skill trong level đó được chọn đều.
- Ô drop phải walkable, không phải spawn cell và không có actor, item, bomb hoặc environmental hazard.
- Item là overlay không block movement; enemy có thể đứng trên item nhưng không nhặt.

Player chỉ nhặt item khi valid standard Move hoặc Dash landing lên ô đó. Intermediate Dash cell không nhặt.

## Pickup và replacement

- Active vào Active slot trống đầu tiên.
- Passive vào Passive slot nếu đang trống.
- Nếu matching slot type đầy, item bị xóa khỏi ground và skill mới vào pending pickup.
- Pending Active tự điền nếu một Active slot trống trước khi replacement panel mở.
- Nếu vẫn đầy, player thay đúng loại slot hoặc discard skill mới.
- Passive pending chỉ có thể thay Passive hiện tại hoặc bị discard.

Victory short-circuit drop và pending UI. Priority còn lại là high-WC dialog → pickup replacement → phase panel.

## UI skill

UI hiển thị:

- Mana `current/max` và dự đoán `+2` hoặc `+3` khi no-move.
- Ba Active slot với icon, Mana cost, targeting và invalid reason.
- Một Passive slot.
- Dash path, landing validity, `+2 WC`, movement-used state và pressure.
- Ground item, Bomb fuse, Ward/Freeze armed state.
- Replacement/discard panel đúng category.

## Tài liệu liên quan

- [Hệ thống nhịp](./beat-and-action-system.md)
- [Player và combat](./player-and-combat.md)
- [Map và UI](./map-ui-and-game-flow.md)
- [Environmental Hazards](./environmental-hazards.md)

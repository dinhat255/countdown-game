# Countdown — Tóm tắt gameplay

## Game trong một câu

**Countdown** là game chiến thuật theo beat trên grid: Move/Dash để kiểm soát vị trí nhưng tăng pressure; đứng lại để giảm WC và hồi Mana cho consumable skills.

## Mục tiêu

Đưa Win Condition Cooldown (`WC`) về `0` hoặc thấp hơn. Player không có HP, Death, Loss hoặc Game Over. Hit cộng WC và có thể áp status nhưng không reposition player. Enemy có HP; giết hết enemy không phải điều kiện thắng.

## Một beat

```text
Player Phase → Enemy Phase → End-of-beat Update
```

Player chỉ có một valid action/beat: standard Move, Dash, Attack hoặc một Active skill. Mỗi enemy chỉ có một self-directed valid movement/beat theo rule riêng. Invalid candidate không mutate state, tiêu resource hoặc consume player action.

Nếu player không valid self-move:

- Standing Streak tăng; WC giảm trừ beat có successful player Attack.
- Mana hồi `2`, hoặc `3` với Meditation, tối đa `6`.
- Stationary skills không ngăn các update này; successful player Attack chỉ ngăn WC reduction.

Nếu player valid Move/Dash:

- `PlayerMovedThisBeat = true`, streak reset và không hồi Mana.
- Move tạo Move Pressure.
- Dash dùng consumable skill, tốn `1` Mana, đi ba ô, cộng `2 WC` và tạo Dash Pressure.

## Mana, slot và starter skills

Run bắt đầu `3/6` Mana. Player có ba Active slot dùng một lần và một Passive slot bền vững. Active chỉ trừ Mana và biến mất sau khi toàn action đã validate thành công. Passive không tốn Mana và giữ tới khi bị thay. Refresh và Active cooldown không còn.

| Active | Cost | Effect |
| --- | ---: | --- |
| Dash | 1 | Ba ô theo facing, shared movement cap, `+2 WC`, Dash Pressure |
| Snipe | 2 | `3` damage lên enemy đầu tiên trong bốn facing cells |
| Ward | 2 | Chặn WC penalty của hit kế tiếp trước Player Phase sau |
| Bomb | 2 | Đặt fuse hai beat; blast `3×3`, `2` damage, không friendly fire |
| Shockwave | 3 | `2` damage lên mọi enemy kề; cần target |
| Freeze | 4 | Skip Enemy Phase kế tiếp; hazards vẫn chạy |

| Passive | Effect |
| --- | --- |
| WC Dampener | Hit-based WC penalty `-1`, tối thiểu `0`; không áp Dash |
| Damage Up | Offensive skill damage `+1`; player instant-kill Attack và neutral hazard không tăng |
| Meditation | No-move Mana restore `2 → 3` |

## Drop và pickup

Sau enemy spawn ở completed beat `3, 6, 9…`, game thử drop một item bằng deterministic RNG channel riêng. Có tối đa hai ground items; skipped drop không queue. Default level weights là Phase 1 `60/30/10`, Phase 2 `30/50/20`, Phase 3 `20/35/45`.

Item là non-blocking overlay. Chỉ landing của valid player Move/Dash nhặt item. Active vào empty Active slot đầu tiên; Passive vào empty Passive slot. Nếu matching type đầy, item biến mất và player replace đúng loại hoặc discard. Pending Active auto-fill nếu slot trống trước panel.

## Combat và hazards

Enemy maximum HP cấu hình theo Runner/Jumper/Thrower. Snipe, Shockwave và Bomb gây damage. Player Attack chọn và giết một enemy ở ô cardinal kề bên qua shared damage/death resolver. Attack không đổi vị trí hoặc movement flag, nhưng consume player action nên player không thể dùng skill, Attack, Move hoặc Dash khác trong cùng beat. Successful Attack cũng chặn no-move WC reduction của beat đó.

Freeze chỉ skip Enemy Phase. End-of-beat vẫn hồi Mana, tick Bomb fuse/status và resolve due effects theo:

```text
Bomb placement order → Environmental Bomb stable order → Turret stable order
```

Victory short-circuit enemy spawn, skill drop và pending UI.

## Enemy và progression

- Runner đuổi/Attack, weighted cao ở Phase 1.
- Jumper telegraph Jump, weighted cao ở Phase 2.
- Thrower khóa rồi relocate Runner/Jumper; relocation không đổi target movement flag, weighted cao ở Phase 3.

Phase dựa trên Lowest WC Reached nên chỉ tiến, không lùi khi hit làm WC tăng.

## UI priority

```text
Victory → High-WC dialog → Skill replacement → Phase panel → beat mới
```

HUD hiển thị WC, phase, Mana current/max, predicted restoration, ba Active costs/targets, Passive, movement state, ground items, enemy HP, Ward/Freeze và hazard telegraphs.

## Đọc sâu

- [Mục lục](./README.md), [Hệ thống nhịp](./beat-and-action-system.md)
- [WC và phase](./win-condition-and-progression.md), [Player/combat](./player-and-combat.md)
- [Enemy/spawn](./enemies-and-spawning.md), [Skill/item](./skills-and-items.md)
- [Map/UI](./map-ui-and-game-flow.md), [Environmental hazards](./environmental-hazards.md)

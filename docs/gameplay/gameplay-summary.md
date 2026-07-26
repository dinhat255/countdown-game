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

Player Phase tự kết thúc; không có input hoặc nút End Beat.

| WC hiện tại | Thời lượng Player Phase |
| --- | ---: |
| `WC > 10` | `2,4s` |
| `5 < WC ≤ 10` | `1,8s` |
| `0 < WC ≤ 5` | `1,6s` |

`WC = 10` dùng `1,8s`; `WC = 5` dùng `1,6s`.
Nếu WC đổi trong Player Phase, timer chuyển tier ngay và không reset elapsed time.
Timer chỉ chạy trong Player Phase active.
Replacement/dialog modal pause timer; beat mới reset timer.
Timeout không phải action và không hoàn tác action đã resolve.

Nếu player không valid self-move:

- Standing Streak tăng; WC giảm trừ beat có successful player Attack.
- Mana hồi `2`, hoặc `3` với Meditation, tối đa `6`.
- Stationary skills không ngăn các update này; successful player Attack chỉ ngăn WC reduction.

Nếu player valid Move/Dash:

- `PlayerMovedThisBeat = true`, streak reset và không hồi Mana.
- Move tạo Move Pressure.
- Dash dùng consumable skill, tốn `1` Mana, đi ba ô, cộng `2 WC` và tạo Dash Pressure.

## Validate và action guard

Mọi candidate được validate đầy đủ trước mutation:

- Phase và action guard.
- Mana, slot, target và range.
- Movement path, endpoint, terrain và occupancy.
- Per-beat self-movement cap.

Valid action đầu tiên consume player action của beat.
Invalid candidate không consume action nên player có thể chọn lại trước timeout.
Stationary skill và Attack giữ `PlayerMovedThisBeat = false`.
Chỉ valid standard Move hoặc Dash set flag true.
Targeting UI chưa resolve không phải action và không reset timer.

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

Pickup replacement dùng đúng category:

- Active chỉ replace Active hoặc discard.
- Passive chỉ replace Passive hoặc discard.
- Pending Active tự fill nếu slot trống trước khi panel mở.
- Victory hủy pending UI.

## Combat và hazards

Enemy maximum HP cấu hình theo Runner/Jumper/Thrower. Snipe, Shockwave và Bomb gây damage. Player Attack chọn và giết một enemy ở ô cardinal kề bên qua shared damage/death resolver. Attack không đổi vị trí hoặc movement flag, nhưng consume player action nên player không thể dùng skill, Attack, Move hoặc Dash khác trong cùng beat. Successful Attack cũng chặn no-move WC reduction của beat đó.

Freeze chỉ skip Enemy Phase. End-of-beat vẫn hồi Mana, tick Bomb fuse/status và resolve due effects theo:

```text
Bomb placement order → Environmental Bomb stable order → Turret stable order
```

Victory short-circuit enemy spawn, skill drop và pending UI.

End-of-beat canonical:

1. Hoàn tất hoặc Freeze-skip Enemy Phase.
2. Apply no-move WC/streak và Mana restore.
3. Tick Bomb fuse và status.
4. Resolve due Bomb, Environmental Bomb, Turret.
5. Cập nhật Lowest/Highest WC và phase.
6. Victory short-circuit nếu `WC ≤ 0`.
7. Nếu chưa thắng, tick/spawn rồi thử skill drop.
8. Resolve UI priority trước beat mới.

Environmental Bomb và Turret là neutral hazards.
Freeze không pause hazard tick/resolve/fire.
Hazard hit player cộng WC; hit enemy gây damage.
Hazard không reposition entity hoặc đổi movement flag.

## Enemy và progression

- Runner đuổi/Attack, weighted cao ở Phase 1.
- Jumper telegraph Jump, weighted cao ở Phase 2.
- Thrower khóa rồi relocate Runner/Jumper; relocation không đổi target movement flag, weighted cao ở Phase 3.

Phase dựa trên Lowest WC Reached nên chỉ tiến, không lùi khi hit làm WC tăng.

Spawn cooldown giảm base mỗi completed beat.
Valid Move thêm Move Pressure.
Valid Dash thêm Dash Spawn Pressure mạnh hơn.
Chỉ một pressure event được cộng mỗi beat.
Spawn dùng phase mới sau due effects.

`HighestWCReached` theo dõi threshold đi lên.
Mỗi high-WC threshold chỉ mở dialog một lần/run.
Dialog pause timer và toàn progression liên quan.
Continue trở lại pending UI; Exit là VoluntaryExit, không phải Loss.

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

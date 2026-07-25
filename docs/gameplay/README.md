# Countdown — Bộ tài liệu gameplay

## Tổng quan

Đây là đặc tả gameplay hiện hành của Countdown.

Quy tắc nền:

- Một nhịp gồm Player Phase, Enemy Phase và end-of-beat update.
- Player resolve tối đa một valid action mỗi beat: standard Move, Dash, Attack hoặc một Active skill. Invalid candidate không consume action.
- Mỗi entity resolve tối đa một self-directed valid movement trong mỗi nhịp.
- Player tự đổi vị trí bằng standard Move hoặc Dash hợp lệ; cả hai dùng chung cap một self-movement/beat.
- Enemy chỉ tự đổi vị trí bằng quyết định movement hợp lệ của AI; Jumper dùng Jump. Ngoại lệ Thrower relocation được mô tả riêng bên dưới.
- Input/candidate không hợp lệ bị reject trước resolve, không tạo failure state.
- Mỗi entity có một flag `SelfMovedThisBeat`; `PlayerMovedThisBeat` là alias dùng cho player.
- Successful player Attack giữ `PlayerMovedThisBeat = false` nhưng consume player action, nên player không thể dùng skill, Attack, Move hoặc Dash khác trong cùng beat.
- Thrower là Enemy Lv3 và là ngoại lệ duy nhất được reposition Runner/Jumper; Throw không đổi movement flag của target.
- Dash là voluntary self-movement skill; các skill khác, hit, hazard và status không đổi vị trí player. Không push/pull/throw player.
- Environmental Bomb và Turret là neutral map hazards, không chiếm skill slot.
- Freeze chỉ skip Enemy Phase, không ảnh hưởng hazard tick/resolve/fire.
- No-Move update WC/streak và hồi `2` Mana, hoặc `3` với Meditation; successful player Attack bỏ qua WC reduction nhưng vẫn update streak/Mana. Attack cooldown/hazard timer giữ rule riêng, Bomb fuse/status tick mỗi beat.
- Due effects resolve Bomb theo placement order → Environmental Bomb → Turret theo stable map coordinate/order, apply từng effect ngay.
- Player không có HP/Loss; hit cộng WC và có thể áp status được định nghĩa, nhưng status không reposition.
- Victory duy nhất khi `WC ≤ 0`; Victory short-circuit spawn và UI pending khác.

## Mục lục

| Tài liệu | Nội dung |
| --- | --- |
| [Tóm tắt gameplay](./gameplay-summary.md) | Bản đọc nhanh một file dành cho người mới |
| [Hệ thống nhịp và hành động](./beat-and-action-system.md) | Phase, valid movement và resolution |
| [WC, Standing Streak và phase](./win-condition-and-progression.md) | Victory, tiến trình và threshold |
| [Người chơi và chiến đấu](./player-and-combat.md) | Move, hit lên player và outgoing damage |
| [Enemy và spawn](./enemies-and-spawning.md) | Runner, Jumper, Thrower và spawn |
| [Skill và item](./skills-and-items.md) | Mana, ba Active slot, một Passive slot, drop và starter skills |
| [Environmental Hazards](./environmental-hazards.md) | Environmental Bomb, Turret và timing theo self-movement |
| [Map, UI và game flow](./map-ui-and-game-flow.md) | Grid, phase UI, WC và panel |

## Thuật ngữ

| Thuật ngữ | Nghĩa |
| --- | --- |
| Valid Move | Move candidate đã validate và thực sự đổi ô actor |
| Valid Dash | Dash theo facing đã validate toàn path/endpoint và resolve nguyên khối |
| `SelfMovedThisBeat` | Flag riêng từng entity; true sau self-directed valid Move/Jump/Dash |
| `PlayerMovedThisBeat` | Alias của `SelfMovedThisBeat` trên player |
| Dash Spawn Pressure | Pressure event của valid Dash; mạnh hơn Move Pressure, không cộng chồng |
| Throw relocation | Non-self-movement exception; không set/clear/reset target flag, eligibility phụ thuộc flag hiện tại |
| Environmental Bomb | Hazard neutral đặt sẵn; khác Bomb skill |
| Turret | Hazard neutral đặt trên wall/non-walkable |
| Bomb skill fuse | Timer của Bomb đã đặt, tick mỗi beat sau khi consumable Bomb bị dùng |
| Stationary action | Action không đổi vị trí actor hoặc target |
| End Beat | Player kết thúc Player Phase |
| WC | Win Condition Cooldown |
| `LowestWCReached` | WC thấp nhất, dùng cho phase đi xuống |
| `HighestWCReached` | WC cao nhất, dùng cho dialog đi lên |
| VoluntaryExit | Player tự thoát, không phải Loss |

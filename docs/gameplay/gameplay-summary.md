# Countdown — Tóm tắt gameplay

## Game trong một câu

**Countdown** là game sinh tồn chiến thuật theo nhịp trên grid: Move/Dash để đổi vị trí nhưng tăng spawn pressure; không self-Move để giảm WC và tiến gần Victory.
## Mục tiêu của run

Player cần đưa **Win Condition Cooldown (`WC`)** về `0` hoặc thấp hơn.

- `WC ≤ 0` là Victory; player không có HP, Death, Loss hoặc Game Over.
- Hit player cộng WC và có thể áp status được định nghĩa; status không reposition. Environmental Bomb/Turret hiện không áp status.
- WC có thể tăng vượt giá trị ban đầu và không có giới hạn trên.
- Player có thể tự Exit từ dialog WC cao; đây là `VoluntaryExit/Quit`, không phải Loss.

Enemy vẫn có HP. Attack, Bomb skill và modifier damage của player dùng để tiêu diệt enemy, tạo khoảng trống và kiểm soát map; diệt sạch enemy không phải điều kiện thắng.

## Quyết định cốt lõi: self-Move hay đứng lại

Mỗi nhịp, player cân nhắc hai hướng chơi.

### Nếu thực hiện standard Move hoặc Dash hợp lệ

- Player đổi vị trí, đặt `PlayerMovedThisBeat = true`, reset Standing Streak và không nhận WC reduction.
- Standard Move tạo Move Pressure; Dash cộng WC một lần và tạo Dash Spawn Pressure mạnh hơn; không cộng hai loại.
- Active skill/eligible Attack/hazard timer pause; Dash slot cooldown đặt ngay và không tick same beat.
- Cả hai dùng chung cap một self-movement/beat; non-movement action khác vẫn dùng được nếu guard cho phép.

### Nếu không self-Move

- WC giảm một lần ở cuối nhịp.
- Standing Streak tăng một lần.
- Mọi active skill đang cooldown giảm `1`.
- Player vẫn có thể Attack hoặc dùng nhiều skill tại chỗ.
- Enemy vẫn thực hiện Enemy Phase bình thường.

## Một nhịp diễn ra thế nào

```text
Player Phase
→ Enemy Phase
→ End-of-beat Update
```

### Player Phase

- Timer bắt đầu.
- Player có thể thực hiện nhiều action tại chỗ nếu cooldown, resource và điều kiện cho phép.
- Player được resolve tối đa một standard Move hoặc Dash hợp lệ.
- Player Phase kết thúc khi timer hết hoặc player chọn End Beat.

Move input được kiểm tra trước khi resolve.

Nếu standard Move hoặc Dash path/endpoint không hợp lệ:

- Input bị reject nguyên khối; không partial movement.
- Không tiêu cooldown/resource, cộng WC, tạo pressure hoặc consume movement.
- Dash không xuyên wall/obstacle, có thể xuyên enemy nhưng không tự gây damage.
- Player có thể chọn lại hướng khác.

### Enemy Phase

- Enemy Phase chạy một lần sau Player Phase.
- Mỗi enemy resolve finite action sequence.
- Mỗi enemy được tự Move hoặc Jump hợp lệ tối đa một lần.
- Cooldown, điều kiện và giới hạn theo nhịp ngăn enemy spam action.
- Freeze bỏ Enemy Phase nhưng không ảnh hưởng hazard tick/resolve/fire; pending Throw telegraph giữ Paused tới phase đầu tiên hết Freeze.

### End-of-beat Update

Game lần lượt:

1. Hoàn tất/skip Enemy Phase, chốt `PlayerMovedThisBeat`.
2. No-self-Move: WC/streak, active skill CD, eligible Attack CD và hazard timer tick; standard Move/Dash pause timer và reset streak.
3. Bomb skill fuse/status tick bất kể self-movement; resolve Bomb theo placement order → Env Bomb → Turret, mỗi hazard theo stable map coordinate/order; áp từng effect ngay.
4. Cập nhật Lowest/Highest WC, phase và threshold; phase mới dùng cho spawn.
5. Victory short-circuit spawn/UI pending; nếu chưa Victory mới tick/spawn rồi high-WC → skill replacement → phase → nhịp mới.

## Player và combat

Player có standard Move bốn hướng và Dash nhiều ô theo facing.

- Dash validate toàn path/endpoint; không xuyên wall/obstacle, có thể xuyên enemy; landing occupancy/path details là `TBD`.
- Standard Move hoặc Dash landing vào Environmental Bomb kích hoạt Dormant → Active; intermediate Dash Bomb/item interaction là `TBD`.
- Attack là action chủ động theo hướng quay mặt, không tự kích hoạt.
- Attack có cooldown riêng để ngăn spam.
- Enemy nhận damage và có thể bị tiêu diệt khi hết HP.

Dash là voluntary self-movement skill. Player không bị push/pull/ném/teleport/knockback; non-movement skill, hit, hazard và status không đổi vị trí.

## Environmental Bomb và Turret

Đây là neutral hazards đặt sẵn, không chiếm skill slot và friendly fire cả player/enemy.

| Hazard | Lifecycle |
| --- | --- |
| Environmental Bomb | Dormant trên ground → player bước vào để kích hoạt → Countdown về `0` thì nổ và bị phá hủy |
| Turret | Trên wall/non-walkable → Reload `0` fire-check mỗi beat; không target giữ Ready `0`, actual shot mới reset |

Countdown/Reload `> 0` chỉ giảm `1` khi no-self-Move; standard Move/Dash pause, không reset. Ready Turret vẫn fire-check. UI phân biệt Dormant/Active, activation no-tick, blast area, Reload pause và Ready check.

## Ba loại enemy

### Runner — Enemy Lv1

Truy đuổi, Move một ô hợp lệ và Attack tại chỗ; có trọng số cao hơn ở Phase 1.

### Jumper — Enemy Lv2

Move hoặc Jump tới ô hợp lệ. Ô đáp được telegraph; impact hit cộng WC. Jumper có trọng số cao hơn ở Phase 2.

### Thrower — Enemy Lv3

- Có thể tự Move một lần như enemy khác.
- Khóa Runner/Jumper target, đường ném và vùng đáp để telegraph; nhịp sau kiểm tra lại.
- Nếu dữ liệu không còn hợp lệ, Throw bị cancel và không retarget.
- Nếu hợp lệ, Thrower ném target; enemy bị ném còn có thể tự Move nếu chưa tự Move và lượt AI chưa qua; nếu đã tự Move thì không được Move lại.
- Nếu player nằm trong impact area, hit cộng WC và có thể áp status được định nghĩa, nhưng không reposition.
- Thrower xuất hiện với trọng số cao hơn ở Phase 3.

Thrower relocation là ngoại lệ duy nhất cho phép một enemy đổi vị trí enemy khác.

## Skill item và ba slot

Pool có sáu skill; player giữ tối đa ba slot.

- Skill item được nhặt khi standard Move hoặc Dash landing vào ô; intermediate Dash pickup là `TBD`.
- Có thể giữ nhiều bản sao của cùng skill.
- Mỗi active skill có cooldown riêng theo slot.
- Khi nhặt skill thứ tư, player chọn thay một slot hoặc bỏ skill mới.

| Level | Skill | Loại | Tác dụng |
| --- | --- | --- | --- |
| 1 | WC Penalty Reduction | Passive | Giảm WC tăng từ hit, gồm impact hit của Thrower |
| 1 | Dash | Active Movement | Nhiều ô theo facing; `+WC`, shared movement cap, Dash Spawn Pressure |
| 2 | Bomb skill | Active | Đặt Bomb skill gây outgoing damage diện rộng |
| 2 | Damage Up | Passive | Tăng Attack và Bomb skill damage; không tăng neutral hazard |
| 3 | Refresh All Skills | Active | Reset cooldown các non-Refresh active skill |
| 3 | Freeze | Active | Làm toàn bộ enemy bỏ Enemy Phase |

Refresh không reset Refresh; có thể reset Dash CD nhưng không cấp lại movement eligibility.

## Cooldown và Standing Streak

Khi `PlayerMovedThisBeat = false`:

- Mọi active skill cooldown `> 0` giảm `1`; skill vừa dùng cũng tick.
- Attack cooldown chỉ giảm nếu không được bắt đầu trong chính beat đó.
- Active Environmental Bomb Countdown và Turret Reload `> 0` giảm `1`; Standing Streak tăng và WC giảm.
- Bomb skill fuse và status duration giảm mỗi beat bất kể self-movement; fuse tách khỏi slot cooldown.

Valid standard Move/Dash phá streak và pause no-Move timers; Dash `+WC` không phải hit nên passive không giảm. Bomb fuse/status và Turret Ready vẫn chạy.

Các breakpoint, bonus và giới hạn của Standing Streak chưa được chốt.

## Spawn và ba phase

Sau threshold/phase update, Victory short-circuit spawn. Nếu chưa Victory: standard Move thêm Move Pressure, Dash thêm pressure mạnh hơn; tối đa một event.

Runner, Jumper và Thrower đều có thể spawn trong mọi phase. Phase chỉ thay đổi trọng số:

| Phase | Khoảng tiến trình | Enemy được tăng trọng số |
| --- | --- | --- |
| Phase 1 | Từ đầu run đến trước mốc `1/3` tiến trình | Runner |
| Phase 2 | Từ `1/3` đến trước `2/3` tiến trình | Jumper |
| Phase 3 | `1/3` cuối trước Victory | Thrower |

Phase được xác định từ WC thấp nhất player từng đạt. Vì vậy phase chỉ tăng và không lùi khi hit làm WC tăng trở lại.

## Dialog khi WC tăng cao

Game có các mốc WC cao được cấu hình theo thứ tự tăng dần.

- Mỗi mốc chỉ được xử lý một lần trong run.
- Nếu một nhịp vượt nhiều mốc, game ghi nhận tất cả nhưng chỉ hiện một dialog cho mốc cao nhất.
- Dialog có giọng playful teasing, ví dụ: “Bạn vẫn muốn chơi tiếp chứ?”.
- Khi dialog mở, phase, timer, spawn và cooldown đều pause.
- Continue đóng dialog và xử lý UI đang chờ tiếp theo.
- Exit kết thúc session dưới dạng `VoluntaryExit/Quit`.

Thứ tự UI cuối nhịp:

```text
Victory
→ High-WC dialog
→ Skill replacement panel
→ Phase panel
→ Nhịp mới
```

## Các cơ chế không có trong gameplay hiện hành

- Player HP, Death, Loss hoặc Game Over.
- Energy chạy.
- Auto Attack.
- Move thất bại có tiêu lượt.
- Push, pull, throw player, teleport, knockback hoặc forced reposition khác ngoài Thrower.
- Victory bằng cách tiêu diệt toàn bộ enemy.

## Thông số cân bằng chưa chốt

Các quan hệ gameplay trong tài liệu này đã được chốt, nhưng giá trị cân bằng sau vẫn là `TBD`:

- Initial WC, WC reduction và Standing Streak breakpoint/bonus/cap.
- WC penalty mỗi hit, stacking/debounce, Player Phase và input buffer.
- Attack/skill cooldown, damage/range/duration; Dash distance/path/landing occupancy/intermediate interaction/i-frame/hit timing.
- Move Pressure/Dash Spawn Pressure amount/formula, spawn cooldown, enemy cap và phase weight.
- Jumper range/telegraph.
- Thrower range, cooldown, impact area, cap và AI ordering.
- Environmental Bomb/Turret countdown, reload, range, damage và targeting.
- High-WC threshold, spacing và dialog copy cụ thể.

Không nên xem các giá trị minh họa trong tài liệu chi tiết là thông số cuối cùng nếu chúng được ghi `TBD` hoặc “đề xuất”.

## Đọc sâu

- [Mục lục](./README.md), [Hệ thống nhịp](./beat-and-action-system.md)
- [WC và phase](./win-condition-and-progression.md), [Player/combat](./player-and-combat.md), [Enemy/spawn](./enemies-and-spawning.md)
- [Skill/item](./skills-and-items.md), [Map/UI](./map-ui-and-game-flow.md)
- [Environmental hazards](./environmental-hazards.md)

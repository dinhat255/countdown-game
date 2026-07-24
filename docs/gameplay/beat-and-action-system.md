# Hệ thống nhịp và hành động

## Tổng quan

```text
Player Phase
→ Enemy Phase
→ End-of-beat Update
```

Mỗi entity resolve tối đa một self-directed valid movement trong nhịp. Non-movement action không dùng movement opportunity và có thể thực hiện nhiều lần nếu guard cho phép.

## Đầu nhịp

```text
Với player và từng enemy:
SelfMovedThisBeat = false

Với player:
AttackCooldownStartedThisBeat = false
```

Không có dual movement flag. Với player, `PlayerMovedThisBeat` là alias của `SelfMovedThisBeat`.

## Validate trước resolve

Mọi movement candidate phải được kiểm tra trước:

- Ô đích trong map.
- Toàn path và endpoint không xuyên tường/vật cản.
- Endpoint/occupancy hợp lệ theo collision rule.
- Action/range/target hợp lệ.
- Entity chưa có `SelfMovedThisBeat = true`.

Dash validate toàn path/endpoint trước resolve. Dash có thể đi xuyên enemy, nhưng không tự gây damage; landing occupancy và chi tiết path còn `TBD`.

Candidate không hợp lệ:

- Bị reject.
- Không resolve.
- Không có failure state.
- Không tiêu cooldown/resource.
- Không cộng WC hoặc tạo pressure.
- Không partial movement và không consume movement opportunity.
- Không đổi facing trừ khi một stationary face action riêng được thiết kế.
- Cho phép actor chọn candidate khác.

## Player Phase

1. Bắt đầu timer.
2. Nhận input.
3. Validate theo trạng thái hiện tại.
4. Resolve action hợp lệ ngay.
5. Valid standard Move hoặc Dash đổi vị trí và đặt `PlayerMovedThisBeat = true`.
6. Valid Dash đi nhiều ô theo facing trong một resolve, cộng WC đúng một lần và đặt slot cooldown ngay.
7. Reject mọi self-movement candidate tiếp theo trong cùng nhịp.
8. Kết thúc khi timeout hoặc End Beat.

Player tự đổi vị trí bằng standard Move hoặc Dash. Dash là Active Movement Skill; skill khác và hit không đổi vị trí.

## Enemy Phase

1. Nếu Freeze hiệu lực, skip toàn phase; pending Throw telegraph giữ Paused.
2. Chạy AI từng enemy theo thứ tự `TBD`.
3. Validate action candidate trước resolve.
4. Resolve chuỗi hành động hữu hạn không tự di chuyển.
5. Resolve tối đa một valid AI movement/enemy.

Runner dùng Move. Jumper dùng Move/Jump. Thrower có thể dùng Move. Valid AI movement đặt `SelfMovedThisBeat = true`.

Thrower còn có sequence riêng:

1. Nhịp chuẩn bị: chọn Runner/Jumper hợp lệ và khóa telegraph target, đường ném, ô/vùng đáp.
2. Nhịp resolve: revalidate locked target, path và landing.
3. Nếu invalid, cancel; không retarget âm thầm.
4. Nếu valid, reposition target tới landing và resolve impact hit.

Throw relocation là non-self-movement action của Thrower:

- Không set, clear, reset hoặc consume `SelfMovedThisBeat` của target.
- Nếu flag của target là false và lượt AI chưa qua, target vẫn còn self-Move eligibility; nếu true thì không được Move lại.
- Timing/AI order sau landing là `TBD`, còn eligibility đã được chốt.
- Player không đổi vị trí khi bị impact hit.

## Non-self-movement action

Bao gồm:

- Attack.
- Bomb skill, Refresh, Freeze.
- Telegraph.
- Throw relocation của Thrower.
- Hit/hazard/status không đổi vị trí.

Mỗi action phải có cooldown/resource/condition/per-beat guard. Action invalid bị reject và không tiêu cost.

Dash không thuộc nhóm này: nó dùng movement opportunity duy nhất. Refresh có thể reset Dash cooldown nhưng không reset `PlayerMovedThisBeat`; nhiều Dash slot không thể chain trong cùng beat.

## End Beat và timeout

- End Beat kết thúc Player Phase ngay.
- Timeout cũng kết thúc phase.
- Action đã resolve không hoàn tác.
- Enemy Phase luôn chạy, trừ Freeze.
- End Beat không phải Wait action.

## End-of-beat update

1. Hoàn tất Enemy Phase, hoặc xác nhận phase đã bị Freeze skip. Pending Throw telegraph giữ Paused; locked data được revalidate ở Enemy Phase đầu tiên hết Freeze.
2. Chốt `PlayerMovedThisBeat`.
3. Nếu `PlayerMovedThisBeat = false`: áp dụng WC reduction và tăng Standing Streak; mọi active skill cooldown `> 0` giảm `1`, gồm skill vừa dùng; Attack cooldown giảm `1` chỉ khi `AttackCooldownStartedThisBeat = false`; mỗi active Environmental Bomb Countdown và Turret Reload `> 0` giảm `1`. Nếu true do standard Move hoặc Dash: WC không giảm, Standing Streak reset và các timer vừa nêu pause, không reset. Dash đã cộng WC một lần khi resolve; đây không phải hit và WC Penalty Reduction không áp dụng.
4. Giảm Bomb skill fuse và status duration `1` bất kể self-movement. Fuse là timer của Bomb đã đặt, tách khỏi cooldown của skill slot.
5. Resolve due effects theo thứ tự cố định: Bomb skill explosion theo placement order → Environmental Bomb explosion theo stable map coordinate/order → Turret Ready fire-check/shot theo stable map coordinate/order. Damage và WC của từng effect áp dụng ngay trước effect kế tiếp.
6. Mọi Turret có Reload `0` đều fire-check, kể cả beat player standard Move/Dash. Không có target thì giữ Ready tại `0`; chỉ actual shot mới reset Reload.
7. Cập nhật `LowestWCReached`, `HighestWCReached`, phase và threshold. Spawn dùng phase mới.
8. Nếu `WC ≤ 0`, Victory short-circuit: không tick/spawn và không mở UI pending khác.
9. Nếu chưa Victory: base spawn tick; standard Move tạo Move Pressure, Dash tạo Dash Spawn Pressure mạnh hơn; không cộng cả hai và tối đa một pressure event/beat; kiểm tra cap, chọn type/point rồi spawn.
10. Priority còn lại: high-WC dialog → skill replacement → phase panel → nhịp mới.

Due effects resolve sau Enemy Phase và trước threshold, Victory, spawn và UI. Thứ tự resolve đã cố định; chỉ target/range/damage và giá trị cân bằng còn `TBD`.

Hazard không set `PlayerMovedThisBeat`, không reset Standing Streak và không tạo Move Pressure.

## Cooldown cuối nhịp

```text
Nếu PlayerMovedThisBeat = false:
    mỗi active skill cooldown > 0 giảm 1
    Attack cooldown > 0 giảm 1 nếu AttackCooldownStartedThisBeat = false
    mỗi active Environmental Bomb Countdown giảm 1
    mỗi Turret Reload > 0 giảm 1
```

Skill vừa dùng nhận slot cooldown tick cùng nhịp. Attack đặt cooldown và `AttackCooldownStartedThisBeat = true`, nên bắt đầu giảm từ no-Move beat sau.

Bomb skill fuse và status duration giảm mỗi end-of-beat bất kể self-movement. Bomb fuse là timer của object đã đặt, không phải slot cooldown.

Dash đặt slot cooldown ngay nhưng vì Dash đặt `PlayerMovedThisBeat = true`, cooldown đó không tick trong cùng beat. Refresh chỉ reset cooldown, không cấp lại movement eligibility.

## Input buffer

- Chỉ buffer action kế tiếp.
- Validate lại trước resolve.
- Invalid input bị bỏ nhưng player có thể nhập lại.
- Sau valid standard Move hoặc Dash, mọi self-movement input khác bị khóa.
- Thời lượng buffer là `TBD`.

## Tài liệu liên quan

- [WC và phase](./win-condition-and-progression.md)
- [Player và combat](./player-and-combat.md)
- [Enemy và spawn](./enemies-and-spawning.md)
- [Skill và item](./skills-and-items.md)
- [Environmental Hazards](./environmental-hazards.md)

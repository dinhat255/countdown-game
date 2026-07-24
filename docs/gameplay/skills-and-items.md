# Skill và item

## Tổng quan

Skill pool hiện tại có sáu skill. Dash là Active Movement Skill; các skill còn lại không đổi vị trí hoặc ảnh hưởng `PlayerMovedThisBeat`.

`Bomb skill` là skill slot của player, khác `Environmental Bomb` neutral đặt sẵn trên map.

Core loop:

> Nhặt skill → quản lý ba slot → cân nhắc Dash hoặc action tại chỗ → không self-Move để hồi.

## Nhận skill

Player nhặt item khi valid standard Move hoặc Dash landing vào ô chứa item:

- Item biến mất.
- Skill vào slot trống đầu tiên.
- Không cần action riêng.

Phase tăng trọng số skill level tương ứng; mọi level vẫn có thể xuất hiện.
Dash đi qua intermediate item có nhặt hay không là `TBD`.

## Ba slot

```text
[Slot 1] [Slot 2] [Slot 3]
```

- Mỗi slot có loại Active/Passive và cooldown riêng.
- Cho phép nhặt trùng.
- Passive có thể stack trong cap.

## Skill thứ tư

Sau end-of-beat update:

1. Pause gameplay.
2. Hiện skill mới, ba slot và cooldown.
3. Thay một slot hoặc bỏ skill.

Skill mới Ready và không kế thừa cooldown.

## Dùng active skill

1. Validate cooldown/resource/target.
2. Invalid candidate bị reject, không tiêu cost.
3. Valid non-movement skill resolve tại chỗ; valid Dash resolve movement nguyên khối.
4. Đặt cooldown/resource ngay.

Chỉ Dash đổi vị trí player và dùng movement opportunity duy nhất.

## Cooldown

```text
Nếu PlayerMovedThisBeat = false:
    mỗi active skill cooldown > 0 giảm 1
```

- Tick một lần cuối nhịp.
- Non-movement skill vừa dùng nhận tick cùng no-Move beat.
- Valid standard Move hoặc Dash làm mất tick của mọi active slot.
- Dash đặt cooldown ngay và không tick trong chính beat Dash.
- Passive không có cooldown.

## Refresh

- Reset cooldown mọi non-Refresh active skill.
- Có thể reset Dash cooldown nhưng không reset `PlayerMovedThisBeat` hoặc cấp lại movement eligibility.
- Không reset chính nó.
- Không reset bất kỳ Refresh slot nào.
- Skill được reset có thể dùng lại trong Player Phase nếu còn thời gian.

## Danh sách sáu skill

| Level | Skill | Loại | Vai trò |
| --- | --- | --- | --- |
| 1 | WC Penalty Reduction | Passive | Giảm WC penalty do hit |
| 1 | Dash | Active Movement | Di chuyển nhiều ô theo facing, cộng WC và tạo Dash Spawn Pressure |
| 2 | Bomb skill | Active | Outgoing damage diện rộng |
| 2 | Damage Up | Passive | Tăng outgoing damage |
| 3 | Refresh All Skills | Active | Reset non-Refresh skill |
| 3 | Freeze | Active | Skip toàn Enemy Phase |

## WC Penalty Reduction

- Chỉ giảm WC tăng từ hit.
- Áp dụng cho impact hit từ enemy bị Thrower ném.
- Áp dụng cho Environmental Bomb/Turret hit.
- Không vô hiệu hóa hit/status.
- Stack cần cap.

```text
Final WC Penalty
= Original WC Penalty × Penalty Multiplier
```

WC do valid Dash không phải hit, nên passive này không giảm.

## Dash

- Đi nhiều ô theo facing trong một resolve; dùng chung cap self-movement với standard Move.
- Validate toàn path/endpoint trước resolve; không xuyên wall/obstacle, có thể xuyên enemy.
- Invalid reject toàn bộ: không partial move, cooldown, WC, pressure hoặc consume movement.
- Valid đặt `PlayerMovedThisBeat = true`, cộng WC đúng một lần, reset streak và đặt slot cooldown.
- Nhiều Dash slot không chain được trong cùng beat; Refresh chỉ reset cooldown.
- Intermediate enemy traverse không tự gây damage.
- Landing Environmental Bomb Dormant kích hoạt Bomb; intermediate Bomb/item interaction là `TBD`.
- Landing occupancy/path details và invulnerability/i-frame/hit timing là `TBD`.
- Dash Spawn Pressure mạnh hơn Move Pressure; không cộng chồng, tối đa một pressure event/beat.

## Bomb skill

- Player đặt Bomb skill ở ô hợp lệ.
- Không đổi vị trí player/enemy.
- Object đã đặt có fuse riêng, tách khỏi cooldown của skill slot.
- Fuse giảm `1` mỗi end-of-beat bất kể player self-movement.
- Fuse vừa đạt `0` nổ ngay trong cùng end-of-beat, trước threshold/Victory.
- Nhiều Bomb skill nổ theo placement order; damage/WC áp ngay sau từng Bomb.
- Nổ `3×3`, gây outgoing damage lên enemy.

```text
X X X
X B X
X X X
```

Bomb skill friendly fire là `TBD`; nếu bật, hit player sẽ cộng WC và không đổi vị trí.

Slot cooldown của Bomb skill chỉ tick ở no-Move beat như active skill khác. Fuse của Bomb đã đặt không dùng timing này hoặc timing của Environmental Bomb.

## Damage Up

- Tăng Attack damage.
- Chắc chắn tăng Bomb skill damage.
- Không tăng neutral Environmental Bomb hoặc Turret damage.
- Công thức/cap là `TBD`.

## Freeze

- Enemy bỏ toàn bộ Enemy Phase.
- Chặn movement, telegraph, Throw và stationary action.
- Pending Throw telegraph giữ Paused; original lock revalidate ở Enemy Phase đầu tiên hết Freeze.
- Không ảnh hưởng Environmental Bomb/Turret tick, resolve, Ready fire-check hoặc shot.
- Enemy mới spawn nhận duration còn lại.
- Freeze mới refresh duration, không vượt cap.

## UI skill

Mỗi slot hiển thị:

- Icon/tên.
- Active/Passive.
- Ready/cooldown.
- Invalid condition.
- Preview tick theo `PlayerMovedThisBeat`.
- Bomb slot cooldown và placed fuse hiển thị thành hai timer tách biệt.
- Dash hiển thị facing path, landing validity, `+WC`, movement-used state và Dash Spawn Pressure.

## Tài liệu liên quan

- [Hệ thống nhịp](./beat-and-action-system.md)
- [Enemy và spawn](./enemies-and-spawning.md)
- [Map và UI](./map-ui-and-game-flow.md)
- [Environmental Hazards](./environmental-hazards.md)

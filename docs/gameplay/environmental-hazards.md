# Environmental Hazards

## Tổng quan

Map có hai neutral hazards đặt sẵn:

- **Environmental Bomb** trên ground.
- **Turret** trên wall hoặc ô non-walkable.

Hazard không thuộc player hay enemy, không chiếm skill slot và có thể hit cả hai phía.

`Environmental Bomb` khác hoàn toàn `Bomb skill`:

- Environmental Bomb có sẵn trên map và được kích hoạt khi standard Move hoặc Dash landing vào ô.
- Bomb skill do player sử dụng từ skill slot.

## Quy tắc timing chung

Hazard tick sau khi Enemy Phase hoàn tất hoặc bị Freeze skip.

Nếu `PlayerMovedThisBeat = false`:

- Mỗi active Environmental Bomb Countdown giảm đúng `1`.
- Mỗi Turret Reload lớn hơn `0` giảm đúng `1`.

Nếu player dùng valid standard Move hoặc Dash:

- Countdown/Reload pause.
- Giá trị không reset.

Sau decrement, due effects resolve ngay trong cùng end-of-beat update:

1. Bomb skill explosion theo placement order.
2. Environmental Bomb explosion theo stable map coordinate/order.
3. Turret Ready fire-check/shot theo stable map coordinate/order.

Damage và WC của từng effect được áp dụng ngay trước effect kế tiếp. Sau toàn bộ due effects, game mới cập nhật threshold/phase, kiểm tra Victory rồi mới có thể tick/spawn.

Mọi Turret Reload `0` đều fire-check trong bước này, kể cả beat player standard Move/Dash. Self-movement chỉ pause Reload `> 0`, không pause Ready fire-check.

Hazard không:

- Set `PlayerMovedThisBeat`.
- Reset Standing Streak.
- Tạo Move Pressure hoặc Dash Spawn Pressure.
- Reposition player hoặc enemy.

## Environmental Bomb

### Placement và trạng thái ban đầu

- Đặt sẵn trên ô ground.
- Ban đầu ở trạng thái Dormant.
- Không đếm ngược khi chưa kích hoạt.

### Kích hoạt

Khi player thực hiện valid standard Move hoặc Dash landing vào ô Environmental Bomb:

1. Environmental Bomb chuyển sang Active.
2. Hiện Countdown/CD.
3. Nhịp kích hoạt đã có `PlayerMovedThisBeat = true` nên Countdown không giảm trong chính nhịp đó.

Dash đi qua Environmental Bomb ở intermediate tile có kích hoạt hay không là `TBD`.

### Countdown

- Active Countdown chỉ giảm khi `PlayerMovedThisBeat = false`.
- Mỗi nhịp hợp lệ giảm đúng `1`.
- Valid standard Move hoặc Dash làm pause, không reset.

### Phát nổ

Khi Countdown vừa đạt `0`:

- Environmental Bomb nổ ngay cuối nhịp.
- Explosion áp dụng trong blast range.
- Player trong range nhận hit và WC penalty; không mất HP.
- Enemy trong range mất HP.
- Explosion không reposition entity.
- Environmental Bomb bị phá hủy sau khi nổ.

## Turret

### Placement

- Đặt sẵn trên wall hoặc ô non-walkable.
- Stationary và không thể bị reposition.
- Có Range và Reload CD.

### Reload

- Reload chỉ giảm khi `PlayerMovedThisBeat = false`.
- Mỗi nhịp hợp lệ giảm đúng `1`.
- Valid standard Move hoặc Dash làm pause, không reset.

### Ready và bắn

Khi Reload bằng `0`:

- Turret fire-check mỗi end-of-beat, kể cả beat player standard Move/Dash.
- Nếu có target hợp lệ trong range, Turret bắn.
- Shot có thể hit player hoặc enemy trên đường/điểm va chạm.
- Sau khi **thực sự bắn**, Turret reset Reload.
- Nếu không có target hợp lệ, Turret không bắn và giữ Ready tại `0` qua các beat.

Target priority, tie-break, LOS, projectile blocking và collision là `TBD`.

## Damage và friendly fire

Hazard dùng cùng hit semantics:

| Target | Kết quả |
| --- | --- |
| Player | Cộng WC penalty; không có HP damage |
| Enemy | Trừ HP |

- WC Penalty Reduction áp dụng cho hazard hit lên player.
- Damage Up không tăng neutral hazard damage.
- Environmental Bomb/Turret hiện không áp status.
- Hazard hit không đổi vị trí target.
- Bomb skill friendly fire vẫn là rule riêng và hiện `TBD`.

## Freeze

Freeze chỉ skip Enemy Phase. Freeze không pause hoặc vô hiệu Environmental Bomb Countdown, Turret Reload, Ready fire-check hay Turret shot.

## UI

### Environmental Bomb

- Icon/name khác Bomb skill.
- Dormant/Active state.
- Activation feedback khi valid standard Move hoặc Dash landing vào ô Bomb.
- Countdown.
- Blast area.
- Activation beat hiển thị Pause/no tick; các beat sau preview Tick hoặc Pause theo `PlayerMovedThisBeat`.
- Explosion warning.

### Turret

- Reload.
- Range.
- Ready state tại `0`.
- Aim/target.
- Shot telegraph và hit feedback.
- Reload `> 0` hiển thị Tick hoặc Pause; Ready `0` vẫn hiển thị fire-check kể cả standard Move/Dash beat.

## Thông số TBD

- Environmental Bomb Countdown ban đầu, blast range, enemy damage và WC penalty.
- Số lượng/vị trí Environmental Bomb.
- Turret Reload value, range, enemy damage và WC penalty.
- Target priority/tie-break.
- LOS, projectile blocking, travel và collision.
- Hazard cap.
- Dash intermediate Environmental Bomb activation.

## Tài liệu liên quan

- [Gameplay Summary](./gameplay-summary.md)
- [Hệ thống nhịp](./beat-and-action-system.md)
- [WC và phase](./win-condition-and-progression.md)
- [Player và combat](./player-and-combat.md)
- [Enemy và spawn](./enemies-and-spawning.md)
- [Skill và item](./skills-and-items.md)
- [Map và UI](./map-ui-and-game-flow.md)

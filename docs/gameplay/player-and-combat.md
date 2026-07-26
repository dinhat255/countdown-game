# Người chơi và chiến đấu

## Movement

Player chỉ đổi vị trí bằng valid standard Move hoặc consumable Dash.

- Standard Move đi một ô cardinal.
- Dash đi ba ô theo facing và dùng chung một self-movement cap.
- Full path/endpoint được validate trước resolve.
- Invalid movement không mutate state và không làm mất no-move Mana restoration.
- Valid movement đặt `PlayerMovedThisBeat = true`, tạo pressure và chỉ nhặt ground item ở landing.
- Dash cộng `2 WC` đúng một lần; đây không phải hit nên Ward/WC Dampener không giảm.

Hit, hazard, status, Snipe, Ward, Bomb, Shockwave và Freeze không reposition player. Gameplay không có push, pull, teleport, knockback hoặc enemy throw lên player.

## Mana và stationary action

Player có Mana `3/6` lúc bắt đầu run. Active hợp lệ trừ Mana, bị consume và dùng player action duy nhất của beat. Sau đó player không thể dùng skill, Attack, Move hoặc Dash khác tới beat sau.

Nếu không valid self-move trong beat, player hồi `2` Mana cuối beat, hoặc `3` với Meditation. Mana restore xảy ra sau WC/Standing Streak và trước due hazards.

## Hit lên player

Player không có HP, Death, Loss hoặc Game Over. Hit hợp lệ:

1. Emit hit và áp status được định nghĩa.
2. Nếu Ward armed, WC penalty của hit này thành `0` và Ward bị consume.
3. Nếu không có Ward nhưng WC Dampener equipped, penalty giảm `1`, tối thiểu `0`.
4. Phần penalty còn lại cộng WC.

Ward không xóa status. Dash WC không đi qua hit resolver.

## Enemy HP và damage

Enemy dùng chung health/damage resolver với maximum HP cấu hình theo type. Damage emit event; HP về `0` emit death và enemy không còn block cell hoặc act.

Starter offensive damage:

- Snipe: `3`, target đầu tiên trong bốn facing cells.
- Shockwave: `2`, mọi enemy trong tám ô kề.
- Bomb: `2`, blast `3×3`.
- Damage Up cộng `1` vào các offensive skill; player Attack là instant kill nên không nhận modifier.
- Neutral Environmental Bomb/Turret không nhận Damage Up.

Damage không đổi movement flag của source hoặc target.

## Attack

Player Attack bằng cách chọn một enemy còn sống ở ô cardinal kề bên. Attack giết enemy được chọn qua shared damage/death resolver, cập nhật facing về phía target, nhưng không đổi vị trí hoặc `PlayerMovedThisBeat`.

Attack là stationary action chủ động và không dùng Active slot. Successful Attack consume player action duy nhất của beat, nên skill, Attack, standard Move và Dash khác đều bị reject trong phần còn lại của beat. Player vẫn ở nguyên cell và `PlayerMovedThisBeat` vẫn false.

Ở end-of-beat, successful Attack vẫn tăng Standing Streak và hồi no-move Mana nhưng bỏ qua WC reduction. Attack vì vậy không làm giảm WC trực tiếp hoặc qua no-move update.

## End Beat và UI

End Beat không hoàn tác action. UI player cần hiện:

- Mana current/max và predicted no-move restore.
- Movement-used state và valid/invalid Move/Dash path.
- Hit/WC penalty, Ward và passive modifier.
- Enemy HP, damage/death feedback.
- Active slot consumption, targeting reason và pressure.

## Tài liệu liên quan

- [Hệ thống nhịp](./beat-and-action-system.md)
- [Enemy và spawn](./enemies-and-spawning.md)
- [Skill và item](./skills-and-items.md)
- [Environmental Hazards](./environmental-hazards.md)

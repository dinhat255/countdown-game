# Người chơi và chiến đấu

## Tổng quan

Player tự đổi vị trí bằng standard Move hoặc Dash hợp lệ. Dash là Active Movement Skill; Attack, hit, hazard, status và các skill khác không đổi vị trí player.

## Standard Move

Move theo bốn hướng, tối đa một lần resolve mỗi nhịp.

Trước resolve, game kiểm tra:

- Ô trong map.
- Không phải tường/vật cản.
- Không bị entity chặn.
- Player chưa dùng self-movement trong nhịp.

Nếu invalid, input bị reject hoàn toàn và player có thể chọn lại.

Nếu valid:

- Player đổi một ô.
- `PlayerMovedThisBeat = true`.
- Tạo một Move Pressure.
- Nếu vào ô Environmental Bomb Dormant, kích hoạt Bomb, hiện Countdown và không tick trong activation beat.
- Mọi self-movement khác bị khóa đến nhịp sau.

## Dash

Dash là Active Movement Skill Lv1:

- Resolve nhiều ô theo facing trong một self-movement.
- Không xuyên wall/obstacle; có thể đi xuyên enemy nhưng không tự gây damage.
- Toàn path và endpoint phải valid trước resolve. Landing occupancy/path details là `TBD`.
- Invalid Dash reject toàn bộ: không partial move, cooldown, WC, pressure hoặc consume movement.
- Valid Dash đặt `PlayerMovedThisBeat = true`, dùng chung cap với standard Move, cộng WC đúng một lần và đặt slot cooldown ngay.
- WC từ Dash không phải hit; WC Penalty Reduction không giảm.
- Tạo một Dash Spawn Pressure mạnh hơn Move Pressure; không cộng cả hai.
- Landing trên Environmental Bomb Dormant kích hoạt Bomb. Intermediate tile có kích hoạt Bomb/nhặt item hay không là `TBD`.
- Invulnerability/i-frame và hit timing trong Dash là `TBD`.

Refresh có thể reset Dash cooldown nhưng không cấp lại self-movement. Nhiều Dash slot không thể chain cùng beat.

## Không reposition player

Gameplay hiện hành không có:

- Push/pull.
- Throw.
- Teleport.
- Knockback.
- Hit/hazard/status đổi vị trí.

Player position chỉ đổi do standard Move hoặc Dash hợp lệ do chính player chủ động.

Thrower chỉ reposition Runner/Jumper. Impact hit tại ô đáp không đổi vị trí player.

## Hit lên player

Player không có HP, Death hoặc Loss. Invulnerability/i-frame riêng trong Dash là `TBD`.

Mỗi hit hợp lệ:

- Cộng WC penalty.
- Có thể áp status không đổi vị trí.
- Không trực tiếp kết thúc run.

Collision debounce/stacking là `TBD`.

Environmental Bomb và Turret dùng cùng hit semantics:

- Hazard hit player cộng WC.
- Hazard hit enemy trừ HP.
- Hazard hiện không áp status.
- Hazard không reposition target.
- WC Penalty Reduction áp dụng cho hazard hit.

## Stationary action

Player có thể dùng nhiều action tại chỗ nếu guard cho phép:

- Attack.
- Bomb skill.
- Refresh.
- Freeze.
- Các action tương lai không đổi vị trí.

## Attack

- Chủ động, không auto.
- Đánh theo facing.
- Không đổi vị trí actor/target.
- Đặt attack cooldown ngay.
- Có thể dùng cùng stationary skill.

Vùng Attack đề xuất:

```text
P X X
```

Đề xuất chọn enemy gần nhất; tie-break theo trục chính. Chỉ một enemy nhận damage.

## Attack cooldown

- Ngăn Attack spam.
- Không tick cuối chính nhịp vừa Attack.
- Đầu nhịp reset `AttackCooldownStartedThisBeat = false`; khi Attack, đặt flag thành true.
- Ở end-of-beat no-Move, giảm `1` chỉ khi `AttackCooldownStartedThisBeat = false`.
- Valid standard Move hoặc Dash pause Attack cooldown; Bomb skill fuse và status duration vẫn tick.
- Refresh không reset Attack.

## Outgoing damage

Enemy vẫn có HP.

```text
Final Damage
= Base Damage × Damage Multiplier
```

Damage Up chắc chắn tăng Attack và Bomb skill damage. Nó không tăng neutral Environmental Bomb/Turret damage. Công thức/cap là `TBD`.

## End Beat

- Kết thúc Player Phase.
- Không hoàn tác action.
- Enemy Phase bắt đầu.
- WC/streak, active skill cooldown, eligible Attack cooldown và hazard timer dựa trên `PlayerMovedThisBeat` từ standard Move hoặc Dash.
- Bomb skill fuse/status duration tick mỗi end-of-beat bất kể self-movement.

## Phản hồi UI

- Valid/invalid standard Move và Dash path/landing.
- `PlayerMovedThisBeat`.
- Timer/End Beat.
- Attack/skill cooldown.
- Hit và WC penalty.
- Outgoing damage.
- Move Pressure hoặc Dash Spawn Pressure, tối đa một event/beat.

## Tài liệu liên quan

- [Hệ thống nhịp](./beat-and-action-system.md)
- [Enemy và spawn](./enemies-and-spawning.md)
- [Skill và item](./skills-and-items.md)
- [Environmental Hazards](./environmental-hazards.md)

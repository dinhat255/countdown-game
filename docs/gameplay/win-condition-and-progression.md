# WC, Standing Streak và phase

## Tổng quan

WC là tiến trình Victory và thước đo áp lực duy nhất của player. Hit/effect có thể cộng WC; WC không có upper clamp.

## Điều kiện theo self-movement

```text
Nếu PlayerMovedThisBeat = false:
    Standing Streak tăng một lần
    WC giảm một lần
    mọi active skill cooldown > 0 giảm 1, gồm skill vừa dùng
    Attack cooldown giảm 1 nếu AttackCooldownStartedThisBeat = false
    active Environmental Bomb CD giảm 1
    Turret Reload > 0 giảm 1

Nếu PlayerMovedThisBeat = true:
    Standing Streak reset
    WC không giảm
    active skill/Attack cooldown và environmental hazard timers > 0 pause

Mọi beat:
    Bomb skill fuse giảm 1
    status duration giảm 1
    Turret Ready tại 0 vẫn fire-check
```

`PlayerMovedThisBeat` true sau valid standard Move hoặc Dash. Dash dùng chung self-movement cap, cộng WC đúng một lần khi resolve và đặt slot cooldown ngay.

Standard Move/Dash invalid bị reject trước resolve, vì vậy:

- Không partial movement hoặc failure state.
- Không đổi flag.
- Không tiêu cooldown/resource, cộng WC hoặc tạo pressure.
- Player có thể chọn lại.

## Standing Streak

```text
Nhịp 1: Attack + Bomb skill, không Move → Streak 1
Nhịp 2: Refresh + Attack          → Streak 2
Nhịp 3: valid Move hoặc Dash      → Streak 0
Nhịp 4: End Beat, không Move      → Streak 1
```

Stationary action không cản streak.

Đề xuất:

| Streak | WC reduction |
| --- | ---: |
| 1–2 | 1 |
| 3–4 | 2 |
| 5+ | 3 |

Threshold và streak cap cần cân bằng.

## WC tăng

WC có thể tăng do:

- Enemy/hazard hit hợp lệ.
- Valid Dash: cộng đúng một lần, không phải hit.
- Effect đặc biệt được định nghĩa; hit có thể áp status nhưng status không reposition.

WC Penalty Reduction giảm WC tăng từ enemy, Environmental Bomb và Turret hit; không vô hiệu hóa status.
WC Penalty Reduction không giảm WC từ Dash.
Environmental Bomb/Turret hiện không áp status.

`Initial WC` chỉ là baseline. WC có thể tăng vượt baseline không giới hạn gameplay.

## Thứ tự cuối nhịp

1. Hoàn tất/Freeze-skip Enemy Phase và chốt `PlayerMovedThisBeat`.
2. Update WC/streak cùng active skill, eligible Attack và hazard no-Move timers theo flag từ standard Move/Dash; Dash slot cooldown không tick same beat.
3. Tick Bomb skill fuse/status bất kể self-movement.
4. Resolve Bomb skill theo placement order → Environmental Bomb theo stable map order → Turret theo stable map order; apply damage/WC sau từng effect.
5. Mọi Turret Ready `0` fire-check kể cả standard Move/Dash beat; no target giữ `0`, actual shot mới reset.
6. Ghi `LowestWCReached`, `HighestWCReached`, phase và threshold.
7. Nếu `WC ≤ 0`, Victory short-circuit spawn và UI pending khác.
8. Nếu chưa Victory, base spawn tick + đúng một pressure event: Move Pressure cho standard Move hoặc Dash Spawn Pressure mạnh hơn cho Dash; rồi cap/type/spawn bằng phase mới.
9. High-WC dialog → skill replacement → phase panel → nhịp mới.

## Phase đi xuống

```text
Progress
= (Initial WC - LowestWCReached) / Initial WC
```

| Phase | WC còn lại | Enemy trọng tâm |
| --- | --- | --- |
| Phase 1 | 100% đến trên 66,67% | Runner |
| Phase 2 | 66,67% đến trên 33,33% | Jumper |
| Phase 3 | 33,33% đến 0% | Thrower |

Thrower là Enemy Lv3. Cả Runner, Jumper và Thrower có thể spawn ở mọi phase; phase tương ứng tăng weight của level tương ứng. Thrower có weight cao hơn ở Phase 3.

Impact hit từ enemy bị Thrower ném cộng WC như hit hợp lệ khác và có thể áp status được định nghĩa. Throw không đổi vị trí player hoặc `SelfMovedThisBeat` của target, và không ảnh hưởng `PlayerMovedThisBeat`, Standing Streak hoặc skill cooldown.

Phase dùng `LowestWCReached`, chỉ tăng và không lùi.

## High-WC threshold đi lên

```text
HighestWCReached = Initial WC
HandledHighWcThresholds = {}
```

Sau reduction và toàn bộ positive penalty:

1. Lưu `previousHighest`.
2. `newHighest = max(previousHighest, WC)`.
3. Tìm các mốc `T` thỏa `previousHighest < T <= newHighest`.
4. Bỏ mốc đã handled.
5. Mark toàn bộ mốc mới là handled.
6. Hiện một dialog cho mốc cao nhất.
7. Gán `HighestWCReached = newHighest`.

Mỗi mốc chỉ trigger một lần/run.

## Teasing dialog

Ví dụ:

> Bạn vẫn muốn chơi tiếp chứ?

- Pause phase, timer, spawn, skill cooldown và environmental hazard timer.
- Continue: đóng dialog, tiếp tục UI pending.
- Exit: `VoluntaryExit/Quit`, không phải Loss.

## UI priority

1. Victory nếu `WC ≤ 0`; không tick/spawn và không mở UI pending khác.
2. Nếu chưa Victory: high-WC dialog.
3. Skill replacement panel.
4. Phase panel.
5. Nhịp mới.

Victory là kết quả gameplay duy nhất.

## Tài liệu liên quan

- [Hệ thống nhịp](./beat-and-action-system.md)
- [Enemy và spawn](./enemies-and-spawning.md)
- [Skill và item](./skills-and-items.md)
- [Environmental Hazards](./environmental-hazards.md)

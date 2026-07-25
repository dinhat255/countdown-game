using CountdownGame.Core;
using UnityEngine;

namespace CountdownGame.Unity
{
    [CreateAssetMenu(menuName = "Countdown/Movement Configuration", fileName = "MovementConfig")]
    public sealed class MovementConfig : ScriptableObject
    {
        [Min(1)] public int dashDistance = 3;
        [Min(0)] public int movePressure = 1;
        [Min(0)] public int dashPressure = 2;
        [Min(0)] public int dashWcIncrease = 2;

        public MovementTuning ToModel() => new MovementTuning
        {
            DashDistance = dashDistance,
            MovePressure = movePressure,
            DashPressure = dashPressure,
            DashWcIncrease = dashWcIncrease
        };
    }
}

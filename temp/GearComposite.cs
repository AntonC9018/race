using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.Layouts;

namespace Race.Gameplay
{
    public enum GearInteractionType
    {
        None,
        Clutch,
        Up,
        Down,
    }

    #if UNITY_EDITOR
        [InitializeOnLoad]
    #endif
    [DisplayStringFormat("{clutchId}+{gearUpId}|{gearDownId}")]
    public class GearComposite : InputBindingComposite<GearInteractionType>
    {
        [InputControl(layout = "Button")]
        public int clutchPart;

        [InputControl(layout = "Button")]
        public int gearUpPart;

        [InputControl(layout = "Button")]
        public int gearDownPart;
        
        public float timeBeforeCanChangeGearsAfterClutchGetsEnabled;

        private GearInteractionType _previousEvaluatedType;

        public override GearInteractionType ReadValue(ref InputBindingCompositeContext context)
        {
            // TODO: take into account the parameter.
            bool clutch = context.ReadValueAsButton(clutchPart);
            if (!clutch)
            {
                _previousEvaluatedType = GearInteractionType.None;
                return GearInteractionType.None;
            }

            float up = context.ReadValue<float>(gearUpPart);
            float down = context.ReadValue<float>(gearDownPart);
            float sum = up - down;

            if (Mathf.Approximately(sum, 0))
            {
                _previousEvaluatedType = GearInteractionType.Clutch;
                return GearInteractionType.Clutch;
            }

            if (_previousEvaluatedType != GearInteractionType.Clutch)
                return GearInteractionType.Clutch;

            if (sum > 0)
            {
                _previousEvaluatedType = GearInteractionType.Up;
                return GearInteractionType.Up;
            }

            _previousEvaluatedType = GearInteractionType.Down;
            return GearInteractionType.Down;
        }

        static GearComposite()
        {
            InputSystem.RegisterBindingComposite<GearComposite>("Gears");
        }

        [RuntimeInitializeOnLoadMethod]
        static void Init() {} // Trigger static constructor.
    }
}
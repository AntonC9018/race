using EngineCommon;
using UnityEngine;
using static EngineCommon.Assertions;

namespace Race.Gameplay
{
    public class BotInputView : ICarInputView
    {
        public RaceProperties RaceProperties { get; set; }
        public int OwnIndex { get; set; }

        public CarMovementInputValues Movement
        {
            get
            {
                var raceModel = RaceProperties.DataModel;
                ref var participants = ref raceModel.participants;
                var ownPoint = participants.track.points[OwnIndex];
                var track = raceModel.Track;
                var carDataModel = participants.driver[OwnIndex].carProperties.DataModel;
                var transform = carDataModel.ColliderParts.body.transform;

                var (tangent, perp, normal) = track.GetUnitVectors(ownPoint);
                var currentSpeed = carDataModel.GetCurrentSpeed();
                var maxSpeed = carDataModel.GetMaxSpeed();

                float inputFactorDrivingToCenter;
                {
                    var roadMiddlePosition = track.GetRoadMiddlePosition(ownPoint);
                    var diff = roadMiddlePosition - transform.position;
                    var diffLength = Vector3.Dot(diff, perp);
                    
                    var trackWidth = raceModel.trackInfo.visualWidth;
                    var normalizedDiffLength = diffLength / (trackWidth / 2);

                    // The higher the speed, the less it tries going towards the center.
                    var normalizedSpeed = Mathf.Clamp01(Mathf.Abs(currentSpeed) / maxSpeed);
                    const float dampingPower = 1.0f / 2.0f;
                    var willingnessFactor = 1 - Mathf.Pow(normalizedSpeed, dampingPower);

                    inputFactorDrivingToCenter = willingnessFactor * normalizedDiffLength;
                }

                float inputFactorTryingToStraightenTheCar;
                {
                    var forward = transform.forward;
                    var turnDiff = Vector3.Cross(forward, tangent);
                    var normalizedTurnDiff = Vector3.Dot(turnDiff, normal);

                    inputFactorTryingToStraightenTheCar = normalizedTurnDiff;
                }

                float desiredTurnFactor = (inputFactorDrivingToCenter + inputFactorTryingToStraightenTheCar) / 2;

                const float allowedTurnChangePerSecondAtMinimumSpeed = 1.0f;
                float actualTurnFactor = CarDataModelHelper.DampValueDependingOnSpeed(
                    desiredTurnFactor,
                    carDataModel.DrivingState.steeringInputFactor,
                    allowedTurnChangePerSecondAtMinimumSpeed,
                    currentSpeed,
                    maxSpeed);

                return new CarMovementInputValues
                {
                    Forward = 1.0f,
                    Brakes = 0.0f,
                    Turn = actualTurnFactor,
                };
            }
        }

        public bool Clutch => false;
        public GearInputType Gear => GearInputType.None;

        public void OnDrivingStateChanged(CarProperties properties)
        {
            // do stuff.
        }

        public void ResetTo(CarProperties properties)
        {
            properties.OnDrivingStateChanged.AddListener(OnDrivingStateChanged);
        }
    }
}
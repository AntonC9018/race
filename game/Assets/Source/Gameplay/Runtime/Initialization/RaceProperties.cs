using System;
using UnityEngine;
using static EngineCommon.Assertions;

namespace Race.Gameplay
{
    // 2
    public struct ParticipantsDriverInfo
    {
        // 3
        public DriverInfo[] infos;
        public int playerCount;
        public int botCount;

        public readonly Span<DriverInfo> Players => infos.AsSpan(0, playerCount);
        public readonly Span<DriverInfo> Bots => infos.AsSpan(playerCount, botCount);

        // NOTE:
        // I in the end decided to go with a method,
        // because this method is pretty much about the invariance of the data.
        // I was really tempted to go with an extension method here, but a member
        // method actually makes sense in this case. 
        public void Reset(DriverInfo[] players, DriverInfo[] bots)
        {
            Array.Resize(ref this.infos, players.Length + bots.Length);
            
            this.playerCount = players.Length;
            players.CopyTo(this.Players);
            
            this.botCount = bots.Length;
            bots.CopyTo(this.Bots);
        }
    }

    public struct ParticipantsTrackInfo
    {
        // 2, 3
        public RoadPoint[] positions;
        public RoadPoint[] checkpoints;
    }

    public struct Participants
    {
        public ParticipantsDriverInfo driver;
        public ParticipantsTrackInfo track;

        public readonly int Count => driver.infos.Length;
    }

    public struct TrackRaceInfo
    {
        // 1
        public IStaticTrack track;
        public float visualWidth;
        public float actualWidth;
    }

    // TODO: this being a value type would be nice?
    public class RaceDataModel
    {
        public Participants participants;
        public Transform mapTransform;
        public Transform trackTransform;
        public TrackRaceInfo trackInfo;

        public IStaticTrack Track => trackInfo.track;
    }

    public static class RaceDataModelHelper
    {
        public static void ResizeTrackParticipantDataToParticipantDriverData(ref Participants participants)
        {
            Array.Resize(ref participants.track.positions, participants.Count); 
            Array.Resize(ref participants.track.checkpoints, participants.Count); 
        }

        public static void PlaceParticipants(RaceDataModel dataModel)
        {
            // For now, only position in a grid
            var placementData = CarPlacement.GetGridPlacementData(dataModel.participants.driver, dataModel.trackInfo);
            
            ref var participants = ref dataModel.participants;
            
            for (int i = 0; i < participants.Count; i++)
            {
                var (pos, rot) = CarPlacement.GetPositionAndRotation(placementData, i);

                ref readonly var driverInfo = ref participants.driver.infos[i];
                var t = driverInfo.transform;

                // Visual
                CarDataModelHelper.ResetPositionAndRotationOfBackOfCar(
                    t, driverInfo.carProperties, pos, rot);

                SynchronizeTrackLocationToWorldPositionOfParticipant(dataModel, t, i);
            }
        }

        private static void SynchronizeTrackLocationToWorldPositionOfParticipant(
            RaceDataModel dataModel, Transform transform, int participantIndex)
        {
            ref var location = ref dataModel.participants.track.positions[participantIndex];
            location = dataModel.Track.UpdateRoadPoint(location, transform.position);
        }
    }

    public class RaceProperties : MonoBehaviour
    {
        private RaceDataModel _dataModel;
        public RaceDataModel DataModel => _dataModel;

        void Awake()
        {
            _dataModel = new RaceDataModel();
        }
    }
}
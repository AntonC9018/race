using EngineCommon;
using UnityEngine;

using static EngineCommon.Assertions;

namespace Race.Gameplay
{
    public struct GridPlacementData
    {
        public int colCount;
        public int rowCount;
        public int columnCountOnLastRow;
        public bool HasIncompleteLastRow => columnCountOnLastRow != 0;

        // Assume the start is linear.
        // The system cannot handle arbitrary turns and road segment sizes without
        // any assumptions of the underlying track.
        public Vector3 startingPosition;
        public Vector3 tangentDirection;
        public Vector3 perpendicularDirection;
        public Quaternion startingRotation;

        public float trackWidth;
        public float maxCarLength;
    }
    
    public static class CarPlacement
    {
        public static GridPlacementData GetGridPlacementData(
            (float width, float length) max, int carCount, in TrackRaceInfo trackInfo)
        {
            assert(trackInfo.track is not null);

            GridPlacementData result;

            {
                int numCarsPerRow = Mathf.FloorToInt(trackInfo.visualWidth / max.width);
                if (numCarsPerRow == 0)
                    numCarsPerRow = 1;

                var colCount = numCarsPerRow;
                result.colCount = colCount;

                var rowCount = MathHelper.CeilDivide(carCount, result.colCount);
                result.rowCount = rowCount;

                var lastRowCount = carCount % colCount;
                result.columnCountOnLastRow = lastRowCount;

                result.maxCarLength = max.length;
                result.trackWidth = trackInfo.visualWidth;
            }

            {
                var track = trackInfo.track;
                var start = RoadPoint.CreateStartOf(track.StartingSegment);
                var rotation = track.GetRegularRotation(start);
                var position = track.GetRoadMiddlePosition(start);

                var perpendicularDirection = rotation * Vector3.right;
                var tangentDirection = rotation * Vector3.forward;

                var offsetPerp = -trackInfo.visualWidth / 2;
                var offsetTangent = max.length / 2;

                result.startingPosition = position + offsetPerp * perpendicularDirection + offsetTangent * tangentDirection;
                result.startingRotation = rotation;
                result.tangentDirection = tangentDirection;
                result.perpendicularDirection = perpendicularDirection;
            }


            return result;
        }

        public static (float width, float length) ComputeMaxSizes(DriverInfo[] driver, float visualWidth)
        {
            float maxWidth = 0;
            float maxLength = 0;
            for (int i = 0; i < driver.Length; i++)
            {
                ref readonly var participant = ref driver[i];
                var size = participant.carProperties.DataModel.GetBodySize();

                maxWidth = Mathf.Max(size.x, maxWidth);
                maxLength = Mathf.Max(size.z, maxLength);
            }

            return (maxWidth, maxLength);
        }


        // Just use a random access implementation for now, but an iterator will work better here.
        public static (Vector3 position, Quaternion rotation) GetPositionAndRotation(in GridPlacementData data, int carIndex)
        {
            int row = carIndex / data.colCount;
            int column = carIndex % data.colCount;

            int colCount;
            if (data.HasIncompleteLastRow && row == data.rowCount - 1)
                colCount = data.columnCountOnLastRow;
            else
                colCount = data.colCount;

            float segmentWidth = data.trackWidth / colCount;

            Vector3 position = data.startingPosition;
            position += data.maxCarLength * row * data.tangentDirection;
            position += segmentWidth * (column + 0.5f) * data.perpendicularDirection;

            return (position, data.startingRotation);
        }
    }
}
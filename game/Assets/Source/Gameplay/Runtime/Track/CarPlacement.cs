using EngineCommon;
using UnityEngine;

using static EngineCommon.Assertions;

namespace Race.Gameplay
{
    public struct GridPlacementData
    {
        public Vector2 cellSize;
        public int colCount;

        // Assume the start is linear.
        // The system cannot handle arbitrary turns and road segment sizes without
        // any assumptions of the underlying track.
        public Vector3 startingPosition;
        public Vector3 tangentDirection;
        public Vector3 perpendicularDirection;
        public Quaternion startingRotation;
    }

    public static class CarPlacement
    {
        public static GridPlacementData GetGridPlacementData(
            in ParticipantsDriverInfo driver, in TrackRaceInfo trackInfo)
        {
            assert(trackInfo.track is not null);
            assert(driver.infos is not null);

            GridPlacementData result;

            {
                var track = trackInfo.track;
                var start = RoadPoint.CreateStartOf(track.StartingSegment);
                var rotation = track.GetRegularRotation(start);
                var position = track.GetRoadMiddlePosition(start);

                var perpendicularDirection = rotation * Vector3.right;
                result.startingPosition = position - trackInfo.visualWidth / 2 * perpendicularDirection;
                result.startingRotation = rotation;
                result.tangentDirection = rotation * Vector3.forward;
                result.perpendicularDirection = perpendicularDirection;
            }

            float maxWidth = 0;
            {
                float maxLength = 0;
                for (int i = 0; i < driver.infos.Length; i++)
                {
                    ref readonly var participant = ref driver.infos[i];
                    var size = participant.carProperties.DataModel.GetBodySize();

                    maxWidth = Mathf.Max(size.x, maxWidth);
                    maxLength = Mathf.Max(size.z, maxLength);
                }

                int numCarsPerRow = Mathf.FloorToInt(trackInfo.visualWidth / maxWidth);
                if (numCarsPerRow == 0)
                    numCarsPerRow = 1;

                result.colCount = numCarsPerRow;

                float lengthEachCellUntilMultiple = (trackInfo.visualWidth % maxWidth) / numCarsPerRow;
                result.cellSize = new Vector2(maxWidth + lengthEachCellUntilMultiple, maxLength);
                // _rowCount = MathHelper.CeilDivide(participants.Length, numCarsPerRow);
            }

            return result;
        }

        public static (Vector3 position, Quaternion rotation) GetPositionAndRotation(in GridPlacementData data, int carIndex)
        {
            int row = carIndex / data.colCount;
            int column = carIndex % data.colCount;

            Vector3 position = data.startingPosition;
            position += data.cellSize.y * row * data.tangentDirection;
            position += data.cellSize.x * column * data.perpendicularDirection;

            return (position, data.startingRotation);
        }
    }
}
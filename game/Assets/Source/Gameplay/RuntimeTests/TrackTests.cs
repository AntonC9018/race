using UnityEngine;
using NUnit.Framework;

namespace Race.Gameplay.Tests
{
    public static class Helper
    {
        public static void AssertClose(Vector3 expected, Vector3 actual, float epsilon, string message = "")
        {
            Assert.AreEqual(expected.x, actual.x, epsilon, "x was not equal: {0}", message);
            Assert.AreEqual(expected.y, actual.y, epsilon, "y was not equal: {0}", message);
            Assert.AreEqual(expected.z, actual.z, epsilon, "z was not equal: {0}", message);
        }
    }
    // TODO: more tests; these helped me catch a bug, but there are more.
    public class TrackTests
    {
        internal StraightTrack track;

        [SetUp]
        public void Setup()
        {
            var start = new Vector3(1, 1, 1);
            var end = new Vector3(1, 1, 10);
            var width = 3;
            track = new StraightTrack(start, end, width);
        }

        [Test]
        public void StartingSegment_IsValidAndInsideTrack()
        {
            var startPoint = GetStartPoint();
            Assert.True(startPoint.IsValid);
            Assert.True(startPoint.IsInsideTrack);
        }

        private RoadPoint GetStartPoint()
        {
            var startSegment = track.StartingSegment;
            var startPoint = RoadPoint.CreateStartOf(startSegment);
            return startPoint;
        }

        [Test]
        public void QueriedInitialPosition_IsTheSameAsTheActualInitialPosition()
        {
            var startPoint = GetStartPoint();
            var positionOfRoadStart = track.GetRoadMiddlePosition(startPoint);
            Helper.AssertClose(track._startPosition, positionOfRoadStart, 0.01f);
        }

        private Vector3 GetCenterOfTrack()
        {
            return (track._endPosition + track._startPosition) / 2;
        }

        [Test]
        public void PointInsideTrack_StaysInsideTrackAfterUpdate()
        {
            float t = 0.3f;
            var startSegment = track.StartingSegment;
            var startPoint = new RoadPoint(startSegment, t);

            Assert.That(startPoint.IsInsideTrack);
            var position = track.GetRoadMiddlePosition(startPoint);
            var updated = track.UpdateRoadPoint(startPoint, position);
        }

        [Test]
        public void FinalPoint_HasProportionOf1()
        {
            var point = track.GetRoadPointAt(track._endPosition);
            Assert.AreEqual(1.0f, point.position, 0.01f);
        }

        [Test]
        public void QueringCentralPoint_GetsCenter()
        {
            var centerPoint = new RoadPoint(0, 0.5f);
            var centerPos = track.GetRoadMiddlePosition(centerPoint);
            Helper.AssertClose(GetCenterOfTrack(), centerPos, 0.01f);
        }

        [Test]
        public void CenterPoint_HasHalfPositionProportion()
        {
            var center = GetCenterOfTrack();
            var point = track.GetRoadPointAt(center);
            Assert.AreEqual(0.5f, point.position, 0.01f);
        }


        [Test]
        public void Position_ToPoint_ToPosition_AreEqual()
        {
            var positionOnRoad = GetCenterOfTrack();
            var point = track.GetRoadPointAt(positionOnRoad);
            var positionAfter = track.GetRoadMiddlePosition(point);
            Helper.AssertClose(positionOnRoad, positionAfter, 0.01f);
        }
    }


    public class PositioningTests
    {
        internal StraightTrack track;

        [SetUp]
        public void Setup()
        {
            var start = new Vector3(1, 1, 1);
            var end = new Vector3(1, 1, 10);
            var width = 1;
            track = new StraightTrack(start, end, width);
        }

        private GridPlacementData GetDataForTheTrack((float width, float length) max, int carCount)
        {
            var trackInfo = TrackInfo;
            var data = CarPlacement.GetGridPlacementData(max, carCount, trackInfo);
            return data;
        }

        private TrackRaceInfo TrackInfo
        {
            get
            {
                return new TrackRaceInfo
                {
                    track = track,
                    actualWidth = track._roadWidth,
                    visualWidth = track._roadWidth,
                };
            }
        }

        [Test]
        public void SingleCar_SingleRowAndColumn()
        {
            var trackInfo = TrackInfo;
            {
                var a = GetDataForTheTrack((trackInfo.visualWidth, 1), carCount: 1);
                Assert.AreEqual(1, a.colCount);
                Assert.AreEqual(1, a.rowCount);
                Assert.True(!a.HasIncompleteLastRow);
            }
            {
                var a = GetDataForTheTrack((trackInfo.visualWidth * 100, trackInfo.visualWidth * 100), carCount: 1);
                Assert.AreEqual(1, a.colCount);
                Assert.AreEqual(1, a.rowCount);
                Assert.True(!a.HasIncompleteLastRow);
            }
        }

        [Test]
        public void TwoCars_NumberOfRowsAndColumns_CorrespondToHowManyCarsCanFitInSingleRow()
        {
            var trackInfo = TrackInfo;
            {
                var a = GetDataForTheTrack((trackInfo.visualWidth, 1), carCount: 2);
                Assert.AreEqual(1, a.colCount);
                Assert.AreEqual(2, a.rowCount);
                Assert.True(!a.HasIncompleteLastRow);
            }
            {
                var a = GetDataForTheTrack((trackInfo.visualWidth * 100, trackInfo.visualWidth * 100), carCount: 2);
                Assert.AreEqual(1, a.colCount);
                Assert.AreEqual(2, a.rowCount);
                Assert.True(!a.HasIncompleteLastRow);
            }
            {
                var a = GetDataForTheTrack((trackInfo.visualWidth / 2.1f, 1), carCount: 2);
                Assert.AreEqual(2, a.colCount);
                Assert.AreEqual(1, a.rowCount);
                Assert.True(!a.HasIncompleteLastRow);
            }
            {
                var a = GetDataForTheTrack((trackInfo.visualWidth / 3.1f, 1), carCount: 2);
                Assert.AreEqual(3, a.colCount);
                Assert.AreEqual(1, a.rowCount);
                Assert.True(a.HasIncompleteLastRow);
                Assert.True(a.columnCountOnLastRow == 2);
            }
        }

        [Test]
        public void ManyCars_RowsAndColumnsAreCorrect()
        {
            var trackInfo = TrackInfo;
            {
                var a = GetDataForTheTrack((trackInfo.visualWidth, 1), carCount: 4);
                Assert.AreEqual(1, a.colCount);
                Assert.AreEqual(4, a.rowCount);
                Assert.True(!a.HasIncompleteLastRow);
            }
            {
                var a = GetDataForTheTrack((trackInfo.visualWidth * 2, 1), carCount: 4);
                Assert.AreEqual(1, a.colCount);
                Assert.AreEqual(4, a.rowCount);
                Assert.True(!a.HasIncompleteLastRow);
            }
            {
                var a = GetDataForTheTrack((trackInfo.visualWidth / 2.1f, 1), carCount: 4);
                Assert.AreEqual(2, a.colCount);
                Assert.AreEqual(2, a.rowCount);
                Assert.True(!a.HasIncompleteLastRow);
            }
            {
                var a = GetDataForTheTrack((trackInfo.visualWidth / 3.1f, 1), carCount: 4);
                Assert.AreEqual(3, a.colCount);
                Assert.AreEqual(2, a.rowCount);
                Assert.True(a.HasIncompleteLastRow);
                Assert.True(a.columnCountOnLastRow == 1);
            }
            {
                var a = GetDataForTheTrack((trackInfo.visualWidth / 4.1f, 1), carCount: 4);
                Assert.AreEqual(4, a.colCount);
                Assert.AreEqual(1, a.rowCount);
                Assert.True(!a.HasIncompleteLastRow);
            }
            {
                var a = GetDataForTheTrack((trackInfo.visualWidth / 2.1f, 1), carCount: 3);
                Assert.AreEqual(2, a.colCount);
                Assert.AreEqual(2, a.rowCount);
                Assert.True(a.HasIncompleteLastRow);
                Assert.True(a.columnCountOnLastRow == 1);
            }
        }

        [Test]
        public void SingleCar_IsPositionedCorrectly()
        {
            // Equal dimensions
            AssertExpectedPositionForSingleCar((1, 1));
            
            // Unequal dimensions
            AssertExpectedPositionForSingleCar((1, 2));
            AssertExpectedPositionForSingleCar((2.9f, 1));
        }

        private void AssertExpectedPositionForSingleCar((float width, float length) dims)
        {
            var data = GetDataForTheTrack(dims, 1);
            Assert.True(!data.HasIncompleteLastRow);

            var expected = track._startPosition;
            expected.z += dims.length / 2;

            var (pos, rot) = CarPlacement.GetPositionAndRotation(data, 0);
            Helper.AssertClose(expected, pos, 0.01f);
        }


        [Test]
        public void TwoCars_ArePositionedCorrectly()
        {
            var trackInfo = TrackInfo;
            var length = 1.0f;

            {
                var max = (width: trackInfo.visualWidth / 2.1f, length);
                var data = CarPlacement.GetGridPlacementData(max, carCount: 2, trackInfo);
                
                {
                    var expected0 = track._startPosition;
                    expected0.z += length / 2;
                    expected0.x -= trackInfo.visualWidth * 0.25f;
                    var actual = CarPlacement.GetPositionAndRotation(data, 0).position;
                    
                    Helper.AssertClose(expected0, actual, 0.01f);
                }

                {
                    var expected1 = track._startPosition;
                    expected1.z += length / 2;
                    expected1.x += trackInfo.visualWidth * 0.25f;
                    var actual = CarPlacement.GetPositionAndRotation(data, 1).position;
                    
                    Helper.AssertClose(expected1, actual, 0.01f);
                }
            }

            {
                var max = (width: trackInfo.visualWidth / 2.1f, length);
                var data = CarPlacement.GetGridPlacementData(max, carCount: 3, trackInfo);
                
                {
                    var expected0 = track._startPosition;
                    expected0.z += length / 2;
                    expected0.x -= trackInfo.visualWidth * 0.25f;
                    var actual = CarPlacement.GetPositionAndRotation(data, 0).position;
                    
                    Helper.AssertClose(expected0, actual, 0.01f);
                }

                {
                    var expected1 = track._startPosition;
                    expected1.z += length / 2;
                    expected1.x += trackInfo.visualWidth * 0.25f;
                    var actual = CarPlacement.GetPositionAndRotation(data, 1).position;
                    
                    Helper.AssertClose(expected1, actual, 0.01f);
                }

                {
                    var expected2 = track._startPosition;
                    expected2.z += length * 1.5f;
                    var actual = CarPlacement.GetPositionAndRotation(data, 2).position;
                    
                    Helper.AssertClose(expected2, actual, 0.01f);
                }
            }
        }
    }
}
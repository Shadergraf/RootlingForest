using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Manatea.SplineTool
{
    /// <summary> Represents a single arc within a spline. </summary>
    [System.Serializable]
    public abstract class SplineSegment : ScriptableObject
    {

        #region SerializedParameters

        /// <summary> Defines the presion with which this Spline is sampled </summary>
        [SerializeField]
        protected SplineQuality quality = SplineQuality.Default;

        /// <summary> The start point of this segment </summary>
        [SerializeReference]
        private SplinePoint startPoint;

        /// <summary> The ent point of this segment </summary>
        [SerializeReference]
        private SplinePoint endPoint;


        /// <summary>  </summary>
        [SerializeField][HideInInspector]
        public float[] arcLengths;

        /// <summary>  </summary>
        [SerializeField][HideInInspector]
        protected Vector3[] arcPoints;

        #endregion

        #region Properties        

        public SplinePoint StartPoint
        {
            get { return startPoint; }
        }
        public SplinePoint EndPoint
        {
            get { return endPoint; }
        }

        public float Length 
        { 
            get
            {
                return arcLengths[arcLengths.Length - 1]; 
            } 
        }
        public Vector3[] ArcPoints
        {
            get 
            {
                RecalculateArcLengths();
                return arcPoints;
            }
            private set
            {
                arcPoints = value;
            }
        }
        public SplinePoint[] Points
        {
            get 
            {
                return new SplinePoint[] {startPoint, endPoint};
            }
        }


        #endregion

        public static SplineSegment CreateSegment<T>(SplinePoint start, SplinePoint end, SplineQuality quality = SplineQuality.Default) where T : SplineSegment
        {
            var segment = ScriptableObject.CreateInstance<T>();
            
            segment.quality = quality;
            segment.startPoint = start;
            segment.endPoint = end;

            Action<SplinePoint> recalc = (SplinePoint point) => {segment.RecalculateArcLengths();};

            segment.startPoint.OnUpdate += recalc;
            segment.endPoint.OnUpdate += recalc;

            segment.RecalculateArcLengths();

            return segment;
        }        

        protected virtual void RecalculateArcLengths()
        {
            var roughLength =   StartPoint.OutHandlePosition.magnitude
                              + Vector3.Distance(StartPoint.OutHandlePosition + StartPoint.Position, EndPoint.InHandlePosition + EndPoint.Position) 
                              + EndPoint.InHandlePosition.magnitude;

            var arcStep     = 1 / (roughLength * (int)quality);

            BezierUtils.CalculateArcLengths(StartPoint.Position, StartPoint.OutHandlePosition + StartPoint.Position, EndPoint.InHandlePosition + EndPoint.Position, EndPoint.Position, arcStep, out arcLengths, out arcPoints);
        }

        public Vector3 EvaluateSegmentAtTime(float time)
        {
            return EvaluateSegmentAtDistance(time * Length);
        }

        public Vector3 EvaluateSegmentAtDistance(float distance)
        {
            distance = Mathf.Clamp(distance, 0, Length);

            var endIndex    = Array.FindIndex<float>(arcLengths, (float arcLength) => { return arcLength >= distance; } );
            var startIndex  = endIndex - 1;

            var segmentLength   = arcLengths[endIndex] - arcLengths[startIndex];
            var segmentProgress = Mathf.Abs(arcLengths[startIndex] - distance) / segmentLength;
            
            return Vector3.Lerp(arcPoints[startIndex], arcPoints[endIndex], segmentProgress);
        }

    }
}

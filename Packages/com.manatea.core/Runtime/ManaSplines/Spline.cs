using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

namespace Manatea.SplineTool
{
    /// <summary>
    /// Represents a Spline as a collection of SplinePoints, connected via SplineSegments
    /// </summary>
    public class Spline : ScriptableObject
    {

        [SerializeField]
        /// <summary> Holds the points that make up this spline </summary>
        private List<SplinePoint> points;

        /// <summary> The segments connecting the points of this spline </summary>
        [SerializeField]
        private List<SplineSegment> segments;


        public bool IsClosed
        {
            get {return segments[0].StartPoint.Equals(segments[segments.Count -1].EndPoint); }
        }

        public List<SplinePoint> Points 
        {
            get { return points; }
        }

        public List<SplineSegment> Segments 
        {
            get { return segments; }
        }


        /// <summary> Create a new Spline and populate it with values </summary>
        public static Spline CreateSpline()
        {
            //Create instance
            var instance = ScriptableObject.CreateInstance<Spline>();

            //Create Points
            instance.points = new List<SplinePoint>() { SplinePoint.CreateSplinePoint(new Vector3(-1,0,0)), SplinePoint.CreateSplinePoint(new Vector3(1,0,0)) };

            //Create Segment
            instance.segments = new List<SplineSegment>() {SplineSegment.CreateSegment<BezierSegment>(instance.points[0], instance.points[1])};

            return instance;
        }

        /// <summary> Create a new segment for this spline (only for open splines) </summary>
        public void CreateSegment(SplinePoint start, SplinePoint end)
        {

        }

        /// <summary> Combine to neighbouring segments into one </summary>
        public void CombineSegements(SplineSegment segmentA, SplineSegment segmentB)
        {

        }

        /// <summary> Split an existing segment into two </summary>
        public void SplitSegment(SplineSegment segment)
        {

        }

    }
}

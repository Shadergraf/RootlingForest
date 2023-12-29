using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Manatea.SplineTool
{
    [System.Serializable]
    public class SplinePoint : ScriptableObject
    {

        /// <summary> The handle Type of this point </summary>
        [SerializeField]
        private HandleType handleType = HandleType.BROKEN;

        /// <summary> The point's position in local space </summary>
        [SerializeField]
        private Vector3 position;

        /// <summary> The position of this point's inHandle in local space </summary>
        [SerializeField]
        private Vector3 inHandlePosition;

        /// <summary> The position of this point's outHandle in local space </summary>
        [SerializeField]
        private Vector3 outHandlePosition;

        [SerializeField]
        private SplineConstraint[] constraints; //TODO


        public Vector3 Position 
        {
            get 
            {
                return position;
                }
            set 
            {
                position = value;
                OnUpdate?.Invoke(this);
            }
        }

        public Vector3 InHandlePosition
        {
            get
            {
                return (handleType != HandleType.CORNER) ? inHandlePosition : Vector3.zero;
            }
            set 
            {
                inHandlePosition = value;

                if(handleType == HandleType.ALIGNED)
                {
                    outHandlePosition = -inHandlePosition.normalized * outHandlePosition.magnitude;
                }

                OnUpdate?.Invoke(this);
            }
        }

        public Vector3 OutHandlePosition
        {
            get
            {
                return (handleType != HandleType.CORNER) ? outHandlePosition : Vector3.zero;
            }
            set 
            {
                outHandlePosition = value;

                if(handleType == HandleType.ALIGNED)
                {
                    inHandlePosition = -outHandlePosition.normalized * inHandlePosition.magnitude;
                }

                OnUpdate?.Invoke(this);
            }
        }


//        public Vector3 WorldPosition
//        {
//            get
//            {
//                return parent.transform.TransformPoint(Position);
//            }
//            set
//            {
//                Position = parent.transform.InverseTransformPoint(value);
//            }
//        }
//
//        public Vector3 InHandleWorldPosition
//        {
//            get 
//            {
//                return parent.transform.TransformPoint(InHandlePosition);
//            }
//            set
//            {
//                InHandlePosition = parent.transform.InverseTransformPoint(value);
//            }
//        }
//
//        public Vector3 OutHandleWorldPosition
//        {
//            get 
//            {
//                return parent.transform.localToWorldMatrix.MultiplyPoint(OutHandlePosition);
//            }
//            set
//            {
//                OutHandlePosition = parent.transform.worldToLocalMatrix.MultiplyPoint(value);
//            }
//        }

        public event Action<SplinePoint> OnUpdate;

        public static SplinePoint CreateSplinePoint()
        {
            return CreateSplinePoint(Vector3.zero);
        }

        public static SplinePoint CreateSplinePoint(Vector3 position)
        {
            return CreateSplinePoint(position, new Vector3(-1,0,0), new Vector3(1,0,0));
        }

        public static SplinePoint CreateSplinePoint(Vector3 position, Vector3 inHandle, Vector3 outHandle)
        {
            var instance = ScriptableObject.CreateInstance<SplinePoint>();
            
            instance.position           = position;
            instance.inHandlePosition   = inHandle;
            instance.outHandlePosition  = outHandle;

            return instance;
        }



    }
}

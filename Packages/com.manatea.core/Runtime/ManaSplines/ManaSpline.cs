using UnityEngine;

namespace Manatea.SplineTool
{

    public class ManaSpline : MonoBehaviour
    {

        [SerializeField]
        public Spline splineData;

        void OnValidate()
        {

            if(splineData == null)
            {
                splineData = Spline.CreateSpline();
            }

        }

    }

}
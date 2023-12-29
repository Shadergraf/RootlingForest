using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Manatea
{
    public enum UpdateCycle
    {
        Update = 0,
        LateUpdate = 1,
        FixedUpdate = 2,
        UpdateUnscaled = 3,
        LateUpdateUnscaled = 4,
        FixedUpdateUnscaled = 5,
    }
}

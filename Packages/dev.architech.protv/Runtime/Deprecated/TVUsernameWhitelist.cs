using System;
using UnityEngine;
#pragma warning disable CS0169

namespace ArchiTech.ProTV
{
    [AddComponentMenu(""), Obsolete("Use TVManagedWhitelist instead")]
    public class TVUsernameWhitelist : TVManagedWhitelist
    {
        private bool preCheck;
    }
}
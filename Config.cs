using System.Collections.Generic;
using UnityEngine;

namespace QuantumTele
{
    public class Config
    {
        public string MenuEnableKey;
        public bool ShouldTakeMoney;
        public bool DistancePricing;
        public int GlobalPrice;
        public int MinimumDistanceBasedPrice;
        public int MaximumDistanceBasedPrice;
        public float DistancePriceMultiplier;
        public List<TeleportDestination> TeleportDestinations;
    }

    public class TeleportDestination
    {
        public string DestinationName;
        public Vector3 DestinationLocation;
        public bool MapExtensionRequired = false;
        public List<KeyCode> DestinationHotKeys;
    }
}
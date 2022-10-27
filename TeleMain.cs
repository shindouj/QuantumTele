using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using SimplePartLoader;
using UnityEngine;
using Path = System.IO.Path;

namespace QuantumTele
{
    public class TeleMain : Mod
    {
        public sealed override string ID => "net.jeikobu.QuantumTele";
        public sealed override string Name => "QuantumTele";
        public sealed override string Author => "Shindou Jeikobu";
        public sealed override string Version => "0.0.1";

        private Config _config;
        private Rect _teleporterWindow = new Rect(20, 80, 200, 200);
        private int _page = 0;
        private bool _showWindow = false;

        private string _configFilePath =
            Path.Combine(Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath),
                "QuantumTele.json");

        private HashSet<KeyCode> _currentlyPressedKeys = new HashSet<KeyCode>();

        public TeleMain()
        {
            if (!File.Exists(_configFilePath))
            {
                using (Stream resource =
                       Assembly.GetExecutingAssembly().GetManifestResourceStream("QuantumTele.defaultConfig.json"))
                {
                    if (resource == null)
                    {
                        Debug.LogError($"[{Name}]: Default config has NOT been found in .dll!");
                        throw new ArgumentException("Default config not available in .dll!");
                    }

                    using (Stream output = File.OpenWrite(_configFilePath))
                    {
                        resource.CopyTo(output);
                    }
                }
            }

            _config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(_configFilePath));

            Debug.Log($"{Name} v{Version} by {Author} loaded successfully.");
        }

        public override void OnGUI()
        {
            base.OnGUI();

            if (Event.current.isKey && Event.current.keyCode != KeyCode.None)
            {
                if (Event.current.type == EventType.KeyDown)
                {
                    _currentlyPressedKeys.Add(Event.current.keyCode);
                }
                else if (Event.current.type == EventType.KeyUp)
                {
                    _currentlyPressedKeys.Remove(Event.current.keyCode);
                }
            }

            if (Event.current.Equals(Event.KeyboardEvent(_config.MenuEnableKey)))
            {
                _showWindow = !_showWindow;
            }

            if (_showWindow)
            {
                _teleporterWindow = GUILayout.Window(2137, _teleporterWindow, BuildWindow, "QuantumTele");
            }

            foreach (var destination in _config.TeleportDestinations)
            {
                var player = GameObject.Find("Player").transform;
                
                if (destination.DestinationHotKeys != null &&
                    destination.DestinationHotKeys.Count > 0 &&
                    destination.DestinationHotKeys.All(value => _currentlyPressedKeys.Contains(value)))
                {
                    Teleport(player, destination);
                    _currentlyPressedKeys.Clear();
                }
            }
        }

        private void BuildWindow(int windowID)
        {
            var pageSize = 5;
            if (_config.ShouldTakeMoney)
            {
                pageSize = 3;
            }
            
            var mapExtensionEnabled = MapExtensionEnabled();
            var destinations = _config.TeleportDestinations;

            if (!mapExtensionEnabled)
            {
                destinations = destinations.FindAll(it => !it.MapExtensionRequired);
            }
            
            destinations = destinations.Skip(_page * pageSize).Take(pageSize).ToList();
            
            var player = GameObject.Find("Player").transform;
            var absolutePlayerPosition = ModUtils.UnshiftCoords(player.position);
            GUILayout.Label(absolutePlayerPosition.ToString());

            foreach (var destination in destinations)
            {
                var price = GetTeleportPrice(player, destination);
                var label = destination.DestinationName;

                if (destination.DestinationHotKeys != null && destination.DestinationHotKeys.Count > 0)
                {
                    var keyboardShortcut = String.Join("+", destination.DestinationHotKeys);
                    label = $"{label} [{keyboardShortcut}]";
                }

                if (_config.ShouldTakeMoney)
                {
                    label = $"{label}\nPrice: {price:F}";
                }

                if (GUILayout.Button(label))
                {
                    Teleport(player, destination);
                }
            }

            GUILayout.BeginHorizontal();
            if (_page > 0 && GUILayout.Button("<-"))
            {
                Debug.Log("A1" + _config.TeleportDestinations.Count + " " + _page + " " + pageSize);
                _page--;
            }

            if (_config.TeleportDestinations.Count > ((_page + 1) * pageSize) && GUILayout.Button("->"))
            {
                Debug.Log("A2" + _config.TeleportDestinations.Count + " " + _page + " " + pageSize);
                _page++;
            }

            GUILayout.EndHorizontal();
        }

        private bool MapExtensionEnabled()
        {
            return PlayerPrefs.HasKey("MapExtension") && PlayerPrefs.GetFloat("MapExtension").Equals(1.0f);
        }

        private void Teleport(Transform player, TeleportDestination destination)
        {
            if (HandlePayment(destination))
            {
                player.position = ModUtils.ShiftCoords(destination.DestinationLocation);
            }
        }

        private float GetTeleportPrice(Transform player, TeleportDestination destination)
        {
            var absolutePlayerPosition = ModUtils.UnshiftCoords(player.transform.position);
            var distance = Vector3.Distance(absolutePlayerPosition, destination.DestinationLocation);
            
            if (_config.DistancePricing)
            {
                return Math.Max(_config.MinimumDistanceBasedPrice, Math.Min(distance * _config.DistancePriceMultiplier, _config.MaximumDistanceBasedPrice));
            }
            else
            {
                return _config.GlobalPrice;
            }
        }
        
        private bool HandlePayment(TeleportDestination destination)
        {
            var player = GameObject.Find("Player").transform;

            if (_config.ShouldTakeMoney)
            {
                var price = GetTeleportPrice(player, destination);
                if (tools.money >= price)
                {
                    tools.money -= price;
                    ModUtils.PlayCashSound();
                    return true;
                }
                return false;
            }

            return true;
        }
    }
}
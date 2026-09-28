#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace OpenGSCore
{
    public class GameScene
    {
        private JObject? _lastSnapshot = null;
        private readonly List<AbstractGameObject> _objects = new();
        private readonly Dictionary<string, AbstractGameObject> _objectIndex = new(StringComparer.Ordinal);
        private readonly object _syncRoot = new();

        public int ObjectCount
        {
            get
            {
                lock (_syncRoot)
                {
                    return _objects.Count;
                }
            }
        }

        public bool AddBullet(BulletGameObject bullet) => AddObject(bullet);

        public bool AddPlayerCharacter(PlayerGameObject character) => AddObject(character);

        public bool AddFieldItem(AbstractFieldItem item) => AddObject(item);

        public bool AddGrenade(AbstractGameObject grenade) => AddObject(grenade);

        public bool AddFlag(AbstractGameObject flag) => AddObject(flag);

        public bool RemoveObject(string objectId)
        {
            if (string.IsNullOrWhiteSpace(objectId)) return false;

            lock (_syncRoot)
            {
                if (!_objectIndex.TryGetValue(objectId, out var obj)) return false;

                _objects.Remove(obj);
                _objectIndex.Remove(objectId);
                return true;
            }
        }

        public AbstractGameObject? FindObject(string objectId)
        {
            if (string.IsNullOrWhiteSpace(objectId))
            {
                Console.WriteLine("[GameScene] FindObject called with empty objectId.");
                return null;
            }
            lock (_syncRoot)
            {
                return _objectIndex.TryGetValue(objectId, out var obj) ? obj : null;
            }
        }

        public void UpdatePlayerPosition(string playerId, float posX, float posY)
        {
            var player = FindObject(playerId) as PlayerGameObject;
            if (player != null)
            {
                player.Posx = posX;
                player.Posy = posY;
            }
        }

        public void UpdateFrame()
        {
            foreach (var obj in Snapshot())
            {
                obj.Update();
            }

            // save sync state after updates
            foreach (var obj in Snapshot())
            {
                obj.SaveSyncState();
            }
        }

        // Deterministic tick entry point
        public void FixedUpdate() => UpdateFrame();

        public JObject ToJson()
        {
            var array = new JArray();
            foreach (var item in Snapshot())
            {
                array.Add(CreateObjectJson(item));
            }

            return new JObject
            {
                ["Objects"] = array
            };
        }

        public JObject GetSnapshot()
        {
            var snap = ToJson();
            _lastSnapshot = snap.DeepClone() as JObject;
            return snap;
        }

        public void ApplySnapshot(JObject snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            _lastSnapshot = snapshot.DeepClone() as JObject;

            if (snapshot["Objects"] is not JArray objects)
            {
                return;
            }

            lock (_syncRoot)
            {
                foreach (var token in objects.OfType<JObject>())
                {
                    ApplyObjectSnapshot(token);
                }
            }
        }

        public JObject? GetSnapshotDelta()
        {
            var current = ToJson();
            if (_lastSnapshot == null)
            {
                _lastSnapshot = current.DeepClone() as JObject;
                return current;
            }

            var lastCount = (_lastSnapshot["Objects"] as JArray)?.Count ?? 0;
            var curCount = (current["Objects"] as JArray)?.Count ?? 0;
            if (lastCount != curCount)
            {
                _lastSnapshot = current.DeepClone() as JObject;
                return current;
            }

            Console.WriteLine("[GameScene] No snapshot delta available.");
            return null;
        }

        public JObject AllPlayerDataToJson()
        {
            var players = new JArray();
            foreach (var player in Snapshot().OfType<PlayerGameObject>())
            {
                players.Add(CreateObjectJson(player));
            }

            return new JObject
            {
                ["Players"] = players
            };
        }

        private bool AddObject(AbstractGameObject gameObject)
        {
            if (gameObject == null) return false;

            lock (_syncRoot)
            {
                if (string.IsNullOrWhiteSpace(gameObject.Id)) gameObject.Id = Guid.NewGuid().ToString("N");
                if (_objectIndex.ContainsKey(gameObject.Id)) return false;
                _objects.Add(gameObject);
                _objectIndex.Add(gameObject.Id, gameObject);
            }

            // initialize sync state
            gameObject.SaveSyncState();
            return true;
        }

        private List<AbstractGameObject> Snapshot()
        {
            lock (_syncRoot)
            {
                return _objects.ToList();
            }
        }

        private PlayerGameObject? FindPlayerObject(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return null;
            }

            lock (_syncRoot)
            {
                return _objects.OfType<PlayerGameObject>()
                    .FirstOrDefault(player => string.Equals(player.PlayerId, playerId, StringComparison.OrdinalIgnoreCase));
            }
        }

        private void ApplyObjectSnapshot(JObject json)
        {
            var id = json["id"]?.ToString() ?? json["ID"]?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            if (!_objectIndex.TryGetValue(id, out var obj) || obj == null)
            {
                obj = CreateObjectFromSnapshot(json);
                if (obj == null)
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(obj.Id))
                {
                    obj.Id = id;
                }

                _objects.Add(obj);
                _objectIndex[id] = obj;
            }

            obj.Id = id;
            obj.Name = json["name"]?.ToString() ?? json["Name"]?.ToString() ?? obj.Name;
            obj.Posx = json["posx"]?.ToObject<float>() ?? json["PosX"]?.ToObject<float>() ?? obj.Posx;
            obj.Posy = json["posy"]?.ToObject<float>() ?? json["PosY"]?.ToObject<float>() ?? obj.Posy;
            obj.Frame = json["frame"]?.ToObject<int>() ?? json["Frame"]?.ToObject<int>() ?? obj.Frame;

            if (obj is PlayerGameObject player)
            {
                player.PlayerId = json["playerId"]?.ToString() ?? json["PlayerId"]?.ToString() ?? player.PlayerId;
                if (player.Status != null)
                {
                    player.Status.Hp = json["hp"]?.ToObject<float>() ?? json["Hp"]?.ToObject<float>() ?? player.Status.Hp;
                    player.Status.MaxHp = json["maxHp"]?.ToObject<float>() ?? json["MaxHp"]?.ToObject<float>() ?? player.Status.MaxHp;
                    player.Status.Booster = json["booster"]?.ToObject<float>() ?? json["Booster"]?.ToObject<float>() ?? player.Status.Booster;
                    player.Status.MaxBooster = json["maxBooster"]?.ToObject<float>() ?? json["MaxBooster"]?.ToObject<float>() ?? player.Status.MaxBooster;
                }
            }

            if (obj is AbstractBullet bullet)
            {
                bullet.Damage = json["damage"]?.ToObject<float>() ?? bullet.Damage;
                bullet.Angle = json["angle"]?.ToObject<float>() ?? bullet.Angle;
                bullet.Speed = json["speed"]?.ToObject<float>() ?? bullet.Speed;
                bullet.StoppingPower = json["stoppingPower"]?.ToObject<float>() ?? bullet.StoppingPower;
            }
        }

        private static AbstractGameObject? CreateObjectFromSnapshot(JObject json)
        {
            var type = json["type"]?.ToString() ?? json["Type"]?.ToString();
            var name = json["name"]?.ToString() ?? json["Name"]?.ToString() ?? string.Empty;
            var x = json["posx"]?.ToObject<float>() ?? json["PosX"]?.ToObject<float>() ?? 0f;
            var y = json["posy"]?.ToObject<float>() ?? json["PosY"]?.ToObject<float>() ?? 0f;
            var playerId = json["playerId"]?.ToString() ?? json["PlayerId"]?.ToString() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(playerId))
            {
                return new PlayerGameObject(playerId, name, x, y);
            }

            return type switch
            {
                nameof(eGameObjectType.Character) => new Character(name, x, y),
                nameof(eGameObjectType.FieldItem) => new FieldItem(x, y),
                nameof(eGameObjectType.Grenade) => new NormalGranade(x, y),
                "Bullet" => new BulletGameObject(0, 0, 0),
                _ => null
            };
        }

        private static JObject CreateObjectJson(AbstractGameObject item)
        {
            if (item == null) return new JObject();
            return item.ToJSon() ?? new JObject
            {
                ["Name"] = item.Name ?? string.Empty,
                ["ID"] = item.Id ?? string.Empty,
                ["PosX"] = item.Posx,
                ["PosY"] = item.Posy
            };
        }
    }
}


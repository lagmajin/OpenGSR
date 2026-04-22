using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenGS
{
    /// <summary>
    /// Holds information about banned weapons. Designed to have no Unity side-effects.
    /// </summary>
    public class WeaponLimit
    {
        private readonly HashSet<eWeaponType> banned = new HashSet<eWeaponType>();

        public WeaponLimit()
        {
        }

        public WeaponLimit(IEnumerable<eWeaponType> initialBanned)
        {
            if (initialBanned != null)
            {
                foreach (var w in initialBanned) banned.Add(w);
            }
        }

        public IReadOnlyCollection<eWeaponType> GetBanned() => banned.ToList().AsReadOnly();

        public bool IsBanned(eWeaponType weapon) => banned.Contains(weapon);

        public bool Ban(eWeaponType weapon)
        {
            if (weapon == eWeaponType.None) return false;
            return banned.Add(weapon);
        }

        public bool Unban(eWeaponType weapon)
        {
            if (weapon == eWeaponType.None) return false;
            return banned.Remove(weapon);
        }

        public void Clear()
        {
            banned.Clear();
        }

        public void BanAll()
        {
            banned.Clear();
            foreach (eWeaponType v in Enum.GetValues(typeof(eWeaponType)))
            {
                if (v == eWeaponType.None) continue;
                banned.Add(v);
            }
        }

        public bool Toggle(eWeaponType weapon)
        {
            if (IsBanned(weapon))
            {
                Unban(weapon);
                return false;
            }
            else
            {
                Ban(weapon);
                return true;
            }
        }

        // Convenience string-based APIs. Lookups are case-insensitive.
        public bool TryBanByName(string name)
        {
            if (TryParseName(name, out var w)) return Ban(w);
            return false;
        }

        public bool TryUnbanByName(string name)
        {
            if (TryParseName(name, out var w)) return Unban(w);
            return false;
        }

        public bool IsBannedByName(string name)
        {
            if (TryParseName(name, out var w)) return IsBanned(w);
            return false;
        }

        private bool TryParseName(string name, out eWeaponType weapon)
        {
            weapon = eWeaponType.None;
            if (string.IsNullOrWhiteSpace(name)) return false;

            if (Enum.TryParse<eWeaponType>(name, true, out var parsed))
            {
                weapon = parsed;
                return true;
            }

            if (WeaponType.dic != null)
            {
                foreach (var kv in WeaponType.dic)
                {
                    if (string.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase))
                    {
                        weapon = kv.Value;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}

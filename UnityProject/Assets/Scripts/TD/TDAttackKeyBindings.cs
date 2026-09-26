using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TDAnnihilation
{
    public sealed class TDAttackKeyBindings
    {
        private const string PreferencesKey = "TDAnnihilation.AttackKeyBindings";

        private Key light = Key.F;
        private Key heavy = Key.Q;
        private Key mega = Key.E;

        public Key GetKey(TDAttackType type)
        {
            switch (type)
            {
                case TDAttackType.Heavy: return heavy;
                case TDAttackType.Mega: return mega;
                default: return light;
            }
        }

        public bool TrySetKey(TDAttackType type, Key key)
        {
            if (!IsValidKey(key) || !Enum.IsDefined(typeof(TDAttackType), type)) return false;
            if (type != TDAttackType.Light && light == key) return false;
            if (type != TDAttackType.Heavy && heavy == key) return false;
            if (type != TDAttackType.Mega && mega == key) return false;

            switch (type)
            {
                case TDAttackType.Light: light = key; break;
                case TDAttackType.Heavy: heavy = key; break;
                case TDAttackType.Mega: mega = key; break;
            }
            return true;
        }

        public void Save()
        {
            PlayerPrefs.SetString(PreferencesKey, ((int)light) + "," + (int)heavy + "," + (int)mega);
            PlayerPrefs.Save();
        }

        public static TDAttackKeyBindings Load()
        {
            var bindings = new TDAttackKeyBindings();
            string[] values = PlayerPrefs.GetString(PreferencesKey, string.Empty).Split(',');
            if (values.Length != 3 ||
                !int.TryParse(values[0], out int lightValue) ||
                !int.TryParse(values[1], out int heavyValue) ||
                !int.TryParse(values[2], out int megaValue)) return bindings;

            Key loadedLight = (Key)lightValue;
            Key loadedHeavy = (Key)heavyValue;
            Key loadedMega = (Key)megaValue;
            if (!IsValidKey(loadedLight) || !IsValidKey(loadedHeavy) || !IsValidKey(loadedMega) ||
                loadedLight == loadedHeavy || loadedLight == loadedMega || loadedHeavy == loadedMega) return bindings;

            bindings.light = loadedLight;
            bindings.heavy = loadedHeavy;
            bindings.mega = loadedMega;
            return bindings;
        }

        private static bool IsValidKey(Key key) =>
            Enum.IsDefined(typeof(Key), key) && key != Key.None && key != Key.Escape;
    }
}

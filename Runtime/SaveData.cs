using System;
using System.Collections.Generic;
using UnityEngine;

namespace HunterAllen.SaveSystem
{
    [Serializable]
    public class SaveData : Dictionary<string, SaveDataBase>, ISerializationCallbackReceiver
    {
        [SerializeField]
        List<string> _keys = new List<string>();

        [SerializeField]
        List<SaveDataBase> _saveValues = new List<SaveDataBase>();

        public void OnBeforeSerialize()
        {
            _keys.Clear();
            _saveValues.Clear();

            foreach(KeyValuePair<string, SaveDataBase> pair in this)
            {
                _keys.Add(pair.Key);
                pair.Value.OnBeforeSerialize();
                _saveValues.Add(pair.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            this.Clear();

            if (_keys.Count != _saveValues.Count)
            {
                throw new DataMisalignedException($"Keys ({_keys.Count}) and Values ({_saveValues.Count}) did not have the same length.");
            }

            for (int i = 0; i < _keys.Count; i++)
            {
                _saveValues[i].OnAfterDeserialize();
                this.Add(_keys[i], _saveValues[i]);
            }
        }
    }

    [Serializable]
    public abstract class SaveDataBase : ISerializationCallbackReceiver
    {
        public abstract object Get();
        public abstract void Set(string id, object data);

        public virtual void OnBeforeSerialize() { }
        public virtual void OnAfterDeserialize() { }
    }

    [Serializable]
    public class SaveData<T> : SaveDataBase, ISerializationCallbackReceiver
    {
        public Dictionary<string, T> data = new();

        [SerializeField]
        List<string> _keys = new List<string>();

        [SerializeField]
        List<T> _saveValues = new List<T>();

        public override object Get() => data;
        public override void Set(string id, object d) => data[id] = (T)d;

        public override void OnBeforeSerialize()
        {
            _keys.Clear();
            _saveValues.Clear();

            foreach (KeyValuePair<string, T> pair in data)
            {
                _keys.Add(pair.Key);
                _saveValues.Add(pair.Value);
            }
        }

        public override void OnAfterDeserialize()
        {
            data.Clear();

            if (_keys.Count != _saveValues.Count)
            {
                throw new DataMisalignedException($"Keys ({_keys.Count}) and Values ({_saveValues.Count}) did not have the same length.");
            }

            for (int i = 0; i < _keys.Count; i++)
            {
                data.Add(_keys[i], _saveValues[i]);
            }
        }

        public static implicit operator Dictionary<string, T>(SaveData<T> sd) => sd.data;
    }
}

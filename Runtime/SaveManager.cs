using UnityEngine;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HunterAllen.SaveSystem
{
    public static class SaveManager
    {
        //                     Data Name   Data
        public static Dictionary<string, object> Data = new();
        static Dictionary<Type, Action<IData>> _onLoadEvents = new();
        public static bool DebugLogs;

        static IDataSaveHandler _dataHandler;

        /// <summary>
        /// Called as soon as the game starts via GameBootstrapper.cs
        /// </summary>
        public static void Initialize()
        {
            Data = new();
            _onLoadEvents = new();
            _dataHandler = new FileDataHandler(Application.persistentDataPath + "/saves/");
        }

        #region Events
        public static void BindLoadEvent<T>(this IDataHandler<T> handler) where T : IData
        {
            var @event = GetNotifyEvent<T>();
            @event += handler.HandleData;
            _onLoadEvents[typeof(T)] = @event;
        }
        public static void UnbindLoadEvent<T>(this IDataHandler<T> handler) where T : IData
        {
            var @event = GetNotifyEvent<T>();
            @event -= handler.HandleData;
            _onLoadEvents[typeof(T)] = @event;
        }
        public static async void NotifyDataHandlers<T>(string dataName, bool waitForFrame = false) where T : IData
        {
            if (!Data.ContainsKey(dataName))
            {
                Debug.LogWarning($"Data dictionary does not contain key {dataName}.");
                return;
            }
            
            var data = (T)Data[dataName];

            if (waitForFrame)
            {
                await Task.Yield();
                if (Application.exitCancellationToken.IsCancellationRequested) return;
            }

            var @event = GetNotifyEvent<T>();
            @event?.Invoke(data);
        }

        static Action<IData> GetNotifyEvent<T>()
        {
            if (!_onLoadEvents.TryGetValue(typeof(T), out var @event))
            {
                _onLoadEvents.Add(typeof(T), delegate { });
                @event = _onLoadEvents[typeof(T)];
            }
            return @event;
        }
        #endregion
        
        #region Saving and Loading
        /// <summary>
        /// Creates save data at the given file path and assigns the given data name.
        /// </summary>
        public static void New<T>(T t, string dataName, string fileName, int profile = 0)
        {
            Data[dataName] = t;
            Save(t, fileName, profile);
            if (DebugLogs) Debug.Log($"Created new {typeof(T).Name}.");
        }

        /// <summary>
        /// Saves data with the corresponding data name to the given file path.
        /// </summary>
        public static void Save(string dataName, string fileName, int profile = 0)
        {
            Save(Get(dataName), fileName, profile);
        }
        /// <summary>
        /// Saves data to the given file path.
        /// </summary>
        public static void Save<T>(T data, string fileName, int profile = 0)
        {
            // Save data
            Data[typeof(T).Name] = data;
            _dataHandler.Save(data, fileName + profile);
        }

        /// <summary>
        /// Finds all IDataProviders<T> and saves the data to the given file path.
        /// </summary>
        public static void SaveAllData<T>(string dataName, string fileName, int profile = 0)
        {
#if UNITY_6000_0_OR_NEWER
            var objects = GameObject.FindObjectsByType<MonoBehaviour>().OfType<IDataProvider<T>>();
#else
            var objects = GameObject.FindObjectsOfType<MonoBehaviour>().OfType<IDataProvider<T>>();
#endif
            var data = (SaveData)Data[dataName];

            foreach (var obj in objects)
            {
                if (!Data.ContainsKey(dataName))
                {
                    Data.Add(dataName, new SaveData());
                }
                if (!data.ContainsKey(typeof(T).Name))
                {
                    data.Add(typeof(T).Name, new SaveData<T>());
                }
                data[typeof(T).Name].Set(obj.Id, obj.ProvideData());
            }

            _dataHandler.Save(Data[dataName], fileName + profile);
        }

        /// <summary>
        /// Attempts to load data with the corresponding data name form the given file path.
        /// </summary>
        public static T Load<T>(string dataName, string fileName, int profile = 0)
        {
            // Load data
            T data = _dataHandler.Load<T>(fileName + profile, out bool successful);

            if (!successful)
            {
                if (DebugLogs) Debug.LogWarning($"No data of type {typeof(T).Name} found at {Application.persistentDataPath + "/saves/" + fileName + profile}.dat, initial data needs to be created.");
                return default;
            }

            Data[dataName] = data;
            return data;
        }
        /// <summary>
        /// Attempts to load data with the corresponding data name form the given file path.
        /// </summary>
        public static T Load<T>(string dataName, string fileName, out bool successful, int profile = 0)
        {
            // Load data
            T data = _dataHandler.Load<T>(fileName + profile, out successful);

            if (!successful)
            {
                if (DebugLogs) Debug.LogWarning($"No data of type {typeof(T).Name} found at {Application.persistentDataPath + "/saves/" + fileName + profile}.dat, initial data needs to be created.");
                return default;
            }

            Data[dataName] = data;
            return data;
        }

        /// <summary>
        /// Provides all IDataHandler<T>'s with data of type T with the corresponding data name from the given file path.
        /// </summary>
        public static T LoadAll<T>(string dataName, string fileName, int profile = 0) where T : IData
        {
            // Load data
            T saveData = _dataHandler.Load<T>(fileName + profile, out bool successful);

            if (!successful || saveData == null)
            {
                if (DebugLogs) Debug.LogWarning($"No data of type {typeof(T).Name} found at {Application.persistentDataPath + "/saves/" + fileName + profile}.dat, initial data needs to be created.");
                return default;
            }

            Data[dataName] = saveData;

#if UNITY_6000_0_OR_NEWER
            var objects = GameObject.FindObjectsByType<MonoBehaviour>().OfType<IDataHandler<T>>();
#else
            var objects = GameObject.FindObjectsOfType<MonoBehaviour>().OfType<IDataHandler<T>>();
#endif
            var data = (T)Data[dataName];

            foreach (var obj in objects)
            {
                obj.HandleData(data);
            }

            return data;
        }

        /// <summary>
        /// Attempts to get data of type T and the given data name.
        /// </summary>
        public static T Get<T>(string dataName)
        {
            if (!Data.ContainsKey(dataName))
            {
                if (DebugLogs) Debug.LogWarning($"SaveManager does not contain data of type {typeof(T).Name}");
                return default;
            }
            return (T)Data[dataName];
        }
        /// <summary>
        /// Attempts to get data of type T and the given data name.
        /// </summary>
        public static T Get<T>(string dataName, out bool successful)
        {
            if (!Data.ContainsKey(dataName))
            {
                if (DebugLogs) Debug.LogWarning($"SaveManager does not contain data of type {typeof(T).Name}");
                successful = false;
                return default;
            }
            successful = true;
            return (T)Data[dataName];
        }
        /// <summary>
        /// Attempts to get data of type T and the given data name.
        /// </summary>
        public static object Get(string dataName)
        {
            if (!Data.ContainsKey(dataName))
            {
                if (DebugLogs) Debug.LogWarning($"SaveManager does not contain data with name {dataName}");
                return default;
            }
            return Data[dataName];
        }
        /// <summary>
        /// Attempts to get data of type T and the given data name, creates data if it doesn't exist.
        /// </summary>
        public static T GetSafe<T>(string dataName, string filePath, int profile = 0)
        {
            T t = Get<T>(dataName, out bool successful);

            if (t != null && successful) return t;

            t = Load<T>(dataName, filePath, out successful, profile);

            if (t != null && successful) return t;

            t = default(T);
            New<T>(t, dataName, filePath, profile);
            return t;
        }
        #endregion
    }
}
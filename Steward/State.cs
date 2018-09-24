using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using Newtonsoft.Json;

namespace StewardCielo {
    public static class State {
        public delegate void PersistenceEventHandler(object sender, EventArgs args);
        private static Timer _timer;
        private static ConcurrentDictionary<string, object> _cache = new ConcurrentDictionary<string, object>();
        private static object _sync = new object();

        public static void SchedulePersistence() {
            _timer = new Timer(_ => {
                Persistence(null, new EventArgs());
                lock (_sync) {
                    foreach (var pair in _cache) {
                        var json = JsonConvert.SerializeObject(pair.Value);
                        File.WriteAllText(pair.Key + ".json", json);
                    }
                }
            }, null, 1000 * 60 * 10, 1000 * 60 * 10);
        }

        public static T Get<T>() where T : new() {
            lock (_sync) {
                var name = typeof(T).Name;
                if (_cache.TryGetValue(name, out object rv)) {
                    return (T)rv;
                } else if (File.Exists(name + ".json")) {
                    var json = File.ReadAllText(name + ".json");
                    var obj = JsonConvert.DeserializeObject<T>(json);
                    _cache.TryAdd(name, obj);
                    return obj;
                } else {
                    return new T();
                }
            }
        }
        public static void Store<T>(T state) {
            var name = typeof(T).Name;
            _cache.AddOrUpdate(name, state, (a, b) => state);
        }
        public static event PersistenceEventHandler Persistence;
    }
}
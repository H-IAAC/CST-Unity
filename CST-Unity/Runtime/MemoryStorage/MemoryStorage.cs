using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NRedisStack;
using StackExchange.Redis;
using HIAAC.CstUnity.Core.Entities;
using System.Linq;
using Newtonsoft.Json;


namespace HIAAC.CstUnity.MemoryStorage
{
    /// <summary>
    /// Synchronizes local memories with a Redis database.
    /// </summary>
    public class MemoryStorageCodelet : Codelet, IDisposable
    {
        private readonly Mind _mind;
        private readonly string _mindName;
        private readonly string _nodeName;
        private readonly double _requestTimeout; // Seconds

        private readonly Dictionary<string, WeakReference<Memory>> _memories = new Dictionary<string, WeakReference<Memory>>();

        // Redis
        private readonly ConnectionMultiplexer _redis;
        private readonly IDatabase _db;
        private readonly ISubscriber _pubsub;

        private readonly Dictionary<string, long> _lastUpdate = new();
        private readonly Dictionary<string, LogicalTime> _memoryLogicalTime = new();
        private readonly HashSet<string> _waitingRetrieve = new();

        private readonly Dictionary<string, ManualResetEventSlim> _waitingRequestEvents = new();
        private LamportTime _currentTime = new LamportTime();

        private void LogInfo(string msg) => UnityEngine.Debug.Log($"[INFO] MemoryStorageCodelet: {msg}");//Console.WriteLine
        private void LogWarning(string msg) => UnityEngine.Debug.LogWarning($"[WARN] MemoryStorageCodelet: {msg}");//Console.WriteLine

        public MemoryStorageCodelet(Mind mind, string nodeName = null, string mindName = null, double requestTimeout = 0.5, string redisConnectionString = "localhost")
            : base()
        {
            _mind = mind;
            _requestTimeout = requestTimeout;
            _mindName = mindName ?? "default_mind";

            var options = ConfigurationOptions.Parse(redisConnectionString);
            _redis = ConnectionMultiplexer.Connect(options);
            _db = _redis.GetDatabase();
            _pubsub = _redis.GetSubscriber();

            if (nodeName == null) nodeName = "node";
            string baseName = nodeName;

            string nodesKey = $"{_mindName}:nodes";
            if (_db.SetContains(nodesKey, nodeName))
            {
                long nodeNumber = _db.SetLength(nodesKey);
                nodeName = baseName + nodeNumber;
                while (_db.SetContains(nodesKey, nodeName))
                {
                    nodeNumber++;
                    nodeName = baseName + nodeNumber;
                }
            }

            _nodeName = nodeName;
            _db.SetAdd(nodesKey, _nodeName);

            // Subscriptions
            string transferServiceAddr = $"{_mindName}:nodes:{_nodeName}:transfer_memory";
            _pubsub.Subscribe(transferServiceAddr, (channel, message) => HandlerTransferMemory(message));

            string transferDoneAddr = $"{_mindName}:nodes:{_nodeName}:transfer_done";
            _pubsub.Subscribe(transferDoneAddr, (channel, message) => HandlerNotifyTransfer(message));
        }

        public override void proc()
        {
            var deadKeys = _memories.Where(kvp => !kvp.Value.TryGetTarget(out _)).Select(kvp => kvp.Key).ToList();
            foreach (var key in deadKeys) _memories.Remove(key);

            // Check new memories
            var mindMemories = new Dictionary<string, Memory>();

            foreach (var memory in _mind.getRawMemory().getAllMemoryObjects())
            {
                if (string.IsNullOrEmpty(memory.getName())) continue;
                mindMemories[memory.getName()] = memory;
            }

            var mindMemoriesNames = new HashSet<string>(mindMemories.Keys);
            var currentMemoriesNames = new HashSet<string>(_memories.Keys);


            currentMemoriesNames.IntersectWith(mindMemoriesNames);
            var difference = mindMemoriesNames.Where(n => !currentMemoriesNames.Contains(n));

            foreach (var memoryName in difference)
            {
                var memory = mindMemories[memoryName];
                _memories[memoryName] = new WeakReference<Memory>(memory);
                _memoryLogicalTime[memoryName] = _currentTime;

                string memoryKey = $"{_mindName}:memories:{memoryName}";

                if (_db.KeyExists(memoryKey))
                {
                    // Submit to thread pool (Task.Run)
                    Task.Run(() => RetrieveMemory(memory));
                }
                else
                {
                    // Send impostor with owner
                    var memoryImpostor = new HashEntry[]
                    {
                        new HashEntry("name", memory.getName()),
                        new HashEntry("evaluation", 0.0),
                        new HashEntry("I", ""),
                        new HashEntry("id", 0),
                        new HashEntry("owner", _nodeName),
                        new HashEntry("logical_time", _currentTime.ToString())
                    };

                    _db.HashSet(memoryKey, memoryImpostor);
                    _currentTime = (LamportTime)_currentTime.Increment();
                }

                // Subscribe to updates
                _pubsub.Subscribe($"{_mindName}:memories:{memoryName}:update", (ch, msg) => UpdateMemory(memoryName));
            }

            // Update memories
            var toUpdate = _lastUpdate.Keys.ToList();
            foreach (var memoryName in toUpdate)
            {
                if (!_memories.ContainsKey(memoryName))
                {
                    _lastUpdate.Remove(memoryName);
                    _memoryLogicalTime.Remove(memoryName);
                    continue;
                }

                if (_memories[memoryName].TryGetTarget(out Memory memory))
                {
                    if (memory.getTimestamp() > _lastUpdate[memoryName])
                    {
                        _memoryLogicalTime[memoryName] = _currentTime;
                        UpdateMemory(memoryName);
                    }
                }
            }
        }

        public void UpdateMemory(string memoryName)
        {
            LogInfo($"Updating memory [{memoryName}@{_nodeName}]");

            if (!_memories.ContainsKey(memoryName))
            {
                _pubsub.Unsubscribe($"{_mindName}:memories:{memoryName}:update");
                return;
            }

            string memoryRedisKey = $"{_mindName}:memories:{memoryName}";
            RedisValue messageTimeStr = _db.HashGet(memoryRedisKey, "logical_time");

            if (messageTimeStr.IsNull) return;

            LamportTime messageTime = LamportTime.FromString(messageTimeStr);
            var memoryTime = _memoryLogicalTime[memoryName];

            if (_memories[memoryName].TryGetTarget(out Memory memory))
            {
                if (memoryTime.CompareTo(messageTime) < 0) // memoryTime < messageTime
                {
                    Task.Run(() => RetrieveMemory(memory));
                }
                else if (memoryTime.CompareTo(messageTime) > 0) // memoryTime > messageTime
                {
                    SendMemory(memory);
                }

                _lastUpdate[memoryName] = memory.getTimestamp();
            }
        }

        private void SendMemory(Memory memory)
        {
            string memoryName = memory.getName();
            LogInfo($"Sending memory [{memoryName}@{_nodeName}]");

            var dict = MemoryEncoder.ToDict(memory, jsonifyInfo: true);
            dict["owner"] = "";
            dict["logical_time"] = _memoryLogicalTime[memoryName].ToString();

            HashEntry[] entries = dict.Select(kv => new HashEntry(kv.Key, kv.Value)).ToArray();

            string key = $"{_mindName}:memories:{memoryName}";
            _db.HashSet(key, entries);
            _db.Publish($"{key}:update", "");

            _currentTime = (LamportTime)_currentTime.Increment();
        }

        private void RetrieveMemory(Memory memory)
        {
            string memoryName = memory.getName();
            LogInfo($"Retrieving memory [{memoryName}@{_nodeName}]");

            lock (_waitingRetrieve)
            {
                if (_waitingRetrieve.Contains(memoryName)) return;
                _waitingRetrieve.Add(memoryName);
            }

            try
            {
                string key = $"{_mindName}:memories:{memoryName}";
                var entries = _db.HashGetAll(key);
                var memoryDict = entries.ToStringDictionary();

                if (memoryDict.ContainsKey("owner") && memoryDict["owner"] != "")
                {
                    var evt = new ManualResetEventSlim(false);
                    lock (_waitingRequestEvents)
                    {
                        _waitingRequestEvents[memoryName] = evt;
                    }

                    RequestMemory(memoryName, memoryDict["owner"]);

                    if (!evt.Wait(TimeSpan.FromSeconds(_requestTimeout)))
                    {
                        LogWarning($"Request failed [{memoryName}@{memoryDict["owner"]} to {_nodeName}]");

                        SendMemory(memory);
                        return;
                    }

                    // Reload from Redis after transfer
                    entries = _db.HashGetAll(key);
                    memoryDict = entries.ToStringDictionary();
                }

                MemoryEncoder.LoadMemory(memory, memoryDict);

                if (memoryDict.ContainsKey("logical_time"))
                {
                    var messageTime = LamportTime.FromString(memoryDict["logical_time"]);
                    _currentTime = LamportTime.Synchronize(_currentTime, messageTime);
                    _memoryLogicalTime[memoryName] = messageTime;
                }

                _lastUpdate[memoryName] = memory.getTimestamp();
            }
            finally
            {
                lock (_waitingRetrieve)
                {
                    _waitingRetrieve.Remove(memoryName);
                }
            }
        }

        private void RequestMemory(string memoryName, string ownerName)
        {
            LogInfo($"Requesting memory [{memoryName}@{ownerName} to {_nodeName}]");

            string requestAddr = $"{_mindName}:nodes:{ownerName}:transfer_memory";

            var requestDict = new { memory_name = memoryName, node = _nodeName };
            var fullRequest = new { request = requestDict, logical_time = _currentTime.ToString() };

            string jsonRequest = JsonConvert.SerializeObject(fullRequest);
            _redis.GetSubscriber().Publish(requestAddr, jsonRequest);
        }

        private void HandlerNotifyTransfer(RedisValue message)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(message);
                if (data.ContainsKey("logical_time"))
                {
                    var messageTime = LamportTime.FromString(data["logical_time"]);
                    _currentTime = LamportTime.Synchronize(messageTime, (LamportTime)_currentTime);
                }

                string memoryName = data["memory_name"];

                lock (_waitingRequestEvents)
                {
                    if (_waitingRequestEvents.ContainsKey(memoryName))
                    {
                        _waitingRequestEvents[memoryName].Set();
                        _waitingRequestEvents.Remove(memoryName);
                    }
                }
            }
            catch (Exception ex)
            {
                LogWarning($"Error handling notify transfer: {ex.Message}\n{ex.StackTrace}\n");
            }
        }

        private class TransferRequestPayload
        {
            [JsonProperty("memory_name")]
            public string MemoryName { get; set; }

            [JsonProperty("node")]
            public string Node { get; set; }
        }

        private class TransferMessagePayload
        {
            [JsonProperty("request")]
            public TransferRequestPayload Request { get; set; }

            [JsonProperty("logical_time")]
            public string LogicalTime { get; set; }
        }

        private void HandlerTransferMemory(RedisValue message)
        {
            try
            {
                // Substituição do dynamic por deserialização tipada
                var data = JsonConvert.DeserializeObject<TransferMessagePayload>(message);

                if (data == null) return;

                if (!string.IsNullOrEmpty(data.LogicalTime))
                {
                    LamportTime messageTime = LamportTime.FromString(data.LogicalTime);
                    _currentTime = LamportTime.Synchronize(messageTime, (LamportTime)_currentTime);
                }

                if (data.Request == null) return;

                string memoryName = data.Request.MemoryName;
                string requestingNode = data.Request.Node;

                LogInfo($"Transfering memory to server [{memoryName}@{_nodeName}]");

                Memory memory;
                if (_memories.ContainsKey(memoryName) && _memories[memoryName].TryGetTarget(out Memory existingMem))
                {
                    memory = existingMem;
                }
                else
                {
                    memory = new MemoryObject();
                    memory.setName(memoryName);
                }

                _memoryLogicalTime[memoryName] = _currentTime;

                SendMemory(memory);

                var response = new { memory_name = memoryName, logical_time = _currentTime.ToString() };
                string responseStr = JsonConvert.SerializeObject(response);

                string responseAddr = $"{_mindName}:nodes:{requestingNode}:transfer_done";
                _db.Publish(responseAddr, responseStr);
            }
            catch (Exception ex)
            {
                LogWarning($"Error handling transfer memory: {ex.Message}\n{ex.StackTrace}\n");
            }
        }

        public void Stop()
        {
            _redis.Close();
        }

        public void Dispose()
        {
            _redis?.Dispose();
        }

        public override void calculateActivation() { }
        public override void accessMemoryObjects() { }
    }
}
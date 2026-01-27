using System.Collections.Generic;
using HIAAC.CstUnity.Core.Entities;
using System.Globalization;
using Newtonsoft.Json;

namespace HIAAC.CstUnity.MemoryStorage
{
    /// <summary>
    /// Encodes and decodes Memories.
    /// </summary>
    public static class MemoryEncoder
    {
        /// <summary>
        /// Encodes a memory to a dictionary suitable for Redis hash storage.
        /// </summary>
        /// <param name="memory">Memory to encode.</param>
        /// <param name="jsonifyInfo">If True, dumps the info to JSON string.</param>
        /// <returns>Dictionary with string keys and values.</returns>
        public static Dictionary<string, string> ToDict(Memory memory, bool jsonifyInfo = false)
        {
            var data = new Dictionary<string, string>
            {
                { "timestamp", memory.getTimestamp().ToString() },
                { "evaluation", memory.getEvaluation().ToString(CultureInfo.InvariantCulture) },
                { "name", memory.getName() },
                { "id", memory.getId().ToString() }
            };

            object info = memory.getI();

            if (jsonifyInfo)
            {
                data["I"] = JsonConvert.SerializeObject(info);
            }
            else
            {
                // Fallback se info for string, senão serializa de qualquer forma para representação textual
                data["I"] = info is string s ? s : JsonConvert.SerializeObject(info);
            }

            return data;
        }

        /// <summary>
        /// Load a memory from a dict.
        /// </summary>
        /// <param name="memory">Memory to store the loaded info.</param>
        /// <param name="memoryDict">Dict encoded memory (from Redis).</param>
        public static void LoadMemory(Memory memory, Dictionary<string, string> memoryDict)
        {
            if (memoryDict.ContainsKey("evaluation"))
            {
                memory.setEvaluation(float.Parse(memoryDict["evaluation"], CultureInfo.InvariantCulture));
            }

            if (memoryDict.ContainsKey("id"))
            {
                memory.setId(int.Parse(memoryDict["id"]));
            }

            if (memoryDict.ContainsKey("I"))
            {
                string infoJson = memoryDict["I"];

                object info = JsonConvert.DeserializeObject<object>(infoJson);
                memory.setI(info);
            }
        }
    }
}
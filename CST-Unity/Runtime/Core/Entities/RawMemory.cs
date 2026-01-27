using System;
using System.Collections.Generic;

namespace HIAAC.CstUnity.Core.Entities
{
    public class RawMemory
    {
        private List<Memory> allMemories;


        private static long lastid = 0;


        /// <summary>
        /// Creates a Raw Memory.
        /// </summary>
        public RawMemory()
        {
            allMemories = new List<Memory>();
        }


        private readonly object getAllMemoryObjectsLock = new();


        /// <summary>
        /// Gets all memories inside the raw memory.
        /// </summary>
        /// <returns>the allMemoryObjects</returns>
        public List<Memory> getAllMemoryObjects()
        {
            lock (getAllMemoryObjectsLock)
            {
                lock (allMemories)
                {
                    return allMemories;
                }
            }
        }


        private readonly object getAllOfTypeLock = new();


        /// <summary>
        /// Returns a list of all memories in raw memory of a given type
        /// </summary>
        /// <param name="type">of memory</param>
        /// <returns>list of Ms of a given type</returns>
        public List<Memory> getAllOfType(string type)
        {
            lock (getAllOfTypeLock)
            {
                List<Memory> listOfType = new List<Memory>();


                lock (allMemories)
                {
                    foreach (Memory mo in allMemories)
                    {
                        if (mo.getName() != null)
                        {
                            if (mo.getName().Equals(type, StringComparison.OrdinalIgnoreCase))
                            {
                                listOfType.Add(mo);
                            }
                        }
                    }
                }


                return listOfType;
            }
        }

        private readonly object setAllMemoriesLock = new();


        /// <summary>
        /// Sets the list of all memories inside raw memory.
        /// </summary>
        /// <param name="allMemories">the allMemoryObjects to set.</param>
        public void setAllMemories(List<Memory> allMemories)
        {
            lock (setAllMemoriesLock)
            {
                lock (this.allMemories)
                {
                    this.allMemories = allMemories;
                    foreach (Memory m in allMemories)
                    {

                        m.setId(lastid);
                        lastid++;
                    }
                }
            }
        }


        private readonly object printContentLock = new();


        /// <summary>
        /// Print Raw Memory contents.
        /// </summary>
        public void printContent()
        {
            lock (printContentLock)
            {
                lock (allMemories)
                {
                    foreach (Memory mo in allMemories)
                    {
                        Console.WriteLine(mo.ToString());
                    }
                }
            }
        }

        private readonly object addMemoryLock = new();


        /// <summary>
        /// Adds a new Memory to the Raw Memory.
        /// </summary>
        /// <param name="mo">memory to be added.</param>
        public void addMemory(Memory mo)
        {
            lock (addMemoryLock)
            {
                lock (allMemories)
                {
                    allMemories.Add(mo);
                    mo.setId(lastid);
                    lastid++;
                }
            }
        }


        private readonly object createMemoryContainerLock = new();


        /// <summary>
        /// Creates a memory container of the type passed.
        /// </summary>
        /// <param name="name">the type of the memory container passed.</param>
        /// <returns>the memory container created.</returns>
        //public MemoryContainer createMemoryContainer(string name)
        //{
        //    lock (createMemoryContainerLock)
        //    {
        //        MemoryContainer mc = new MemoryContainer(name);
        //        this.addMemory(mc);
        //
        //
        //        return mc;
        //    }
        //}

        private readonly object createMemoryObject_2Lock = new();


        /// <summary>
        /// Creates a new MemoryObject and adds it to the Raw Memory, using provided
        /// info and type.
        /// </summary>
        /// <param name="name">memory object type.</param>
        /// <param name="info">memory object info.</param>
        /// <returns>mo created MemoryObject.</returns>
        public MemoryObject createMemoryObject(string name, object info)
        {
            lock (createMemoryObject_2Lock)
            {
                MemoryObject mo = new MemoryObject();
                mo.setI(info);
                mo.setTimestamp(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                mo.setEvaluation(0.0d);
                mo.setName(name);


                this.addMemory(mo);
                return mo;
            }
        }

        private readonly object createMemoryObject_1Lock = new();


        /// <summary>
        /// Creates a memory object of the type passed.
        /// </summary>
        /// <param name="name">the type of the memory object created.</param>
        /// <returns>the memory object created.</returns>
        public MemoryObject createMemoryObject(String name)
        {
            lock (createMemoryObject_1Lock)
            {
                return createMemoryObject(name, "");
            }
        }



        private readonly object destroyMemoryLock = new();


        /// <summary>
        /// Destroys a given memory from raw memory
        /// </summary>
        /// <param name="memory">the memory to destroy.</param>
        public void destroyMemory(Memory memory)
        {
            lock (destroyMemoryLock)
            {
                lock (allMemories)
                {
                    allMemories.Remove(memory);
                }
            }
        }


        private readonly object sizeLock = new();


        /// <summary>
        /// Gets the size of the raw memory.
        /// </summary>
        /// <returns>size of Raw Memory.</returns>
        public int size()
        {
            lock (sizeLock)
            {
                lock (allMemories)
                {
                    return allMemories.Count;
                }
            }
        }

        /// <summary>
        /// Removes all memory objects from RawMemory.
        /// </summary>
        public void shutDown()
        {
            lock (allMemories)
            {
                allMemories = new List<Memory>();
            }
        }
    }
}
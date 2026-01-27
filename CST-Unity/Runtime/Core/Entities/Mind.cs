using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace HIAAC.CstUnity.Core.Entities
{
    /// <summary>
    /// This class represents the Mind of the agent, wrapping all the CST's core
    /// entities.
    /// </summary>
    public class Mind
    {
        protected CodeRack codeRack;
        protected RawMemory rawMemory;
        protected ConcurrentDictionary<string, List<Codelet>> codeletGroups;
        protected ConcurrentDictionary<string, List<Memory>> memoryGroups;

        /// <summary>
        /// Creates the Mind.
        /// </summary>
        public Mind()
        {
            codeRack = new();
            rawMemory = new();
            codeletGroups = new();
            memoryGroups = new();
        }

        private readonly object getCodeRackLock = new();

        /// <summary>
        /// Gets the CodeRack.
        /// </summary>
        /// <returns>the codeRack.</returns>
        public CodeRack getCodeRack()
        {
            lock (getCodeRackLock)
            {
                return codeRack;
            }
        }

        private readonly object getRawMemoryLock = new();

        /// <summary>
        /// Gets the RawMemory.
        /// </summary>
        /// <returns>the rawMemory.</returns>
        public RawMemory getRawMemory()
        {
            lock (getRawMemoryLock)
            {
                return rawMemory;
            }
        }

        private readonly object createCodeletGroupLock = new();

        /// <summary>
        /// Creates a Codelet Group
        /// </summary>
        /// <param name="groupName">The Group name</param>
        public void createCodeletGroup(string groupName)
        {
            lock (createCodeletGroupLock)
            {
                List<Codelet> group = new();
                codeletGroups[groupName] = group;
            }
        }

        private readonly object createMemoryGroupLock = new();

        /// <summary>
        /// Creates a Memory Group
        /// </summary>
        /// <param name="groupName">The Group name</param>
        public void createMemoryGroup(string groupName)
        {
            lock (createMemoryGroupLock)
            {
                List<Memory> group = new();
                memoryGroups[groupName] = group;
            }
        }

        /// <summary>
        /// Returns the full HashMap which for every codelet group Name it is associated a list of codeletGroups
        /// </summary>
        /// <returns>the HashMap with all pairs (groupname,list of codeletGroups belonging to groupname)</returns>
        public ConcurrentDictionary<string, List<Codelet>> getCodeletGroups()
        {
            return codeletGroups;
        }

        /// <summary>
        /// Returns the full HashMap which for every memory group Name it is associated a list of codeletGroups
        /// </summary>
        /// <returns>the HashMap with all pairs (groupname,list of codeletGroups belonging to groupname)</returns>
        public ConcurrentDictionary<string, List<Memory>> getMemoryGroups()
        {
            return memoryGroups;
        }

        /// <summary>
        /// Returns the number of registered codelet groups
        /// </summary>
        /// <returns>the number of registered groups</returns>
        public int getCodeletGroupsNumber()
        {
            return codeletGroups.Count;
        }

        /// <summary>
        /// Returns the number of registered memory groups
        /// </summary>
        /// <returns>the number of registered groups</returns>
        public int getMemoryGroupsNumber()
        {
            return memoryGroups.Count;
        }

        private readonly object createMemoryContainerLock = new();

        /// <summary>
        /// Creates a Memory Container inside the Mind of a given type.
        /// </summary>
        /// <param name="name">the type of the Memory Container to be created inside the Mind.</param>
        /// <returns>the Memory Container created.</returns>
        //public MemoryContainer createMemoryContainer(string name)
        //{
        //    lock (createMemoryContainerLock)
        //    {
        //        MemoryContainer mc = null;
        //
        //        if (rawMemory != null)
        //            mc = rawMemory.createMemoryContainer(name);
        //
        //        return mc;
        //    }
        //}


        private readonly object createMemoryObject_2Lock = new();

        /// <summary>
        /// Creates a new MemoryObject and adds it to the Raw Memory, using provided
        /// info and type.
        /// </summary>
        /// <param name="name">memory object name.</param>
        /// <param name="info">memory object info.</param>
        /// <returns>mo created MemoryObject.</returns>
        public MemoryObject createMemoryObject(string name, Object info)
        {
            lock (createMemoryObject_2Lock)
            {
                MemoryObject mo = null;

                if (rawMemory != null)
                    mo = rawMemory.createMemoryObject(name, info);

                return mo;
            }
        }

        private readonly object createMemoryObject_1Lock = new();

        /// <summary>
        /// Creates a new MemoryObject and adds it to the Raw Memory, using provided
        /// type.
        /// </summary>
        /// <param name="name">memory object type.</param>
        /// <returns>created MemoryObject.</returns>
        public MemoryObject createMemoryObject(string name)
        {
            lock (createMemoryObject_1Lock)
            {
                return createMemoryObject(name, null);
            }
        }

        /// <summary>
        /// Inserts the Codelet passed in the Mind's CodeRack.
        /// </summary>
        /// <param name="co">the Codelet passed</param>
        /// <returns>the Codelet.</returns>
        public Codelet insertCodelet(Codelet co)
        {
            if (codeRack != null)
                codeRack.addCodelet(co);
            return co;
        }

        /// <summary>
        /// Inserts the Codelet passed in the Mind's CodeRack.
        /// </summary>
        /// <param name="co">the Codelet to be inserted in the Mind</param>
        /// <param name="groupName">the Codelet group name</param>
        /// <returns>the Codelet.</returns>
        public Codelet insertCodelet(Codelet co, string groupName)
        {
            insertCodelet(co);
            registerCodelet(co, groupName);
            return co;
        }

        /// <summary>
        /// Register a Codelet within a group
        /// </summary>
        /// <param name="co">the Codelet</param>
        /// <param name="groupName">the group name</param>
        public void registerCodelet(Codelet co, string groupName)
        {
            if (codeletGroups.TryGetValue(groupName, out List<Codelet> groupList))
                groupList.Add(co);
            else
                Debug.WriteLine($"The Codelet Group {groupName} still does not have been created ... create it first with createCodeletGroup");
        }

        /// <summary>
        /// Register a Memory within a group
        /// </summary>
        /// <param name="m">the Memory</param>
        /// <param name="groupName">the group name</param>
        public void registerMemory(Memory m, string groupName)
        {
            if (memoryGroups.TryGetValue(groupName, out List<Memory> groupList))
                groupList.Add(m);
            else
                Debug.WriteLine($"The Memory Group {groupName} still does not have been created ... create it first with createMemoryGroup");
        }

        /// <summary>
        /// Register a Memory within a group by name.
        /// </summary>
        /// <param name="m">the Memory</param>
        /// <param name="groupName">the group name</param>
        public void registerMemory(string m, string groupName)
        {
            if (memoryGroups.TryGetValue(groupName, out List<Memory> groupList))
            {
                RawMemory rm = getRawMemory();
                if (rm != null)
                {
                    List<Memory> all = rm.getAllOfType(m);
                    foreach (Memory mem in all)
                    {
                        groupList.Add(mem);
                    }
                }
            }
        }

        /// <summary>
        /// Get a list of all Codelets belonging to a group
        /// </summary>
        /// <param name="groupName">the group name to which the Codelets belong</param>
        /// <returns>A list of all codeletGroups belonging to the group indicated by groupName</returns>
        public List<Codelet> getCodeletGroupList(string groupName)
        {
            codeletGroups.TryGetValue(groupName, out List<Codelet> list);
            return list;
        }

        /// <summary>
        /// Get a list of all Memories belonging to a group
        /// </summary>
        /// <param name="groupName">the group name to which the Memory belong</param>
        /// <returns>A list of all memoryGroups belonging to the group indicated by groupName</returns>
        public List<Memory> getMemoryGroupList(string groupName)
        {
            memoryGroups.TryGetValue(groupName, out List<Memory> list);
            return list;
        }

        /// <summary>
        /// Starts all codeletGroups in coderack.
        /// </summary>
        public void start()
        {
            if (codeRack != null)
                codeRack.start();
        }

        /// <summary>
        /// Stops codeletGroups thread.
        /// </summary>
        public void shutDown()
        {
            if (codeRack != null)
                codeRack.shutDown();
        }
    }
}
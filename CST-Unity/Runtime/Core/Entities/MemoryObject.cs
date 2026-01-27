using System.Collections.Generic;
using System.Collections.Concurrent;
using System;

namespace HIAAC.CstUnity.Core.Entities
{
    public class MemoryObject : Memory
    {
        private const long serialVersionUID = 1L;

        private long id;


        private volatile int timestamp;

        private volatile float evaluation;

        private volatile object I;

        private string name;

        [NonSerialized]
        private volatile IDictionary<MemoryObserver, byte> memoryObservers;

        public MemoryObject()
        {
            evaluation = 0.0f;
        }

        private readonly object getIdLock = new();
        public long getId()
        {
            lock (getIdLock)
            {
                return id;
            }
        }

        private readonly object setIdLock = new();
        public void setId(long id)
        {
            lock (setIdLock)
            {
                this.id = id;
            }
        }


        private readonly object getILock = new();
        public object getI()
        {
            lock (getILock)
            {
                return I;
            }

        }

        private readonly object setILock = new();
        public int setI(object info)
        {
            lock (setILock)
            {
                I = info;
                setTimestamp(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                notifyMemoryObservers();

                return -1;
            }

        }

        private readonly object notifyMemoryObserversLock = new();
        private void notifyMemoryObservers()
        {
            lock (notifyMemoryObserversLock)
            {
                if (memoryObservers != null && !(memoryObservers.Count == 0))
                {
                    foreach (MemoryObserver memoryObserver in memoryObservers.Keys)
                    {
                        memoryObserver.notifyCodelet();
                    }
                }
            }

        }

        private readonly object getTimestampLock = new();
        public long getTimestamp()
        {
            lock (getTimestampLock)
            {
                return timestamp;
            }

        }

        private readonly object setTimestampLock = new();
        public void setTimestamp(long timestamp)
        {
            lock (setTimestampLock)
            {
                this.timestamp = (int)timestamp;
            }
        }


        private readonly object getNameLock = new();
        public string getName()
        {
            lock (getNameLock)
            {
                return name;
            }
        }


        private readonly object setNameLock = new();
        public void setName(string name)
        {
            lock (setNameLock)
            {
                this.name = name;
            }
        }


        private readonly object getEvaluationLock = new();
        public double getEvaluation()
        {
            lock (getEvaluationLock)
            {
                return evaluation;
            }
        }

        private readonly object setEvaluationLock = new();
        public void setEvaluation(double evaluation)
        {
            lock (setEvaluationLock)
            {
                this.evaluation = (float)evaluation;
            }
        }

        public override string ToString()
        {
            return "Memoryobject [idmemoryobject=" + id + ", timestamp=" + timestamp + ", evaluation="
                    + evaluation + ", I=" + I + ", name=" + name + "]";
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = 1;
            result = prime * result + ((I == null) ? 0 : I.GetHashCode());
            result = prime * result + evaluation.GetHashCode();
            result = prime * result + id.GetHashCode();
            result = prime * result + name.GetHashCode();
            result = prime * result + timestamp.GetHashCode();
            return result;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (obj == null)
                return false;
            if (GetType() != obj.GetType())
                return false;

            MemoryObject other = (MemoryObject)obj;
            if (I == null)
            {
                if (other.I != null)
                    return false;
            }
            else if (!I.Equals(other.I))
                return false;

            if (!evaluation.Equals(other.evaluation))
                return false;

            if (!id.Equals(other.id))
                return false;
            if (name == null)
            {
                if (other.name != null)
                    return false;
            }
            else if (!name.Equals(other.name))
                return false;

            if (!timestamp.Equals(other.timestamp))
                return false;
            return true;
        }

        private readonly object addMemoryObserverLock = new();
        public void addMemoryObserver(MemoryObserver memoryObserver)
        {
            lock (addMemoryObserverLock)
            {
                if (memoryObservers == null)
                {
                    memoryObservers = new ConcurrentDictionary<MemoryObserver, byte>();
                }
                memoryObservers.Add(memoryObserver, 0);
            }

        }

        private readonly object removeMemoryObserverLock = new();
        public void removeMemoryObserver(MemoryObserver memoryObserver)
        {
            lock (removeMemoryObserverLock)
            {
                if (memoryObservers != null)
                {
                    memoryObservers.Remove(memoryObserver);
                }
            }
        }
    }
}
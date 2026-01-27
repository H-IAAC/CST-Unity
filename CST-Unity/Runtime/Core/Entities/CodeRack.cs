using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace HIAAC.CstUnity.Core.Entities
{
    public class CodeRack
    {
        private List<Codelet> allCodelets;

        public CodeRack()
        {
            allCodelets = new List<Codelet>();
        }

        private readonly object getAllCodeletsLock = new();
        public List<Codelet> getAllCodelets()
        {
            lock (getAllCodeletsLock)
            {
                return allCodelets;
            }
        }

        public void setAllCodelets(List<Codelet> allCodelets)
        {
            this.allCodelets = allCodelets;
        }

        public void addCodelet(Codelet co)
        {
            allCodelets.Add(co);
        }


        public Codelet insertCodelet(Codelet co)
        {

            addCodelet(co);

            return co;
        }


        public Codelet createCodelet(double activation, List<Memory> broadcast, List<Memory> inputs, List<Memory> outputs,
                Codelet co)
        {
            try
            {
                co.setActivation(activation);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }

            co.setBroadcast(broadcast);
            co.setInputs(inputs);
            co.setOutputs(outputs);
            addCodelet(co);
            return co;
        }


        public void destroyCodelet(Codelet co)
        {
            co.stop();
            allCodelets.Remove(co);

        }


        public void shutDown()
        {
            foreach (Codelet co in getAllCodelets())
            {
                co.stop();
            }

            allCodelets.Clear();
        }


        public void start()
        {
            foreach (Codelet co in getAllCodelets())
            {
                lock (co)
                {
                    co.start();
                }
            }
        }


        public void stop()
        {
            foreach (Codelet co in getAllCodelets())
            {
                co.stop();
            }
        }
    }
}
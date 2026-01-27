using System.Collections.Generic;
using System.Threading;
using System;
using System.Diagnostics;

namespace HIAAC.CstUnity.Core.Entities
{
    public abstract class Codelet : MemoryObserver
    {
        protected volatile float activation = 0.0f;


        protected volatile float threshold = 0.0f;

        protected volatile List<Memory> inputs = new List<Memory>();


        protected volatile List<Memory> outputs = new List<Memory>();

        protected volatile List<Memory> broadcast = new List<Memory>();

        protected volatile bool loop = true; //

        protected volatile bool isMemoryObserver = false;


        protected long timeStep = 300;


        private volatile bool enabled = true;

        private int enable_count = 0;

        protected string name = Thread.CurrentThread.Name;

        //volatile float laststarttime = 0f;

        public volatile object codeletLock = new();

        public abstract void accessMemoryObjects();

        public abstract void calculateActivation();

        public abstract void proc();

        private volatile System.Timers.Timer timer = new();

        private bool isProfiling = false;

        private readonly object runLock = new();
        public void run()
        {
            lock (runLock)
            {
                try
                {
                    timer.Stop();
                    timer.Dispose();
                    timer = new();


                    timer.Elapsed += (sender, e) => task();
                    timer.AutoReset = false;

                    task();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.ToString());
                }
            }
        }

        private readonly object taskLock = new();
        private void task()
        {
            lock (taskLock)
            {
                long startTime = 0L;
                //long endTime = 0l;
                //long duration = 0l;

                try
                {

                    if (isProfiling)
                        startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                    if (!isMemoryObserver)
                        accessMemoryObjects();// tries to connect to memory objects

                    if (enable_count == 0)
                    {
                        if (isMemoryObserver == false)
                        {
                            calculateActivation();
                            if (activation >= threshold)
                                proc();
                        }
                    }
                    else
                    {
                        raiseException();
                    }

                    enable_count = 0;

                }
                catch (Exception ex)
                {
                    Debug.WriteLine(GetType().Name + ex.Message);
                }
                finally
                {

                    if (!isMemoryObserver && shouldLoop())
                    {
                        timer.Interval = timeStep;
                        timer.Start();
                    }
                    //if (Codelet.this.codeletProfiler != null)
                    //{
                    //    Codelet.this.codeletProfiler.profile(Codelet.this);
                    //}
                    //if (isProfiling)
                    //{
                    //    endTime = System.currentTimeMillis();
                    //    duration = (endTime - startTime);
                    //    ProfileInfo pi = new ProfileInfo(duration, startTime, laststarttime);
                    //    profileInfo.add(pi);
                    //    laststarttime = startTime;
                    //
                    //    if (profileInfo.size() >= 50)
                    //    {
                    //
                    //        ExecutionTimeWriter executionTimeWriter = new ExecutionTimeWriter();
                    //        executionTimeWriter.setCodeletName(name);
                    //        executionTimeWriter.setPath("profile/");
                    //        executionTimeWriter.setProfileInfo(profileInfo);
                    //
                    //        Thread thread = new Thread(executionTimeWriter);
                    //        thread.start();
                    //
                    //        profileInfo = new ArrayList<>();
                    //    }
                    //}
                }
            }
        }


        private readonly object startLock = new();
        public void start()
        {
            lock (startLock)
            {
                if (isMemoryObserver == false)
                {
                    Thread t = new Thread(new ThreadStart(run));
                    t.Start();
                }
            }
        }

        private readonly object stopLock = new();
        public void stop()
        {
            lock (stopLock)
            {
                setLoop(false);
            }
        }

        private readonly object impendingAccessLock = new();
        public bool impendingAccess(Codelet accesing)
        {
            lock (impendingAccessLock)
            {
                bool myLock = false;
                bool yourLock = false;
                try
                {
                    myLock = Monitor.TryEnter(codeletLock);
                    yourLock = Monitor.TryEnter(accesing.codeletLock);
                }
                finally
                {
                    if (!(myLock && yourLock))
                    {
                        if (myLock)
                        {
                            Monitor.Exit(codeletLock);
                        }
                        if (yourLock)
                        {
                            Monitor.Exit(accesing.codeletLock);
                        }
                    }
                }
                return myLock && yourLock;
            }
        }


        private readonly object shouldLoopLock = new();
        public bool shouldLoop()
        {
            lock (shouldLoopLock)
            {
                return loop;
            }
        }

        private readonly object setLoopLock = new();
        public void setLoop(bool loop)
        {
            lock (setLoopLock)
            {
                this.loop = loop;
            }
        }

        private readonly object getEnabledLock = new();
        public bool GetEnabled()
        {
            lock (getEnabledLock)
            {
                return enabled;
            }
        }

        private readonly object setEnabledLock = new();
        public void SetEnabled(bool status)
        {
            lock (setEnabledLock)
            {
                enabled = status;
                if (status == true)
                {
                    enable_count = 0;
                }
            }
        }

        private readonly object getNameLock = new();
        public string GetName()
        {
            lock (getNameLock)
            {
                return name;
            }
        }

        private readonly object setNameLock = new();
        public void SetName(string name)
        {
            lock (setNameLock)
            {
                this.name = name;
            }
        }

        private readonly object isLoopLock = new();
        public bool IsLoop()
        {
            lock (isLoopLock)
            {
                return loop;
            }
        }

        private readonly object getActivationLock = new();
        public double GetActivation()
        {
            lock (getActivationLock)
            {
                return activation;
            }
        }

        private readonly object setActivationLock = new();
        public void setActivation(double activation)
        {
            lock (setActivationLock)
            {
                if (activation > 1.0d)
                {
                    this.activation = 1.0f;
                    throw new Exception("Codelet activation set to value > 1.0");
                }
                else if (activation < 0.0d)
                {
                    this.activation = 0.0f;
                    throw new Exception("Codelet activation set to value < 0.0");
                }
                else
                {
                    this.activation = (float)activation;
                }
            }
        }

        private readonly object getInputsLock = new();
        public List<Memory> GetInputs()
        {
            lock (getInputsLock)
            {
                return inputs;
            }
        }

        private readonly object setInputsLock = new();
        public void setInputs(List<Memory> inputs)
        {
            lock (setInputsLock)
            {
                this.inputs = inputs;
            }
        }

        private readonly object addInputLock = new();
        public void addInput(Memory input)
        {
            lock (addInputLock)
            {
                if (isMemoryObserver)
                {
                    input.addMemoryObserver(this);
                }
                inputs.Add(input);
            }
        }

        private readonly object addInputsLock = new();
        public void addInputs(List<Memory> inputs)
        {
            lock (addInputsLock)
            {
                if (isMemoryObserver)
                {
                    foreach (Memory memory in inputs)
                    {
                        memory.addMemoryObserver(this);
                    }
                }
                this.inputs.AddRange(inputs);
            }
        }

        private readonly object addOutputLock = new();
        public void addOutput(Memory output)
        {
            lock (addOutputLock)
            {
                outputs.Add(output);
            }
        }

        private readonly object removesOutputLock = new();
        public void removesOutput(Memory output)
        {
            lock (removesOutputLock)
            {
                outputs.Remove(output);
            }
        }

        private readonly object removesInputLock = new();
        public void removesInput(Memory input)
        {
            lock (removesInputLock)
            {
                inputs.Remove(input);
            }
        }

        private readonly object removeFromOutputLock = new();
        public void removeFromOutput(List<Memory> outputs)
        {
            lock (removeFromOutputLock)
            {
                this.outputs.RemoveAll(m => outputs.Contains(m));
            }
        }

        private readonly object removeFromInputLock = new();
        public void RemoveAllemoveFromInput(List<Memory> inputs)
        {
            lock (removeFromInputLock)
            {
                this.inputs.RemoveAll(m => inputs.Contains(m));
            }
        }

        private readonly object addOutputsLock = new();
        public void addOutputs(List<Memory> outputs)
        {
            lock (addOutputsLock)
            {
                this.outputs.AddRange(outputs);
            }
        }

        private readonly object getOutputsLock = new();
        public List<Memory> getOutputs()
        {
            lock (getOutputsLock)
            {
                return outputs;
            }
        }

        private readonly object getOutputsOfTypeLock = new();
        public List<Memory> getOutputsOfType(string type)
        {
            lock (getOutputsOfTypeLock)
            {
                List<Memory> outputsOfType = new();

                if (outputs != null && outputs.Count > 0)
                    foreach (Memory mo in outputs)
                    {
                        if (mo.getName() != null && mo.getName().Equals(type, StringComparison.OrdinalIgnoreCase))
                        {
                            outputsOfType.Add(mo);
                        }
                    }

                return outputsOfType;
            }
        }

        private readonly object getInputsOfTypeLock = new();
        public List<Memory> getInputsOfType(string type)
        {
            lock (getInputsOfTypeLock)
            {
                List<Memory> inputsOfType = new();

                if (inputs != null && inputs.Count > 0)
                    foreach (Memory mo in inputs)
                    {
                        if (mo.getName() != null && mo.getName().Equals(type, StringComparison.OrdinalIgnoreCase))
                        {
                            inputsOfType.Add(mo);
                        }
                    }

                return inputsOfType;
            }
        }

        private readonly object setOutputsLock = new();
        public void setOutputs(List<Memory> outputs)
        {
            lock (setOutputsLock)
            {
                this.outputs = outputs;
            }
        }

        private readonly object getBroadcastLock = new();
        public List<Memory> getBroadcast()
        {
            lock (getBroadcastLock)
            {
                return broadcast;
            }
        }

        private readonly object setBroadcastLock = new();
        public void setBroadcast(List<Memory> broadcast)
        {
            lock (setBroadcastLock)
            {
                this.broadcast = broadcast;
            }
        }

        private readonly object getBroadcastLock2 = new();
        public Memory getBroadcast(string name)
        {
            lock (getBroadcastLock2)
            {
                if (broadcast != null && broadcast.Count > 0)
                    foreach (Memory mo in broadcast)
                    {
                        if (mo.getName() != null && mo.getName().Equals(name, StringComparison.OrdinalIgnoreCase))
                            return mo;
                    }
                return null;
            }
        }

        private readonly object addBroadcastLock = new();
        public void addBroadcast(Memory b)
        {
            lock (addBroadcastLock)
            {
                if (isMemoryObserver)
                {
                    b.addMemoryObserver(this);
                }
                broadcast.Add(b);
            }
        }

        private readonly object addBroadcastsLock = new();
        public void addBroadcasts(List<Memory> broadcast)
        {
            lock (addBroadcastsLock)
            {
                if (isMemoryObserver)
                {
                    foreach (Memory memory in broadcast)
                    {
                        memory.addMemoryObserver(this);
                    }
                }
                this.broadcast.AddRange(broadcast);
            }
        }

        private readonly object getThreadNameLock = new();
        public string GetThreadName()
        {
            lock (getThreadNameLock)
            {
                return Thread.CurrentThread.Name;
            }
        }

        private readonly object toStringLock = new();
        public override string ToString()
        {
            lock (toStringLock)
            {
                const int maxLen = 10;
                return "Codelet [activation=" + activation + ", " + "name=" + name + ", "
                        + (broadcast != null ? "broadcast=" + broadcast.GetRange(0, Math.Min(broadcast.Count, maxLen)) + ", "
                                : "")
                        + (inputs != null ? "inputs=" + inputs.GetRange(0, Math.Min(inputs.Count, maxLen)) + ", " : "")
                        + (outputs != null ? "outputs=" + outputs.GetRange(0, Math.Min(outputs.Count, maxLen)) : "") + "]";
            }
        }


        private readonly object getInputLock = new();
        public Memory getInput(string type, int index)
        {
            lock (getInputLock)
            {
                Memory inputMO = null;
                List<Memory> listMO = new List<Memory>();

                if (inputs != null && inputs.Count > 0)
                    foreach (Memory mo in inputs)
                    {
                        if (mo.getName() != null && mo.getName().Equals(type, StringComparison.OrdinalIgnoreCase))
                        {
                            listMO.Add(mo);
                        }
                    }

                if (listMO.Count >= index + 1)
                {
                    inputMO = listMO[index];
                    enabled = true;
                }
                else
                {
                    enabled = false; // It must not run proc yet, for it still
                                     // needs to find this mo it wants
                    enable_count++;
                }

                return inputMO;
            }
        }

        private readonly object getInputLock2 = new();
        public Memory getInput(string name)
        {
            lock (getInputLock2)
            {
                if (inputs != null && inputs.Count > 0)
                    foreach (Memory mo in inputs)
                    {
                        if (mo.getName() != null && mo.getName().Equals(name, StringComparison.OrdinalIgnoreCase))
                            return mo;
                    }

                return null;
            }
        }

        private readonly object getOutputLock = new();
        public Memory getOutput(string type, int index)
        {
            lock (getOutputLock)
            {
                Memory outputMO = null;
                List<Memory> listMO = new List<Memory>();

                if (outputs != null && outputs.Count > 0)
                    foreach (Memory mo in outputs)
                    {
                        if (mo != null && type != null && mo.getName() != null && mo.getName().Equals(type, StringComparison.OrdinalIgnoreCase))
                        {
                            listMO.Add(mo);
                        }
                    }

                if (listMO.Count >= index + 1)
                {
                    outputMO = listMO[index];
                    enabled = true;
                }
                else
                {
                    enabled = false; // It must not run proc yet, for it still
                                     // needs to find this mo it wants
                    enable_count++;
                }

                return outputMO;
            }
        }

        private readonly object getOutputLock2 = new();
        public Memory getOutput(string name)
        {
            lock (getOutputLock2)
            {
                if (outputs != null && outputs.Count > 0)
                    foreach (Memory mo in outputs)
                    {
                        if (mo.getName() != null && mo.getName().Equals(name, StringComparison.OrdinalIgnoreCase))
                            return mo;
                    }

                return null;
            }
        }

        private readonly object getBroadcastLock3 = new();
        public Memory getBroadcast(string type, int index)
        {
            lock (getBroadcastLock3)
            {
                Memory broadcastMO = null;

                List<Memory> listMO = new List<Memory>();

                if (broadcast != null && broadcast.Count > 0)
                {
                    foreach (Memory mo in broadcast)
                    {
                        if (mo.getName() != null && mo.getName().Equals(type, StringComparison.OrdinalIgnoreCase))
                        {
                            listMO.Add(mo);
                        }
                    }
                }

                if (listMO.Count >= index + 1)
                {
                    broadcastMO = listMO[index];
                }

                return broadcastMO;
            }
        }

        private readonly object getThresholdLock = new();
        public double getThreshold()
        {
            lock (getThresholdLock)
            {
                return threshold;
            }
        }

        private readonly object setThresholdLock = new();
        public void setThreshold(double threshold)
        {
            lock (setThresholdLock)
            {
                if (threshold > 1.0d)
                {
                    this.threshold = 1.0f;
                    throw new Exception("Codelet threshold set to value > 1.0");
                }
                else if (threshold < 0.0d)
                {

                    this.threshold = 0.0f;
                    throw new Exception("Codelet threshold set to value < 0.0");
                }
                else
                {
                    this.threshold = (float)threshold;
                }
            }
        }

        private readonly object getTimeStepLock = new();
        public long getTimeStep()
        {
            lock (getTimeStepLock)
            {
                return timeStep;
            }
        }


        private readonly object setTimeStepLock = new();
        public void setTimeStep(long timeStep)
        {
            lock (setTimeStepLock)
            {
                this.timeStep = timeStep;
            }
        }


        private readonly object isProfilingLock = new();
        public bool IsProfiling()
        {
            lock (isProfilingLock)
            {
                return isProfiling;
            }
        }


        private readonly object setProfilingLock = new();
        public void setProfiling(bool isProfiling)
        {
            lock (setProfilingLock)
            {
                this.isProfiling = isProfiling;
            }
        }


        private readonly object setIsMemoryObserverLock = new();
        public void setIsMemoryObserver(bool isMemoryObserver)
        {
            lock (setIsMemoryObserverLock)
            {
                this.isMemoryObserver = isMemoryObserver;
            }
        }


        private readonly object isPublishSubscribeLock = new();
        public bool isPublishSubscribe()
        {
            lock (isPublishSubscribeLock)
            {
                return isMemoryObserver;
            }
        }

        private readonly object setPublishSubscribeLock = new();
        public void setPublishSubscribe(bool enable)
        {
            lock (setPublishSubscribeLock)
            {
                if (enable)
                {
                    setIsMemoryObserver(true);
                    foreach (Memory m in inputs)
                    {
                        m.addMemoryObserver(this);
                    }
                }
                else
                {
                    foreach (Memory m in inputs)
                    {
                        m.removeMemoryObserver(this);
                    }

                    setIsMemoryObserver(false);

                    try
                    {
                        Monitor.Wait(setPublishSubscribeLock, 300);
                    }
                    catch (ThreadInterruptedException)
                    {
                        // just ignore exception
                    }

                    run();
                }
            }
        }



        private void raiseException()
        {
            throw new Exception(
                "This Codelet could not find a memory object it needs: " + this.name);
        }

        public void notifyCodelet()
        {
            long startTime = 0L;
            //long endTime = 0L;
            //long duration = 0L;

            try
            {
                if (isProfiling)
                    startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                accessMemoryObjects();

                if (enable_count == 0)
                {
                    calculateActivation();
                    if (activation >= threshold)
                        proc();
                }
                else
                {
                    raiseException();
                }

                enable_count = 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(typeof(Codelet).Name + ex.Message);
            }
            finally
            {
                //if (this.codeletProfiler != null)
                //{
                //    this.codeletProfiler.Profile(this);
                //}

                //if (isProfiling)
                //{
                //    endTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                //    duration = (endTime - startTime);
                //
                //    ProfileInfo pi = new ProfileInfo(duration, startTime, laststarttime);
                //    profileInfo.Add(pi);
                //    laststarttime = startTime;
                //
                //    if (profileInfo.Count >= 50)
                //    {
                //        ExecutionTimeWriter executionTimeWriter = new ExecutionTimeWriter();
                //        executionTimeWriter.SetCodeletName(name);
                //        executionTimeWriter.SetPath("profile/");
                //        executionTimeWriter.SetProfileInfo(profileInfo);
                //
                //        Thread thread = new Thread(executionTimeWriter.Run);
                //        thread.Start();
                //
                //        profileInfo = new List<ProfileInfo>();
                //    }
                //}
            }
        }
    }
}
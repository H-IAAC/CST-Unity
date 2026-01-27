using System;
using UnityEngine;
using HIAAC.CstUnity.Core.Entities;
using System.Collections;
using System.Collections.Generic;
using HIAAC.CstUnity.MemoryStorage;

using NRedisStack;
using NRedisStack.RedisStackCommands;
using StackExchange.Redis;

namespace HIAAC.CstUnity
{
    //C# equivalent of https://h-iaac.github.io/CST-Python/_build/html/_examples/Memory%20Storage.html
    public class MemoryStorageExample : MonoBehaviour
    {
        Memory firstnode_memory;
        Memory secondnode_memory;

        void Start()
        {
            var muxer = ConnectionMultiplexer.Connect("localhost,allowAdmin=true");
            var server = muxer.GetServer("localhost", 6379);
            server.FlushAllDatabases();

            var firstnode_mind = new Mind();
            firstnode_memory = firstnode_mind.createMemoryObject("MyMemory", "");

            var firstnode_mscodelet = new MemoryStorageCodelet(firstnode_mind);
            firstnode_mscodelet.setTimeStep(50);
            firstnode_mind.insertCodelet(firstnode_mscodelet);

            firstnode_mind.start();

            firstnode_memory.setI("First node info");


            var secondnode_mind = new Mind();
            secondnode_memory = secondnode_mind.createMemoryObject("MyMemory", "");

            print($"After creating second memory | First: {firstnode_memory.getI()} | Second: {secondnode_memory.getI()}");

            var secondnode_mscodelet = new MemoryStorageCodelet(secondnode_mind);
            secondnode_mscodelet.setTimeStep(50);
            secondnode_mind.insertCodelet(secondnode_mscodelet);

            secondnode_mind.start();

            print($"After starting mind 2 | First: {firstnode_memory.getI()} | Second: {secondnode_memory.getI()}");
        }

        void Update()
        {
            print($"First: {firstnode_memory.getI()} ({firstnode_memory.getI().GetType()}) | Second: {secondnode_memory.getI()} ({secondnode_memory.getI().GetType()})");

            if (secondnode_memory.getI() as string == "First node info")
            {
                secondnode_memory.setI("Second node info");
            }
            if (firstnode_memory.getI() as string == "Second node info")
            {
                firstnode_memory.setI(new List<int> { 1, 2, 3 });
            }

        }

    }
}
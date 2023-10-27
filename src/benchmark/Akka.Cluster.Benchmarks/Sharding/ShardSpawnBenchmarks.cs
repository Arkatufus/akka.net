//-----------------------------------------------------------------------
// <copyright file="ShardSpawnBenchmarks.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2023 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Benchmarks.Configurations;
using Akka.Cluster.Sharding;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using static Akka.Cluster.Benchmarks.Sharding.ShardingHelper;

namespace Akka.Cluster.Benchmarks.Sharding
{
    [Config(typeof(MonitoringConfig))]
    [SimpleJob(RunStrategy.ColdStart, warmupCount:0, launchCount:5)]
    public class ShardSpawnBenchmarks
    {
        [Params(StateStoreMode.Persistence, StateStoreMode.DData)]
        public StateStoreMode StateMode;

        [Params(1000)]
        public int EntityCount;

        [Params(true, false)]
        public bool RememberEntities;

        public int BatchSize = 20;

        private ActorSystem _sys1;
        private ActorSystem _sys2;

        private IActorRef _shardRegion1;
        private IActorRef _shardRegion2;

        public static int _shardRegionId = 0;
        
        [IterationSetup]
        public void IterationSetup()
        {
            var config = StateMode switch
            {
                StateStoreMode.Persistence => CreatePersistenceConfig(RememberEntities),
                StateStoreMode.DData => CreateDDataConfig(RememberEntities),
                _ => null
            };

            _sys1 = ActorSystem.Create("BenchSys", config);
            _sys2 = ActorSystem.Create("BenchSys", config);

            var c1 = Cluster.Get(_sys1);
            var c2 = Cluster.Get(_sys2);

            Task.WhenAll(
                c1.JoinAsync(c1.SelfAddress),
                c2.JoinAsync(c1.SelfAddress)).Wait();
            
            _shardRegion1 = StartShardRegion(_sys1, "entities" + _shardRegionId);
            _shardRegion2 = StartShardRegion(_sys2, "entities" + _shardRegionId);
            _shardRegionId++;
        }

        [Benchmark]
        public async Task SpawnEntities()
        {
            var tasks = Enumerable.Range(0, EntityCount)
                .Select(i =>
                {
                    var msg = new ShardedMessage(i.ToString(), i);
                    return _shardRegion1.Ask<ShardedMessage>(msg);
                });
            await Task.WhenAll(tasks);
        }
        
        [IterationCleanup]
        public void Cleanup()
        {
            var t2 = CoordinatedShutdown.Get(_sys2).Run(CoordinatedShutdown.ActorSystemTerminateReason.Instance);
            var t1 = CoordinatedShutdown.Get(_sys1).Run(CoordinatedShutdown.ActorSystemTerminateReason.Instance);
           
            Task.WhenAll(t1, t2).Wait();
        }
    }
}

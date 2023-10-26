// //-----------------------------------------------------------------------
// // <copyright file="ShardRememberEntitiesSpawnBenchmark.cs" company="Akka.NET Project">
// //     Copyright (C) 2009-2023 Lightbend Inc. <http://www.lightbend.com>
// //     Copyright (C) 2013-2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
// // </copyright>
// //-----------------------------------------------------------------------

using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Benchmarks.Configurations;
using Akka.Cluster.Sharding;
using Akka.Configuration;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using static Akka.Cluster.Benchmarks.Sharding.ShardingHelper;

namespace Akka.Cluster.Benchmarks.Sharding
{
    [Config(typeof(MonitoringConfig))]
    [SimpleJob(RunStrategy.ColdStart, targetCount:1, warmupCount:0, launchCount:5)]
    public class ShardRememberEntitiesRespawnBenchmark
    {
        [Params(StateStoreMode.Persistence, StateStoreMode.DData)]
        public StateStoreMode StateMode;

        [Params(1000, 5000, 10000)]
        public int EntityCount;

        public int BatchSize = 20;

        private Config _config;
        private ActorSystem _sys1;

        private IActorRef _shardRegion1;

        [GlobalSetup]
        public async Task Setup()
        {
            _config = StateMode switch
            {
                StateStoreMode.Persistence => CreatePersistenceConfig(true),
                StateStoreMode.DData => CreateDDataConfig(true),
                _ => null
            };

            _sys1 = ActorSystem.Create("BenchSys", _config);

            var c1 = Cluster.Get(_sys1);
            await c1.JoinAsync(c1.SelfAddress);
            
            _shardRegion1 = StartShardRegion(_sys1, "entities");

            await SpawnEntities();

            await CoordinatedShutdown.Get(_sys1).Run(CoordinatedShutdown.ActorSystemTerminateReason.Instance);
        }

        [IterationSetup]
        public void IterationSetup()
        {
            _sys1 = ActorSystem.Create("BenchSys", _config);

            var c1 = Cluster.Get(_sys1);
            c1.JoinAsync(c1.SelfAddress).Wait();
        }

        [Benchmark]
        public async Task SpawnEntities()
        {
            _shardRegion1 = StartShardRegion(_sys1, "entities");
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
            CoordinatedShutdown.Get(_sys1).Run(CoordinatedShutdown.ActorSystemTerminateReason.Instance).Wait();
        }
    }
}
//-----------------------------------------------------------------------
// <copyright file="ShardRememberEntitiesSpawnBenchmark.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2023 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Benchmarks.Configurations;
using Akka.Cluster.Sharding;
using Akka.Configuration;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Microsoft.Data.Sqlite;
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
        private SqliteConnection _dbCon;
        
        private ActorSystem _sys1;

        private IActorRef _shardRegion1;

        [GlobalSetup]
        public async Task Setup()
        {
            // delete older lmdb database, if it exists
            var ddataDb = Path.Join(".", "ddata-BenchSys-replicator-55555", "data.mdb");
            if(File.Exists(ddataDb))
                File.Delete(ddataDb);
            
            // ddata lmdb uses a combination of actor system name and port number to name the state database
            _config = ConfigurationFactory.ParseString("akka.remote.dot-netty.tcp.port = 55555");
            
            _config = StateMode switch
            {
                StateStoreMode.Persistence => _config.WithFallback(CreatePersistenceConfig(true)),
                StateStoreMode.DData => _config.WithFallback(CreateDDataConfig(true)),
                _ => null
            };
            
            // Sqlite in-memory database connection has to be kept open so that it would not be disposed
            if (string.IsNullOrEmpty(ConnectionString))
            {
                _dbCon = null;
            }
            else
            {
                _dbCon = new SqliteConnection(ConnectionString);
                await _dbCon.OpenAsync();
            }

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
        public void IterationCleanup()
        {
            CoordinatedShutdown.Get(_sys1).Run(CoordinatedShutdown.ActorSystemTerminateReason.Instance).Wait();
        }

        [GlobalCleanup]
        public async Task Cleanup()
        {
            if(_dbCon != null)
                await _dbCon.CloseAsync();
        }
    }
}
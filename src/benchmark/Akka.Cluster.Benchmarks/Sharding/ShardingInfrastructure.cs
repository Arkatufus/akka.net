//-----------------------------------------------------------------------
// <copyright file="ShardingInfrastructure.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2023 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Configuration;
using Akka.Util.Internal;

namespace Akka.Cluster.Benchmarks.Sharding
{
    public sealed class ShardedEntityActor : ReceiveActor
    {
        public sealed class Resolve
        {
            public static readonly Resolve Instance = new();
            private Resolve(){}
        }

        public sealed class ResolveResp
        {
            public ResolveResp(string entityId, Address addr)
            {
                EntityId = entityId;
                Addr = addr;
            }

            public string EntityId { get; }
            
            public Address Addr { get; }
        }
        
        public ShardedEntityActor()
        {
            Receive<ShardingEnvelope>(e =>
            {
                Sender.Tell(new ResolveResp(e.EntityId, Cluster.Get(Context.System).SelfAddress));
            });
            
            ReceiveAny(_ => Sender.Tell(_));
        }
    }



    public sealed class ShardedProxyEntityActor : ReceiveActor, IWithUnboundedStash
    {
        private IActorRef _shardRegion;
        private IActorRef _sender;

        

      

        public ShardedProxyEntityActor(IActorRef shardRegion)
        {
            _shardRegion = shardRegion;
            WaitRequest();
        }

        public void WaitRequest()
        {
           
            Receive<SendShardedMessage>(e =>
            {
                _sender = Sender;
                _shardRegion.Tell(e.Message);
                Become(WaitResult);

            });

            ReceiveAny(x => {
                Sender.Tell(x);
                });

            
        }


        public void WaitResult()
        {
            Receive<ShardedMessage>((msg) => {
                _sender.Tell(msg);
                Stash.UnstashAll();
                Become(WaitRequest);
            });

            ReceiveAny(_ =>
                Stash.Stash()
            );


        }

        public IStash Stash { get; set; }

    }



    public sealed class BulkSendActor : ReceiveActor
    {
        public sealed class BeginSend
        {
            public BeginSend(ShardedMessage msg, IActorRef target, int batchSize)
            {
                Msg = msg;
                Target = target;
                BatchSize = batchSize;
            }

            public ShardedMessage Msg { get; }
            
            public IActorRef Target { get; }
            
            public int BatchSize { get; }
        }
        
        private int _remaining;

        private readonly TaskCompletionSource<bool> _tcs;

        public BulkSendActor(TaskCompletionSource<bool> tcs, int remaining)
        {
            _tcs = tcs;
            _remaining = remaining;

            Receive<BeginSend>(b =>
            {
                for (var i = 0; i < b.BatchSize; i++)
                {
                    b.Target.Tell(b.Msg);
                }
            });

            Receive<ShardedMessage>(s =>
            {
                if (--remaining > 0)
                {
                    Sender.Tell(s);
                }
                else
                {
                    Context.Stop(Self); // shut ourselves down
                    _tcs.TrySetResult(true);
                }
            });
        }
    }
    
    public sealed class ShardedMessage
    {
        public ShardedMessage(string entityId, int message)
        {
            EntityId = entityId;
            Message = message;
        }

        public string EntityId { get; }
            
        public int Message { get; }
    }



    public sealed class SendShardedMessage
    {
        public SendShardedMessage(string entityId, ShardedMessage message)
        {
            EntityId = entityId;
            Message = message;
        }

        public string EntityId { get; }

        public ShardedMessage Message { get; }
    }


    /// <summary>
    /// Use a default <see cref="IMessageExtractor"/> even though it takes extra work to setup the benchmark
    /// </summary>
    public sealed class ShardMessageExtractor : HashCodeMessageExtractor
    {
        /// <summary>
        /// We only ever run with a maximum of two nodes, so ~10 shards per node
        /// </summary>
        public ShardMessageExtractor(int shardCount = 20) : base(shardCount)
        {
        }

        public override string EntityId(object message)
        {
            if(message is ShardedMessage sharded)
            {
                return sharded.EntityId;
            }

            if (message is ShardingEnvelope e)
            {
                return e.EntityId;
            }

            return null;
        }
    }

    public static class ShardingHelper
    {
        public readonly static AtomicCounter DbId = new AtomicCounter(0);
        public static string ConnectionString { get; private set; }

        internal static string BoolToToggle(bool val)
        {
            return val ? "on" : "off";
        }
        
        public static Config CreatePersistenceConfig(
            StateStoreMode storeMode, 
            bool rememberEntities = false, 
            RememberEntitiesStore entitiesStoreMode = RememberEntitiesStore.DData)
        {
            ConnectionString = "Filename=file:memdb-journal-" + DbId.IncrementAndGet() + ".db;Mode=Memory;Cache=Shared";
            var config = $$"""
akka {
    actor.provider = cluster
    remote.dot-netty.tcp.port = 0
    cluster {
        sharding {
            state-store-mode = {{storeMode.ToStateStoreMode()}}
            remember-entities = {{BoolToToggle(rememberEntities)}}
            remember-entities-store = {{entitiesStoreMode.ToRememberEntitiesStore()}}
        }
    }
    persistence {
        journal {
            plugin = "akka.persistence.journal.sqlite"
            sqlite {
                class = "Akka.Persistence.Sqlite.Journal.SqliteJournal, Akka.Persistence.Sqlite"
                auto-initialize = on
                connection-string = "{{ConnectionString}}"
            }
        }
        snapshot-store {
            plugin = "akka.persistence.snapshot-store.sqlite"
            sqlite {
                class = "Akka.Persistence.Sqlite.Snapshot.SqliteSnapshotStore, Akka.Persistence.Sqlite"
                auto-initialize = on
                connection-string = "{{ConnectionString}}"
            }
        }
    }
}
""";

            return config;
        }

        public static string ToStateStoreMode(this StateStoreMode mode)
            => mode switch
            {
                StateStoreMode.Persistence => "persistence",
                StateStoreMode.DData => "ddata",
                _ => throw new IndexOutOfRangeException()
            };

        public static string ToRememberEntitiesStore(this RememberEntitiesStore mode)
            => mode switch
            {
                RememberEntitiesStore.Eventsourced => "eventsourced",
                RememberEntitiesStore.DData => "ddata",
                _ => throw new IndexOutOfRangeException()
            };

        public static IActorRef StartShardRegion(ActorSystem system, string entityName = "entities")
        {
            var props = Props.Create(() => new ShardedEntityActor());
            var sharding = ClusterSharding.Get(system);
            return sharding.Start(entityName, _ => props, ClusterShardingSettings.Create(system),
                new ShardMessageExtractor());
        }
    }
}

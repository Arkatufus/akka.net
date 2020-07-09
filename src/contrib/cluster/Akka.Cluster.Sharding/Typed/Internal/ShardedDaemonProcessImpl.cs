//-----------------------------------------------------------------------
// <copyright file="ClusterSharding.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2020 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2020 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Immutable;
using System.Linq;
using Akka.Actor;
using Akka.Annotations;
using Akka.Event;
using Akka.Util;
using Akka.Util.Extensions;
using Akka.Util.Internal;
using EntityId = System.String;

namespace Akka.Cluster.Sharding.Typed.Internal
{
    // Sharded Daemon: This actor is a back ported version of the scala Actor.Typed version
    internal class KeepAlivePinger : ReceiveActor, IWithTimers
    {
        internal interface IEvent { }

        private sealed class Tick : IEvent
        {
            public static Tick Instance { get; } = new Tick();
            private Tick() { }
        }

        private readonly ILoggingAdapter _log;

        public string Name { get; }
        public ImmutableArray<EntityId> Identities { get; }
        public IActorRef ShardingRef { get; }
        public ShardedDaemonProcessSettings Settings { get; }

        public ITimerScheduler Timers { get; set; }

        public static Props Props(
            ShardedDaemonProcessSettings settings, 
            string name, 
            ImmutableArray<EntityId> identities, 
            IActorRef shardingRef) 
            => Actor.Props.Create(() => new KeepAlivePinger(settings, name, identities, shardingRef));

        public KeepAlivePinger(
            ShardedDaemonProcessSettings settings,
            string name,
            ImmutableArray<EntityId> identities,
            IActorRef shardingRef)
        {
            Settings = settings;
            Name = name;
            Identities = identities;
            ShardingRef = shardingRef;

            _log = Context.GetLogger();

            Receive<IEvent>(msg =>
            {
                switch (msg)
                {
                    case Tick _:
                        TriggerStartAll();
                        _log.Debug("Periodic ping sent to [{0}] processes", Identities.Length);
                        break;
                }
            });
        }

        protected override void PreStart()
        {
            base.PreStart();

            _log.Debug("Starting Sharded Daemon Process KeepAlivePinger for [{0}], with ping interval [{1}]", Name, ((TimeSpan?)Settings.KeepAliveInterval).Pretty());

            // Sharded Daemon: This is supposed to be StartTimerWithFixedDelay in the scala version.
            Timers.StartPeriodicTimer(Tick.Instance, Tick.Instance, Settings.KeepAliveInterval);
            TriggerStartAll();
        }

        private void TriggerStartAll() => Identities.ForEach(id => ShardingRef.Tell(new ShardRegion.StartEntity(id)));
    }

#nullable enable
    internal sealed class MessageExtractor<T> 
        : ShardingMessageExtractor<ShardingEnvelope<T>, T> 
        where T : class?
    {
        public MessageExtractor(int maxNumberOfShards)
            : base(maxNumberOfShards)
        { }

        public override string EntityId(ShardingEnvelope<T> message)
            => message.EntityId;

        public override string ShardId(string entityId)
            => entityId;

        public override T UnwrapMessage(ShardingEnvelope<T> message)
            => message.Message;
    }
#nullable disable

    /// <summary>
    /// <para>This extension runs a pre set number of actors in a cluster.</para>
    /// <para>
    /// The typical use case is when you have a task that can be divided in a number of workers, each doing a
    /// sharded part of the work, for example consuming the read side events from Akka Persistence through
    /// tagged events where each tag decides which consumer that should consume the event.
    /// </para>
    /// <para>Each named set needs to be started on all the nodes of the cluster on start up.</para>
    /// <para>
    /// The processes are spread out across the cluster, when the cluster topology changes the processes may be stopped
    /// and started anew on a new node to rebalance them.
    /// </para>
    /// <para>Not for user extension.</para>
    /// </summary>
    [ApiMayChange]
    internal class ShardedDaemonProcessImpl : IExtension
    {
        private readonly ExtendedActorSystem _system;

        public ShardedDaemonProcessImpl(ExtendedActorSystem system) => _system = system;

        public static ShardedDaemonProcessImpl Get(ActorSystem system) =>
            system.WithExtension<ShardedDaemonProcessImpl, ShardedDaemonProcessExtensionProvider>();

        /// <summary>
        /// Start a specific number of actors that is then kept alive in the cluster.
        /// </summary>
        /// <param name="name">TBD</param>
        /// <param name="numberOfInstances">TBD</param>
        /// <param name="propsFactory">Given a unique id of `0` until `numberOfInstance` create an entity actor.</param>
        public void Init<T>(string name, int numberOfInstances, Func<int, Props> propsFactory)
        {
            Init(name, numberOfInstances, propsFactory, ShardedDaemonProcessSettings.Create(_system));
        }

        /// <summary>
        /// Start a specific number of actors, each with a unique numeric id in the set, that is then kept alive in the cluster.
        /// </summary>
        /// <param name="name">TBD</param>
        /// <param name="numberOfInstances">TBD</param>
        /// <param name="propsFactory">Given a unique id of `0` until `numberOfInstance` create an entity actor.</param>
        /// <param name="settings">TBD</param>
        public void Init<T>(
            string name, 
            int numberOfInstances, 
            Func<int, Props> propsFactory, 
            ShardedDaemonProcessSettings settings,
            Option<T> stopMessage)
        {
            // is this important?
            // val entityTypeKey = EntityTypeKey[T](s"sharded-daemon-process-$name")
            var entityTypeKey = $"sharded-daemon-process-{name}";

            // One shard per actor identified by the numeric id encoded in the entity id
            var numberOfShards = numberOfInstances;
            var entityIds = Enumerable.Range(0, numberOfInstances).Select(i => i.ToString()).ToImmutableArray();

            var shardingBaseSettings = settings.ShardingSettings;
            if (shardingBaseSettings == null)
            {
                // Defaults in `akka.cluster.sharding` but allow overrides specifically for actor-set   
                shardingBaseSettings = ClusterShardingSettings.FromConfig(
                    _system.Settings.Config.GetConfig("akka.cluster.sharded-daemon-process.sharding"));
            }

            var shardingSettings = new ClusterShardingSettings(
                numberOfShards,
                shardingBaseSettings.Role,
                shardingBaseSettings.DataCenter,
                false, // remember entities disabled
                "",
                "",
                TimeSpan.Zero, // passivation disabled
                shardingBaseSettings.ShardRegionQueryTimeout,
                ClusterShardingSettings.StateStoreModeDData.Instance,
                shardingBaseSettings.TuningParameters,
                shardingBaseSettings.CoordinatorSingletonSettings);

            if (!shardingSettings.Role.HasValue || Cluster.Get(_system).SelfRoles.Contains(shardingSettings.Role.Value))
            {
                var shardRegion = ClusterSharding.Get(_system).Start(
                    typeName: entityTypeKey,
                    entityPropsFactory: entityId => propsFactory(int.Parse(entityId)),
                    settings: shardingSettings,
                    messageExtractor: new MessageExtractor(numberOfShards));

                _system.ActorOf(
                    KeepAlivePinger.Props(settings, name, entityIds, shardRegion),
                    $"ShardedDaemonProcessKeepAlive-{name}");
            }
        }
    }

    public class ShardedDaemonProcessExtensionProvider : ExtensionIdProvider<ShardedDaemonProcessImpl>
    {
        public override ShardedDaemonProcessImpl CreateExtension(ExtendedActorSystem system) => new ShardedDaemonProcessImpl(system);
    }
}

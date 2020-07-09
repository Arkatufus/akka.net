using System;
using Akka.Actor;
using Akka.Cluster.Tools.Singleton;
using Akka.Configuration;
using Akka.Util;
using ClassicShardingSettings = Akka.Cluster.Sharding.ClusterShardingSettings;
using DataCenter = System.String;

namespace Akka.Cluster.Sharding.Typed
{
    public sealed class ClusterShardingSettings
    {
        public static ClusterShardingSettings Apply(ActorSystem system)
            => FromConfig(system.Settings.Config.GetConfig("akka.cluster.sharding"));

        public static ClusterShardingSettings FromConfig(Config config)
        {
            var classicSettings = new ClassicShardingSettings(config);
            var numberOfShards = config.GetInt("number-of-shards");
            return FromClassicSettings(numberOfShards, classicSettings);
        }

        /// <summary>
        /// INTERNAL API
        ///
        /// Intended only for internal use, it is not recommended to keep converting between the setting types
        /// </summary>
        /// <param name="numberOfShards"></param>
        /// <param name="classicSettings"></param>
        /// <returns></returns>
        internal static ClusterShardingSettings FromClassicSettings(
            int numberOfShards,
            ClassicShardingSettings classicSettings)
            => new ClusterShardingSettings(
                numberOfShards: numberOfShards,
                role: classicSettings.Role,
                dataCenter: null,
                rememberEntities: classicSettings.RememberEntities,
                journalPluginId: classicSettings.JournalPluginId,
                snapshotPluginId: classicSettings.SnapshotPluginId,
                passivateIdleEntityAfter: classicSettings.PassivateIdleEntityAfter,
                shardRegionQueryTimeout: classicSettings.ShardRegionQueryTimeout,
                stateStoreMode: StateStoreModeByName(classicSettings.StateStoreMode.ToString().ToLowerInvariant()),
                new TuningParameters(classicSettings.TuningParameters),
                new ClusterSingletonManagerSettings(
                    classicSettings.CoordinatorSingletonSettings.SingletonName,
                    classicSettings.CoordinatorSingletonSettings.Role,
                    classicSettings.CoordinatorSingletonSettings.RemovalMargin,
                    classicSettings.CoordinatorSingletonSettings.HandOverRetryInterval
                )
            );

        internal ClassicShardingSettings ToClassicSettings(ClusterShardingSettings settings)
            => new ClassicShardingSettings(
                role: settings.Role.Value,
                rememberEntities: settings.RememberEntities,
                journalPluginId: settings.JournalPluginId,
                snapshotPluginId: settings.SnapshotPluginId,
                stateStoreMode: (StateStoreMode)Enum.Parse(typeof(StateStoreMode), settings.StateStoreMode.Name),
                passivateIdleEntityAfter: settings.PassivateIdleEntityAfter,
                shardRegionQueryTimeout: settings.PassivateIdleEntityAfter,
                new Sharding.TuningParameters(
                    bufferSize: settings.TuningParameters.BufferSize,
                    coordinatorFailureBackoff: settings.TuningParameters.CoordinatorFailureBackoff,
                    retryInterval: settings.TuningParameters.RetryInterval,
                    handOffTimeout: settings.TuningParameters.HandOffTimeout,
                    shardStartTimeout: settings.TuningParameters.ShardStartTimeout,
                    shardFailureBackoff: settings.TuningParameters.ShardFailureBackoff,
                    entityRestartBackoff: settings.TuningParameters.EntityRestartBackoff,
                    rebalanceInterval: settings.TuningParameters.RebalanceInterval,
                    snapshotAfter: settings.TuningParameters.SnapshotAfter,
                    keepNrOfBatches: settings.TuningParameters.KeepNrOfBatches,
                    leastShardAllocationRebalanceThreshold: settings.TuningParameters
                        .LeastShardAllocationRebalanceThreshold, // TODO extract it a bit
                    leastShardAllocationMaxSimultaneousRebalance:
                    settings.TuningParameters.LeastShardAllocationMaxSimultaneousRebalance,
                    waitingForStateTimeout: settings.TuningParameters.WaitingForStateTimeout,
                    updatingStateTimeout: settings.TuningParameters.UpdatingStateTimeout,
                    entityRecoveryStrategy: settings.TuningParameters.EntityRecoveryStrategy,
                    entityRecoveryConstantRateStrategyFrequency:
                    settings.TuningParameters.EntityRecoveryConstantRateStrategyFrequency,
                    entityRecoveryConstantRateStrategyNumberOfEntities:
                    settings.TuningParameters.EntityRecoveryConstantRateStrategyNumberOfEntities,
                    coordinatorStateWriteMajorityPlus: settings.TuningParameters.CoordinatorStateWriteMajorityPlus,
                    coordinatorStateReadMajorityPlus: settings.TuningParameters.CoordinatorStateReadMajorityPlus),
                new ClusterSingletonManagerSettings(
                    settings.CoordinatorSingletonSettings.SingletonName,
                    settings.CoordinatorSingletonSettings.Role,
                    settings.CoordinatorSingletonSettings.RemovalMargin,
                    settings.CoordinatorSingletonSettings.HandOverRetryInterval),
                leaseSettings: null);

        public interface IStateStoreMode
        {
            string Name { get; }
        }

        public sealed class StateStoreModePersistence : IStateStoreMode
        {
            public static StateStoreModePersistence Instance { get; } = new StateStoreModePersistence();

            public string Name => "persistence";

            private StateStoreModePersistence()
            {
            }

            public override string ToString() => Name;
        }

        public sealed class StateStoreModeDData : IStateStoreMode
        {
            public static StateStoreModeDData Instance { get; } = new StateStoreModeDData();

            public string Name => "ddata";

            private StateStoreModeDData()
            {
            }

            public override string ToString() => Name;
        }

        public static IStateStoreMode StateStoreModeByName(string name)
            => name switch
            {
                "persistence" => StateStoreModePersistence.Instance,
                "ddata" => StateStoreModeDData.Instance,
                _ => throw new ArgumentException("Not recognized StateStoreMode, only 'ddata' is supported.")
            };


        /// <summary>
        /// number of shards used by the default <see cref="HashCodeMessageExtractor"/>
        /// </summary>
        public int NumberOfShards { get; }

        public Option<string> Role { get; }

        /// <summary>
        /// The data center of the cluster nodes where the cluster sharding is running.
        /// If the dataCenter is not specified then the same data center as current node. If the given
        /// dataCenter does not match the data center of the current node the `ShardRegion` will be started
        /// in proxy mode.
        /// </summary>
        public Option<DataCenter> DataCenter { get; }

        public bool RememberEntities { get; }
        public string JournalPluginId { get; }
        public string SnapshotPluginId { get; }
        public TimeSpan PassivateIdleEntityAfter { get; }
        public TimeSpan ShardRegionQueryTimeout { get; }

        public IStateStoreMode StateStoreMode { get; }

        public TuningParameters TuningParameters { get; }

        public ClusterSingletonManagerSettings CoordinatorSingletonSettings { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="numberOfShards">number of shards used by the default <see cref="HashCodeMessageExtractor"/></param>
        /// <param name="role">specifies that this entity type requires cluster nodes with a specific role.
        ///   If the role is not specified all nodes in the cluster are used.</param>
        /// <param name="dataCenter">The data center of the cluster nodes where the cluster sharding is running.
        ///   If the dataCenter is not specified then the same data center as current node.If the given
        ///   dataCenter does not match the data center of the current node the `ShardRegion` will be started
        ///   in proxy mode.</param>
        /// <param name="rememberEntities">true if active entity actors shall be automatically restarted upon `Shard`
        ///   restart.i.e. if the `Shard` is started on a different `ShardRegion` due to rebalance or crash.</param>
        /// <param name="journalPluginId">Absolute path to the journal plugin configuration entity that is to
        ///   be used for the internal persistence of ClusterSharding.If not defined the default
        ///   journal plugin is used.Note that this is not related to persistence used by the entity
        ///   actors.</param>
        /// <param name="snapshotPluginId">Absolute path to the snapshot plugin configuration entity that is to
        ///   be used for the internal persistence of ClusterSharding.If not defined the default
        ///   snapshot plugin is used.Note that this is not related to persistence used by the entity
        ///   actors.</param>
        /// <param name="passivateIdleEntityAfter">Passivate entities that have not received any message in this interval.
        ///   Note that only messages sent through sharding are counted, so direct messages
        ///   to the `ActorRef` of the actor or messages that it sends to itself are not counted as activity.
        ///   Use 0 to disable automatic passivation.It is always disabled if `rememberEntities` is enabled.</param>
        /// <param name="shardRegionQueryTimeout">the timeout for querying a shard region, see descriptions in reference.conf</param>
        /// <param name="stateStoreMode">TBD</param>
        /// <param name="tuningParameters">additional tuning parameters, see descriptions in reference.conf</param>
        /// <param name="coordinatorSingletonSettings">TBD</param>
        public ClusterShardingSettings(
            int numberOfShards,
            Option<string> role,
            Option<DataCenter> dataCenter,
            bool rememberEntities,
            string journalPluginId,
            string snapshotPluginId,
            TimeSpan passivateIdleEntityAfter,
            TimeSpan shardRegionQueryTimeout,
            IStateStoreMode stateStoreMode,
            TuningParameters tuningParameters,
            ClusterSingletonManagerSettings coordinatorSingletonSettings)
        {
            NumberOfShards = numberOfShards;
            Role = role;
            DataCenter = dataCenter;
            RememberEntities = rememberEntities;
            JournalPluginId = journalPluginId;
            SnapshotPluginId = snapshotPluginId;
            PassivateIdleEntityAfter = passivateIdleEntityAfter;
            ShardRegionQueryTimeout = shardRegionQueryTimeout;
            StateStoreMode = stateStoreMode;
            TuningParameters = tuningParameters;
            CoordinatorSingletonSettings = coordinatorSingletonSettings;

            if (!ReferenceEquals(stateStoreMode, StateStoreModePersistence.Instance) &&
                !ReferenceEquals(stateStoreMode, StateStoreModeDData.Instance))
                throw new ConfigurationException(
                    $"Unknown 'state-store-mode' [{stateStoreMode}], valid values are '{StateStoreModeDData.Instance}' or '{StateStoreModePersistence.Instance}'");
        }

        // Shared Daemon: we should check for data center here!
        /*
         * private[akka] def shouldHostShard(cluster: Cluster): Boolean =
         * role.forall(cluster.selfMember.roles.contains) &&
         * dataCenter.forall(_ == cluster.selfMember.dataCenter)
         */
        /// <summary>
        /// INTERNAL API
        ///
        /// If true, this node should run the shard region, otherwise just a shard proxy should started on this node.
        /// It's checking if the `role` and `dataCenter` are matching.
        /// </summary>
        /// <param name="cluster"></param>
        /// <returns></returns>
        internal bool ShouldHostShard(Cluster cluster)
            => !Role.HasValue || cluster.SelfMember.Roles.Contains(Role.Value);

        // no withNumberOfShards because it should be defined in configuration to be able to verify same
        // value on all nodes with `JoinConfigCompatChecker`

        public ClusterShardingSettings WithRole(string role) 
            => Copy(role: new Option<string>(role));

        public ClusterShardingSettings WithDataCenter(DataCenter dataCenter) 
            => Copy(dataCenter: new Option<DataCenter>(dataCenter));

        public ClusterShardingSettings WithRememberEntities(bool rememberEntities) 
            => Copy(rememberEntities: rememberEntities);

        public ClusterShardingSettings WithJournalPluginId(string journalPluginId) 
            => Copy(journalPluginId: journalPluginId);

        public ClusterShardingSettings WithSnapshotPluginId(string snapshotPluginId) 
            => Copy(snapshotPluginId: snapshotPluginId);

        public ClusterShardingSettings WithTuningParameters(TuningParameters tuningParameters) 
            => Copy(tuningParameters: tuningParameters);

        public ClusterShardingSettings WithStateStoreMode(IStateStoreMode stateStoreMode) 
            => Copy(stateStoreMode: stateStoreMode);

        public ClusterShardingSettings WithPassivateIdleEntityAfter(TimeSpan duration) 
            => Copy(passivateIdleEntityAfter: duration);

        public ClusterShardingSettings WithShardRegionQueryTimeout(TimeSpan duration) 
            => Copy(shardRegionQueryTimeout: duration);

        /**
         * The `role` of the `ClusterSingletonManagerSettings` is not used. The `role` of the
         * coordinator singleton will be the same as the `role` of `ClusterShardingSettings`.
         */
        public ClusterShardingSettings WithCoordinatorSingletonSettings(ClusterSingletonManagerSettings settings)
            => Copy(coordinatorSingletonSettings: settings);

        private ClusterShardingSettings Copy(
            Option<string>? role = null,
            Option<DataCenter>? dataCenter = null,
            bool? rememberEntities = null,
            string journalPluginId = null,
            string snapshotPluginId = null,
            IStateStoreMode stateStoreMode = null,
            TuningParameters tuningParameters = null,
            ClusterSingletonManagerSettings coordinatorSingletonSettings = null,
            TimeSpan? passivateIdleEntityAfter = null,
            TimeSpan? shardRegionQueryTimeout = null)
            => new ClusterShardingSettings(
                    NumberOfShards,
                    role??Role,
                    dataCenter??DataCenter,
                    rememberEntities??RememberEntities,
                    journalPluginId??JournalPluginId,
                    snapshotPluginId??SnapshotPluginId,
                    passivateIdleEntityAfter??PassivateIdleEntityAfter,
                    shardRegionQueryTimeout??ShardRegionQueryTimeout,
                    stateStoreMode??StateStoreMode,
                    tuningParameters??TuningParameters,
                    coordinatorSingletonSettings??CoordinatorSingletonSettings
                );
    }
}

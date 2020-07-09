//-----------------------------------------------------------------------
// <copyright file="ClusterShardingSettings.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2020 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2020 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Linq;
using Akka.Actor;
using Akka.Cluster.Tools.Singleton;
using Akka.Configuration;
using Akka.Coordination;
using Akka.Util;

namespace Akka.Cluster.Sharding
{
    /// <summary>
    /// TBD
    /// </summary>
    [Serializable]
    public class TuningParameters
    {
        /// <summary>
        /// TBD
        /// </summary>
        public TimeSpan CoordinatorFailureBackoff { get; }
        /// <summary>
        /// TBD
        /// </summary>
        public TimeSpan RetryInterval { get; }
        /// <summary>
        /// TBD
        /// </summary>
        public int BufferSize { get; }
        /// <summary>
        /// TBD
        /// </summary>
        public TimeSpan HandOffTimeout { get; }
        /// <summary>
        /// TBD
        /// </summary>
        public TimeSpan ShardStartTimeout { get; }
        /// <summary>
        /// TBD
        /// </summary>
        public TimeSpan ShardFailureBackoff { get; }
        /// <summary>
        /// TBD
        /// </summary>
        public TimeSpan EntityRestartBackoff { get; }
        /// <summary>
        /// TBD
        /// </summary>
        public TimeSpan RebalanceInterval { get; }
        /// <summary>
        /// TBD
        /// </summary>
        public int SnapshotAfter { get; }
        /// <summary>
        /// The shard deletes persistent events (messages and snapshots) after doing snapshot
        /// keeping this number of old persistent batches.
        /// Batch is of size <see cref="SnapshotAfter"/>.
        /// When set to 0 after snapshot is successfully done all messages with equal or lower sequence number will be deleted.
        /// Default value of 2 leaves last maximum 2*<see cref="SnapshotAfter"/> messages and 3 snapshots (2 old ones + fresh snapshot)
        /// </summary>
        public int KeepNrOfBatches { get; }
        /// <summary>
        /// TBD
        /// </summary>
        public int LeastShardAllocationRebalanceThreshold { get; }
        /// <summary>
        /// TBD
        /// </summary>
        public int LeastShardAllocationMaxSimultaneousRebalance { get; }

        public TimeSpan WaitingForStateTimeout { get; }

        public TimeSpan UpdatingStateTimeout { get; }

        public string EntityRecoveryStrategy { get; }
        public TimeSpan EntityRecoveryConstantRateStrategyFrequency { get; }
        public int EntityRecoveryConstantRateStrategyNumberOfEntities { get; }
        public int CoordinatorStateWriteMajorityPlus { get; }
        public int CoordinatorStateReadMajorityPlus { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="coordinatorFailureBackoff">TBD</param>
        /// <param name="retryInterval">TBD</param>
        /// <param name="bufferSize">TBD</param>
        /// <param name="handOffTimeout">TBD</param>
        /// <param name="shardStartTimeout">TBD</param>
        /// <param name="shardFailureBackoff">TBD</param>
        /// <param name="entityRestartBackoff">TBD</param>
        /// <param name="rebalanceInterval">TBD</param>
        /// <param name="snapshotAfter">TBD</param>
        /// <param name="keepNrOfBatches">Keep this number of old persistent batches</param>
        /// <param name="leastShardAllocationRebalanceThreshold">TBD</param>
        /// <param name="leastShardAllocationMaxSimultaneousRebalance">TBD</param>
        /// <param name="waitingForStateTimeout">TBD</param>
        /// <param name="updatingStateTimeout">TBD</param>
        /// <param name="entityRecoveryStrategy">TBD</param>
        /// <param name="entityRecoveryConstantRateStrategyFrequency">TBD</param>
        /// <param name="entityRecoveryConstantRateStrategyNumberOfEntities">TBD</param>
        /// <param name="coordinatorStateWriteMajorityPlus">TBD</param>
        /// <param name="coordinatorStateReadMajorityPlus">TBD</param>
        /// <exception cref="ArgumentException">
        /// This exception is thrown when the specified <paramref name="entityRecoveryStrategy"/> is invalid.
        /// Acceptable values include: all | constant
        /// </exception>
        public TuningParameters(
            TimeSpan coordinatorFailureBackoff,
            TimeSpan retryInterval,
            int bufferSize,
            TimeSpan handOffTimeout,
            TimeSpan shardStartTimeout,
            TimeSpan shardFailureBackoff,
            TimeSpan entityRestartBackoff,
            TimeSpan rebalanceInterval,
            int snapshotAfter,
            int keepNrOfBatches,
            int leastShardAllocationRebalanceThreshold,
            int leastShardAllocationMaxSimultaneousRebalance,
            TimeSpan waitingForStateTimeout,
            TimeSpan updatingStateTimeout,
            string entityRecoveryStrategy,
            TimeSpan entityRecoveryConstantRateStrategyFrequency,
            int entityRecoveryConstantRateStrategyNumberOfEntities,
            int coordinatorStateWriteMajorityPlus,
            int coordinatorStateReadMajorityPlus)
        {
            if (entityRecoveryStrategy != "all" && entityRecoveryStrategy != "constant")
                throw new ArgumentException($"Unknown 'entity-recovery-strategy' [{entityRecoveryStrategy}], valid values are 'all' or 'constant'");

            CoordinatorFailureBackoff = coordinatorFailureBackoff;
            RetryInterval = retryInterval;
            BufferSize = bufferSize;
            HandOffTimeout = handOffTimeout;
            ShardStartTimeout = shardStartTimeout;
            ShardFailureBackoff = shardFailureBackoff;
            EntityRestartBackoff = entityRestartBackoff;
            RebalanceInterval = rebalanceInterval;
            SnapshotAfter = snapshotAfter;
            KeepNrOfBatches = keepNrOfBatches;
            LeastShardAllocationRebalanceThreshold = leastShardAllocationRebalanceThreshold;
            LeastShardAllocationMaxSimultaneousRebalance = leastShardAllocationMaxSimultaneousRebalance;
            WaitingForStateTimeout = waitingForStateTimeout;
            UpdatingStateTimeout = updatingStateTimeout;
            EntityRecoveryStrategy = entityRecoveryStrategy;
            EntityRecoveryConstantRateStrategyFrequency = entityRecoveryConstantRateStrategyFrequency;
            EntityRecoveryConstantRateStrategyNumberOfEntities = entityRecoveryConstantRateStrategyNumberOfEntities;
            CoordinatorStateWriteMajorityPlus = coordinatorStateWriteMajorityPlus;
            CoordinatorStateReadMajorityPlus = coordinatorStateReadMajorityPlus;
        }

        [Obsolete("Use the ClusterShardingSettings factory methods or the constructor including coordinatorStateWriteMajorityPlus and coordinatorStateReadMajorityPlus instead")]
        public TuningParameters(
            TimeSpan coordinatorFailureBackoff,
            TimeSpan retryInterval,
            int bufferSize,
            TimeSpan handOffTimeout,
            TimeSpan shardStartTimeout,
            TimeSpan shardFailureBackoff,
            TimeSpan entityRestartBackoff,
            TimeSpan rebalanceInterval,
            int snapshotAfter,
            int keepNrOfBatches,
            int leastShardAllocationRebalanceThreshold,
            int leastShardAllocationMaxSimultaneousRebalance,
            TimeSpan waitingForStateTimeout,
            TimeSpan updatingStateTimeout,
            string entityRecoveryStrategy,
            TimeSpan entityRecoveryConstantRateStrategyFrequency,
            int entityRecoveryConstantRateStrategyNumberOfEntities) 
            : this (
                coordinatorFailureBackoff,
                retryInterval,
                bufferSize,
                handOffTimeout,
                shardStartTimeout,
                shardFailureBackoff,
                entityRestartBackoff,
                rebalanceInterval,
                snapshotAfter,
                keepNrOfBatches,
                leastShardAllocationRebalanceThreshold,
                leastShardAllocationMaxSimultaneousRebalance,
                waitingForStateTimeout,
                updatingStateTimeout,
                entityRecoveryStrategy,
                entityRecoveryConstantRateStrategyFrequency,
                entityRecoveryConstantRateStrategyNumberOfEntities,
                5,
                5
            ) { }

        [Obsolete("Use the ClusterShardingSettings factory methods or the full constructor instead")]
        public TuningParameters(
            TimeSpan coordinatorFailureBackoff,
            TimeSpan retryInterval,
            int bufferSize,
            TimeSpan handOffTimeout,
            TimeSpan shardStartTimeout,
            TimeSpan shardFailureBackoff,
            TimeSpan entityRestartBackoff,
            TimeSpan rebalanceInterval,
            int snapshotAfter,
            int leastShardAllocationRebalanceThreshold,
            int leastShardAllocationMaxSimultaneousRebalance,
            TimeSpan waitingForStateTimeout,
            TimeSpan updatingStateTimeout,
            string entityRecoveryStrategy,
            TimeSpan entityRecoveryConstantRateStrategyFrequency,
            int entityRecoveryConstantRateStrategyNumberOfEntities) 
            : this(
                coordinatorFailureBackoff,
                retryInterval,
                bufferSize,
                handOffTimeout,
                shardStartTimeout,
                shardFailureBackoff,
                entityRestartBackoff,
                rebalanceInterval,
                snapshotAfter,
                2,
                leastShardAllocationRebalanceThreshold,
                leastShardAllocationMaxSimultaneousRebalance,
                waitingForStateTimeout,
                updatingStateTimeout,
                entityRecoveryStrategy,
                entityRecoveryConstantRateStrategyFrequency,
                entityRecoveryConstantRateStrategyNumberOfEntities
            ) { }

        [Obsolete("Use the ClusterShardingSettings factory methods or the full constructor instead")]
        public TuningParameters(
            TimeSpan coordinatorFailureBackoff,
            TimeSpan retryInterval,
            int bufferSize,
            TimeSpan handOffTimeout,
            TimeSpan shardStartTimeout,
            TimeSpan shardFailureBackoff,
            TimeSpan entityRestartBackoff,
            TimeSpan rebalanceInterval,
            int snapshotAfter,
            int leastShardAllocationRebalanceThreshold,
            int leastShardAllocationMaxSimultaneousRebalance,
            TimeSpan waitingForStateTimeout,
            TimeSpan updatingStateTimeout)
            : this(
                coordinatorFailureBackoff,
                retryInterval,
                bufferSize,
                handOffTimeout,
                shardStartTimeout,
                shardFailureBackoff,
                entityRestartBackoff,
                rebalanceInterval,
                snapshotAfter,
                2,
                leastShardAllocationRebalanceThreshold,
                leastShardAllocationMaxSimultaneousRebalance,
                waitingForStateTimeout,
                updatingStateTimeout,
                "all",
                TimeSpan.FromMilliseconds(100),
                5
            )
        { }
    }

    public enum StateStoreMode
    {
        Persistence,
        DData
    }

    /// <summary>
    /// TBD
    /// </summary>
    [Serializable]
    public sealed class ClusterShardingSettings : INoSerializationVerificationNeeded
    {
        private const string StateStoreModePersistence = "persistence";
        private const string StateStoreModeDData = "ddata";

        /// <summary>
        /// Create settings from the default configuration `akka.cluster.sharding`.
        /// </summary>
        /// <param name="system">TBD</param>
        /// <returns>TBD</returns>
        public static ClusterShardingSettings Create(ActorSystem system)
            => Apply(system);

        /// <summary>
        /// Create settings from the default configuration `akka.cluster.sharding`.
        /// </summary>
        /// <param name="system"></param>
        /// <returns></returns>
        public static ClusterShardingSettings Apply(ActorSystem system)
            => Apply(system.Settings.Config.GetConfig("akka.cluster.sharding"));

        /// <summary>
        /// Create settings from a configuration with the same layout as
        /// the default configuration `akka.cluster.sharding`.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static ClusterShardingSettings Apply(Config config)
            => new ClusterShardingSettings(config);

        /// <summary>
        /// Specifies that this entity type requires cluster nodes with a specific role.
        /// If the role is not specified all nodes in the cluster are used.
        /// </summary>
        public string Role { get; }

        /// <summary>
        /// True if active entity actors shall be automatically restarted upon <see cref="Shard"/> restart.i.e.
        /// if the <see cref="Shard"/> is started on a different <see cref="ShardRegion"/> due to rebalance or crash.
        /// </summary>
        public bool RememberEntities { get; }

        /// <summary>
        /// Absolute path to the journal plugin configuration entity that is to be used for the internal
        /// persistence of ClusterSharding.If not defined the default journal plugin is used. Note that
        /// this is not related to persistence used by the entity actors.
        /// </summary>
        public string JournalPluginId { get; }

        /// <summary>
        /// Absolute path to the snapshot plugin configuration entity that is to be used for the internal persistence
        /// of ClusterSharding. If not defined the default snapshot plugin is used.Note that this is not related
        /// to persistence used by the entity actors.
        /// </summary>
        public string SnapshotPluginId { get; }

        public StateStoreMode StateStoreMode { get; }

        /// <summary>
        /// Passivate entities that have not received any message in this interval.
        /// Note that only messages sent through sharding are counted, so direct messages
        /// to the <see cref="IActorRef"/> of the actor or messages that it sends to itself are not counted as activity.
        /// Use 0 to disable automatic passivation. It is always disabled if `RememberEntities` is enabled.
        /// </summary>
        public TimeSpan PassivateIdleEntityAfter { get; }

        public TimeSpan ShardRegionQueryTimeout { get; }

        /// <summary>
        /// Additional tuning parameters, see descriptions in reference.conf
        /// </summary>
        public TuningParameters TuningParameters { get; }

        /// <summary>
        /// TBD
        /// </summary>
        public ClusterSingletonManagerSettings CoordinatorSingletonSettings { get; }

        /// <summary>
        /// TBD
        /// </summary>
        public LeaseUsageSettings LeaseSettings { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="config">TBD</param>
        /// <returns>TBD</returns>
        public static ClusterShardingSettings Create(Config config)
            => new ClusterShardingSettings(config);

        public ClusterShardingSettings(Config config)
        {
            if (config.IsNullOrEmpty())
                throw ConfigurationException.NullOrEmptyConfig<ClusterShardingSettings>("akka.cluster.sharding");

            var singletonConfig = config.GetConfig("coordinator-singleton");
            if(singletonConfig.IsNullOrEmpty())
                throw ConfigurationException.NullOrEmptyConfig<ClusterShardingSettings>("akka.cluster.sharding.coordinator-singleton");

            int ConfigMajorityPlus(string p)
            {
                if (config.GetString(p).Equals("all", StringComparison.InvariantCultureIgnoreCase))
                    return int.MaxValue;
                return config.GetInt(p);
            }

            var tuningParameters = new TuningParameters(
                coordinatorFailureBackoff: config.GetTimeSpan("coordinator-failure-backoff"),
                retryInterval: config.GetTimeSpan("retry-interval"),
                bufferSize: config.GetInt("buffer-size"),
                handOffTimeout: config.GetTimeSpan("handoff-timeout"),
                shardStartTimeout: config.GetTimeSpan("shard-start-timeout"),
                shardFailureBackoff: config.GetTimeSpan("shard-failure-backoff"),
                entityRestartBackoff: config.GetTimeSpan("entity-restart-backoff"),
                rebalanceInterval: config.GetTimeSpan("rebalance-interval"),
                snapshotAfter: config.GetInt("snapshot-after"),
                keepNrOfBatches: config.GetInt("keep-nr-of-batches"),
                leastShardAllocationRebalanceThreshold: config.GetInt("least-shard-allocation-strategy.rebalance-threshold"),
                leastShardAllocationMaxSimultaneousRebalance: config.GetInt("least-shard-allocation-strategy.max-simultaneous-rebalance"),
                waitingForStateTimeout: config.GetTimeSpan("waiting-for-state-timeout"),
                updatingStateTimeout: config.GetTimeSpan("updating-state-timeout"),
                entityRecoveryStrategy: config.GetString("entity-recovery-strategy"),
                entityRecoveryConstantRateStrategyFrequency: config.GetTimeSpan("entity-recovery-constant-rate-strategy.frequency"),
                entityRecoveryConstantRateStrategyNumberOfEntities: config.GetInt("entity-recovery-constant-rate-strategy.number-of-entities"),
                coordinatorStateWriteMajorityPlus: ConfigMajorityPlus("coordinator-state.write-majority-plus"),
                coordinatorStateReadMajorityPlus: ConfigMajorityPlus("coordinator-state.read-majority-plus"));

            var coordinatorSingletonSettings = ClusterSingletonManagerSettings.Create(singletonConfig);

            var usePassivateIdle = config.GetString("passivate-idle-entity-after").ToLowerInvariant();
            var passivateIdleAfter =
                usePassivateIdle.Equals("off") ||
                usePassivateIdle.Equals("false") ||
                usePassivateIdle.Equals("no")
                    ? TimeSpan.Zero
                    : config.GetTimeSpan("passivate-idle-entity-after");

            LeaseUsageSettings lease = null;
            var leaseConfigPath = config.GetString("use-lease");
            if (!string.IsNullOrEmpty(leaseConfigPath))
                lease = new LeaseUsageSettings(leaseConfigPath, config.GetTimeSpan("lease-retry-interval"));

            Role = config.GetString("role", null);
            if (Role == string.Empty) Role = null;

            RememberEntities = config.GetBoolean("remember-entities");
            JournalPluginId = config.GetString("journal-plugin-id");
            SnapshotPluginId = config.GetString("snapshot-plugin-id");

            var stateStoreMode = config.GetString("state-store-mode").ToLowerInvariant();
            if(stateStoreMode != StateStoreModePersistence && stateStoreMode != StateStoreModeDData)
                throw new ConfigurationException($"Unknown 'state-store-mode' [{stateStoreMode}], valid values are '{StateStoreModeDData}' or '{StateStoreModePersistence}'");
            StateStoreMode = (StateStoreMode)Enum.Parse(typeof(StateStoreMode), stateStoreMode, ignoreCase: true);

            PassivateIdleEntityAfter = passivateIdleAfter;
            ShardRegionQueryTimeout = config.GetTimeSpan("shard-region-query-timeout");
            // TuningParameters =
            CoordinatorSingletonSettings = coordinatorSingletonSettings;
            LeaseSettings = lease;
        }

        [Obsolete("Use the ClusterShardingSettings factory methods or the constructor including shardRegionQueryTimeout instead")]
        public ClusterShardingSettings(
            string role,
            bool rememberEntities,
            string journalPluginId,
            string snapshotPluginId,
            StateStoreMode stateStoreMode,
            TimeSpan passivateIdleEntityAfter,
            TuningParameters tuningParameters,
            ClusterSingletonManagerSettings coordinatorSingletonSettings,
            LeaseUsageSettings leaseSettings)
            : this(
                role, 
                rememberEntities, 
                journalPluginId, 
                snapshotPluginId,
                stateStoreMode,
                passivateIdleEntityAfter, 
                TimeSpan.FromSeconds(3),
                tuningParameters, 
                coordinatorSingletonSettings, 
                leaseSettings)
        { }

        [Obsolete("Use the ClusterShardingSettings factory methods or the constructor including shardRegionQueryTimeout instead")]
        public ClusterShardingSettings(
            string role,
            bool rememberEntities,
            string journalPluginId,
            string snapshotPluginId,
            StateStoreMode stateStoreMode,
            TimeSpan passivateIdleEntityAfter,
            TuningParameters tuningParameters,
            ClusterSingletonManagerSettings coordinatorSingletonSettings)
            : this(
                role,
                rememberEntities,
                journalPluginId,
                snapshotPluginId,
                stateStoreMode,
                passivateIdleEntityAfter,
                tuningParameters,
                coordinatorSingletonSettings,
                null)
        { }

        [Obsolete("Use the ClusterShardingSettings factory methods or the constructor including passivateIdleEntityAfter instead")]
        public ClusterShardingSettings(
            string role,
            bool rememberEntities,
            string journalPluginId,
            string snapshotPluginId,
            StateStoreMode stateStoreMode,
            TuningParameters tuningParameters,
            ClusterSingletonManagerSettings coordinatorSingletonSettings)
            : this(
                role,
                rememberEntities,
                journalPluginId,
                snapshotPluginId,
                stateStoreMode,
                TimeSpan.Zero,
                tuningParameters,
                coordinatorSingletonSettings)
        { }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="role">specifies that this entity type requires cluster nodes with a specific role.
        ///   If the role is not specified all nodes in the cluster are used.</param>
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
        /// <param name="leaseSettings">TBD</param>
        public ClusterShardingSettings(
            string role,
            bool rememberEntities,
            string journalPluginId,
            string snapshotPluginId,
            StateStoreMode stateStoreMode,
            TimeSpan passivateIdleEntityAfter,
            TimeSpan shardRegionQueryTimeout,
            TuningParameters tuningParameters,
            ClusterSingletonManagerSettings coordinatorSingletonSettings,
            LeaseUsageSettings leaseSettings)
        {
            Role = role;
            RememberEntities = rememberEntities;
            JournalPluginId = journalPluginId;
            SnapshotPluginId = snapshotPluginId;
            PassivateIdleEntityAfter = passivateIdleEntityAfter;
            ShardRegionQueryTimeout = shardRegionQueryTimeout;
            StateStoreMode = stateStoreMode;
            TuningParameters = tuningParameters;
            CoordinatorSingletonSettings = coordinatorSingletonSettings;
            LeaseSettings = leaseSettings;
        }

        /// <summary>
        /// If true, this node should run the shard region, otherwise just a shard proxy should started on this node.
        /// </summary>
        /// <param name="cluster"></param>
        /// <returns></returns>
        internal bool ShouldHostShard(Cluster cluster)
        {
            return string.IsNullOrEmpty(Role) || cluster.SelfRoles.Contains(Role);
        }

        /// <summary>
        /// If true, idle entities should be passivated if they have not received any message by this interval, otherwise it is not enabled.
        /// </summary>
        internal bool ShouldPassivateIdleEntities => PassivateIdleEntityAfter > TimeSpan.Zero && !RememberEntities;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="role">TBD</param>
        /// <returns>TBD</returns>
        public ClusterShardingSettings WithRole(string role)
        {
            return Copy(role: role);
        }

        public ClusterShardingSettings WithRole(Option<string> role)
        {
            return Copy(role: role.Value);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="rememberEntities">TBD</param>
        /// <returns>TBD</returns>
        public ClusterShardingSettings WithRememberEntities(bool rememberEntities)
        {
            return Copy(rememberEntities: rememberEntities);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="journalPluginId">TBD</param>
        /// <returns>TBD</returns>
        public ClusterShardingSettings WithJournalPluginId(string journalPluginId)
        {
            return Copy(journalPluginId: journalPluginId ?? string.Empty);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="snapshotPluginId">TBD</param>
        /// <returns>TBD</returns>
        public ClusterShardingSettings WithSnapshotPluginId(string snapshotPluginId)
        {
            return Copy(snapshotPluginId: snapshotPluginId ?? string.Empty);
        }

        public ClusterShardingSettings WithStateStoreMode(StateStoreMode mode)
        {
            return Copy(stateStoreMode: mode);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="tuningParameters">TBD</param>
        /// <exception cref="ArgumentNullException">
        /// This exception is thrown when the specified <paramref name="tuningParameters"/> is undefined.
        /// </exception>
        /// <returns>TBD</returns>
        public ClusterShardingSettings WithTuningParameters(TuningParameters tuningParameters)
        {
            if (tuningParameters == null)
                throw new ArgumentNullException(nameof(tuningParameters), $"ClusterShardingSettings requires {nameof(tuningParameters)} to be provided");

            return Copy(tuningParameters: tuningParameters);
        }

        public ClusterShardingSettings WithPassivateIdleAfter(TimeSpan duration)
        {
            return Copy(passivateIdleAfter: duration);
        }

        public ClusterShardingSettings WithShardRegionQueryTimeout(TimeSpan duration)
            => Copy(shardRegionQueryTimeout: duration);

        public ClusterShardingSettings WithLeaseSettings(LeaseUsageSettings leaseSettings)
        {
            return Copy(leaseSettings: leaseSettings);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="coordinatorSingletonSettings">TBD</param>
        /// <exception cref="ArgumentNullException">
        /// This exception is thrown when the specified <paramref name="coordinatorSingletonSettings"/> is undefined.
        /// </exception>
        /// <returns>TBD</returns>
        public ClusterShardingSettings WithCoordinatorSingletonSettings(ClusterSingletonManagerSettings coordinatorSingletonSettings)
        {
            if (coordinatorSingletonSettings == null)
                throw new ArgumentNullException(nameof(coordinatorSingletonSettings), $"ClusterShardingSettings requires {nameof(coordinatorSingletonSettings)} to be provided");

            return Copy(coordinatorSingletonSettings: coordinatorSingletonSettings);
        }

        private ClusterShardingSettings Copy(
            Option<string> role = default,
            bool? rememberEntities = null,
            string journalPluginId = null,
            string snapshotPluginId = null,
            TimeSpan? passivateIdleAfter = null,
            TimeSpan? shardRegionQueryTimeout = null,
            StateStoreMode? stateStoreMode = null,
            TuningParameters tuningParameters = null,
            ClusterSingletonManagerSettings coordinatorSingletonSettings = null,
            Option<LeaseUsageSettings> leaseSettings = default)
        {
            return new ClusterShardingSettings(
                role: role.HasValue ? role.Value : Role,
                rememberEntities: rememberEntities ?? RememberEntities,
                journalPluginId: journalPluginId ?? JournalPluginId,
                snapshotPluginId: snapshotPluginId ?? SnapshotPluginId,
                passivateIdleEntityAfter: passivateIdleAfter ?? PassivateIdleEntityAfter,
                shardRegionQueryTimeout: shardRegionQueryTimeout ?? ShardRegionQueryTimeout,
                stateStoreMode: stateStoreMode ?? StateStoreMode,
                tuningParameters: tuningParameters ?? TuningParameters,
                coordinatorSingletonSettings: coordinatorSingletonSettings ?? CoordinatorSingletonSettings,
                leaseSettings: leaseSettings.HasValue ? leaseSettings.Value : LeaseSettings);
        }
    }
}

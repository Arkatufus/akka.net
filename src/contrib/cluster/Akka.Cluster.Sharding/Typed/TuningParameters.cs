using System;
using System.Collections.Generic;
using System.Text;
using Akka.Configuration;
using ClassicTuningParameters = Akka.Cluster.Sharding.TuningParameters;

namespace Akka.Cluster.Sharding.Typed
{
    public sealed class TuningParameters
    {
        public int BufferSize { get; }
        public TimeSpan CoordinatorFailureBackoff { get; }
        public TimeSpan EntityRecoveryConstantRateStrategyFrequency { get; }
        public int EntityRecoveryConstantRateStrategyNumberOfEntities { get; }
        public string EntityRecoveryStrategy { get; }
        public TimeSpan EntityRestartBackoff { get; }
        public TimeSpan HandOffTimeout { get; }
        public int KeepNrOfBatches { get; }
        public int LeastShardAllocationMaxSimultaneousRebalance { get; }
        public int LeastShardAllocationRebalanceThreshold { get; }
        public TimeSpan RebalanceInterval { get; }
        public TimeSpan RetryInterval { get; }
        public TimeSpan ShardFailureBackoff { get; }
        public TimeSpan ShardStartTimeout { get; }
        public int SnapshotAfter { get; }
        public TimeSpan UpdatingStateTimeout { get; }
        public TimeSpan WaitingForStateTimeout { get; }
        public int CoordinatorStateWriteMajorityPlus { get; }
        public int CoordinatorStateReadMajorityPlus { get; }

        public TuningParameters(
            int bufferSize,
            TimeSpan coordinatorFailureBackoff,
            TimeSpan entityRecoveryConstantRateStrategyFrequency,
            int entityRecoveryConstantRateStrategyNumberOfEntities,
            string entityRecoveryStrategy,
            TimeSpan entityRestartBackoff,
            TimeSpan handOffTimeout,
            int keepNrOfBatches,
            int leastShardAllocationMaxSimultaneousRebalance,
            int leastShardAllocationRebalanceThreshold,
            TimeSpan rebalanceInterval,
            TimeSpan retryInterval,
            TimeSpan shardFailureBackoff,
            TimeSpan shardStartTimeout,
            int snapshotAfter,
            TimeSpan updatingStateTimeout,
            TimeSpan waitingForStateTimeout,
            int coordinatorStateWriteMajorityPlus,
            int coordinatorStateReadMajorityPlus
        )
        {
            BufferSize = bufferSize;
            CoordinatorFailureBackoff = coordinatorFailureBackoff;
            EntityRecoveryConstantRateStrategyFrequency = entityRecoveryConstantRateStrategyFrequency;
            EntityRecoveryConstantRateStrategyNumberOfEntities = entityRecoveryConstantRateStrategyNumberOfEntities;
            EntityRecoveryStrategy = entityRecoveryStrategy;
            EntityRestartBackoff = entityRestartBackoff;
            HandOffTimeout = handOffTimeout;
            KeepNrOfBatches = keepNrOfBatches;
            LeastShardAllocationMaxSimultaneousRebalance = leastShardAllocationMaxSimultaneousRebalance;
            LeastShardAllocationRebalanceThreshold = leastShardAllocationRebalanceThreshold;
            RebalanceInterval = rebalanceInterval;
            RetryInterval = retryInterval;
            ShardFailureBackoff = shardFailureBackoff;
            ShardStartTimeout = shardStartTimeout;
            SnapshotAfter = snapshotAfter;
            UpdatingStateTimeout = updatingStateTimeout;
            WaitingForStateTimeout = waitingForStateTimeout;
            CoordinatorStateWriteMajorityPlus = coordinatorStateWriteMajorityPlus;
            CoordinatorStateReadMajorityPlus = coordinatorStateReadMajorityPlus;

            if (EntityRecoveryStrategy != "all" && EntityRecoveryStrategy != "constant")
                throw new ConfigurationException($"Unknown 'entity-recovery-strategy' [{EntityRecoveryStrategy}], valid values are 'all' or 'constant'");
        }

        public TuningParameters(ClassicTuningParameters classic) : this
            (
                bufferSize: classic.BufferSize,
                coordinatorFailureBackoff: classic.CoordinatorFailureBackoff,
                retryInterval: classic.RetryInterval,
                handOffTimeout: classic.HandOffTimeout,
                shardStartTimeout: classic.ShardStartTimeout,
                shardFailureBackoff: classic.ShardFailureBackoff,
                entityRestartBackoff: classic.EntityRestartBackoff,
                rebalanceInterval: classic.RebalanceInterval,
                snapshotAfter: classic.SnapshotAfter,
                keepNrOfBatches: classic.KeepNrOfBatches,
                leastShardAllocationRebalanceThreshold: classic.LeastShardAllocationRebalanceThreshold, // TODO extract it a bit
                leastShardAllocationMaxSimultaneousRebalance: classic.LeastShardAllocationMaxSimultaneousRebalance,
                waitingForStateTimeout: classic.WaitingForStateTimeout,
                updatingStateTimeout: classic.UpdatingStateTimeout,
                entityRecoveryStrategy: classic.EntityRecoveryStrategy,
                entityRecoveryConstantRateStrategyFrequency: classic.EntityRecoveryConstantRateStrategyFrequency,
                entityRecoveryConstantRateStrategyNumberOfEntities: classic.EntityRecoveryConstantRateStrategyNumberOfEntities,
                coordinatorStateWriteMajorityPlus: classic.CoordinatorStateWriteMajorityPlus,
                coordinatorStateReadMajorityPlus: classic.CoordinatorStateReadMajorityPlus
            )
        { }

        public TuningParameters WithBufferSize(int value) 
            => Copy(bufferSize: value);

        public TuningParameters WithCoordinatorFailureBackoff(TimeSpan value) 
            => Copy(coordinatorFailureBackoff: value);

        public TuningParameters WithEntityRecoveryConstantRateStrategyFrequency(TimeSpan value) 
            => Copy(entityRecoveryConstantRateStrategyFrequency: value);

        public TuningParameters WithEntityRecoveryConstantRateStrategyNumberOfEntities(int value) 
            => Copy(entityRecoveryConstantRateStrategyNumberOfEntities: value);

        public TuningParameters WithEntityRecoveryStrategy(string value) 
            => Copy(entityRecoveryStrategy: value);

        public TuningParameters WithEntityRestartBackoff(TimeSpan value) 
            => Copy(entityRestartBackoff: value);

        public TuningParameters WithHandOffTimeout(TimeSpan value) 
            => Copy(handOffTimeout: value);

        public TuningParameters WithKeepNrOfBatches(int value) 
            => Copy(keepNrOfBatches: value);

        public TuningParameters WithLeastShardAllocationMaxSimultaneousRebalance(int value) 
            => Copy(leastShardAllocationMaxSimultaneousRebalance: value);

        public TuningParameters WithLeastShardAllocationRebalanceThreshold(int value) 
            => Copy(leastShardAllocationRebalanceThreshold: value);

        public TuningParameters WithRebalanceInterval(TimeSpan value) 
            => Copy(rebalanceInterval: value);

        public TuningParameters WithRetryInterval(TimeSpan value) 
            => Copy(retryInterval: value);

        public TuningParameters WithShardFailureBackoff(TimeSpan value) 
            => Copy(shardFailureBackoff: value);

        public TuningParameters WithShardStartTimeout(TimeSpan value) 
            => Copy(shardStartTimeout: value);

        public TuningParameters WithSnapshotAfter(int value) 
            => Copy(snapshotAfter: value);

        public TuningParameters WithUpdatingStateTimeout(TimeSpan value) 
            => Copy(updatingStateTimeout: value);

        public TuningParameters WithWaitingForStateTimeout(TimeSpan value) 
            => Copy(waitingForStateTimeout: value);

        public TuningParameters WithCoordinatorStateWriteMajorityPlus(int value) 
            => Copy(coordinatorStateWriteMajorityPlus: value);

        public TuningParameters WithCoordinatorStateReadMajorityPlus(int value) 
            => Copy(coordinatorStateReadMajorityPlus: value);

        private TuningParameters Copy(
            int? bufferSize = null,
            TimeSpan? coordinatorFailureBackoff = null,
            TimeSpan? entityRecoveryConstantRateStrategyFrequency = null,
            int? entityRecoveryConstantRateStrategyNumberOfEntities = null,
            string entityRecoveryStrategy = null,
            TimeSpan? entityRestartBackoff = null,
            TimeSpan? handOffTimeout = null,
            int? keepNrOfBatches = null,
            int? leastShardAllocationMaxSimultaneousRebalance = null,
            int? leastShardAllocationRebalanceThreshold = null,
            TimeSpan? rebalanceInterval = null,
            TimeSpan? retryInterval = null,
            TimeSpan? shardFailureBackoff = null,
            TimeSpan? shardStartTimeout = null,
            int? snapshotAfter = null,
            TimeSpan? updatingStateTimeout = null,
            TimeSpan? waitingForStateTimeout = null,
            int? coordinatorStateWriteMajorityPlus = null,
            int? coordinatorStateReadMajorityPlus = null
        )
        {
            return new TuningParameters(
                    bufferSize ?? BufferSize,
                    coordinatorFailureBackoff ?? CoordinatorFailureBackoff,
                    entityRecoveryConstantRateStrategyFrequency ?? EntityRecoveryConstantRateStrategyFrequency,
                    entityRecoveryConstantRateStrategyNumberOfEntities ?? EntityRecoveryConstantRateStrategyNumberOfEntities,
                    entityRecoveryStrategy ?? EntityRecoveryStrategy,
                    entityRestartBackoff ?? EntityRestartBackoff,
                    handOffTimeout ?? HandOffTimeout,
                    keepNrOfBatches ?? KeepNrOfBatches,
                    leastShardAllocationMaxSimultaneousRebalance ?? LeastShardAllocationMaxSimultaneousRebalance,
                    leastShardAllocationRebalanceThreshold ?? LeastShardAllocationRebalanceThreshold,
                    rebalanceInterval ?? RebalanceInterval,
                    retryInterval ?? RetryInterval,
                    shardFailureBackoff ?? ShardFailureBackoff,
                    shardStartTimeout ?? ShardStartTimeout,
                    snapshotAfter ?? SnapshotAfter,
                    updatingStateTimeout ?? UpdatingStateTimeout,
                    waitingForStateTimeout ?? WaitingForStateTimeout,
                    coordinatorStateWriteMajorityPlus ?? CoordinatorStateWriteMajorityPlus,
                    coordinatorStateReadMajorityPlus ?? CoordinatorStateReadMajorityPlus
                );
        }

        public override string ToString()
            =>
                $"TuningParameters({BufferSize},{CoordinatorFailureBackoff},{EntityRecoveryConstantRateStrategyFrequency},{EntityRecoveryConstantRateStrategyNumberOfEntities},{EntityRecoveryStrategy},{EntityRestartBackoff},{HandOffTimeout},{KeepNrOfBatches},{LeastShardAllocationMaxSimultaneousRebalance},{LeastShardAllocationRebalanceThreshold},{RebalanceInterval},{RetryInterval},{ShardFailureBackoff},{ShardStartTimeout},{SnapshotAfter},{UpdatingStateTimeout},{WaitingForStateTimeout},{CoordinatorStateReadMajorityPlus},{CoordinatorStateReadMajorityPlus})";
    }
}

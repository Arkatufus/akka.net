using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.Event;
using Akka.Persistence.TestKit;
using Akka.TestKit;
using Akka.TestKit.Xunit2.Internals;
using Xunit;
using Xunit.Abstractions;
using FluentAssertions;

namespace Akka.Persistence.Tests
{
    public class PersistentActorRecoveryTimeoutSpec2:PersistenceTestKit
    {
        private static Config Config 
        {
            get
            {
                var specString = $@"
akka.loglevel = DEBUG
akka.stdout-loglevel = DEBUG
akka.debug {{
    receive = on
    autoreceive = on
    lifecycle = on
    fsm = on
    event-stream = on
    unhandled = on
    router-misconfiguration = on
}}";

                return ConfigurationFactory.ParseString(specString);
            }
        }

        private readonly TestProbe _probe;

        public PersistentActorRecoveryTimeoutSpec2(ITestOutputHelper output):base(Config, "BugTest", output)
        {
            _probe = CreateTestProbe();
        }

        [Fact]
        public async Task Bug4265_Persistent_actor_stuck_with_RecoveryTimedOutException_after_circuit_breaker_opens()
        {
            var journalProbe = new JournalInspector(Output);
            journalProbe.Next = JournalInterceptors.Noop.Instance;
            await Journal.OnWrite.SetInterceptorAsync(journalProbe);
            await Journal.OnRecovery.SetInterceptorAsync(journalProbe);

            var snapshotProbe = new SnapshotInspector(Output);
            snapshotProbe.Next = SnapshotStoreInterceptors.Noop.Instance;
            await Snapshots.OnSave.SetInterceptorAsync(snapshotProbe);
            await Snapshots.OnLoad.SetInterceptorAsync(snapshotProbe);

            var actor = ActorOf(() => new PersistActor(_probe));
            Watch(actor);
            _probe.ExpectMsg("ReplaySuccess");
            _probe.ExpectMsg<RecoveryCompleted>();

            actor.Tell(new PersistActor.WriteJournal("1"), TestActor);
            _probe.ExpectMsg("1");

            actor.Tell(new PersistActor.WriteJournal("2"), TestActor);
            _probe.ExpectMsg("2");
            // Journal should contain 1, 2

            actor.Tell(new PersistActor.SaveSnapshotMessage("snapshot"), TestActor);
            _probe.ExpectMsg<SaveSnapshotSuccess>();
            // snapshot should be "snapshot"

            await actor.GracefulStop(TimeSpan.FromSeconds(3));
            ExpectTerminated(actor);

            await WithJournalRecovery(
                recovery => recovery.Fail(),
                () =>
                {
                    actor = ActorOf(() => new PersistActor(_probe));
                    _probe.ExpectMsg<SnapshotOffer>().Snapshot.Should().Be("snapshot");
                    _probe.ExpectMsg("ReplaySuccess");
                    _probe.ExpectMsg<RecoveryCompleted>();
                });
        }

    }

    internal class JournalInspector : IJournalInterceptor
    {
        private readonly ITestOutputHelper _output;
        public IJournalInterceptor Next { get; set; }

        public JournalInspector(ITestOutputHelper output)
        {
            _output = output;
        }

        public async Task InterceptAsync(IPersistentRepresentation message)
        {
            _output.WriteLine($"[JournalInspector] message: {message}, payload: {message.Payload}");
            await Next.InterceptAsync(message);
        }
    }

    internal class SnapshotInspector : ISnapshotStoreInterceptor
    {
        private readonly ITestOutputHelper _output;
        public ISnapshotStoreInterceptor Next { get; set; }

        public SnapshotInspector(ITestOutputHelper output)
        {
            _output = output;
        }

        public async Task InterceptAsync(string persistenceId, SnapshotSelectionCriteria criteria)
        {
            _output.WriteLine($"[SnapshotInspector] Id: {persistenceId}, criteria: {criteria}");
            await Next.InterceptAsync(persistenceId, criteria);
        }
    }

    internal class PersistActor : UntypedPersistentActor
    {
        public event EventHandler<SnapshotOffer> SnapshotOffered;
        public event EventHandler<RecoveryCompleted> RecoveryCompleted;

        private readonly ILoggingAdapter _log;

        public PersistActor(IActorRef probe)
        {
            _probe = probe;
            _log = Context.GetLogger();
        }

        private readonly IActorRef _probe;

        public override string PersistenceId => "foo";

        protected override void OnCommand(object message)
        {
            switch (message)
            {
                case WriteJournal w:
                    Persist(w.Data, msg => _probe.Tell(msg));
                    break;

                case SaveSnapshotMessage s:
                    SaveSnapshot(s.Data);
                    return;

                case "load":
                    LoadSnapshot(PersistenceId, SnapshotSelectionCriteria.Latest, 3);
                    break;

                case SaveSnapshotSuccess _:
                case SaveSnapshotFailure _:
                case DeleteSnapshotSuccess _:
                case DeleteSnapshotFailure _:
                case DeleteSnapshotsSuccess _:
                case DeleteSnapshotsFailure _:
                    _probe.Tell(message);
                    return;

                default:
                    return;
            }
        }

        protected override void OnRecover(object message)
        {
            _log.Debug($"[OnRecover] Message: {message}");
            switch (message)
            {
                case SnapshotOffer offer:
                    SnapshotOffered?.Invoke(this, offer);
                    _probe.Tell(offer);
                    break;
                case RecoveryCompleted complete:
                    RecoveryCompleted?.Invoke(this, complete);
                    _probe.Tell(complete);
                    break;
            }
        }

        protected override void OnRecoveryFailure(Exception reason, object message = null)
        {
            _probe.Tell(new RecoveryFailure(reason, message));
            base.OnRecoveryFailure(reason, message);
        }

        protected override void OnReplaySuccess()
        {
            _probe.Tell("ReplaySuccess");
            base.OnReplaySuccess();
        }

        protected override void OnPersistFailure(Exception cause, object @event, long sequenceNr)
        {
            _probe.Tell("failure");
            base.OnPersistFailure(cause, @event, sequenceNr);
        }

        protected override void OnPersistRejected(Exception cause, object @event, long sequenceNr)
        {
            _probe.Tell("rejected");
            base.OnPersistRejected(cause, @event, sequenceNr);
        }

        public class WriteJournal
        {
            public WriteJournal(string data)
            {
                Data = data;
            }

            public string Data { get; }
        }

        public class SaveSnapshotMessage
        {
            public SaveSnapshotMessage(string data)
            {
                Data = data;
            }

            public string Data { get; }
        }

        public class RecoveryFailure
        {
            public RecoveryFailure(Exception reason, object message)
            {
                Reason = reason;
                Message = message;
            }

            public Exception Reason { get; }
            public object Message { get; }
        }
    }

}

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.Persistence.Sqlite.Journal;
using Akka.Persistence.TestKit;

namespace Akka.Persistence.Sqlite.Tests.Internal.Journal
{
    public class TestSqliteJournal : SqliteJournal
    {
        private IJournalInterceptor _writeInterceptor = JournalInterceptors.Noop.Instance;
        private IJournalInterceptor _recoveryInterceptor = JournalInterceptors.Noop.Instance;

        public TestSqliteJournal(Config journalConfig) : base(journalConfig)
        {
        }

        protected override bool ReceivePluginInternal(object message)
        {
            switch (message)
            {
                case TestJournal.UseWriteInterceptor use:
                    _writeInterceptor = use.Interceptor;
                    Sender.Tell(TestJournal.Ack.Instance);
                    return true;

                case TestJournal.UseRecoveryInterceptor use:
                    _recoveryInterceptor = use.Interceptor;
                    Sender.Tell(TestJournal.Ack.Instance);
                    return true;

                default:
                    return base.ReceivePluginInternal(message);
            }
        }

        protected override async Task<IImmutableList<Exception>> WriteMessagesAsync(IEnumerable<AtomicWrite> messages)
        {
            var internalMessages = messages.ToArray();
            var exceptions = new List<Exception>();
            var exceptionThrown = false;
            foreach (var w in internalMessages)
            {
                foreach (var p in (IEnumerable<IPersistentRepresentation>)w.Payload)
                {
                    try
                    {
                        await _writeInterceptor.InterceptAsync(p);
                        exceptions.Add(null);
                    }
                    catch (TestJournalRejectionException rejected)
                    {
                        // i.e. problems with data: corrupted data-set, problems in serialization, constraints, etc.
                        exceptions.Add(rejected);
                        exceptionThrown = true;
                    }
                    // TestJournalFailureException: data-store problems: network, invalid credentials, etc.
                    // Auto throw.
                }
            }

            return exceptionThrown ? exceptions.ToImmutableList() : await base.WriteMessagesAsync(internalMessages);
        }

        public override async Task ReplayMessagesAsync(IActorContext context, string persistenceId, long fromSequenceNr, long toSequenceNr, long max, Action<IPersistentRepresentation> recoveryCallback)
        {
            await base.ReplayMessagesAsync(context, persistenceId, fromSequenceNr, toSequenceNr, max, msg =>
            {
                _recoveryInterceptor.InterceptAsync(msg)
                    .ContinueWith(task => recoveryCallback.Invoke(msg))
                    .Wait();
            });
        }

        /// <summary>
        ///     Create proxy object from journal actor reference which can alter behavior of journal.
        /// </summary>
        /// <remarks>
        ///     Journal actor must be of <see cref="TestJournal"/> type.
        /// </remarks>
        /// <param name="actor">Journal actor reference.</param>
        /// <returns>Proxy object to control <see cref="TestJournal"/>.</returns>
        public static ITestJournal FromRef(IActorRef actor)
        {
            return new TestSqliteJournalWrapper(actor);
        }

        public sealed class UseWriteInterceptor
        {
            public UseWriteInterceptor(IJournalInterceptor interceptor)
            {
                Interceptor = interceptor;
            }

            public IJournalInterceptor Interceptor { get; }
        }

        public sealed class UseRecoveryInterceptor
        {
            public UseRecoveryInterceptor(IJournalInterceptor interceptor)
            {
                Interceptor = interceptor;
            }

            public IJournalInterceptor Interceptor { get; }
        }

        public sealed class Ack
        {
            public static readonly TestJournal.Ack Instance = new TestJournal.Ack();
        }

        internal class TestSqliteJournalWrapper : ITestJournal
        {
            public TestSqliteJournalWrapper(IActorRef actor)
            {
                _actor = actor;
            }

            private readonly IActorRef _actor;

            public JournalWriteBehavior OnWrite => new JournalWriteBehavior(new JournalWriteBehaviorSetter(_actor));

            public JournalRecoveryBehavior OnRecovery => new JournalRecoveryBehavior(new JournalRecoveryBehaviorSetter(_actor));
        }

    }
}

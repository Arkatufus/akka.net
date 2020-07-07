using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Persistence.Sqlite.Journal;
using Akka.Persistence.TestKit;

namespace Akka.Persistence.Sqlite.Tests.Internal.Journal
{
    public abstract class TestBatchingSqliteJournal : BatchingSqliteJournal
    {
        private IBatchingSqlJournalInterceptor _interceptor = BatchingSqlJournalInterceptors.Noop.Instance;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="config">TBD</param>

        protected TestBatchingSqliteJournal(BatchingSqliteJournalSetup setup) : base(setup)
        {

        }

        protected override bool ReceivePluginInternal(object message)
        {
            switch (message)
            {
                case UseInterceptor use:
                    _interceptor = use.Interceptor;
                    Sender.Tell(Ack.Instance);
                    return true;

                default:
                    return base.ReceivePluginInternal(message);
            }
        }

        protected override async Task<BatchComplete> ExecuteChunk(RequestChunk chunk, IActorContext context)
        {
            foreach (var request in chunk.Requests)
            {
                try
                {
                    await _interceptor.InterceptAsync(request);
                }
                catch (TestJournalRejectionException rejected)
                {
                    // i.e. problems with data: corrupted data-set, problems in serialization, constraints, etc.
                    return new BatchComplete(chunk.ChunkId, chunk.Requests.Length, TimeSpan.FromSeconds(1), rejected);
                }
                // data-store problems: network, invalid credentials, etc., auto throw.
            }
            return await base.ExecuteChunk(chunk, context);
        }

        /// <summary>
        ///     Create proxy object from journal actor reference which can alter behavior of journal.
        /// </summary>
        /// <remarks>
        ///     Journal actor must be of <see cref="TestBatchingSqliteJournal{TConnection,TCommand}"/> type.
        /// </remarks>
        /// <param name="actor">Journal actor reference.</param>
        /// <returns>Proxy object to control <see cref="TestBatchingSqliteJournal{TConnection,TCommand}"/>.</returns>
        public static ITestBatchingSqlJournal FromRef(IActorRef actor)
        {
            return new TestBatchingSqlJournalWrapper(actor);
        }

        public sealed class UseInterceptor
        {
            public UseInterceptor(IBatchingSqlJournalInterceptor interceptor)
            {
                Interceptor = interceptor;
            }

            public IBatchingSqlJournalInterceptor Interceptor { get; }
        }

        public sealed class Ack
        {
            public static readonly Ack Instance = new Ack();
        }

        internal class TestBatchingSqlJournalWrapper : ITestBatchingSqlJournal
        {
            public TestBatchingSqlJournalWrapper(IActorRef actor)
            {
                _actor = actor;
            }

            private readonly IActorRef _actor;

            public BatchingSqlJournalBehavior OnBatching => new BatchingSqlJournalBehavior(new BatchingSqlJournalWriteBehaviorSetter(_actor));
        }

    }
}

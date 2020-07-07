//-----------------------------------------------------------------------
// <copyright file="JournalRecoveryBehavior.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2020 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2020 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Threading.Tasks;

namespace Akka.Persistence.Sqlite.Tests.Internal.Journal
{
    /// <summary>
    ///     Built-in Journal interceptors who will alter messages Recovery and/or Write of <see cref="TestBatchingSqliteJournal"/>.
    /// </summary>
    public class BatchingSqlJournalBehavior
    {
        internal BatchingSqlJournalBehavior(IBatchingSqlJournalBehaviorSetter setter)
        {
            this.Setter = setter;
        }

        private IBatchingSqlJournalBehaviorSetter Setter { get; }

        /// <summary>
        ///     Use custom, user defined interceptor.
        /// </summary>
        /// <param name="interceptor">User defined interceptor which implements <see cref="IBatchingSqlJournalInterceptor"/> interface.</param>
        /// <exception cref="ArgumentNullException">When <paramref name="interceptor"/> is <c>null</c>.</exception>
        public Task SetInterceptorAsync(IBatchingSqlJournalInterceptor interceptor)
        {
            if (interceptor == null) throw new ArgumentNullException(nameof(interceptor));

            return Setter.SetInterceptorAsync(interceptor);
        }

        /// <summary>
        ///     Pass all messages to journal without interfering.
        /// </summary>
        /// <remarks>
        ///     By using this interceptor <see cref="TestBatchingSqliteJournal"/> all journal operations will work like
        ///     in standard <see cref="Akka.Persistence.Journal.MemoryJournal"/>.
        /// </remarks>
        public Task Pass() => SetInterceptorAsync(BatchingSqlJournalInterceptors.Noop.Instance);

        /// <summary>
        ///     Delay passing all messages to journal by <paramref name="delay"/>.
        /// </summary>
        /// <remarks>
        ///     Each message will be delayed individually.
        /// </remarks>
        /// <param name="delay">Time by which recovery operation will be delayed.</param>
        /// <exception cref="ArgumentException">When <paramref name="delay"/> is less or equal to <see cref="TimeSpan.Zero"/>.</exception>
        public Task PassWithDelay(TimeSpan delay)
        {
            if (delay <= TimeSpan.Zero) throw new ArgumentException("Delay must be greater than zero", nameof(delay));

            return SetInterceptorAsync(new BatchingSqlJournalInterceptors.Delay(delay, BatchingSqlJournalInterceptors.Noop.Instance));
        }

        /// <summary>
        ///     Always fail all messages.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Journal will crash and <see cref="Eventsourced.OnPersistFailure">UntypedPersistentActor.OnPersistFailure</see> will be called on persistent actor.
        ///     </para>
        ///     <para>
        ///         Use this Journal behavior when it is needed to verify how well a persistent actor will handle network problems
        ///         and similar issues with underlying journal.
        ///     </para>
        /// </remarks>
        public Task Fail() => SetInterceptorAsync(BatchingSqlJournalInterceptors.Failure.Instance);

        /// <summary>
        ///     Fail message during processing message of type <typeparamref name="TMessage"/>.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Journal will crash and <see cref="Eventsourced.OnPersistFailure">UntypedPersistentActor.OnPersistFailure</see> will be called on persistent actor.
        ///     </para>
        ///     <para>
        ///         Use this Journal behavior when it is needed to verify how well a persistent actor will handle network problems
        ///         and similar issues with underlying journal.
        ///     </para>
        ///     <para>
        ///         <list type="bullet">
        ///             <item>If <typeparamref name="TMessage"/> is interface, journal will fail when message implements that interface.</item>
        ///             <item>If <typeparamref name="TMessage"/> is class, journal will fail when message can be assigned to <typeparamref name="TMessage"/>.</item>
        ///         </list>
        ///     </para>
        /// </remarks>
        /// <typeparam name="TMessage"></typeparam>
        public Task FailOnType<TMessage>() => FailOnType(typeof(TMessage));

        /// <summary>
        ///     Fail message during processing message of type <paramref name="messageType"/>.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Journal will crash and <see cref="Eventsourced.OnPersistFailure">UntypedPersistentActor.OnPersistFailure</see> will be called on persistent actor.
        ///     </para>
        ///     <para>
        ///         Use this Journal behavior when it is needed to verify how well a persistent actor will handle network problems
        ///         and similar issues with underlying journal.
        ///     </para>
        ///     <para>
        ///         <list type="bullet">
        ///             <item>If <paramref name="messageType"/> is interface, journal will fail when message implements that interface.</item>
        ///             <item>If <paramref name="messageType"/> is class, journal will fail when message can be assigned to <paramref name="messageType"/>.</item>
        ///         </list>
        ///     </para>
        /// </remarks>
        /// <param name="messageType"></param>
        /// <exception cref="ArgumentNullException">When <paramref name="messageType"/> is <c>null</c>.</exception>
        public Task FailOnType(Type messageType)
        {
            if (messageType == null) throw new ArgumentNullException(nameof(messageType));

            return SetInterceptorAsync(new BatchingSqlJournalInterceptors.OnType(messageType, BatchingSqlJournalInterceptors.Failure.Instance));
        }

        /// <summary>
        ///     Fail message if predicate <paramref name="predicate"/> will return <c>true</c>.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Journal will crash and <see cref="Eventsourced.OnPersistFailure">UntypedPersistentActor.OnPersistFailure</see> will be called on persistent actor.
        ///     </para>
        ///     <para>
        ///         Use this Journal behavior when it is needed to verify how well a persistent actor will handle network problems
        ///         and similar issues with underlying journal.
        ///     </para>
        /// </remarks>
        /// <param name="predicate"></param>
        /// <exception cref="ArgumentNullException">When <paramref name="predicate"/> is <c>null</c>.</exception>
        public Task FailIf(Func<IJournalRequest, bool> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return SetInterceptorAsync(new BatchingSqlJournalInterceptors.OnCondition(predicate, BatchingSqlJournalInterceptors.Failure.Instance));
        }

        /// <summary>
        ///     Fail message if async predicate <paramref name="predicate"/> will return <c>true</c>.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Journal will crash and <see cref="Eventsourced.OnPersistFailure">UntypedPersistentActor.OnPersistFailure</see> will be called on persistent actor.
        ///     </para>
        ///     <para>
        ///         Use this Journal behavior when it is needed to verify how well a persistent actor will handle network problems
        ///         and similar issues with underlying journal.
        ///     </para>
        /// </remarks>
        /// <param name="predicate"></param>
        /// <exception cref="ArgumentNullException">When <paramref name="predicate"/> is <c>null</c>.</exception>
        public Task FailIf(Func<IJournalRequest, Task<bool>> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return SetInterceptorAsync(new BatchingSqlJournalInterceptors.OnCondition(predicate, BatchingSqlJournalInterceptors.Failure.Instance));
        }

        /// <summary>
        ///     Fail message unless predicate <paramref name="predicate"/> will return <c>true</c>.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Journal will crash and <see cref="Eventsourced.OnPersistFailure">UntypedPersistentActor.OnPersistFailure</see> will be called on persistent actor.
        ///     </para>
        ///     <para>
        ///         Use this Journal behavior when it is needed to verify how well a persistent actor will handle network problems
        ///         and similar issues with underlying journal.
        ///     </para>
        /// </remarks>
        /// <param name="predicate"></param>
        /// <exception cref="ArgumentNullException">When <paramref name="predicate"/> is <c>null</c>.</exception>
        public Task FailUnless(Func<IJournalRequest, bool> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return SetInterceptorAsync(new BatchingSqlJournalInterceptors.OnCondition(predicate, BatchingSqlJournalInterceptors.Failure.Instance, negate: true));
        }

        /// <summary>
        ///     Fail message unless async predicate <paramref name="predicate"/> will return <c>true</c>.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Journal will crash and <see cref="Eventsourced.OnPersistFailure">UntypedPersistentActor.OnPersistFailure</see> will be called on persistent actor.
        ///     </para>
        ///     <para>
        ///         Use this Journal behavior when it is needed to verify how well a persistent actor will handle network problems
        ///         and similar issues with underlying journal.
        ///     </para>
        /// </remarks>
        /// <param name="predicate"></param>
        /// <exception cref="ArgumentNullException">When <paramref name="predicate"/> is <c>null</c>.</exception>
        public Task FailUnless(Func<IJournalRequest, Task<bool>> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return SetInterceptorAsync(new BatchingSqlJournalInterceptors.OnCondition(predicate, BatchingSqlJournalInterceptors.Failure.Instance, negate: true));
        }

        /// <summary>
        ///     Fail message after specified delay.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Each message will be delayed individually.
        ///     </para>
        ///     <para>
        ///         Journal will crash and <see cref="Eventsourced.OnPersistFailure">UntypedPersistentActor.OnPersistFailure</see> will be called on persistent actor.
        ///     </para>
        ///     <para>
        ///         Use this Journal behavior when it is needed to verify how well a persistent actor will handle network problems
        ///         and similar issues with underlying journal.
        ///     </para>
        /// </remarks>
        /// <param name="delay"></param>
        /// <exception cref="ArgumentException">When <paramref name="delay"/> is less or equal to <see cref="TimeSpan.Zero"/>.</exception>
        public Task FailWithDelay(TimeSpan delay)
        {
            if (delay <= TimeSpan.Zero)  throw new ArgumentException("Delay must be greater than zero", nameof(delay));

            return SetInterceptorAsync(new BatchingSqlJournalInterceptors.Delay(delay, BatchingSqlJournalInterceptors.Failure.Instance));
        }

        /// <summary>
        ///     Fail message after specified delay if async predicate <paramref name="predicate"/>
        ///     will return <c>true</c>.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Each message will be delayed individually.
        ///     </para>
        ///     <para>
        ///         Journal will crash and <see cref="Eventsourced.OnPersistFailure">UntypedPersistentActor.OnPersistFailure</see> will be called on persistent actor.
        ///     </para>
        ///     <para>
        ///         Use this Journal behavior when it is needed to verify how well a persistent actor will handle network problems
        ///         and similar issues with underlying journal.
        ///     </para>
        /// </remarks>
        /// <param name="delay"></param>
        /// <param name="predicate"></param>
        /// <exception cref="ArgumentException">When <paramref name="delay"/> is less or equal to <see cref="TimeSpan.Zero"/>.</exception>
        /// <exception cref="ArgumentNullException">When <paramref name="predicate"/> is <c>null</c>.</exception>
        public Task FailIfWithDelay(TimeSpan delay, Func<IJournalRequest, Task<bool>> predicate)
        {
            if (delay <= TimeSpan.Zero) throw new ArgumentException("Delay must be greater than zero", nameof(delay));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return SetInterceptorAsync(new BatchingSqlJournalInterceptors.OnCondition(
                predicate, 
                new BatchingSqlJournalInterceptors.Delay(delay, BatchingSqlJournalInterceptors.Failure.Instance)
            ));
        }

        /// <summary>
        ///     Fail message after specified delay if predicate <paramref name="predicate"/>
        ///     will return <c>true</c>.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Each message will be delayed individually.
        ///     </para>
        ///     <para>
        ///         Journal will crash and <see cref="Eventsourced.OnPersistFailure">UntypedPersistentActor.OnPersistFailure</see> will be called on persistent actor.
        ///     </para>
        ///     <para>
        ///         Use this Journal behavior when it is needed to verify how well a persistent actor will handle network problems
        ///         and similar issues with underlying journal.
        ///     </para>
        /// </remarks>
        /// <param name="delay"></param>
        /// <param name="predicate"></param>
        /// <exception cref="ArgumentException">When <paramref name="delay"/> is less or equal to <see cref="TimeSpan.Zero"/>.</exception>
        /// <exception cref="ArgumentNullException">When <paramref name="predicate"/> is <c>null</c>.</exception>
        public Task FailIfWithDelay(TimeSpan delay, Func<IJournalRequest, bool> predicate)
        {
            if (delay <= TimeSpan.Zero) throw new ArgumentException("Delay must be greater than zero", nameof(delay));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return SetInterceptorAsync(new BatchingSqlJournalInterceptors.OnCondition(
                predicate, 
                new BatchingSqlJournalInterceptors.Delay(delay, BatchingSqlJournalInterceptors.Failure.Instance)
            ));
        }

        /// <summary>
        ///     Fail message after specified delay unless predicate <paramref name="predicate"/>
        ///     will return <c>true</c>.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Each message will be delayed individually.
        ///     </para>
        ///     <para>
        ///         Journal will crash and <see cref="Eventsourced.OnPersistFailure">UntypedPersistentActor.OnPersistFailure</see> will be called on persistent actor.
        ///     </para>
        ///     <para>
        ///         Use this Journal behavior when it is needed to verify how well a persistent actor will handle network problems
        ///         and similar issues with underlying journal.
        ///     </para>
        /// </remarks>
        /// <param name="delay"></param>
        /// <param name="predicate"></param>
        /// <exception cref="ArgumentException">When <paramref name="delay"/> is less or equal to <see cref="TimeSpan.Zero"/>.</exception>
        /// <exception cref="ArgumentNullException">When <paramref name="predicate"/> is <c>null</c>.</exception>
        public Task FailUnlessWithDelay(TimeSpan delay, Func<IJournalRequest, bool> predicate)
        {
            if (delay <= TimeSpan.Zero) throw new ArgumentException("Delay must be greater than zero", nameof(delay));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return SetInterceptorAsync(new BatchingSqlJournalInterceptors.OnCondition(
                predicate, 
                new BatchingSqlJournalInterceptors.Delay(delay, BatchingSqlJournalInterceptors.Failure.Instance),
                negate: true
            ));
        }

        /// <summary>
        ///     Fail message after specified delay unless async predicate <paramref name="predicate"/>
        ///     will return <c>true</c>.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Each message will be delayed individually.
        ///     </para>
        ///     <para>
        ///         Journal will crash and <see cref="Eventsourced.OnPersistFailure">UntypedPersistentActor.OnPersistFailure</see> will be called on persistent actor.
        ///     </para>
        ///     <para>
        ///         Use this Journal behavior when it is needed to verify how well a persistent actor will handle network problems
        ///         and similar issues with underlying journal.
        ///     </para>
        /// </remarks>
        /// <param name="delay"></param>
        /// <param name="predicate"></param>
        /// <exception cref="ArgumentException">When <paramref name="delay"/> is less or equal to <see cref="TimeSpan.Zero"/>.</exception>
        /// <exception cref="ArgumentNullException">When <paramref name="predicate"/> is <c>null</c>.</exception>
        public Task FailUnlessWithDelay(TimeSpan delay, Func<IJournalRequest, Task<bool>> predicate)
        {
            if (delay <= TimeSpan.Zero) throw new ArgumentException("Delay must be greater than zero", nameof(delay));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            
            return SetInterceptorAsync(new BatchingSqlJournalInterceptors.OnCondition(
                predicate, 
                new BatchingSqlJournalInterceptors.Delay(delay, BatchingSqlJournalInterceptors.Failure.Instance),
                negate: true
            ));
        }

        /// <summary>
        ///     Fail message after specified delay if recovering message of type <typeparamref name="TMessage"/>.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Each message will be delayed individually.
        ///     </para>
        ///     <para>
        ///         Journal will crash and <see cref="Eventsourced.OnPersistFailure">UntypedPersistentActor.OnPersistFailure</see> will be called on persistent actor.
        ///     </para>
        ///     <para>
        ///         Use this Journal behavior when it is needed to verify how well a persistent actor will handle network problems
        ///         and similar issues with underlying journal.
        ///     </para>
        ///     <para>
        ///         <list type="bullet">
        ///             <item>If <typeparamref name="TMessage"/> is interface, journal will fail when message implements that interface.</item>
        ///             <item>If <typeparamref name="TMessage"/> is class, journal will fail when message can be assigned to <typeparamref name="TMessage"/>.</item>
        ///         </list>
        ///     </para>
        /// </remarks>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="delay"></param>
        /// <exception cref="ArgumentException">When <paramref name="delay"/> is less or equal to <see cref="TimeSpan.Zero"/>.</exception>
        public Task FailOnTypeWithDelay<TMessage>(TimeSpan delay) => FailOnTypeWithDelay(delay, typeof(TMessage));

        /// <summary>
        ///     Fail message after specified delay if recovering message of type <paramref name="messageType"/>.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Each message will be delayed individually.
        ///     </para>
        ///     <para>
        ///         Journal will crash and <see cref="Eventsourced.OnPersistFailure">UntypedPersistentActor.OnPersistFailure</see> will be called on persistent actor.
        ///     </para>
        ///     <para>
        ///         Use this Journal behavior when it is needed to verify how well a persistent actor will handle network problems
        ///         and similar issues with underlying journal.
        ///     </para>
        ///     <para>
        ///         <list type="bullet">
        ///             <item>If <paramref name="messageType"/> is interface, journal will fail when message implements that interface.</item>
        ///             <item>If <paramref name="messageType"/> is class, journal will fail when message can be assigned to <paramref name="messageType"/>.</item>
        ///         </list>
        ///     </para>
        /// </remarks>
        /// <param name="delay"></param>
        /// <param name="messageType"></param>
        /// <exception cref="ArgumentException">When <paramref name="delay"/> is less or equal to <see cref="TimeSpan.Zero"/>.</exception>
        /// <exception cref="ArgumentNullException">When <paramref name="messageType"/> is <c>null</c>.</exception>
        public Task FailOnTypeWithDelay(TimeSpan delay, Type messageType)
        {
            if (delay <= TimeSpan.Zero) throw new ArgumentException("Delay must be greater than zero", nameof(delay));
            if (messageType == null) throw new ArgumentNullException(nameof(messageType));

            return SetInterceptorAsync(new BatchingSqlJournalInterceptors.OnType(
                messageType, 
                new BatchingSqlJournalInterceptors.Delay(delay, BatchingSqlJournalInterceptors.Failure.Instance)
            ));
        }
    }
}

//-----------------------------------------------------------------------
// <copyright file="JournalInterceptors.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2020 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2020 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Akka.Persistence.TestKit;

namespace Akka.Persistence.Sqlite.Tests.Internal.Journal
{
    internal static class BatchingSqlJournalInterceptors
    {
        internal class Noop : IBatchingSqlJournalInterceptor
        {
            public static readonly IBatchingSqlJournalInterceptor Instance = new Noop();

            public Task InterceptAsync(IJournalRequest message) => Task.FromResult(true);
        }

        internal class Failure : IBatchingSqlJournalInterceptor
        {
            public static readonly IBatchingSqlJournalInterceptor Instance = new Failure();

            public Task InterceptAsync(IJournalRequest message) => throw new TestJournalFailureException(); 
        }

        internal class Rejection : IBatchingSqlJournalInterceptor
        {
            public static readonly IBatchingSqlJournalInterceptor Instance = new Rejection();

            public Task InterceptAsync(IJournalRequest message) => throw new TestJournalRejectionException(); 
        }
        
        internal class Delay : IBatchingSqlJournalInterceptor
        {
            public Delay(TimeSpan delay, IBatchingSqlJournalInterceptor next)
            {
                _delay = delay;
                _next = next;
            }

            private readonly TimeSpan _delay;
            private readonly IBatchingSqlJournalInterceptor _next;

            public async Task InterceptAsync(IJournalRequest message)
            {
                await Task.Delay(_delay);
                await _next.InterceptAsync(message);
            }
        }

        internal sealed class OnCondition : IBatchingSqlJournalInterceptor
        {
            public OnCondition(Func<IJournalRequest, Task<bool>> predicate, IBatchingSqlJournalInterceptor next, bool negate = false)
            {
                _predicate = predicate;
                _next = next;
                _negate = negate;
            }

            public OnCondition(Func<IJournalRequest, bool> predicate, IBatchingSqlJournalInterceptor next, bool negate = false)
            {
                _predicate = message => Task.FromResult(predicate(message));
                _next = next;
                _negate = negate;
            }

            private readonly Func<IJournalRequest, Task<bool>> _predicate;
            private readonly IBatchingSqlJournalInterceptor _next;
            private readonly bool _negate;

            public async Task InterceptAsync(IJournalRequest message)
            {
                var result = await _predicate(message);
                if ((_negate && !result) || (!_negate && result))
                {
                    await _next.InterceptAsync(message);
                }
            }
        }

        internal class OnType : IBatchingSqlJournalInterceptor
        {
            public OnType(Type messageType, IBatchingSqlJournalInterceptor next)
            {
                _messageType = messageType;
                _next = next;
            }

            private readonly Type _messageType;
            private readonly IBatchingSqlJournalInterceptor _next;

            public async Task InterceptAsync(IJournalRequest message)
            {
                var type = message.GetType();

                if (_messageType.IsAssignableFrom(type))
                {
                    await _next.InterceptAsync(message);
                }
            }
        }
    }
}

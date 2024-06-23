﻿// -----------------------------------------------------------------------
//  <copyright file="ClusterClientDiscovery.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2024 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2024 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Discovery;
using Akka.Event;

#nullable enable
namespace Akka.Cluster.Tools.Client;

public class ClusterClientDiscovery: ActorBase, IWithUnboundedStash
{
    #region Discovery messages

    internal sealed class DiscoverTick: IEquatable<DiscoverTick>
    {
        public static readonly DiscoverTick Instance = new();

        private DiscoverTick() { }
        public bool Equals(DiscoverTick? other) => other is not null;
        public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is DiscoverTick;
        public override int GetHashCode() => 0;
    }

    private sealed record Contact(ActorPath Path, ActorSelection Selection);
    private sealed record DiscoveryFailure(Exception Cause);
    private sealed record ResolveResult(Contact Contact, IActorRef? Subject);
    
    #endregion
    
    private readonly TimeSpan _defaultReconnectTimeout = TimeSpan.FromSeconds(10);
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly ClusterClientSettings _settings;
    
    private readonly ClusterClientDiscoverySettings _discoverySettings;
    private readonly ServiceDiscovery? _serviceDiscovery;
    private readonly Lookup? _lookup;
    private readonly TimeSpan _discoveryTimeout;
    private ICancelable? _discoveryCancelable;
    private readonly string _targetActorSystemName;
    private readonly string _transportProtocol;
    
    public ClusterClientDiscovery(ClusterClientSettings settings)
    {
        _settings = settings;
        _discoverySettings = settings.DiscoverySettings;
        
        if(_settings.InitialContacts.Count > 0)
            _log.Warning("Initial contacts is being ignored because ClusterClient contacts discovery is being used");
        
        var discoveryMethod = _discoverySettings.DiscoveryMethod;
        if(string.IsNullOrWhiteSpace(discoveryMethod) || discoveryMethod == "<method>")
            discoveryMethod = Context.System.Settings.Config.GetString("akka.discovery.method");
        if (string.IsNullOrWhiteSpace(discoveryMethod) || discoveryMethod == "<method>")
        {
            _log.Warning(
                "No default initial contacts discovery implementation configured in both\n" +
                "`akka.cluster.client.discovery.method` and `akka.discovery.method`.\n" +
                "Make sure to configure this setting to your preferred implementation such as 'config'\n" +
                "in your application.conf (from the akka-discovery module). Falling back to default config\n" +
                "based discovery method");
            discoveryMethod = "config";
        }

        if (_settings.ReconnectTimeout is null)
        {
            _log.Warning(
                "No reconnect timeout were configured in `akka.cluster.client.reconnect-timeout`,\n" +
                "this setting is required when using cluster client initial contact discovery feature.\n" +
                "Falling back to default value ({0}) instead.", _defaultReconnectTimeout);
            _settings = _settings.WithReconnectTimeout(_defaultReconnectTimeout);
        }

        if (string.IsNullOrWhiteSpace(_discoverySettings.ActorSystemName))
        {
            _log.Warning(
                "No target ActorSystem name configured in `akka.cluster.client.discovery.actor-system-name`,\n" +
                "falling back to this ActorSystem name ({0}) instead.", Context.System.Name);
        }
        _targetActorSystemName = string.IsNullOrWhiteSpace(_discoverySettings.ActorSystemName) 
            ? Context.System.Name : _discoverySettings.ActorSystemName;
        _transportProtocol = ((ExtendedActorSystem)Context.System).Provider.DefaultAddress.Protocol;

        _lookup = new Lookup(_discoverySettings.ServiceName, _discoverySettings.PortName);
        _serviceDiscovery = Discovery.Discovery.Get(Context.System)
            .LoadServiceDiscovery(discoveryMethod);
        _discoveryTimeout = _discoverySettings.DiscoveryTimeout;
        
        Rediscover();
    }

    public IStash Stash { get; set; } = null!;
    
    protected override bool Receive(object message)
    {
        throw new NotImplementedException("Should never reach this code");
    }

    protected override void PostStop()
    {
        _discoveryCancelable?.Cancel();
        _discoveryCancelable = null;
        base.PostStop();
    }

    private ActorPath ResolvedTargetToReceptionistActorPath(ServiceDiscovery.ResolvedTarget target)
    {
        var networkAddress = string.IsNullOrWhiteSpace(target.Host) ? target.Address.ToString() : target.Host;
        var address = new Address(_transportProtocol, _targetActorSystemName, networkAddress, target.Port);
        return new RootActorPath(address) / "system" / _discoverySettings.ReceptionistName;
    }
    
    private static async Task<ResolveResult> ResolveContact(Contact contact, TimeSpan timeout, CancellationToken ct)
    {
        try
        {
            var identity = await contact.Selection.Ask<ActorIdentity>(new Identify(null), timeout, ct);
            return new ResolveResult(contact, identity.Subject);
        }
        catch (Exception)
        {
            return new ResolveResult(contact, null);
        }
    }
    
    private void Rediscover()
    {
        Become(Discovering);
        _serviceDiscovery!.Lookup(_lookup, _discoveryTimeout)
            .PipeTo(Self, Self, failure: cause => new DiscoveryFailure(cause));
    }
    
    private bool Discovering(object message)
    {
        switch (message)
        {
            case DiscoverTick:
                Rediscover();
                return true;

            case ServiceDiscovery.Resolved resolved:
            {
                _discoveryCancelable?.Cancel();
                _discoveryCancelable = null;
                
                if (resolved.Addresses.Count == 0)
                {
                    // discovery didn't find any contacts, retry discovery
                    _discoveryCancelable = Context.System.Scheduler.ScheduleTellOnceCancelable(
                        delay: _settings.DiscoverySettings.DiscoveryRetryInterval, 
                        receiver: Self,
                        message: DiscoverTick.Instance,
                        sender: Self);
                    return true;
                }

                var contacts = resolved.Addresses.Select(address => {
                    var path = ResolvedTargetToReceptionistActorPath(address);
                    return new Contact(path, Context.ActorSelection(path));
                }).ToImmutableHashSet();

                VerifyContacts().PipeTo(Self, Self);

                return true;

                async Task<ResolveResult[]> VerifyContacts()
                {
                    var tasks = contacts.Select(c => ResolveContact(c, TimeSpan.FromSeconds(1), default));
                    return await Task.WhenAll(tasks);
                }
            }

            case ResolveResult[] resolved:
            {
                var contacts = resolved.Where(r => r.Subject is not null).Select(r => r.Contact).ToArray();
                if (contacts.Length == 0)
                {
                    _log.Warning("Cluster.Client contact point resolution phase failed, will try again.");
                    _discoveryCancelable = Context.System.Scheduler.ScheduleTellOnceCancelable(
                        delay: _settings.DiscoverySettings.DiscoveryRetryInterval, 
                        receiver: Self,
                        message: DiscoverTick.Instance,
                        sender: Self);
                }
                else
                {
                    Become(Active(contacts));
                }
                
                return true;
            }
            
            case DiscoveryFailure fail:
                _log.Warning(fail.Cause, "Cluster.Client contact point service discovery phase failed, will try again.");
                _discoveryCancelable = Context.System.Scheduler.ScheduleTellOnceCancelable(
                    delay: _settings.DiscoverySettings.DiscoveryRetryInterval, 
                    receiver: Self,
                    message: DiscoverTick.Instance,
                    sender: Self);
                return true;
            
            default:
                Stash.Stash();
                return true;
        }
    }

    private Receive Active(Contact[] contacts)
    {
        var currentSettings = _settings.WithInitialContacts(contacts.Select(c => c.Path).ToImmutableHashSet());
        var clusterClient = Context.System.ActorOf(ClusterClient.Props(currentSettings), "clusterClient");
        Context.Watch(clusterClient);
        Stash.UnstashAll();

        return message =>
        {
            switch (message)
            {
                case Terminated terminated:
                    if (terminated.ActorRef.Equals(clusterClient))
                    {
                        Become(Discovering);
                    }
                    else
                    {
                        clusterClient.Forward(message);
                    }
                    break;
                
                default:
                    clusterClient.Forward(message);
                    break;
            }

            return true;
        };
    }
}
namespace Buttr.Core.Benchmarks;

public interface IPlugin { }

public class Ping { }
public class Pong { }
public class Ding { }
public class Zero { }

public class TriadConsumer {
    public readonly Ping Ping;
    public readonly Pong Pong;
    public readonly Ding Ding;

    public TriadConsumer(Ping ping, Pong pong, Ding ding) {
        Ping = ping;
        Pong = pong;
        Ding = ding;
    }
}

public sealed class PluginA : IPlugin { }
public sealed class PluginB : IPlugin { }
public sealed class PluginC : IPlugin { }
public sealed class PluginD : IPlugin { }
public sealed class PluginE : IPlugin { }

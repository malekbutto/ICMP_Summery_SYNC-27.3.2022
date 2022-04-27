using System.Diagnostics;
using System.Net.NetworkInformation;

class Program
{
    #region Fields & Properties
    static int _PingCount = 2;
    static int _PingInterval = 500;
    static Stopwatch _StopWatch;
    static List<string> _HostsNames = new List<string>()
        {
            "cnn.com",
            "sbs.com.au",
            "bbc.co.uk",
            "maariv.co.il",
            "brazilian.report"
        };
    static string _Menu = @"Choose async method invokation that you would like to compare to sync invokation:
                        t = Thread
                        tp = ThreadPool
                        ta = Task
                        pf = Parallel for
                        pfe = Parallel for each
                        pi = Parallel invoke
                        aw = Async Await
                        OR ctrl+C to break...";

    #endregion
    public static void Main()
    {
        Console.WriteLine(_Menu);
        string userInput = Console.ReadLine().ToLower().Trim();
        Console.Clear();
        //
        PrintStars();
        PrintReport(GetHostsReplies);
        //        
        PrintStars();
        if (userInput == "t")
            PrintReport(GetHostsRepliesWithThreads);
        else if (userInput == "tp")
            PrintReport(GetHostsRepliesWithThreadPool);
        else if (userInput == "ta")
            PrintReport(GetHostsRepliesWithTasks);
        else if (userInput == "pf")
            PrintReport(GetHostsRepliesWithParallelFor);
        else if (userInput == "pfe")
            PrintReport(GetHostsRepliesWithParallelForEach);
        else if (userInput == "pi")
            PrintReport(GetHostsRepliesWithParallelInvoke);
        else if (userInput == "aw")
            PrintReport(GetHostsRepliesWithAsyncAwaitWrapper);
        else Console.WriteLine("invalid input...");
    }

    #region  GetHostsReplies
    static Dictionary<string, List<PingReply>> GetHostsReplies()
    {
        Dictionary<string, List<PingReply>> hostsReplies = new Dictionary<string, List<PingReply>>();
        foreach (var hostName in _HostsNames)
        {
            Ping ping = new Ping();
            List<PingReply> pingReplies = new List<PingReply>();
            for (int i = 0; i < _PingCount; i++)
            {
                pingReplies.Add(ping.Send(hostName));
                Thread.Sleep(_PingInterval);
            }
            hostsReplies.Add(hostName, pingReplies);
        }
        return hostsReplies;
    }
    static Dictionary<string, List<PingReply>> GetHostsRepliesWithThreads()
    {
        Dictionary<string, List<PingReply>> hostsReplies = new Dictionary<string, List<PingReply>>();
        List<Thread> threads = new List<Thread>();
        foreach (var hostName in _HostsNames)
        {
            Ping ping = new Ping();
            List<PingReply> pingReplies = new List<PingReply>();
            threads.Add(new Thread(() =>
            {
                for (int i = 0; i < _PingCount; i++)
                {
                    pingReplies.Add(ping.Send(hostName));
                    Thread.Sleep(_PingInterval);
                }
                hostsReplies.Add(hostName, pingReplies);
            }));
        }
        foreach (var thread in threads)
            thread.Start();
        foreach (var thread in threads)
            thread.Join();
        return hostsReplies;
    }

    static Dictionary<string, List<PingReply>> GetHostsRepliesWithThreadPool()
    {
        Dictionary<string, List<PingReply>> hostsReplies = new Dictionary<string, List<PingReply>>();
        List<ManualResetEvent> mres = new List<ManualResetEvent>();
        ManualResetEvent mre2Add;
        WaitCallback wc2Add;
        foreach (var hostName in _HostsNames)
        {
            mre2Add = new ManualResetEvent(false);
            wc2Add = new WaitCallback((o) =>
           {
               hostsReplies.Add(hostName, GetPingReplies(hostName));
               (o as ManualResetEvent).Set();
           });
            mres.Add(mre2Add);
            ThreadPool.QueueUserWorkItem(wc2Add, mre2Add);
        }
        //Wait for all threads to finish their work and join the main thread
        foreach (var mreItem in mres)
            mreItem.WaitOne();
        return hostsReplies;
    }
    static Dictionary<string, List<PingReply>> GetHostsRepliesWithTasks()
    {
        Dictionary<string, List<PingReply>> hostsReplies = new Dictionary<string, List<PingReply>>();
        List<Task> Tasks = new List<Task>();
        foreach (var hostName in _HostsNames)
        {
            Ping ping = new Ping();
            List<PingReply> pingReplies = new List<PingReply>();
            Tasks.Add(new Task(() =>
            {
                for (int i = 0; i < _PingCount; i++)
                {
                    pingReplies.Add(ping.Send(hostName));
                    Task.Delay(_PingInterval)/*.Wait(//_PingInterval)*/;
                }
                hostsReplies.Add(hostName, pingReplies);
            }));
        }
        foreach (Task task in Tasks)
            task.Start();
        foreach (Task task in Tasks)
            task.Wait();
        //Task.WaitAll(Tasks.ToArray()); //instead of the foreach above (wait  )
        return hostsReplies;
    }

    static Dictionary<string, List<PingReply>> GetHostsRepliesWithParallelInvoke()
    {
        Dictionary<string, List<PingReply>> hostsReplies = new Dictionary<string, List<PingReply>>();
        List<Action> actions = new List<Action>();
        foreach (var hostName in _HostsNames)
        {
            Ping ping = new Ping();
            List<PingReply> pingReplies = new List<PingReply>();
            actions.Add(() =>
            {
                for (int i = 0; i < _PingCount; i++)
                {
                    pingReplies.Add(ping.Send(hostName));
                    Thread.Sleep(_PingInterval);
                }
                hostsReplies.Add(hostName, pingReplies);
            });
        }
        Parallel.Invoke(actions.ToArray());
        return hostsReplies;
    }

    static Dictionary<string, List<PingReply>> GetHostsRepliesWithParallelForEach()
    {
        Dictionary<string, List<PingReply>> hostsReplies = new Dictionary<string, List<PingReply>>();
        Parallel.ForEach(_HostsNames, hostName =>
        {
            Ping ping = new Ping();
            List<PingReply> pingReplies = new List<PingReply>();
            for (int i = 0; i < _PingCount; i++)
            {
                pingReplies.Add(ping.Send(hostName));
                Thread.Sleep(_PingInterval);
            }
            hostsReplies.Add(hostName, pingReplies);
        });
        return hostsReplies;
    }

    static Dictionary<string, List<PingReply>> GetHostsRepliesWithParallelFor()
    {
        Dictionary<string, List<PingReply>> hostsReplies = new Dictionary<string, List<PingReply>>();
        Parallel.For(0, _HostsNames.Count, j =>
        {
            Ping ping = new Ping();
            List<PingReply> pingReplies = new List<PingReply>();
            for (int i = 0; i < _PingCount; i++)
            {
                pingReplies.Add(ping.Send(_HostsNames[j]));
                Thread.Sleep(_PingInterval);
            }
            hostsReplies.Add(_HostsNames[j], pingReplies);
        });
        return hostsReplies;
    }
    static Dictionary<string, List<PingReply>> GetHostsRepliesWithTPL()
    {
        return null;
    }
    static Dictionary<string, List<PingReply>> GetHostsRepliesWithAsyncAwaitWrapper()
    {
        Task<Dictionary<string, List<PingReply>>> t = GetHostsRepliesWithAsyncAwait();
        t.Wait();
        return t.Result;
    }

    async static Task<Dictionary<string, List<PingReply>>> GetHostsRepliesWithAsyncAwait()
    {
        Dictionary<string, List<PingReply>> hostsReplies = new Dictionary<string, List<PingReply>>();
        Dictionary<string, Task<List<PingReply>>> hostsTasks = new Dictionary<string, Task<List<PingReply>>>();
        foreach (var hostName in _HostsNames)
        {
            hostsTasks.Add(hostName, GetPingRepliesAsyncAwait(hostName, _PingCount, _PingInterval));
        }
        foreach (var hostTask in hostsTasks)
        {
            var pingReply = await hostTask.Value;
            hostsReplies.Add(hostTask.Key, pingReply);
        }
        return hostsReplies;
    }

    async static Task<List<PingReply>> GetPingRepliesAsyncAwait(string hostName, int pingCount = 1, int pingInterval = 1)
    {
        return await Task.Run(() => 
        {
            return GetPingReplies(hostName, pingCount, pingInterval); 
        });
    }
    #endregion

    #region GetPingReplies
    

    #region Print
    static void PrintLine() => Console.WriteLine("---------------------------");
    static void PrintStars() => Console.WriteLine("***************************");
    static void PrintReport(Func<Dictionary<string, List<PingReply>>> getHostsReplies)
    {
        Console.WriteLine($"Started {getHostsReplies.Method.Name}");
        _StopWatch = Stopwatch.StartNew();
        Dictionary<string, List<PingReply>> hostsReplies = getHostsReplies();
        _StopWatch.Stop();
        Console.WriteLine($"Finished {getHostsReplies.Method.Name}");
        PrintLine();
        Console.WriteLine($"Printing {getHostsReplies.Method.Name} report:");
        if (hostsReplies != null)
            PrintHostsRepliesReports(hostsReplies);
        PrintLine();
    }
    static void PrintHostsRepliesReports(Dictionary<string, List<PingReply>> hostsReplies)
    {
        long hostsTotalRoundtripTime = 0;
        Dictionary<string, PingReplyStatistics> hrs = GetHostsRepliesStatistics(hostsReplies);
        PrintTotalRoundtripTime(hrs);
        PrintLine();
        hostsTotalRoundtripTime = hrs.Sum(hr => hr.Value.TotalRoundtripTime);
        Console.WriteLine($"Report took {_StopWatch.ElapsedMilliseconds} ms to generate,{_PingCount * _HostsNames.Count} total pings took total {hostsTotalRoundtripTime} ms hosts roundtrip time");
    }
    static void PrintTotalRoundtripTime(Dictionary<string, PingReplyStatistics> hrs, bool ascendingOrder = true)
    {
        string orderDescription = ascendingOrder ? "ascending" : "descending";
        Console.WriteLine($"Hosts total roundtrip time in {orderDescription} order: (HostName:X,Replies statistics:Y)");
        var orderedHrs = ascendingOrder ? hrs.OrderBy(hr => hr.Value.TotalRoundtripTime) : hrs.OrderByDescending(hr => hr.Value.TotalRoundtripTime);
        foreach (var hr in orderedHrs)
        {
            Console.WriteLine($"{hr.Key},{hr.Value}");
        }
    }
    static void PrintHostsRepliesStatistics(Dictionary<string, PingReplyStatistics> hrs)
    {
        Console.WriteLine("Hosts replies statistics: (HostName:X,Replies statistics:Y)");
        foreach (var hr in hrs)
        {
            Console.WriteLine($"{hr.Key},{hr.Value}");
        }
    }

    #endregion
    #endregion

    static List<PingReply> GetPingReplies(string hostName)
    {
        return GetPingReplies(hostName, _PingCount, _PingInterval);
    }

    static List<PingReply> GetPingReplies(string hostName, int PingCount = 1, int PingInterval = 1)
    {
        Ping ping = new Ping();
        List<PingReply> pingReplies = new List<PingReply>();
        for (int i = 0; i < _PingCount; i++)
        {
            pingReplies.Add(ping.Send(hostName));
            if (PingCount > 1)
                Thread.Sleep(_PingInterval);
        }
        return pingReplies;
    }

    static Dictionary<string, PingReplyStatistics> GetHostsRepliesStatistics(Dictionary<string, List<PingReply>> hostsReplies)
    {
        Dictionary<string, PingReplyStatistics> hrs = new Dictionary<string, PingReplyStatistics>();
        foreach (var hr in hostsReplies)
            hrs.Add(hr.Key, new PingReplyStatistics(hr.Value));
        return hrs;
    }
}
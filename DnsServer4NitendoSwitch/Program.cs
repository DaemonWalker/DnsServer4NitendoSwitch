using ARSoft.Tools.Net;
using ARSoft.Tools.Net.Dns;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace DnsServer4NitendoSwitch
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json");

            var configuration = builder.Build();

            var mode = "-s";
            if (args.Length != 0)
            {
                mode = args[0].ToLower();
            }
            if (mode.Contains("s"))
            {
                DNSServerHelper server = null;
                try
                {
                    server = new DNSServerHelper();
                    
                    server.Run();
                    Console.Write("按回车键停止服务...");
                    Console.ReadLine();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            else
            {
                Console.WriteLine("测试服务端,输入空行结束");
                var site = Console.ReadLine();
                while (!string.IsNullOrWhiteSpace(site))
                {
                    TestClinet(site);
                    site = Console.ReadLine();
                }
            }
        }
        static void TestClinet(string domainName)
        {
            var dnsClient = new DnsClient(IPAddress.Parse("127.0.0.1"), 10000);
            var dnsMessage = dnsClient.Resolve(DomainName.Parse(domainName));
            //若返回结果为空，或者存在错误，则该请求失败。
            if (dnsMessage != null && !(dnsMessage.ReturnCode != ReturnCode.NoError && dnsMessage.ReturnCode != ReturnCode.NxDomain))
            {
                //循环遍历返回结果，将返回的IPV4记录添加到结果集List中。
                foreach (DnsRecordBase dnsRecord in dnsMessage.AnswerRecords)
                {
                    ARecord aRecord = dnsRecord as ARecord;
                    if (aRecord != null)
                    {
                        Console.WriteLine(aRecord.Address.ToString());
                    }
                    else
                    {
                        continue;
                    }
                }
            }
            Console.WriteLine("error");
        }
    }
    class DNSServerHelper
    {
        /// <summary>
        /// 最大连接数
        /// </summary>
        public int MaxConnection { get; set; } = 10;

        /// <summary>
        /// 设置DNS服务器地址
        /// </summary>
        public string DnsServer
        {
            get
            {
                return this.dnsServer;
            }
            set
            {
                dnsServer = value;
                this.dnsClient = new DnsClient(IPAddress.Parse(dnsServer), 10000);
            }
        }

        /// <summary>
        /// 服务器IP地址
        /// </summary>
        public string ServerAddress { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public DNSServerHelper()
        {
            var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json");

            var configuration = builder.Build();
            DomainDict = new Dictionary<string, string>();

            this.DnsServer = configuration["DNSServer"];
            this.ServerAddress = configuration["hostAddress"];
            int index = 0;
            while (configuration[$"customDNS:{index}:domainName"] != null)
            {
                this.DomainDict.Add(
                    configuration[$"customDNS:{index}:domainName"] + ".",
                    configuration[$"customDNS:{index}:ip"]);
                index++;
            }
            foreach (var kv in this.DomainDict)
            {
                Console.WriteLine($"set {kv.Key} to {kv.Value}");
            }
        }

        /// <summary>
        /// 启动DNS服务器
        /// </summary>
        public void Run()
        {
            StartHttpServer();
            DnsServer dnsServer = new DnsServer(MaxConnection, MaxConnection);
            dnsServer.QueryReceived += DnsServer_QueryReceived;
            dnsServer.Start();
        }

        /// <summary>
        /// DNS地址请求
        /// </summary>
        private DnsClient dnsClient;

        /// <summary>
        /// 默认DNS服务器地址
        /// </summary>
        private string dnsServer;

        /// <summary>
        /// 自定义DNS跳转字典
        /// </summary>
        public Dictionary<string, string> DomainDict { get; set; }

        /// <summary>
        /// 消息队列处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        private Task DnsServer_QueryReceived(object sender, QueryReceivedEventArgs eventArgs)
        {
            return Task.Run(() =>
            {
                var message = eventArgs.Query;
                var clientAddress = eventArgs.RemoteEndpoint;
                message.IsQuery = false;
                DnsMessage query = message as DnsMessage;
                if (query == null || query.Questions.Count <= 0)
                    message.ReturnCode = ReturnCode.ServerFailure;
                else
                {
                    if (query.Questions[0].RecordType == RecordType.A)
                    {
                        //自定义解析规则，clientAddress即客户端的IP，dnsQuestion.Name即客户端请求的域名，Resolve为自定义的方法（代码不再贴出），返回解析后的ip，将其加入AnswerRecords中
                        foreach (DnsQuestion dnsQuestion in query.Questions)
                        {
                            string resolvedIp = Resolve(clientAddress.ToString(), dnsQuestion.Name.ToString());
                            ARecord aRecord = new ARecord(query.Questions[0].Name, 36000, IPAddress.Parse(resolvedIp));
                            query.AnswerRecords.Add(aRecord);
                        }
                    }
                }
                eventArgs.Response = message;
            });
        }

        /// <summary>
        /// 解析域名
        /// </summary>
        /// <param name="clientAddress"></param>
        /// <param name="domainName"></param>
        /// <returns></returns>
        private string Resolve(string clientAddress, string domainName)
        {
            var targetIP = SpecSiteResolve(domainName);
            if (string.IsNullOrWhiteSpace(targetIP))
            {
                targetIP = NormalSiteResolve(domainName);
            }
            Console.WriteLine($"{domainName} => {targetIP}");
            return targetIP;
        }

        /// <summary>
        /// 特殊域名处理
        /// </summary>
        /// <param name="domainName"></param>
        /// <returns></returns>
        private string SpecSiteResolve(string domainName)
        {
            domainName = domainName.ToLower();
            if (DomainDict.ContainsKey(domainName))
            {
                return DomainDict[domainName];
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 解析不在hosts.txt上面的域名
        /// </summary>
        /// <param name="domainName"></param>
        /// <returns></returns>
        private string NormalSiteResolve(string domainName)
        {
            var dnsMessage = dnsClient.Resolve(DomainName.Parse(domainName));
            //若返回结果为空，或者存在错误，则该请求失败。
            if (dnsMessage != null && !(dnsMessage.ReturnCode != ReturnCode.NoError && dnsMessage.ReturnCode != ReturnCode.NxDomain))
            {
                //循环遍历返回结果，将返回的IPV4记录添加到结果集List中。
                foreach (DnsRecordBase dnsRecord in dnsMessage.AnswerRecords)
                {
                    ARecord aRecord = dnsRecord as ARecord;
                    if (aRecord != null)
                    {
                        return aRecord.Address.ToString();
                    }
                    else
                    {
                        continue;
                    }
                }
            }
            return null;
        }
        private void StartHttpServer()
        {
            Task.Run(() =>
            {
                string contentRoot = Directory.GetCurrentDirectory();
                IFileProvider fileProvider = new PhysicalFileProvider(contentRoot);

                new WebHostBuilder()
                    .UseUrls("http://*")
                    .UseContentRoot(contentRoot)
                    .UseKestrel()
                    .Configure(app => app
                        .UseDefaultFiles()
                        .UseDefaultFiles(new DefaultFilesOptions
                        {
                            RequestPath = "",
                            FileProvider = fileProvider,
                        })
                        .UseStaticFiles()
                        .UseStaticFiles(new StaticFileOptions
                        {
                            FileProvider = fileProvider,
                            RequestPath = ""
                        })
                        .UseDirectoryBrowser()
                       .UseDirectoryBrowser(new DirectoryBrowserOptions
                       {
                           FileProvider = fileProvider,
                           RequestPath = ""
                       }))
                .Build()
                .Run();
            });
        }
    }
}
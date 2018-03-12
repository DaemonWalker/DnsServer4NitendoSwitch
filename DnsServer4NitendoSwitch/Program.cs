using ARSoft.Tools.Net;
using ARSoft.Tools.Net.Dns;
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
            DNSServerHelper server = null;
            try
            {
                server = new DNSServerHelper();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
    class DNSServerHelper
    {
        /// <summary>
        /// 最大连接数
        /// </summary>
        public int maxConnection { get; set; } = 10;

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
        /// 构造函数
        /// </summary>
        public DNSServerHelper()
        {
            this.dnsClient = new DnsClient(IPAddress.Parse(dnsServer), 10000);
            domainDict = new Dictionary<string, string>();
        }

        /// <summary>
        /// 启动DNS服务器
        /// </summary>
        public void Run()
        {
            DnsServer dnsServer = new DnsServer(maxConnection, maxConnection);
            dnsServer.QueryReceived += DnsServer_QueryReceived;
            dnsServer.Start();
        }

        /// <summary>
        /// 默认DNS服务器地址
        /// </summary>
        private string dnsServer = "202.96.199.133";

        /// <summary>
        /// DNS地址请求
        /// </summary>
        private DnsClient dnsClient;

        private Dictionary<string, string> domainDict;

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
        private void DomainDictInit()
        {
            if (!File.Exists("hosts.txt"))
            {
                throw new Exception("找不到解析规则文件 hosts.txt 请手动创建");
            }
            var roles = File.ReadLines("hosts.txt");
            foreach (var role in roles)
            {
                if (role.StartsWith("#"))
                {
                    continue;
                }
                var roleInfo = role.Split(' ');
                if (roleInfo.Length != 2)
                {
                    continue;
                }
                if (!IPAddress.TryParse(roleInfo[1], out IPAddress temp))
                {
                    throw new Exception($"IP地址{roleInfo[1]}格式有误，请进行检查");
                }
                if (domainDict.ContainsKey(roleInfo[0]))
                {
                    throw new Exception($"域名{roleInfo[0]}出现重复，请检查");
                }
                else
                {
                    domainDict.Add(roleInfo[0], roleInfo[1]);
                    Console.WriteLine($"域名 {roleInfo[0]} 解析到 {roleInfo[1]}");
                }
            }
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
            return targetIP;
        }

        private string SpecSiteResolve(string domainName)
        {
            if (domainDict.ContainsKey(domainName))
            {
                return domainDict[domainName];
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
    }
}
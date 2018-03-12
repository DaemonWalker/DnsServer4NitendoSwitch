# DnsServer4NitendoSwitch
## 用途
Switch只能用来玩游戏实在是太可惜了，然后用这个可以用来上上网什么的。。。

## 简介
在知乎上看到的，但是是用python写的，服务器木有python环境，所以打算写个.Net Core版的。。。<br>
事实证明py版的确实比较精简，Core的库就引用了一大堆-_-||

## 食用方法
### 服务端
1. [下载当前平台的SDK](http://www.microsoft.com/net/learn/get-started/)
2. 下载Relase里面的压缩包，解压
3. dotnet DnsServer4NitendoSwitch.dll
4. 修改appsettings.json，把 所有的 172.16.18.64 改成你服务器的IP，如果是家里搭建的，就用内网IP就行

### Switch
1. 找到WIFI设置
2. 修改Switch的DNS服务器到你服务器的IP
3. 连接WIFI
4. Switch会卡一会，然后会提示需要验证，点Next可以了~

## 配置文件 appsettings.json 的问题
1. hostAddress是服务器IP地址
2. dnsServer是默认DNS服务器，如果请求的网址不在配置文件里面，将从这个服务器解析域名，默认的是北京电信的DNS地址，如果配置错误会导致无法上网的哦~
3. customDNS是自定义DNS块
    1. domainName是域名
    2. ip是该域名要解析到的IP地址
    3. 别忘了加逗号，除了最后一个之外的都要有逗号
    4. e.g:
    ```JSON
    {
      "domainName": "conntest.nintendowifi.net",
      "ip": "172.16.18.64"
    }
    ```
    这样你在地址栏里面访问淘宝就会跳转到京东去了。。。

## 我看不懂怎么办
我在 120.27.214.79 上面建了个临时的，想尝尝鲜的把DNS改成这个就好<br>
当然不保证我那天心情不好就给关了

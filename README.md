# DnsServer4NitendoSwitch
## 简介
在知乎上看到的，但是是用python写的，服务器木有python环境，所以打算写个.Net Core版的。。。<br>
事实证明py版的确实比较精简，Core的库就引用了一大堆-_-||

## 食用方法
### 服务端
1. [下载当前平台的SDK](http://www.microsoft.com/net/learn/get-started/)
2. 下载Relase里面的压缩包，解压
3. dotnet DnsServer4NitendoSwitch.dll
4. 修改hosts.txt，把 ctest.cdn.nintendo.net 后面的IP地址改为服务器的IP地址，注意域名和IP中间有空格哦

### Switch
1. 找到WIFI设置
2. 修改Switch的DNS服务器到你服务器的IP
3. 连接WIFI
4. Switch会卡一会，然后会提示需要验证，点Next可以了~

## 配置文件 host.txt 的问题
格式是 域名 空格 IP地址<br>
前面加 井号 进行注释

## 我看不懂怎么办
我在 120.27.214.79 上面建了个临时的，想尝尝鲜的把DNS改成这个就好<br>
当然不保证我那天心情不好就给关了

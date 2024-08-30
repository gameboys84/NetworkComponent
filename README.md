# NetworkComponent

[TOC]

## 说明

这里只考虑客户端框架，因此需求如下

- C#实现
- 开源或完整代码【尽量，不强求】
- 结构简单，易使用
- 不要和服务器耦合
- 协议编码和网络处理逻辑支持热更新

在整理的过程中，发现了一个网络模块的宝库：[GameNetworkingResources](https://github.com/ThusSpokeNomad/GameNetworkingResources)

以及Unity项目中对网络模块的常见需求，可以作为参考：[Help Me Choose Networking Solution?](https://discussions.unity.com/t/help-me-choose-networking-solution-for-formation-tactics-game/895193/3)


## 优先考虑的网络模块

更详细的解读见文档: [网络模块-探索](https://gameboys.atlassian.net/wiki/spaces/Note/pages/214106113/)

### [ET](https://github.com/egametang/ET)
GitHub星数：8700

意向：放弃

原因：纯ET的肯定不能考虑，和服务器绑定的太紧密了，除非哪天服务器也愿意使用.net来开发的话可以尝试一下，顾虑任然是绑定太紧了🥲。

### [Fantasy](https://github.com/qq362946/Fantasy)
GitHub星数：305

意向：待观察

原因：初步试用了一下，Demo都出现很多问题，虽然还没遇到非常严重的情况，但使用体验不太好

有专门的QQ群进行交流，并且群主也会积极解决出现的问题

从ET衍生的框架，也存在和服务器一定程度上的关联，但还是有办法可以避免耦合

### [GameNetty](https://github.com/ALEXTANGXIAO/GameNetty)
GitHub星数：169

意向：待验证

原因：也是从ET衍生出的框架，但已经被作者拆分出来了，待验证

### [Photon Fusion](https://www.photonengine.com/fusion)

商业软件，免费版本限连接数，限流量，商业化也就意味着没有源码了

早期出的Fusion（状态同步网络库）、PUN（Photon Unity Network）已免费，仅插件免费

- PUN、PUN2，客户端验证，不推荐

- Fusion，服务器验证，且具有防作弊功能，按CCU（同时连接数）收费，如果能接受它的价格的话，可以推荐

### [Mirror](https://github.com/MirrorNetworking/Mirror)
GitHub星数：5000

意向：待验证

原因：Unity商店有免费和收费版插件：[Mirror](https://assetstore.unity.com/packages/tools/network/mirror-129321)

多种网络模式支持，且有大量上线游戏验证

有LTS版本，也最多只需要$80

### [FishNet](https://github.com/FirstGearGames/FishNet)
GitHub星数：1300

意向：待验证

原因：Unity商店有免费和收费版插件：[FishNet](https://assetstore.unity.com/packages/tools/network/fishnet-networking-evolved-207815)

收费版本需要$50

### [Netcode](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects)
GitHub星数：2100

意向：待验证

原因：分为Entities和GameObjects版本，官方出品

 
## 发行
在Stream发行网游也有很多好处：

- 免费网络模块
- 支持中继，且没有IP泄露
- 支持Lobby，且支持好友系统
- 反作弊

Mirror、Netcode和FishNet也有Streamworks转换，对于非Steam平台，EOS也免费提供以上功能，但只有Mirror和FishNet提供EOS转换
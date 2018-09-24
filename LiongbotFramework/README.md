# Liongbot

Liongbot是一套负责聊天消息接收、分发及处理的通用框架。在使用Liongbot之前请先阅读下面的介绍。

## Liongbot能做什么？

`Liongbot.Messaging`负责聊天文本和通用消息对象之间的转换；`Liongbot.Dispatch`负责前后端（机器人框架和插件）之间的消息路由；`Liongbot.Command`负责将聊天文本/通用消息对象剖析成的用户自己定义的类型。

接收到消息时，前端将文本转换成通用消息（`Liongbot.Messaging.Message`），分发器按照优先级将。分发器一个完整的消息处理拓扑将是下面这样的：

```plaintext
[EXTERNAL PROCEDURE] <-------+
 |                           |
 +-> [Liongbot.Dispatch.IFrontEnd] <--+
      |                            |
      +-> [Liongbot.Dispatch.Dispatcher] <---+
	       |                              |
		   +-> [Liongbot.Dispatch.IBackEnd] -+
```

一个消息分发器(Dispatcher)可以和复数个前端和后端绑定，经过处理的消息会原路返回，经由收信的前端发回。

## Liongbot不能做什么？

Liongbot本身并不是一个完整的机器人程序，你需要额外编写下列功能的程序来让机器人运行起来：

* 到外围程序的前端程序。就像是从酷Q机器人收信，并通过事件通知分发器的程序。
* 响应消息的后端程序。即机器人插件。
* 环境配置的读写程序。例如查找本地插件DLL的程序和管理配置文件的程序。

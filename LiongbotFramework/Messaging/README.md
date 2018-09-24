# Liongbot.Messaging

消息合成/包裹中间件。在写插件的时候方便地合成

[x] 消息合成
[x] 消息剖析
[x] 模板
[x] 用户模板
[ ] 消息元数据

## 消息合成

用下面这种方式直接合成消息：

```csharp
using Liongbot.Messaging;

var msg = new Compound(
	new Text("消息的合成是有顺序的"),
	"字符串会被隐式转换成Text消息对象",
	new Image("C:/到图片的绝对路径，中间件会自动转换成相对路径"),
	new Record("C:/和图片一样"),
	new At("10000"), // @别人
	new Empty() // 什么都没有
);

IComposer composer = someComposer;
var composed = composer.Compose(msg);
```

不是很推荐直接合成，因为代码写出来会很乱。

## 消息剖析

通过`IComposer`的`Decompose`方法可以将文本消息转换为一个复合（Compound）消息对象。

## 模板

使用模板可以先预载一个消息模板，然后在处理消息的时候再在占位符（placeholder）的位置插入变量：

```csharp
var template = new Template(
	"foo", new Placeholder(), "bar"
);
var msg = template.MakeMessage("wow");
```

如果填入模板的参数数量和前面占位符的数量不一致，会传出null。

## 用户模板

个人更推荐单独写一个类，然后继承用户模板（user tempalte）这个类。这种写法更方便干净。

```csharp
class DemoMessage : UserTemplate {
    protected override Message[] TemplateMessage {
        get => new Message[] {
            "This is a demo to show you how the new ", Placeholder(),
            "helps you build plugins elegantly. ",
            Image("C:/Users/PENGUINLIONG/Pictures/Desktop.png"), At("10000")
        };
    }
}
```

里面的`Image`之类的不用new也没关系，这些是为了写着方便，从`UserTemplate`继承的方法。

@PENGUINLIONG
2018-07-05T01:17:00+08:00

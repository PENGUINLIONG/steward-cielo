# Liongbot.Command

命令处理中间件，负责将聊天文本转换为命令+参数的形式来简化后面的处理过程。

功能清单：

[x] 参数分割
[x] 结构化命令定义
[x] 语法提供者
[x] 解析简易斜线风格的命令
[ ] 解析Shell风格的命令

## 参数分割

这个模块最基本的功能是拆分参数。将用空格分隔的，和引号包裹的参数拆成参数包：

```csharp
using Liongbot.Command;
// 准备上下文...

var parser = new CommandParser(context);
var args = parser.Process("Liongbot foo bar \"a b\" \'c d\'");
```

这里的`args`就是拆分出来的参数包了，`ArgumentPack`实现了`IEnumerable<string>`接口，所以可以使用`foreach`进行遍历：

```csharp
foreach (var arg in args) {
	Console.WriteLine(arg);
}
// 输出：
// Liongbot
// foo
// bar
// a b
// c d
```

## 结构化命令定义

这个中间件使用一种结构化的命令定义方式，让开发者可以快快速定义命令，并使用自动化工具解析。整套工具链共有下面几个主要部分：

* `ArgumentPack`：参数包工具。负责将字符串转换为通用参数。参数间用空白字符（换行符、制表符和空格）分隔；引号（`\'`、`\"``\``）之间的文本，包括空白字符在内完全保留。引号没有正常闭合时，处理程序会抛出异常。
* `CommandProfile`：命令解析工具。负责分析命令结构体里的特性标签（Attribute）。如果标签违反了特定规则，会抛出异常，这一点在下文会具体讲到。
* `ArgumentInjector`：参数注入工具。负责将命令
* `SyntaxProvider`: 语法提供者接口。提供了一种通用的自定义命令语法的方式。

结构化定义非常简单好用，首先我们定义一个结构体（不可以是类），并为其添加`Command`特性。

```csharp
using Liongbot.Command.Attributes;

[Command(Name="CommandName")]
struct Command {
	// ...
}
```

如果没有另外给出命令的名字，这个结构体本身的名字就会被用作命令的名字（即这里的`Command`）。

在结构体中添加命名参数（Named argument）和自由参数（Free argument），这两种参数都可以指定一个默认值，如果没有提供默认值，除了`string`以外的类型都是CLR默认构造的值。`string`由于没有默认构造函数不能直接构建，作为替代方案，解析器会赋上`String.Empty`：

```csharp
// 命名参数的特性标签
[Arg(Abbreviate = 'P', Name = "Port", IsSwitch = true, Default = 8080)]
public int Port;

// 自由参数的特性标签
[FreeArg(Default = "HeyHeyHeyStartDash")]
public int ISay;
```

特别的，命名参数的特性标签省略所有参数时，参数名将会直接使用字段的名字：

```csharp
[Arg]
public string NameIsAutoDeducted;
```

注意，只有在标记bool类型的字段时可以给`IsSwitch`赋`true`，否则解析工具会抛出异常：

```csharp
[Arg(IsSwitch = true)]
public bool Switch;
```

自由参数是按声明顺序排列的，注入器会先给在顶部的自由参数赋值。最后一个自由参可以设成`List`以接受不确定长度的自由参，这个特别的自由参叫做“列表接收器”（List receptor，不用去查，我自己造的词）。列表接收器在一个命令结构体中能且只能出现一次，而且必须是最后一个自由参。违反这一约定，解析器会抛出异常。

```csharp
[FreeArg]
public List<int> Nyanpass;
```

同时，这两种参数特性只能加在字段（Field）上。如果要加在属性上面，需要特别指出特性标签修饰的对象是字段：

```csharp
[field: Arg(IsSwitch = true)]
public bool Recurse;
```

## 语法提供者

实现语法提供者接口的类型可以非常自由的定义命令文本的语法。在结构化定义时设定的名字会被解析器拆分成单词，具体规则和C#对类型的命名风格约定相同：

* 正常文本按照驼峰分词：`StringBuilder` => `String, Builder`
* 两个字母的缩写，两个字母都大写：`DBAdmin` => `DB, Admin`
* 要写三个字以上的缩写不能大写，要改成驼峰：`XmlDocument` => `Xml Document`

分词完成后，单词列表会交给语法提供者以生成正式剖析（parse）时使用的名字，这个时候可以加入自己的命名风格。随后进行剖析时要分析传入参数的语法，然后为具体的字段赋值。具体请参见`Liongbot.Command.SyntaxProviders`中的示例程序。

## 解析简易斜线风格的命令

作为教学样本的斜线命令格式：

```plaintext
\command switch=true arg1=ping arg2=pong free arguments
```

@PENGUINLIONG
2018-07-05-00:38:00+08:00

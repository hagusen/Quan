## Gobo: GML Formatter

[Try the formatter here!](https://hagusen.github.io/Gobo/)

Gobo is an opinionated formatter for GameMaker Language.

### Input

```js
x = a and b or c  a=0xFG=1 var var var i := 0
do begin
;;;;show_debug_message(i)
;;;;++constructor
end until not constructor < 10 return

call()
```

### Output

```js
x = a && b || c
a = 0xF
G = 1
var i = 0
do
{
    show_debug_message(i)
    ++constructor
} until (!constructor < 10)
return call()

```

## How does it work?
Gobo is written in C# and compiles to a self-contained binary using Native AOT in .NET 8.

Gobo uses a custom GML parser to read your code and ensure that formatted code is equivalent to the original. The parser is designed to only accept valid GML (with a few exceptions) to ensure correctness. There is no officially-documented format for GML's syntax tree, so Gobo uses a format similar to JavaScript parsers. 

Gobo converts your code into an intermediate "Doc" format to make decisions about wrapping lines and printing comments. The doc printing algorithm is taken from [CSharpier](https://github.com/belav/csharpier), which is itself adapted from Prettier.


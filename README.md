
## Easy.IO
Based on [okio](https://github.com/square/okio) project built with .Net Standard 2.0

[![NuGet Version](https://img.shields.io/nuget/v/easy.io.svg?style=flat)](https://www.nuget.org/packages?q=easy.io) 

## ByteStrings and Buffers

Easy.IO is built around two types that pack a lot of capability into a straightforward API:

 * [**ByteString**][3] is an immutable sequence of bytes. For character data, `String`
   is fundamental. `ByteString` is String's long-lost brother, making it easy to
   treat binary data as a value. This class is ergonomic: it knows how to encode
   and decode itself as hex, base64, and UTF-8.

 * [**EasyBuffer**][4] is a mutable sequence of bytes. Like `List`, you don't need
   to size your buffer in advance. You read and write buffers as a queue: write
   data to the end and read it from the front. There's no obligation to manage
   positions, limits, or capacities.

Internally, `ByteString` and `EasyBuffer` do some clever things to save CPU and
memory. If you encode a UTF-8 string as a `ByteString`, it caches a reference to
that string so that if you decode it later, there's no work to do.

`EasyBuffer` is implemented as a linked list of segments. When you move data from
one buffer to another, it _reassigns ownership_ of the segments rather than
copying the data across. This approach is particularly helpful for multithreaded
programs: a thread that talks to the network can exchange data with a worker
thread without any copying or ceremony.


## Get started

- **Read File** 
```csharp
	string tempFile = Path.GetTempFileName();
	BufferedSource source = EasyIO.Buffer(EasyIO.Source(tempFile));
	var str = source.ReadUtf8();
	source.Dispose();
```

- **Write File** 
```csharp
	string tempFile = Path.GetTempFileName();
	BufferedSink sink = EasyIO.Buffer(EasyIO.Sink(tempFile));
	sink.WriteUtf8("Hello, ");
	sink.Dispose();
```

- **Append File** 
```csharp
	string tempFile = Path.GetTempFileName();
	BufferedSink sink = EasyIO.Buffer(EasyIO.Sink(tempFile, FileMode.Append));
	sink.WriteUtf8("Hello, ");
	sink.Dispose();
```



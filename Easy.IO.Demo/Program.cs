using System;
using System.Text;

namespace Easy.IO.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            var path = @"C:\Users\chenr\Desktop\test.txt";
            var source = EasyIO.Source(path);
            var bSource = EasyIO.Buffer(source);
            var str = bSource.ReadByteString();
            Console.WriteLine(str.Utf8);
            bSource.Dispose();
            var sink = EasyIO.Sink(path);
            var bSink = EasyIO.Buffer(sink);
            bSink.Flush();
            Console.WriteLine("Hello World!");
        }
    }
}

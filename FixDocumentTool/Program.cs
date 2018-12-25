using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace FixDocumentTool
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Fix配置文件代码生成!");
            Console.WriteLine("开始生成代码!");

            Stopwatch sw = new Stopwatch();
            sw.Start();

            Generator generator = new Generator();
            generator.StartGenerator();
            Console.WriteLine("生成代码完成!");

            Console.WriteLine("耗时: "+ sw.Elapsed.TotalSeconds+" 秒");
            sw.Stop();

            Console.ReadKey();
        }
    }
}

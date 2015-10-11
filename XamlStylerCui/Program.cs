using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Mono.Options;
using XamlStyler.Core;
using XamlStyler.Core.Options;

namespace XamlStylerCui
{
    class Program
    {
        static void Main(string[] args)
        {
            var inputFilepath = string.Empty;

            var optionSet = new OptionSet
            {
                {"i=|input=", "Input Xaml file.", v => inputFilepath = v},
            };
            var extra = optionSet.Parse(args);

            var styler = StylerService.CreateInstance(new StylerOptions());

            var inputXamlText = File.ReadAllText(inputFilepath);
            var outputXamlText = styler.ManipulateTreeAndFormatInput(inputXamlText);

            Console.WriteLine(inputXamlText);
            Console.WriteLine("---------------------");
            Console.WriteLine(outputXamlText);
        }
    }
}

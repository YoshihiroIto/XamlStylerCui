﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Text;
using Mono.Options;
using Xavalon.XamlStyler.Core;
using Xavalon.XamlStyler.Core.Options;
using YamlDotNet.Serialization;

namespace XamlStylerCui
{
    internal class Program
    {
        private static string DefaultOptionFileName { get; } = "XamlStylerCui.yml";

        private static int Main(string[] args)
        {
            ProfileOptimization.SetProfileRoot(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
            ProfileOptimization.StartProfile("XamlStylerCui.StartupProfile");

            var inputFilepath = string.Empty;
            var outputFilepath = string.Empty;
            var optionsFilepath = string.Empty;
            var isCheckStyling = false;
            var isGenerateDefaultOptionsFile = false;
            var isShowHelp = false;

            var optionSet = new OptionSet
            {
                {"i=|input=", "Input file.", v => inputFilepath = v},
                {"o=|output=", "Output file.", v => outputFilepath = v},
                {"options=", "Options file.", v => optionsFilepath = v},
                {"check", "Check Styling.", v => isCheckStyling = v != null},
                {"gen_default_options", "Generate Default Options file.", v => isGenerateDefaultOptionsFile = v != null},
                {"h|help", "Show help.", v => isShowHelp = v != null}
            };

            try
            {
                var extra = optionSet.Parse(args);

                if (extra.Any())
                    inputFilepath = extra.FirstOrDefault();
            }
            catch (OptionException e)
            {
                Console.WriteLine("error:");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `CommandLineOption --help' for more information.");
                return 1;
            }

            if (args.Any() == false)
            {
                ShowUsage(optionSet);
                return 0;
            }

            if (isShowHelp)
            {
                ShowUsage(optionSet);
                return 0;
            }

            if (isGenerateDefaultOptionsFile)
            {
                GenerateDefaultOptionsFile();
                return 0;
            }

            try
            {
                if (isCheckStyling)
                {
                    var isClean = CheckStyling(inputFilepath, optionsFilepath);

                    Console.WriteLine("{0} is {1}.", inputFilepath, isClean ? "clean" : "dirty");
                    return isClean ? 0 : 1;
                }

                ExecuteStyler(inputFilepath, outputFilepath, optionsFilepath);
            }
            catch (Exception e)
            {
                Console.WriteLine("error:");
                Console.WriteLine(e.Message);
                return 1;
            }

            return 0;
        }

        private static void ShowUsage(OptionSet optionSet)
        {
            Console.Error.WriteLine("Usage:XamlStylerCui [OPTIONS]");
            optionSet.WriteOptionDescriptions(Console.Error);
        }

        private static void GenerateDefaultOptionsFile()
        {
            using (var tw = new StringWriter())
            {
                var serializer = new Serializer(SerializationOptions.EmitDefaults);
                serializer.Serialize(tw, new StylerOptions());

                File.WriteAllText(DefaultOptionFileName, tw.ToString(), Encoding.UTF8);
            }
        }

        private static bool CheckStyling(string inputFilepath, string optionsFilepath)
        {
            if (string.IsNullOrEmpty(inputFilepath))
                throw new Exception("Input file is not specified.");

            if (File.Exists(inputFilepath) == false)
                throw new FileNotFoundException(inputFilepath + " is not found.", inputFilepath);

            var options = MakeOptions(optionsFilepath);
            var styler = new StylerService(options);

            var inputText = File.ReadAllText(inputFilepath, Encoding.UTF8);
            var outputText = styler.StyleDocument(inputText);

            var inputLines = inputText.Split('\n').Select(x => x.TrimEnd());
            var outputLines = outputText.Split('\n').Select(x => x.TrimEnd());

            return inputLines.SequenceEqual(outputLines);
        }

        private static void ExecuteStyler(string inputFilepath, string outputFilepath, string optionsFilepath)
        {
            if (string.IsNullOrEmpty(inputFilepath))
                throw new Exception("Input file is not specified.");

            if (File.Exists(inputFilepath) == false)
                throw new FileNotFoundException(inputFilepath + " is not fount.", inputFilepath);

            var options = MakeOptions(optionsFilepath);
            var styler = new StylerService(options);

            var inputText = File.ReadAllText(inputFilepath, Encoding.UTF8);
            var outputText = styler.StyleDocument(inputText);

            if (string.IsNullOrEmpty(outputFilepath))
            {
                Console.Out.Write(outputText);
                Console.Out.Flush();
            }
            else
                File.WriteAllText(outputFilepath, outputText, Encoding.UTF8);
        }

        private static StylerOptions MakeOptions(string optionsFilepath)
        {
            StylerOptions options;
            {
                var actualOptionsFilepath = FindOptionsFilePath(optionsFilepath);

                if (string.IsNullOrEmpty(actualOptionsFilepath) == false)
                {
                    var optionsText = File.ReadAllText(actualOptionsFilepath);
                    options = new Deserializer().Deserialize<StylerOptions>(new StringReader(optionsText));
                }
                else
                    options = new StylerOptions();
            }

            return options;
        }

        private static string FindOptionsFilePath(string inputOptionsFilepath)
        {
            // current to root
            if (string.IsNullOrEmpty(inputOptionsFilepath) == false)
            {
                string optionsFilepath = inputOptionsFilepath;
                {
                    var file = Path.GetFileName(inputOptionsFilepath);
                    var dir = Path.GetDirectoryName(Path.GetFullPath(inputOptionsFilepath));

                    while (dir != null)
                    {
                        optionsFilepath = Path.Combine(dir, file);

                        if (File.Exists(optionsFilepath))
                            break;

                        var parent = Directory.GetParent(dir);
                        if (parent == null)
                            break;

                        dir = parent.FullName;
                    }
                }

                if (File.Exists(optionsFilepath) == false)
                    throw new FileNotFoundException(inputOptionsFilepath + "\nOptions file is not fount.",
                        inputOptionsFilepath);

                return optionsFilepath;
            }

            // home directory
            {
                var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

                var optionsFilePath = Path.Combine(homeDir, DefaultOptionFileName);

                if (File.Exists(optionsFilePath))
                    return optionsFilePath;
            }

            // no options file
            return string.Empty;
        }
    }
}
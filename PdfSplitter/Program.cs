using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using iTextSharp.text.pdf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.FileExtensions;
using Microsoft.Extensions.Configuration.Json;
using NLog;

namespace PdfSplitter
{
    class Program
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        private static readonly string sejdaPath;
        private static readonly string outputFolder;
        private static readonly string inputFolder;
        private static readonly string archiveFolder;
        private static readonly int pagePerFile;
        private static readonly bool overwriteOutput;
        private static readonly bool infoLogOn;
        private static readonly bool archivateOriginalFiles;
        public static List<string> FilesUnderProcessing { get; set; }
        static Program()
        {
            IConfiguration config = new ConfigurationBuilder()
             .AddJsonFile("appsettings.json", true, true)
             .Build();

            bool correctArgs = true;

            try
            {

                correctArgs &= int.TryParse(config["pagePerFile"], out int page);
                correctArgs &= bool.TryParse(config["overwriteOutput"], out bool overwriteOutput_temp);
                correctArgs &= bool.TryParse(config["infoLogOn"], out bool infoLogOn_temp);
                correctArgs &= bool.TryParse(config["archivateOriginalFiles"], out bool archivateOriginalFiles_temp);

                if (!correctArgs)
                {
                    Console.WriteLine("Hibás argumentum");
                    Console.ReadKey();
                    _logger.Error("Hibás argumentum!");
                    throw new ArgumentException("Hibás argumentum!");
                }

                pagePerFile = page;
                infoLogOn = infoLogOn_temp;
                overwriteOutput = overwriteOutput_temp;
                archivateOriginalFiles = archivateOriginalFiles_temp;
                sejdaPath = Path.Combine(Environment.CurrentDirectory, "sejda", "bin");

                var outputFolderTemp = config["output"];
                var inputFolderTemp = config["input"];
                var archiveFolderTemp = config["archive"];

                if (string.IsNullOrWhiteSpace(outputFolderTemp))
                    outputFolder = Path.Combine(Environment.CurrentDirectory, "output");

                if (string.IsNullOrWhiteSpace(inputFolderTemp))
                    inputFolder = Path.Combine(Environment.CurrentDirectory, "input");

                if (string.IsNullOrWhiteSpace(archiveFolderTemp))
                    archiveFolder = Path.Combine(Environment.CurrentDirectory, "archiv");

                FilesUnderProcessing = new List<string>();
                CreateWorkingFoldersIfNotExists();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Hiba a konstruktorban/argumentumban");
            }
        }

        private static void CreateWorkingFoldersIfNotExists()
        {
            if (!string.IsNullOrWhiteSpace(archiveFolder) && !Directory.Exists(archiveFolder))
            { Directory.CreateDirectory(archiveFolder); }

            if (!string.IsNullOrWhiteSpace(inputFolder) && !Directory.Exists(inputFolder))
            { Directory.CreateDirectory(inputFolder); }

            if (!string.IsNullOrWhiteSpace(outputFolder) && !Directory.Exists(outputFolder))
            { Directory.CreateDirectory(outputFolder); }
        }

        static void Main(string[] args)
        {
            ConfigNlog();
            try
            {
                Run();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Hiba a futáskor!");
            }
        }

        private static void Run()
        {
            _logger.Info("STARTED");
            Console.WriteLine("Press ESC to stop");
            do
            {
                while (!Console.KeyAvailable)
                {

                    var fileList = Directory.EnumerateFiles(inputFolder, "*.pdf", SearchOption.AllDirectories);
                    foreach (var file in fileList)
                    {
                        if (File.Exists(file) && !FilesUnderProcessing.Contains(file))
                        {
                            FilesUnderProcessing.Add(file);
                            SplitPdf(file);
                        }
                    }
                    Thread.Sleep(1000);
                }
            } while (Console.ReadKey(true).Key != ConsoleKey.Escape);
        }

        private static void SplitPdf(string file)
        {
            bool IsFileExistsInOutPut = false;
            /*************************************************************************************************************/
            string archiveDestPath = file.Replace(inputFolder, archiveFolder);
            string archiveDestFolder = Path.GetDirectoryName(archiveDestPath);

            var temp = Path.GetDirectoryName(file).Replace(inputFolder, "");
            if (temp.StartsWith("\\")) { temp = temp.Remove(0, 1); }
            var fileOutputFolder = Path.Combine(outputFolder, temp);

            if (overwriteOutput)
            {
                if (File.Exists(archiveDestPath)) { File.Delete(archiveDestPath); }
            }

            if (File.Exists(archiveDestPath))
            {
                _logger.Error($"a {file} már létezik a kimeneten, előbb töröld az archiv és output mappából");
                Console.WriteLine("Press ESC to stop");
                IsFileExistsInOutPut = true;
            }

            if (IsFileExistsInOutPut)
            {
                if (infoLogOn)
                    _logger.Info($"ERROR;{file};Létezik a kimeneten és/vagy az archívban;{fileOutputFolder};{archiveDestFolder}");
                return;
            }

            if (!Directory.Exists(fileOutputFolder)) Directory.CreateDirectory(fileOutputFolder);
            if (!Directory.Exists(archiveDestFolder) && archivateOriginalFiles) Directory.CreateDirectory(archiveDestFolder);
            /*************************************************************************************************************/
            int numberOfPages = 0;

            using (PdfReader pdfReader = new PdfReader(file))
            {
                numberOfPages = pdfReader.NumberOfPages;
            }
            var outputPath = Path.Combine(fileOutputFolder, Path.GetFileName(file));
            if (numberOfPages < pagePerFile)
            {
                File.Copy(file, outputPath, overwriteOutput);
                ProcessDone(file, fileOutputFolder, archiveDestPath);
            }
            else
                using (var process = new Process())
                {

                    var decoratedFile = $"\"{file}\"";
                    var decoratedFileoutputFolder = $"\"{fileOutputFolder}\"";
                    var overwriteString = overwriteOutput ? "-j overwrite" : "";
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                    process.EnableRaisingEvents = true;
                    process.StartInfo.FileName = "\"" + sejdaPath + "\\sejda-console.bat\"";
                    var args = $"splitbyevery {overwriteString} -p [BASENAME]-[FILENUMBER###] -f {decoratedFile} -o {decoratedFileoutputFolder} -n {pagePerFile}";
                    process.StartInfo.Arguments = args;

                    process.Exited += (sender, e) => Process_Exited(sender, e, file, fileOutputFolder, archiveDestPath);

                    process.StartInfo.CreateNoWindow = true;

                    process.Start();
                    process.WaitForExit();
                }
        }


        private static void Process_Exited(object sender, EventArgs e, string file, string fileOutputFolder, string archiveDestPath)
        {
            ProcessDone(file, fileOutputFolder, archiveDestPath);
        }

        private static void ProcessDone(string file, string fileOutputFolder, string archiveDestPath)
        {
            var archive_temp = "";
            if (archivateOriginalFiles)
            {
                File.Move(file, archiveDestPath);
                archive_temp = Path.GetDirectoryName(archiveDestPath);
            }
            FilesUnderProcessing.Remove(file);

            if (infoLogOn && Directory.GetFiles(fileOutputFolder, "*.pdf", SearchOption.AllDirectories).Length > 0)
                _logger.Info($"OK;{file};Szétvágva;{fileOutputFolder};{archive_temp}");
            else
                _logger.Info($"ERROR;{file};HIBA;{fileOutputFolder};{archive_temp}");

            Console.WriteLine("Press ESC to stop");
        }

        private static void ConfigNlog()
        {
            var config = new NLog.Config.LoggingConfiguration();

            var logfileError = new NLog.Targets.FileTarget("logfileError")
            {
                FileName = $@"{Environment.CurrentDirectory}\Error_log.txt",
                Layout = "${longdate};${level:uppercase=true};${logger};${message};${exception}",
                Encoding = System.Text.Encoding.UTF8
            };

            var logfileInfo = new NLog.Targets.FileTarget("logfileInfo")
            {
                FileName = $@"{Environment.CurrentDirectory}\Info_log.txt",
                Layout = "${longdate};${message}",
                Encoding = System.Text.Encoding.UTF8
            };

            var logConsole = new NLog.Targets.ColoredConsoleTarget("Console_log")
            {
                Layout = "${longdate};${level:uppercase=true}|${exception}|${logger}|${message}|${all-event-properties}"
            };

            config.AddRule(LogLevel.Info, LogLevel.Info, logfileInfo);
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logConsole);
            config.AddRule(LogLevel.Error, LogLevel.Fatal, logfileError);

            NLog.LogManager.Configuration = config;
        }
    }
}

﻿using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bin2Base64
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            //var converter = new IStreamConverter[] {new BinaryToTextConverter()};
            var toBase64Converter = new BinaryToTextConverter();
            var inputPath = @"C:\Git\tests\CustomWpfWindow.7z";
            var inFileInfo = new FileInfo(inputPath);
            await using (var fileStream = new FileStream(inputPath + Constants.TextFileExtension, FileMode.Create))
            {
                await toBase64Converter.Convert(inFileInfo, fileStream, CancellationToken.None);
            }

            var toBinConverter = new TextToBinaryConverter();

            var outFileInfo = new FileInfo(inputPath + Constants.TextFileExtension);
            await using var fileStream2 = new FileStream(inputPath + Constants.TextFileExtension + ".7z", FileMode.Create);
            await toBinConverter.Convert(outFileInfo, fileStream2, CancellationToken.None);
        }
    }

    public static class Constants
    {
        public const string TextFileExtension = ".txt";
    }

    internal interface IStreamConverter
    {
        bool CanConvert(FileSystemInfo fileOrFolder);
        Task Convert(FileSystemInfo fileOrFolder, Stream output, CancellationToken cancelToken);
    }

    internal class BinaryToTextConverter : IStreamConverter
    {
        public bool CanConvert(FileSystemInfo fileOrFolder)
        {
            return fileOrFolder.Exists && fileOrFolder is FileInfo &&
                   fileOrFolder.Extension != Constants.TextFileExtension;
        }

        public async Task Convert(FileSystemInfo fileOrFolder, Stream output, CancellationToken cancelToken)
        {
            if (!CanConvert(fileOrFolder)) throw new ArgumentException("CanConvert failed", nameof(fileOrFolder));

            await using var inputStream = new FileStream(fileOrFolder.FullName, FileMode.Open);
            await using var outputWriter = new StreamWriter(output, Encoding.UTF8, 1024, true);

            var buffer = new byte[1024];
            while (true)
            {
                var bytesRead = await inputStream.ReadAsync(buffer, 0, 1024, cancelToken);
                if (bytesRead == 0) break;

                var base64String = System.Convert.ToBase64String(buffer, 0, bytesRead);
                await outputWriter.WriteLineAsync(base64String);
            }
        }
    }

    internal class TextToBinaryConverter : IStreamConverter
    {
        public bool CanConvert(FileSystemInfo fileOrFolder)
        {
            return fileOrFolder.Exists && fileOrFolder is FileInfo &&
                   fileOrFolder.Extension == Constants.TextFileExtension;
        }

        public async Task Convert(FileSystemInfo fileOrFolder, Stream output, CancellationToken cancelToken)
        {
            if (!CanConvert(fileOrFolder)) throw new ArgumentException("CanConvert failed", nameof(fileOrFolder));

            await using var inputStream = new FileStream(fileOrFolder.FullName, FileMode.Open);
            using var inputReader = new StreamReader(inputStream, Encoding.UTF8, true, 1024, true);

            var buffer = new byte[1024];
            while (!inputReader.EndOfStream)
            {
                var base64String = await inputReader.ReadLineAsync();

                var result = System.Convert.FromBase64String(base64String);
                await output.WriteAsync(result, 0, result.Length, cancelToken);
            }

            output.Close();
        }
    }
}
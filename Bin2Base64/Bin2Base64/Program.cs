using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bin2Base64
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var path = args[0];

            if (Directory.Exists(path))
            {
                var tempName = Path.GetTempFileName();
                File.Delete(tempName);
                ZipFile.CreateFromDirectory(path, tempName);

                var toBase64Converter = new BinaryToTextConverter();
                using var output = new FileStream(path + Constants.ZipTextFileExtension, FileMode.Create);
                await toBase64Converter.Convert(new FileInfo(tempName), output, CancellationToken.None);
                File.Delete(tempName);
            }else if (File.Exists(path))
            {
                var toBase64Converter = new BinaryToTextConverter();
                var toBinaryConverter = new TextToBinaryConverter();
                var file = new FileInfo(path);
                if (file.Extension == Constants.TextFileExtension)
                {
                    var newFileName = path.Substring(0, path.Length - Constants.TextFileExtension.Length);
                    using var output = new FileStream(newFileName, FileMode.Create);
                    await toBinaryConverter.Convert(file, output, CancellationToken.None);
                }else if (file.Extension == Constants.ZipTextFileExtension)
                {
                    var reconstructedDirectoryName = path.Substring(0, path.Length - Constants.ZipTextFileExtension.Length);
                    var tempFile = Path.GetTempFileName();
                    using var output = new FileStream(tempFile, FileMode.Open);
                    await toBinaryConverter.Convert(file, output, CancellationToken.None);
                    ZipFile.ExtractToDirectory(tempFile, reconstructedDirectoryName);
                }
                else
                {
                    using var output = new FileStream(file + Constants.TextFileExtension, FileMode.Create);
                    await toBase64Converter.Convert(file, output, CancellationToken.None);
                }
            }
            else
            {
                Console.WriteLine("Expect first argument to be a path to an existing file or folder");
                Console.ReadKey();
            }
        }
    }

    public static class Constants
    {
        public const string TextFileExtension = ".txt";
        public const string ZipTextFileExtension = ".ziptxt";
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

            using var inputStream = new FileStream(fileOrFolder.FullName, FileMode.Open);
            using var outputWriter = new StreamWriter(output, Encoding.UTF8, 1024, true);

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
                   (fileOrFolder.Extension == Constants.TextFileExtension || fileOrFolder.Extension == Constants.ZipTextFileExtension);
        }

        public async Task Convert(FileSystemInfo fileOrFolder, Stream output, CancellationToken cancelToken)
        {
            if (!CanConvert(fileOrFolder)) throw new ArgumentException("CanConvert failed", nameof(fileOrFolder));

            using var inputStream = new FileStream(fileOrFolder.FullName, FileMode.Open);
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
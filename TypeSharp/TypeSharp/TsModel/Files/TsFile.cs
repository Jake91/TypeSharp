using System;
using System.Collections.Generic;
using System.IO;

namespace TypeSharp.TsModel.Files
{
    public class TsFile
    {
        public string Content { get; }
        public string FileName { get; }
        public IReadOnlyCollection<string> FilePath { get; }
        public TsFileType FileType { get; }


        public TsFile(string content, string fileName, IReadOnlyCollection<string> filePath, TsFileType fileType)
        {
            Content = content;
            FileName = fileName;
            FilePath = filePath;
            FileType = fileType;
        }

        public string GetFullFilePath(string outPutFolder)
        {
            return Path.Combine(outPutFolder, $@"{string.Join(Path.DirectorySeparatorChar.ToString(), FilePath)}{Path.DirectorySeparatorChar}{FileName}.{FileTypeString(FileType)}");
        }

        private static string FileTypeString(TsFileType tsFileType)
        {
            switch (tsFileType)
            {
                case TsFileType.TypeScript:
                    return "ts";
                case TsFileType.Definition:
                    return "d.ts";
                default:
                    throw new ArgumentOutOfRangeException(nameof(tsFileType), tsFileType, null);
            }
        }
    }
}

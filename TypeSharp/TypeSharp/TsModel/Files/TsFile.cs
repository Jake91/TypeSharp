using System;
using System.Collections.Generic;
using System.IO;

namespace TypeSharp.TsModel.Files
{
    public class TsFile
    {
        public string Content { get; }

        private readonly string fileName;

        private readonly IReadOnlyCollection<string> filePaths;
        private readonly TsFileType fileType;


        public TsFile(string content, string fileName, IReadOnlyCollection<string> filePaths, TsFileType fileType)
        {
            Content = content;
            this.fileName = fileName;
            this.filePaths = filePaths;
            this.fileType = fileType;
        }

        public string GetFilePath(string outPutFolder) // todo naming?
        {
            return Path.Combine(outPutFolder, $@"{string.Join(Path.DirectorySeparatorChar.ToString(), filePaths)}{Path.DirectorySeparatorChar}{fileName}.{FileType(fileType)}");
        }

        private static string FileType(TsFileType tsFileType)
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

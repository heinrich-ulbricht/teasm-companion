using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace TeasmCompanion.TeamsTokenRetrieval
{
    public class LevelDbLogFileDecoder
    {
        private readonly ILogger logger;

        enum LevelDbRecordTypes
        {
            FULL = 1,
            FIRST = 2,
            MIDDLE = 3,
            LAST = 4
        }

        public LevelDbLogFileDecoder(ILogger logger)
        {
            this.logger = logger.ForContext<LevelDbLogFileDecoder>();
        }

        // implemented based on information from https://github.com/google/leveldb/blob/master/doc/log_format.md
        public async Task<List<string>> ReadLevelDbLogFilesAsync(TeamsTokenPathes tokenPathes, Action<string> handleFullRecord, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                logger.Debug("Start: ReadLevelDbLogFile...");
                var resultLines = new List<string>();
                var logFiles = new List<string>();

                logFiles = tokenPathes.GetLevelDbLogFilePathes();
                foreach (var path in logFiles)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return resultLines;
                    try
                    {
                        var bytesReadFromFile = 0;
                        var bytesReadForCurrentBlock = 0;
                        using var bin = new BinaryReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                        List<byte> content = new List<byte>();
                        var lastType = LevelDbRecordTypes.FULL;
                        var contentAsString = "";
                        while (bytesReadFromFile < bin.BaseStream.Length)
                        {
                            if (cancellationToken.IsCancellationRequested)
                                return resultLines;

                            var checksum = bin.ReadUInt32();
                            var length = bin.ReadUInt16();
                            var type = bin.ReadByte();
                            if (type <= 0 || type > 4 || length == 0)
                            {
                                logger.Debug("Sanity check triggered - invalid type; might be the end of the file");
                                handleFullRecord(contentAsString);
                                resultLines.Add(contentAsString);
                                return resultLines;
                            }
                            if (lastType == LevelDbRecordTypes.FIRST || lastType == LevelDbRecordTypes.MIDDLE)
                            {
                                content.AddRange(bin.ReadBytes(length));
                            }
                            else
                            {
                                // only clear if we are starting with a fresh record
                                content.Clear();
                                content.AddRange(bin.ReadBytes(length));
                            }
                            contentAsString = Encoding.UTF8.GetString(content.ToArray());

                            var bytesOfRecord = 4 + 2 + 1 + length;
                            bytesReadFromFile += bytesOfRecord;
                            bytesReadForCurrentBlock += bytesOfRecord;

                            if ((LevelDbRecordTypes)type == LevelDbRecordTypes.FULL)
                            {
                                // full record, good
                            }
                            else
                            {
                                logger.Debug("Handling non-full record of type {Type}", type);
                            }
                            var bytesLeftInBlock = 32 * 1024 - bytesReadForCurrentBlock;
                            if (bytesLeftInBlock <= 6)
                            {
                                //"A record never starts within the last six bytes of a block (since it won't fit). Any leftover bytes here form the trailer, which must consist entirely of zero bytes and must be skipped by readers."
                                if (bytesLeftInBlock > 0)
                                    bin.ReadBytes(bytesLeftInBlock);
                                bytesReadFromFile += bytesLeftInBlock;
                                bytesReadForCurrentBlock = 0; // reset for next block
                            }

                            if ((LevelDbRecordTypes)type == LevelDbRecordTypes.FULL || (LevelDbRecordTypes)type == LevelDbRecordTypes.LAST)
                            {
                                resultLines.Add(contentAsString);
                                handleFullRecord(contentAsString);
                            }

                            lastType = (LevelDbRecordTypes)type;
                        }
                        logger.Debug("Handled {Path}", path);
                    }
                    catch (FileNotFoundException ioEx)
                    {
                        logger.Error(ioEx, "File not found");
                    }

                }

                // this dumps everything in the temp directory
                //using (StreamWriter outputFile = new StreamWriter(Path.Combine(@"c:\temp\", "leveldb_log.txt"), false))
                //{
                //    foreach (var l in resultLines)
                //    {
                //        outputFile.WriteLine(l);
                //    }
                //}

                logger.Debug("Done: ReadLevelDbLogFile");
                return resultLines;
            }, cancellationToken).ConfigureAwait(false);
        }
    }
}

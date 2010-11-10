﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web.Hosting;
using Composite.Core.Extensions;
using Composite.Core.IO;
using Composite.Core.Logging;


namespace Composite.Plugins.Logging.LogTraceListeners.FileLogTraceListener
{
    /// <summary>
    /// File logger
    /// </summary>
	internal class FileLogger: IDisposable
	{
        private readonly string _logDirectoryPath;
        private readonly bool _flushImmediately;

        public static event ThreadStart OnReset;


	    private LogFileInfo _fileConnection;
        private readonly object _syncRoot = new object();

        public FileLogger(string logDirectoryPath, bool flushImmediately)
        {
            Verify.ArgumentNotNull(logDirectoryPath, "logDirectoryPath");

            _logDirectoryPath = Path.Combine(HostingEnvironment.ApplicationPhysicalPath, logDirectoryPath); 
            _flushImmediately = flushImmediately;
        }

        public DateTime StartupTime
        {
            get
            {
                lock (_syncRoot)
                {
                    if (_fileConnection != null)
                    {
                        return _fileConnection.StartupTime;
                    }
                }
                return DateTime.Now;
            }
        }

        public void WriteEntry(LogEntry entry)
        {
            string logLine = entry.ToString();

            byte[] bytes = Encoding.UTF8.GetBytes(logLine + "\n");

            EnsureInitialize();

            lock(_syncRoot)
            {
                _fileConnection.NewEntries.Add(entry);

                // Checking whether we should change the file after midnight
                int dayNumber = entry.TimeStamp.Day;
                            
                if(dayNumber != _fileConnection.CreationDate.Day 
                   && dayNumber == DateTime.Now.Day)
                {
                    ResetInitialization();
                }

                // Writing the file in the "catch" block in order to prevent chance of corrupting the file by expiriencing ThreadAbortException.
                Exception thrownException = null;
                try
                {
                }
                finally
                {
                    try
                    {
                        _fileConnection.FileStream.Write(bytes, 0, bytes.Length);

                        if (_flushImmediately)
                        {
                            _fileConnection.FileStream.Flush();
                        }
                    }
                    catch (Exception exception)
                    {
                        thrownException = exception;
                    }
                }
                // ThreadAbortException should have a higher prioriry, and therefore we're doing rethrow in a separate block
                if (thrownException != null) throw thrownException;
            }
        }

        public LogFileReader[] GetLogFiles()
        {
            string[] filePathes = C1Directory.GetFiles(_logDirectoryPath);

            string currentlyOpenedFileName = null;

            var result = new List<LogFileReader>();

            lock (_syncRoot)
            {
                if (_fileConnection != null)
                {
                    currentlyOpenedFileName = _fileConnection.FileName;

                    result.Add(new CurrentFileReader(this));
                }
            }

            foreach (string filePath in filePathes)
            {
                string fileName = Path.GetFileName(filePath);

                // Skipping file to which we're currently using
                if (currentlyOpenedFileName != null
                    && string.Compare(fileName, currentlyOpenedFileName, true) == 0)
                {
                    continue;
                }

                // File names have format yyyymmdd[_number].txt
                if (fileName.Length < 12)
                {
                    continue;
                }

                DateTime date;

                if (!DateTime.TryParseExact(fileName.Substring(0, 8),
                                            "yyyyMMdd",
                                            CultureInfo.InvariantCulture.DateTimeFormat,
                                            DateTimeStyles.None,
                                            out date))
                {
                    continue;
                }

                result.Add(new PlainFileReader(filePath, date));
            }

            // Sorting by date
            result.Sort((a, b) => a.Date.CompareTo(b.Date));

            return result.ToArray();
        }

        [DebuggerStepThrough]
        private static C1FileStream TryOpenFile(string filePath, out Exception e)
        {
            e = null;
            try
            {
                return Composite.Core.IO.File.Open(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            }
            catch (Exception ex)
            {
                e = ex;

                return null;
            }
        }

        [DebuggerStepThrough]
        private static bool TryReadAndOpen(string filePath, out string[] content, out C1FileStream stream, out Exception exception)
        {
            content = null;
            stream = null;
            exception = null;

            try
            {
                // TODO: It should open file only once, not twice
                content = Composite.Core.IO.File.ReadAllLines(filePath);

                stream = Composite.Core.IO.File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
                stream.Seek(stream.Length, SeekOrigin.Begin);
            }
            catch (Exception e)
            {
                exception = e;
                return false;
            }

            return true;
        }

        private void EnsureInitialize()
	    {
	        if (_fileConnection != null) return;

	        lock (_syncRoot)
	        {
	            if (_fileConnection != null) return;

                if (!C1Directory.Exists(_logDirectoryPath))
	            {
                    C1Directory.CreateDirectory(_logDirectoryPath);
	            }

	            DateTime creationDate = DateTime.Now;

                string fileNamePrefix = creationDate.ToString("yyyyMMdd");
	            string fileName;
	            C1FileStream stream = null;
                Exception ex;

	            for (int i = 0; i < 10; i++)
	            {
	                fileName = fileNamePrefix + (i > 0 ? "_" + i : string.Empty) + ".txt";
                    string filePath = Path.Combine(_logDirectoryPath, fileName);

                    if (!Composite.Core.IO.File.Exists(filePath))
	                {
	                    stream = TryOpenFile(filePath, out ex);

                        if(stream == null)
	                    {
	                        // Ignoring this exception if the file has already created
                            if (Composite.Core.IO.File.Exists(filePath)) continue;

	                        throw new Exception("Failed to create file '{0}'".FormatWith(filePath), ex);
	                    }

	                    _fileConnection = new LogFileInfo
	                                          {
                                                  CreationDate = creationDate.Date,
                                                  StartupTime = creationDate,
                                                  FileName = fileName,
                                                  FilePath = filePath,
	                                              FileStream = stream,
	                                              OldEntries = new string[0]
	                                          };

                        WriteUTF8EncodingHeader(stream);
	                    return;
	                }

                    string[] alreadyWritten;

                    if (!TryReadAndOpen(filePath, out alreadyWritten, out stream, out ex))
                    {
                        // Trying another file name, since the file may be in use by another process
                        continue;
                    }
	                
	                _fileConnection = new LogFileInfo
	                                      {
                                              CreationDate = creationDate.Date,
                                              StartupTime = creationDate,
                                              FileName = fileName,
	                                          FilePath = filePath,
	                                          FileStream = stream,
	                                          OldEntries = alreadyWritten
	                                      };
	                return;
	            }
	            throw new InvalidOperationException("Failed to open/create a log file");
	        }

	    }

        private void ResetInitialization()
        {
            lock(_syncRoot)
            {
                if(_fileConnection != null)
                {
                    _fileConnection.Dispose();
                    _fileConnection = null;
                }

                if (OnReset != null)
                {
                    OnReset();
                }

                EnsureInitialize();
            }
        }

        private static void WriteUTF8EncodingHeader(Stream stream)
        {
            byte[] preamble = Encoding.UTF8.GetPreamble();
            stream.Write(preamble, 0, preamble.Length);
        }

        internal abstract class LogFileReader
        {

            public DateTime Date { get; protected set; }

            public abstract int EntriesCount { get; }

            public abstract bool Open();
            public abstract void Close();

            public abstract bool Delete();

            public abstract IEnumerable<LogEntry> GetLogEntries(DateTime timeFrom, DateTime timeFromTo);
        }

        private class PlainFileReader: LogFileReader
        {
            private C1FileStream _file;
            private string _filePath;
            private int? _entriesCount;

            public PlainFileReader(string filePath, DateTime date)
            {
                _filePath = filePath;
                Date = date;
            }

            [DebuggerStepThrough]
            public override bool Open()
            {
                try
                {
                    _file = Composite.Core.IO.File.OpenRead(_filePath);
                }
                catch(Exception)
                {
                    return false;
                }
                return true;
            }

            public override void Close()
            {
                if (_file == null) return;

                _file.Close();
                _file.Dispose();
                _file = null;
            }

            public override IEnumerable<LogEntry> GetLogEntries(DateTime timeFrom, DateTime timeFromTo)
            {
                StringBuilder sb = new StringBuilder();

                LogEntry previousEntry = null;
                using (var reader = new Composite.Core.IO.StreamReader(_file, Encoding.UTF8))
                {
                    while (reader.Peek() >= 0)
                    {
                        string line = reader.ReadLine();

                        LogEntry entry = LogEntry.Parse(line);
                        if (entry != null)
                        {
                            if (previousEntry != null)
                            {
                                if(sb.Length > 0)
                                {
                                    previousEntry.Message = sb.ToString();
                                    sb.Clear();
                                }

                                yield return previousEntry;
                            }
                            previousEntry = entry;
                        }
                        else
                        {
                            if (previousEntry != null)
                            {
                                if(sb.Length == 0)
                                {
                                    sb.Append(previousEntry.Message);
                                }

                                sb.Append("\n").Append(line);
                            }
                        }
                    }
                }
                if(previousEntry != null)
                {
                    if (sb.Length > 0)
                    {
                        previousEntry.Message = sb.ToString();
                        sb.Clear();
                    }

                    yield return previousEntry;
                }
            }

            public override int EntriesCount
            {
                get
                {
                    if(_entriesCount == null)
                    {
                        try
                        {
                            Open();

                            _entriesCount = GetLogEntries(DateTime.MinValue, DateTime.MaxValue).Count();
                        }
                        finally
                        {
                            Close();
                        }
                    }
                    
                    return (int)_entriesCount;
                }
            }

            public override bool Delete()
            {
                try
                {
                    Composite.Core.IO.File.Delete(_filePath);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        private class CurrentFileReader : LogFileReader
        {
            private readonly FileLogger _fileLogger;

            public CurrentFileReader(FileLogger fileLogger)
            {
                _fileLogger = fileLogger;

                lock (_fileLogger._syncRoot)
                {
                    var fileConnection = _fileLogger._fileConnection;

                    if (fileConnection != null)
                    {
                        Date = fileConnection.CreationDate;
                    }
                }

            }

            public override bool Open()
            {
                // do nothing
                return true;
            }

            public override void Close()
            {
                // do nothing
            }

            public override int EntriesCount
            {
                get
                {
                    lock(_fileLogger._syncRoot)
                    {
                        return _fileLogger._fileConnection.OldEntries.Length +
                               _fileLogger._fileConnection.NewEntries.Count;
                    }
                }
            }

            public override IEnumerable<LogEntry> GetLogEntries(DateTime timeFrom, DateTime timeTo)
            {
                if (timeFrom < _fileLogger.StartupTime)
                {
                    foreach (var str in _fileLogger._fileConnection.OldEntries)
                    {
                        var oldEntry = LogEntry.Parse(str);
                        if (oldEntry != null)
                        {
                            yield return oldEntry;
                        }
                    }
                }


                LogEntry[] newEntries = null;

                lock (_fileLogger._syncRoot)
                {
                    var fileConnection = _fileLogger._fileConnection;
                    if (fileConnection != null)
                    {
                        newEntries = fileConnection.NewEntries.ToArray();
                    }
                }

                if (newEntries != null)
                {
                    foreach (var logEntry in newEntries) yield return logEntry;
                }
            }

            public override bool Delete()
            {
                return false;
            }
        }

        private class LogFileInfo: IDisposable
        {
            public string FileName;
            public string FilePath;
            public C1FileStream FileStream;
            public string[] OldEntries;
            public List<LogEntry> NewEntries = new List<LogEntry>();
            public DateTime CreationDate;
            public DateTime StartupTime;

            private bool disposed = false;
            
            public void Dispose()
            {
                if (!disposed)
                {
                    FileStream.Close();
                    disposed = true;
                }
            }

            ~LogFileInfo()
            {
                Dispose();
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            if(_fileConnection != null)
            {
                _fileConnection.Dispose();
            }
        }

        #endregion
    }
}

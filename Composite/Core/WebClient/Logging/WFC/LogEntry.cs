﻿using System;
using System.Runtime.Serialization;

namespace Composite.Core.WebClient.Logging.WCF
{
    [DataContract(Name = "LogEntry", Namespace = "http://schemas.datacontract.org/2004/07/Composite.Logging.WCF")]
	internal class LogEntry
	{
        public LogEntry()
        {
        }

        public LogEntry(Composite.Core.Logging.LogEntry fileLogEntry)
        {
            TimeStamp = fileLogEntry.TimeStamp;
            ApplicationDomainId = fileLogEntry.ApplicationDomainId;
            ThreadId = fileLogEntry.ThreadId;
            Severity = fileLogEntry.Severity;
            Title = fileLogEntry.Title;
            DisplayOptions = fileLogEntry.DisplayOptions;
            Message = fileLogEntry.Message;
        }

        [DataMember]
        public DateTime TimeStamp;

        [DataMember]
        public int ApplicationDomainId;

        [DataMember]
        public int ThreadId;

        [DataMember]
        public string Severity;

        [DataMember]
        public string Title;

        [DataMember]
        public string DisplayOptions;

        [DataMember]
        public string Message;
	}
}

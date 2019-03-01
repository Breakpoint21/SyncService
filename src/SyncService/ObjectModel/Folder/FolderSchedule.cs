using System;
using System.Collections.Generic;

namespace SyncService.ObjectModel.Folder
{
    public interface IFolderSchedule
    {
        
    }

    public class FolderPeriodicSchedule : IFolderSchedule
    {
        public TimeSpan Interval { get; set; }
    }

    public class FolderDateTimeSchedule : IFolderSchedule
    {
        public DateTime Time { get; set; }
        public IEnumerable<DayOfWeek> Days { get; set; }
    }
}
using System;

namespace AudienciasApp.Models
{
    public class RecentFile
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
        public long FileSize { get; set; }
    }
}

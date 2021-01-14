namespace Pruner.Models
{
    class StateLineCoverage
    {
        public long LineNumber { get; set; }
        public string FileId { get; set; }
        public string[] TestIds { get; set; }
    }
}
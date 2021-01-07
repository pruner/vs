namespace Pruner
{
    class StateLineCoverage
    {
        public long LineNumber { get; set; }
        public long FileId { get; set; }
        public long[] TestIds { get; set; }
    }
}
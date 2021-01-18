namespace Pruner.Models
{
    internal class StateFileCoverage
    {
        public string Path { get; set; }
        public long[] LineCoverage { get; set; }
    }
}
namespace Pruner.Models
{
    internal class State
    {
        public StateTest[] Tests { get; set; }
        public StateFile[] Files { get; set; }
        public StateLineCoverage[] Coverage { get; set; }
    }
}
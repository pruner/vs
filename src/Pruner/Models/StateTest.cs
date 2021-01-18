namespace Pruner.Models
{
    class StateTest
    {
        public string Name { get; set; }
        public long Duration { get; set; }
        public StateFileCoverage[] FileCoverage { get; set; }
        public StateTestFailure Failure { get; set; }
    }
}
namespace Pruner
{
    internal class TestViewModel
    {
        public string FilePath { get; set; }
        public string Name { get; set; }

        public StateTestFailure Failure { get; set; }

        public long Duration { get; set; }
    }
}
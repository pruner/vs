namespace Pruner.Models
{
    class StateTestFailure
    {
        public string[] Stdout { get; set; }
        public string Message { get; set; }
        public string[] StackTrace { get; set; }

        public string StdoutString => string.Join("\n", Stdout);
        public string StackTraceString => string.Join("\n", StackTrace);
    }
}
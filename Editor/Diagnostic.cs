using Editor.Range;

namespace Editor
{
    public enum DiagnosticSeverity
    {
        Error = 1,
        Warning,
        Information,
        Hint,
    }

    public enum DiagnosticTag
    {
        Unnecessary,
        Deprecated,
    }

    public class Diagnostic
    {
        public Diagnostic(RangeBase range, string message)
        {
            this.Range = range;
            this.Message = message;
        }

        public RangeBase Range { get; }

        public string Message { get; }

        public DiagnosticSeverity? Severity { get; set; }

        public string? Code { get; set; }

        public string? CodeDescription { get; set; }

        public string? Source { get; set; }

        public DiagnosticTag[]? Tags { get; set; }
    }
}

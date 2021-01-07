using System.Linq;
using Microsoft.VisualStudio.Text.Editor;

namespace Pruner
{
    internal class CoverageTag : IGlyphTag
    {
        public TestViewModel[] Tests { get; set; }

        public string Color => Tests?.Any(t => t.Failure != null) == true ? "#FF0000" : "#00FF00";
    }
}
using System.Linq;
using Microsoft.VisualStudio.Text.Editor;
using Pruner.UI;

namespace Pruner.Tagging
{
    internal class CoverageTag : IGlyphTag
    {
        public LineTestViewModel[] Tests { get; set; }

        public string Color => Tests?.Any(t => t.Failure != null) == true ? "#FF0000" : "#00FF00";
    }
}
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using Pruner.Tagging;
using Pruner.UI.Window;

namespace Pruner.UI.Glyph
{
    /// <summary>
    /// Interaction logic for CoverageGlyph.xaml
    /// </summary>
    public partial class CoverageGlyph : UserControl
    {
        public CoverageGlyph()
        {
            InitializeComponent();
        }

        private async void CoverageGlyph_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            var package = PrunerPackage.Instance;
            if (package == null)
                return;

            var coverageTag = DataContext as CoverageTag;
            if (coverageTag == null)
                throw new InvalidOperationException("Could not find coverage tag.");
            
            var window = (TestsWindow)await package.ShowToolWindowAsync(typeof(TestsWindow), 0, true, package.DisposalToken);
            if (window?.Frame == null)
                throw new NotSupportedException("Cannot create tool window.");

            var content = window.Content as TestsWindowControl;
            if (content == null)
                throw new InvalidOperationException("Could not find tests window view model.");

            var viewModel = new TestsWindowViewModel()
            {
                Tests = coverageTag.Tests
            };
            content.DataContext = viewModel;
        }
    }
}

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Constants = EnvDTE.Constants;

namespace Pruner.UI.Window
{
    /// <summary>
    /// Interaction logic for TestsWindowControl.
    /// </summary>
    public partial class TestsWindowControl : UserControl
    {
        internal TestsWindowViewModel ViewModel => DataContext as TestsWindowViewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestsWindowControl"/> class.
        /// </summary>
        public TestsWindowControl()
        {
            this.InitializeComponent();
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            var test = ViewModel?.SelectedLineTest;
            if (test == null)
                return;
            
            var dte = Package.GetGlobalService(typeof(SDTE)) as DTE2;
            if (dte == null)
                return;

            var projects = dte.Solution.Projects
                .Cast<Project>()
                .ToArray();
            var codeModels = projects
                .Select(x => x.CodeModel)
                .Cast<CodeModel2>()
                .ToArray();
            var codeElements = codeModels
                .Select(x => x.CodeTypeFromFullName(test.FullClassName))
                .Where(x => x != null)
                .SelectMany(x => x
                    .Members
                    .Cast<CodeElement2>())
                .ToArray();
            var matchingMembers = codeElements
                .Where(x => x.FullName == test.FullName)
                .ToArray();

            foreach (var member in matchingMembers)
            {
                var window = member.ProjectItem.Open(Constants.vsViewKindCode);
                window.Visible = true;

                var selection = window.Document.Selection as TextSelection;
                if(selection != null)
                    selection.MoveToPoint(member.StartPoint);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
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

            try
            {
                var test = ViewModel?.SelectedLineTest;
                if (test == null)
                    return;

                var dte = Package.GetGlobalService(typeof(SDTE)) as DTE2;
                if (dte == null)
                    return;

                var projects = dte.Solution.Projects
                    .Cast<Project>()
                    .Where(x => x != null)
                    .ToArray();
                var codeModels = projects
                    .Select(x => x.CodeModel)
                    .Cast<CodeModel2>()
                    .Where(x => x != null)
                    .ToArray();
                var codeElements = codeModels
                    .Select(x => x.CodeTypeFromFullName(test.FullClassName))
                    .Where(x => x != null)
                    .SelectMany(x => x
                        .Members
                        .Cast<CodeElement2>()
                        .Where(f => f != null))
                    .ToArray();
                var matchingMembers = codeElements
                    .Where(x => x.FullName == test.FullName)
                    .ToArray();

                OutputLogger.Log("Matched members", matchingMembers.Length);

                foreach (var member in matchingMembers)
                {
                    var projectItem = GetProjectItemFromCodeElement(member);
                    if (projectItem == null || projectItem.FileCount == 0)
                        continue;

                    OutputLogger.Log("Opening document", projectItem.FileNames[0]);

                    var window = projectItem.Open(Constants.vsViewKindCode);
                    window.Visible = true;

                    var selection = window.Document.Selection as TextSelection;
                    if (selection != null)
                        selection.MoveToPoint(member.StartPoint);
                }
            }
            catch (Exception ex)
            {
                OutputLogger.Log("An error occured while trying to navigate to a test", ex);
            }
        }

        private static ProjectItem GetProjectItemFromCodeElement(CodeElement2 member)
        {
            try
            {
                return member.ProjectItem;
            }
            catch (COMException ex)
            {
                if (ex.ErrorCode == -2147467259)
                {
                    //this may happen in certain scenarios. not sure why yet.
                    return null;
                }

                throw;
            }
        }
    }
}
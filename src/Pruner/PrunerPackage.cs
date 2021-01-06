using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.Workspace;

namespace Pruner
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PrunerPackage.PackageGuidString)]
    public sealed class PrunerPackage : AsyncPackage
    {
        /// <summary>
        /// PrunerPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "d8405035-5302-4c83-af49-41d137287c00";

        internal static StateFileMonitor StateFileMonitor { get; private set; }

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            var dte = await ServiceProvider.GetGlobalServiceAsync(typeof(SDTE)) as DTE2;
            if (dte == null)
                return;
            
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var solutionFileName = dte.Solution.FileName;
            if (solutionFileName == null)
                return;

            var directoryPath = GetPrunerPathFromSolutionPath(solutionFileName);
            if (directoryPath == null)
                return;

            if (StateFileMonitor != null)
                throw new InvalidOperationException("State file monitor already initialized.");

            StateFileMonitor = new StateFileMonitor(directoryPath);
        }

        private static string GetPrunerPathFromSolutionPath(string solutionFileName)
        {
            var directoryPath = Path.GetDirectoryName(solutionFileName);
            while (directoryPath != null)
            {
                var prunerPath = Path.Combine(directoryPath, ".pruner");
                if (!Directory.Exists(prunerPath))
                    continue;

                directoryPath = prunerPath;
                break;
            }

            return directoryPath;
        }

        #endregion
    }
}

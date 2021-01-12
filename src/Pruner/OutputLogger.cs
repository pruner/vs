using System;
using System.Linq;
using System.Windows.Markup;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Pruner
{
    public class OutputLogger
    {
        private static OutputWindowPane _cachedPane;

        public static void Log(params object[] text)
        {
            lock (typeof(OutputLogger))
            {
                var pane = GetPane();
                if (pane == null)
                    return;
                
                ThreadHelper.ThrowIfNotOnUIThread();
                pane.OutputString($"{string.Join("\n\t", text.Select(x => x?.ToString()))}\n");
            }
        }

        private static OutputWindowPane GetPane()
        {
            if (_cachedPane != null)
                return _cachedPane;
            
            var dte = Package.GetGlobalService(typeof(SDTE)) as DTE2;
            if (dte == null)
                return null;
            
            ThreadHelper.ThrowIfNotOnUIThread();
            var panes = dte.ToolWindows.OutputWindow.OutputWindowPanes;

            OutputWindowPane pane;
            try
            {
                pane = panes.Item("Pruner");
            }
            catch (ArgumentException)
            {
                pane = panes.Add("Pruner");
            }

            return _cachedPane = pane;
        }
    }
}
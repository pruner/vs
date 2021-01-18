using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Events;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Task = System.Threading.Tasks.Task;

namespace Pruner.Models
{
    class StateFileMonitor : IDisposable
    {
        private FileSystemWatcher _watcher;

        public event Action StatesChanged;

        public IReadOnlyCollection<State> States { get; private set; } = Array.Empty<State>();

        public string GitDirectoryPath => Path.GetDirectoryName(PrunerDirectoryPath);
        public string StateDirectoryPath => Path.Combine(PrunerDirectoryPath, "state");
        public string PrunerDirectoryPath { get; private set; }

        private static StateFileMonitor _instance;

        public static StateFileMonitor Instance
        {
            get
            {
                lock (typeof(StateFileMonitor))
                {
                    if (_instance != null)
                        return _instance;

                    _instance = new StateFileMonitor();
                    return _instance;
                }
            }
        }

        private StateFileMonitor()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            SolutionEvents.OnAfterBackgroundSolutionLoadComplete += SolutionEvents_OnAfterBackgroundSolutionLoadComplete;
            SolutionEvents.OnBeforeCloseSolution += SolutionEvents_OnBeforeCloseSolution;
        }

        private void SolutionEvents_OnBeforeCloseSolution(object sender, EventArgs e)
        {
            ResetState();
        }

        private void SolutionEvents_OnAfterBackgroundSolutionLoadComplete(object sender, EventArgs e)
        {
            lock (typeof(StateFileMonitor))
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                var dte = Package.GetGlobalService(typeof(SDTE)) as DTE2;

                var solutionFileName = dte?.Solution.FileName;
                if (solutionFileName == null)
                    throw new InvalidOperationException("No solution present.");

                var prunerDirectory = GetPrunerPathFromSolutionPath(solutionFileName);
                if (prunerDirectory == null)
                    return;
                
                PrunerDirectoryPath = prunerDirectory;

                _watcher = new FileSystemWatcher(StateDirectoryPath)
                {
                    EnableRaisingEvents = true,
                    IncludeSubdirectories = false
                };

                _watcher.Changed += _watcher_Changed;
                _watcher.Created += _watcher_Created;
                _watcher.Deleted += _watcher_Deleted;
                _watcher.Renamed += _watcher_Renamed;

                OnFileChangedAsync().ConfigureAwait(false);
            }
        }

        private static string GetPrunerPathFromSolutionPath(string solutionFileName)
        {
            var directoryPath = Path.GetDirectoryName(solutionFileName);
            while (directoryPath != null)
            {
                var prunerPath = Path.Combine(directoryPath, ".pruner");
                if (!Directory.Exists(prunerPath))
                {
                    directoryPath = Path.GetDirectoryName(directoryPath);
                    continue;
                }

                directoryPath = prunerPath;
                break;
            }

            return directoryPath;
        }

        private async Task OnFileChangedAsync()
        {
            await Task.Delay(100);

            lock (typeof(StateFileMonitor))
            {
                try
                {
                    States = Directory
                        .GetFiles(StateDirectoryPath)
                        .Select(x =>
                        {
                            using (var stream = File.Open(x, FileMode.Open, FileAccess.Read, FileShare.Read))
                            using (var reader = new StreamReader(stream))
                                return reader.ReadToEnd();
                        })
                        .Select(json => JsonConvert.DeserializeObject<State>(
                            json,
                            new JsonSerializerSettings()
                            {
                                ContractResolver = new CamelCasePropertyNamesContractResolver()
                            }))
                        .ToImmutableArray();
                    OnStatesChanged();
                }
                catch (IOException)
                {
                    //might be caused due to file being in use.
                }
                catch (Exception ex)
                {
                    OutputLogger.Log("An error occured while deserializing state.", ex);

                    if (Debugger.IsAttached)
                        Debugger.Break();
                }
            }
        }

        private void _watcher_Renamed(object sender, RenamedEventArgs e)
        {
            OnFileChangedAsync().ConfigureAwait(false);
        }

        private void _watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            OnFileChangedAsync().ConfigureAwait(false);
        }

        private void _watcher_Created(object sender, FileSystemEventArgs e)
        {
            OnFileChangedAsync().ConfigureAwait(false);
        }

        private void _watcher_Changed(object sender, FileSystemEventArgs e)
        {
            OnFileChangedAsync().ConfigureAwait(false);
        }

        public void Dispose()
        {
            lock (typeof(StateFileMonitor))
            {
                SolutionEvents.OnAfterBackgroundSolutionLoadComplete -= SolutionEvents_OnAfterBackgroundSolutionLoadComplete;
                SolutionEvents.OnBeforeCloseSolution -= SolutionEvents_OnBeforeCloseSolution;

                ResetState();
            }
        }

        private void ResetState()
        {
            if (_watcher != null)
            {
                _watcher.Changed -= _watcher_Changed;
                _watcher.Created -= _watcher_Created;
                _watcher.Deleted -= _watcher_Deleted;
                _watcher.Renamed -= _watcher_Renamed;

                _watcher.Dispose();
                _watcher = null;
            }

            States = Array.Empty<State>();
        }

        protected virtual void OnStatesChanged()
        {
            StatesChanged?.Invoke();
        }
    }
}

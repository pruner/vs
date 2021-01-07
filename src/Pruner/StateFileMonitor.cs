using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.IO;
using System.Linq;
using System.Text;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Task = System.Threading.Tasks.Task;

namespace Pruner
{
    class StateFileMonitor : IDisposable
    {
        private readonly FileSystemWatcher _watcher;
        private readonly string _stateDirectoryPath;

        public event Action StatesChanged;

        public IReadOnlyCollection<State> States { get; private set; } = Array.Empty<State>();

        public string GitDirectoryPath { get; private set; }

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

            var dte = Package.GetGlobalService(typeof(SDTE)) as DTE2;

            var solutionFileName = dte?.Solution.FileName;
            if (solutionFileName == null)
                throw new InvalidOperationException("No solution present.");

            var directoryPath = GetPrunerPathFromSolutionPath(solutionFileName);
            if (directoryPath == null)
                return;

            GitDirectoryPath = Path.GetDirectoryName(directoryPath);
            _stateDirectoryPath = Path.Combine(directoryPath, "state");
            
            _watcher = new FileSystemWatcher(directoryPath)
            {
                EnableRaisingEvents = true,
                IncludeSubdirectories = true
            };

            _watcher.Changed += _watcher_Changed;
            _watcher.Created += _watcher_Created;
            _watcher.Deleted += _watcher_Deleted;
            _watcher.Renamed += _watcher_Renamed;

            OnFileChangedAsync().ConfigureAwait(false);
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

        private async Task OnFileChangedAsync()
        {
            await Task.Delay(100);

            try
            {
                States = Directory
                    .GetFiles(_stateDirectoryPath)
                    .Select(x =>
                    {
                        using(var stream = File.Open(x, FileMode.Open, FileAccess.Read, FileShare.Read))
                        using(var reader = new StreamReader(stream))
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
            if (_watcher == null)
                return;

            _watcher.Changed -= _watcher_Changed;
            _watcher.Created -= _watcher_Created;
            _watcher.Deleted -= _watcher_Deleted;
            _watcher.Renamed -= _watcher_Renamed;

            _watcher.Dispose();
        }

        protected virtual void OnStatesChanged()
        {
            StatesChanged?.Invoke();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Pruner
{
    class State
    {
        public StateTest[] Tests { get; set; }
        public StateFile[] Files { get; set; }
        public StateLineCoverage[] Coverage { get; set; }
    }

    class StateTest
    {
        public string Name { get; set; }
        public long Id { get; set; }
        public long Duration { get; set; }
        public StateTestFailure Failure { get; set; }
    }

    class StateTestFailure
    {
        public string[] Stdout { get; set; }
        public string Message { get; set; }
        public string[] StackTrace { get; set; }
    }

    class StateFile
    {
        public long Id { get; set; }
        public string Path { get; set; }
    }

    class StateLineCoverage
    {
        public long LineNumber { get; set; }
        public long FileId { get; set; }
        public long[] TestIds { get; set; }
    }

    class StateFileMonitor : IDisposable
    {
        private readonly FileSystemWatcher _watcher;
        private readonly string _stateDirectoryPath;

        public event Action StatesChanged;

        public IReadOnlyCollection<State> States { get; private set; }
        public string GitDirectory { get; private set; }

        public StateFileMonitor(string prunerDirectoryPath)
        {
            GitDirectory = Path.GetDirectoryName(prunerDirectoryPath);

            var stateDirectoryPath = GetStateDirectoryPath(prunerDirectoryPath);
            _stateDirectoryPath = stateDirectoryPath;

            _watcher = new FileSystemWatcher(stateDirectoryPath)
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

        private async Task OnFileChangedAsync()
        {
            await Task.Delay(100);

            try
            {
                States = Directory
                    .GetFiles(_stateDirectoryPath)
                    .Select(x =>
                    {
                        using var stream = File.Open(x, FileMode.Open, FileAccess.Read, FileShare.Read);
                        using var reader = new StreamReader(stream);
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
            _watcher.Changed -= _watcher_Changed;
            _watcher.Created -= _watcher_Created;
            _watcher.Deleted -= _watcher_Deleted;
            _watcher.Renamed -= _watcher_Renamed;

            _watcher.Dispose();
        }

        private static string GetStateDirectoryPath(string prunerDirectoryPath)
        {
            return Path.Combine(prunerDirectoryPath, "state");
        }

        protected virtual void OnStatesChanged()
        {
            StatesChanged?.Invoke();
        }
    }
}

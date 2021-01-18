using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Pruner.Annotations;
using Pruner.Models;
using Pruner.UI.Window;

namespace Pruner.UI
{
    internal class LineTestViewModel : INotifyPropertyChanged
    {
        private bool _isSelected;
        
        public string FullName { get; set; }

        public string Color => IsFailed ? "#FF0000" : "#00FF00";

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value == _isSelected) return;
                _isSelected = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(ShouldShowFailure));
            }
        }

        public bool IsFailed => Failure != null;
        public bool IsSucceeded => Failure == null;

        public bool ShouldShowFailure => IsFailed && IsSelected;

        public string FullClassName
        {
            get
            {
                var segments = FullName.Split('.');
                return string.Join(".", segments.Take(segments.Length - 1));
            }
        }

        public string Name => FullName
            .Substring(FullClassName.Length)
            .Trim('.');

        public StateTestFailure Failure { get; set; }

        public long? Duration { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
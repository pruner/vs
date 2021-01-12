using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Pruner.Annotations;
using Pruner.Models;

namespace Pruner.UI.Window
{
    internal class TestsWindowViewModel
    {
        public LineTestViewModel[] Tests { get; set; }

        public LineTestViewModel SelectedLineTest { get; set; }
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class TestsWindowDesignerViewModel : TestsWindowViewModel
    {
        public TestsWindowDesignerViewModel()
        {
            Tests = new[]
            {
                new LineTestViewModel()
                {
                    FullName = "FluffySpoon.Domain.SomeTest",
                    Failure = new StateTestFailure()
                    {
                        Message = "Some failure message.",
                        StackTrace = new []
                        {
                            "at line 29",
                            "at line 1337"
                        },
                        Stdout = new []
                        {
                            "Hello world 1",
                            "Hello world 2",
                            "Hello world 3"
                        }
                    }
                },
                new LineTestViewModel()
                {
                    FullName = "FluffySpoon.Infrastructure.SomeOtherTest"
                }
            };
        }
    }
}
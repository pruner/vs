//***************************************************************************
// 
//    Copyright (c) Microsoft Corporation. All rights reserved.
//    This code is licensed under the Visual Studio SDK license terms.
//    THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
//    ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
//    IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
//    PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//***************************************************************************

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Classification;
using Pruner;

namespace Pruner
{
    /// <summary>
    /// This class implements ITagger for CoverageTag.  It is responsible for creating
    /// CoverageTag TagSpans, which our GlyphFactory will then create glyphs for.
    /// </summary>
    internal class CoverageTagger : ITagger<CoverageTag>, IDisposable
    {
        private readonly ITextBuffer _textBuffer;
        private readonly ITextView _textView;

        public CoverageTagger(
            ITextView textView,
            ITextBuffer textBuffer)
        {
            _textBuffer = textBuffer;
            _textView = textView;
            
            StateFileMonitor.Instance.StatesChanged += StateFileMonitor_StatesChanged;
        }

        private void StateFileMonitor_StatesChanged()
        {
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(
                new SnapshotSpan(
                    _textView.TextSnapshot, 
                    new Span(0, _textView.TextSnapshot.Length))));
        }

        /// <summary>
        /// This method creates CoverageTag TagSpans over a set of SnapshotSpans.
        /// </summary>
        /// <param name="spans">A set of spans we want to get tags for.</param>
        /// <returns>The list of CoverageTag TagSpans.</returns>
        IEnumerable<ITagSpan<CoverageTag>> ITagger<CoverageTag>.GetTags(NormalizedSnapshotSpanCollection spans)
        {
            var sanitizedFilePath = GetSanitizedFileDirectory();
            if(sanitizedFilePath == null)
                yield break;

            var relevantState = StateFileMonitor.Instance.States
                .SingleOrDefault(x => x
                    .Files
                    .Any(f => f.Path == sanitizedFilePath));
            if (relevantState == null)
                yield break;

            var coveredFile = relevantState.Files
                .Single(x => x.Path == sanitizedFilePath);

            var coveredLines = relevantState.Coverage
                .Where(x => x.FileId == coveredFile.Id)
                .ToImmutableArray();

            var textViewLines = _textView.TextSnapshot.Lines.ToImmutableArray();
            foreach (var coveredLine in coveredLines)
            {
                var coveredTests = coveredLine.TestIds
                    .Select(x => relevantState
                        .Tests
                        .Single(y => x == y.Id))
                    .ToImmutableArray();

                var textViewLine = textViewLines
                    .SingleOrDefault(x => x.LineNumber + 1 == coveredLine.LineNumber);
                if(textViewLine == null)
                    continue;
                
                var coverageSpan = new SnapshotSpan(
                    _textView.TextSnapshot, 
                    new Span(
                        textViewLine.Start, 
                        0));
                yield return new TagSpan<CoverageTag>(
                    coverageSpan, 
                    new CoverageTag()
                    {
                        Tests = coveredTests
                            .Select(x => new TestViewModel()
                            {
                                Failure = x.Failure,
                                Duration = x.Duration,
                                FilePath = coveredFile.Path,
                                Name = x.Name
                            })
                            .ToArray()
                    });
            }
        }

        private string GetSanitizedFileDirectory()
        {
            var gitRootDirectory = StateFileMonitor.Instance.GitDirectoryPath;
            if(gitRootDirectory == null)
                return null;

            var filePath = GetFileName(_textBuffer);
            if (filePath.StartsWith(gitRootDirectory))
                filePath = filePath.Substring(gitRootDirectory.Length + 1);

            return filePath.Replace("\\", "/");
        }

        private string GetFileName(ITextBuffer buffer)
        {
            buffer.Properties.TryGetProperty(
                typeof(ITextDocument), out ITextDocument document);
            return document == null ? null : document.FilePath;
        }

        public void Dispose()
        {
            StateFileMonitor.Instance.StatesChanged -= StateFileMonitor_StatesChanged;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }
}

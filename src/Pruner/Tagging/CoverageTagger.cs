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
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Newtonsoft.Json;
using Pruner.Models;
using Pruner.UI;

namespace Pruner.Tagging
{
    /// <summary>
    /// This class implements ITagger for CoverageTag.  It is responsible for creating
    /// CoverageTag TagSpans, which our GlyphFactory will then create glyphs for.
    /// </summary>
    internal class CoverageTagger : ITagger<CoverageTag>, IDisposable
    {
        private readonly ITextBuffer _textBuffer;
        private readonly ITextView _textView;

        private readonly IDictionary<string, ITagSpan<CoverageTag>[]> _tagSpanCache;

        public CoverageTagger(
            ITextView textView,
            ITextBuffer textBuffer)
        {
            _textBuffer = textBuffer;
            _textView = textView;
            _tagSpanCache = new Dictionary<string, ITagSpan<CoverageTag>[]>();

            StateFileMonitor.Instance.StatesChanged += StateFileMonitor_StatesChanged;
        }

        private void StateFileMonitor_StatesChanged()
        {
            _tagSpanCache.Clear();
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
            lock (typeof(CoverageTagger))
            {
                var sanitizedFilePath = GetSanitizedFilePath();
                if (sanitizedFilePath == null)
                    return Array.Empty<ITagSpan<CoverageTag>>();

                if (_tagSpanCache.ContainsKey(sanitizedFilePath))
                    return _tagSpanCache[sanitizedFilePath];
                
                var relevantTests = StateFileMonitor.Instance.States
                    .SelectMany(x => x.Tests)
                    .Where(t => t.FileCoverage
                        .Any(f => f.Path == sanitizedFilePath))
                    .SelectMany(t => t.FileCoverage
                        .Select(f => new
                        {
                            File = f,
                            Test = t
                        }))
                    .Where(x => x.File.Path == sanitizedFilePath)
                    .ToArray();

                var coveredLines = relevantTests
                    .SelectMany(x => x.File.LineCoverage
                        .Select(l => new
                        {
                            Test = x.Test,
                            Line = l
                        }))
                    .GroupBy(l => l.Line)
                    .Select(g => new
                    {
                        Line = g.Key,
                        Tests = g
                            .Select(t => t.Test)
                            .ToArray()
                    })
                    .ToArray();

                var textViewLines = _textView.TextSnapshot.Lines.ToImmutableArray();

                var result = new List<ITagSpan<CoverageTag>>();
                foreach (var coveredLine in coveredLines)
                {
                    var textViewLine = textViewLines
                        .SingleOrDefault(x => x.LineNumber + 1 == coveredLine.Line);
                    if (textViewLine == null)
                        continue;
                    
                    var coveredTests = coveredLine.Tests;
                    if(coveredTests.Length == 0)
                        OutputLogger.Log("Coverage data exists for line, but no tests matching the test IDs existed.", JsonConvert.SerializeObject(coveredLine));

                    var coverageSpan = new SnapshotSpan(
                        _textView.TextSnapshot,
                        new Span(
                            textViewLine.Start,
                            0));
                    result.Add(new TagSpan<CoverageTag>(
                        coverageSpan,
                        new CoverageTag()
                        {
                            Tests = coveredTests
                                .Select(x => new LineTestViewModel()
                                {
                                    Failure = x.Failure,
                                    Duration = x.Duration,
                                    FullName = x.Name
                                })
                                .ToArray()
                        }));
                }

                var resultArray = result.ToArray();
                _tagSpanCache.Add(sanitizedFilePath, resultArray);

                return resultArray;
            }
        }

        private string GetSanitizedFilePath()
        {
            var gitRootDirectory = StateFileMonitor.Instance.GitDirectoryPath;
            if (gitRootDirectory == null)
                return null;

            var filePath = GetFileName();
            if (filePath.StartsWith(gitRootDirectory))
                filePath = filePath.Substring(gitRootDirectory.Length + 1);

            return filePath.Replace("\\", "/");
        }

        private string GetFileName()
        {
            _textBuffer.Properties.TryGetProperty(
                typeof(ITextDocument), out ITextDocument document);
            return document?.FilePath;
        }

        public void Dispose()
        {
            StateFileMonitor.Instance.StatesChanged -= StateFileMonitor_StatesChanged;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }
}

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
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Editor;
using Pruner;

namespace Pruner
{
    /// <summary>
    /// Export a <see cref="ITaggerProvider"/>
    /// </summary>
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("code")]
    [TagType(typeof(CoverageTag))]
    class CoverageTaggerProvider : IViewTaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            var tagger = new CoverageTagger(textView, buffer);
            return tagger as ITagger<T>;
        }
    }
}

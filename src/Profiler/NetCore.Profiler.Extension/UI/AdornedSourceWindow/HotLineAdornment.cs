/*
 * Copyright 2017 (c) Samsung Electronics Co., Ltd  All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * 	http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Utilities;

using NetCore.Profiler.Analytics.Model;
using NetCore.Profiler.Extension.Session;

namespace NetCore.Profiler.Extension.UI.AdornedSourceWindow
{
    internal class HotLineAdornment
    {

        [Export(typeof(AdornmentLayerDefinition))]
        [Name("HotLineAdornment")]
        [Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Text)]
        private static AdornmentLayerDefinition _editorAdornmentLayer;


        private List<LineData> _linesToAdorn;

        /// <summary>
        /// Text view where the adornment is created.
        /// </summary>
        private readonly IWpfTextView _view;

        /// <summary>
        /// The layer of the adornment
        /// </summary>
        private readonly IAdornmentLayer _layer;

        /// <summary>
        /// Adornment pen.
        /// </summary>
        private readonly Pen _pen;

        public HotLineAdornment()
        {

        }

        public HotLineAdornment(IWpfTextView view, ISourceLineStatistics line, ISourceLinesQueryResult queryResult)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));

            Foo(line, queryResult);

            _layer = view.GetAdornmentLayer("HotLineAdornment");
            _view.LayoutChanged += OnLayoutChanged;

            _pen = new Pen(Brushes.Blue, 0.5);
            _pen.Freeze();

        }

        internal void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            if (_linesToAdorn.Count == 0)
            {
                return;
            }

            foreach (var t in e.NewOrReformattedLines)
            {
                foreach (var ld in _linesToAdorn)
                {
                    if (ld.StartLine == Convert.ToUInt64(t.Start.GetContainingLine().LineNumber + 1))
                    {
                        CreateVisuals(t, ld);
                    }
                }
            }
        }

        private void CreateVisuals(ITextViewLine line, LineData lineData)
        {
            var brush = new SolidColorBrush(Color.FromArgb(Convert.ToByte(lineData.Intensity * 2.55), 0x00, 0x00, 0xff));

            var textViewLines = _view.TextViewLines;

            var span = new SnapshotSpan(_view.TextSnapshot, Span.FromBounds(line.Start + Math.Min(line.Length, (int)lineData.StartColumn - 1), line.End));
            var geometry = textViewLines.GetMarkerGeometry(span);
            if (geometry != null)
            {
                var drawing = new GeometryDrawing(brush, _pen, geometry);
                drawing.Freeze();

                var drawingImage = new DrawingImage(drawing);
                drawingImage.Freeze();

                var image = new Image
                {
                    Source = drawingImage,
                };

                // Align the image with the top of the bounds of the text geometry
                Canvas.SetLeft(image, geometry.Bounds.Left);
                Canvas.SetTop(image, geometry.Bounds.Top);

                _layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, span, null, image, null);
            }
        }

        private void Foo(ISourceLineStatistics lineToShow, ISourceLinesQueryResult queryResult)
        {
            var fileId = lineToShow.SourceFileId;
            _linesToAdorn = new List<LineData>();
            foreach (var line in queryResult.Lines.Where(l => l.SourceFileId == fileId))
            {
                var exists = false;
                foreach (var ld in _linesToAdorn)
                {
                    if (ld.EndColumn == line.EndColumn && ld.EndLine == line.EndLine && ld.StartColumn == line.StartColumn && ld.StartLine == line.StartLine)
                    {
                        exists = true;
                        ld.Value += GetValue(queryResult.StatisticsType, queryResult.Inclusive, line);
                        break;
                    }
                }

                if (exists == false)
                {
                    _linesToAdorn.Add(new LineData
                    {
                        StartLine = line.StartLine,
                        StartColumn = line.StartColumn,
                        EndLine = line.EndLine,
                        EndColumn = line.EndColumn,
                        Value = GetValue(queryResult.StatisticsType, queryResult.Inclusive, line)
                    });
                }
            }

            var maxValue = _linesToAdorn.Max(data => data.Value);
            foreach (var ld in _linesToAdorn)
            {
                ld.Intensity = ld.Value * 100.0 / maxValue;
            }

        }

        private static ulong GetValue(StatisticsType statisticsType, bool inclusive, IProfiledObjectStatistics ld)
        {
            switch (statisticsType)
            {
                case StatisticsType.Sample:
                    return inclusive ? ld.SamplesInclusive : ld.SamplesExclusive;
                case StatisticsType.Memory:
                    return inclusive ? ld.AllocatedMemoryInclusive : ld.AllocatedMemoryExclusive;
                case StatisticsType.Time:
                    return inclusive ? ld.TimeInclusive : ld.TimeExclusive;
                default:
                    return 0;
            }
        }

        private class LineData
        {
            public ulong StartLine { get; set; }
            public ulong EndLine { get; set; }
            public ulong StartColumn { get; set; }
            public ulong EndColumn { get; set; }
            public ulong Value { get; set; }
            public double Intensity { get; set; }
        }


    }
}

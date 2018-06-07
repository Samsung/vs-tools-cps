//The MIT License(MIT)

//Copyright(c) 2016 Alberto Rodriguez & LiveCharts Contributors

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using LiveCharts.Charts;
using LiveCharts.Definitions.Points;
using System;

namespace LiveCharts.Wpf.Points
{
    internal class FilledStepLinePointView : PointView, IStepPointView
    {

        public double DeltaX { get; set; }
        public double DeltaY { get; set; }
        public Rectangle Rectangle { get; set; }
        public Path Shape { get; set; }

        public override void DrawOrMove(ChartPoint previousDrawn, ChartPoint current, int index, ChartCore chart)
        {

            if (DataLabel != null && double.IsNaN(Canvas.GetLeft(DataLabel)))
            {
                Canvas.SetTop(DataLabel, chart.DrawMargin.Height);
                Canvas.SetLeft(DataLabel, current.ChartLocation.X);
            }

            if (HoverShape != null)
            {
                HoverShape.Width = Shape != null ? (Shape.Width > 5 ? Shape.Width : 5) : 5;
                HoverShape.Height = Shape != null ? (Shape.Height > 5 ? Shape.Height : 5) : 5;
                Canvas.SetLeft(HoverShape, current.ChartLocation.X - HoverShape.Width / 2);
                Canvas.SetTop(HoverShape, current.ChartLocation.Y - HoverShape.Height / 2);
            }


            Rectangle.Width = Math.Max(1,DeltaX);
            Rectangle.Height = chart.DrawMargin.Height;
            Canvas.SetLeft(Rectangle, current.ChartLocation.X - DeltaX);
            Canvas.SetTop(Rectangle, chart.DrawMargin.Height- current.ChartLocation.Y);

            if (Shape != null)
            {
                Canvas.SetLeft(Shape, current.ChartLocation.X - Shape.Width / 2);
                Canvas.SetTop(Shape, current.ChartLocation.Y - Shape.Height / 2);
            }

            if (DataLabel != null)
            {
                DataLabel.UpdateLayout();
                var xl = CorrectXLabel(current.ChartLocation.X - DataLabel.ActualWidth * .5, chart);
                var yl = CorrectYLabel(current.ChartLocation.Y - DataLabel.ActualHeight * .5, chart);
                Canvas.SetLeft(DataLabel, xl);
                Canvas.SetTop(DataLabel, yl);
            }
        }

        public override void RemoveFromView(ChartCore chart)
        {
            chart.View.RemoveFromDrawMargin(HoverShape);
            chart.View.RemoveFromDrawMargin(Shape);
            chart.View.RemoveFromDrawMargin(DataLabel);
            chart.View.RemoveFromDrawMargin(Rectangle);
        }

        public override void OnHover(ChartPoint point)
        {
            var lineSeries = (FilledStepLineSeries)point.SeriesView;
            if (Shape != null) Shape.Fill = Shape.Stroke;
            lineSeries.StrokeThickness = lineSeries.StrokeThickness + 1;
        }

        public override void OnHoverLeave(ChartPoint point)
        {
            var lineSeries = (FilledStepLineSeries)point.SeriesView;
            if (Shape != null)
                Shape.Fill = point.Fill == null
                    ? lineSeries.PointForeground
                    : (Brush)point.Fill;
            lineSeries.StrokeThickness = lineSeries.StrokeThickness - 1;
        }

        protected double CorrectXLabel(double desiredPosition, ChartCore chart)
        {
            if (desiredPosition + DataLabel.ActualWidth * .5 < -0.1) return -DataLabel.ActualWidth;

            if (desiredPosition + DataLabel.ActualWidth > chart.DrawMargin.Width)
                desiredPosition -= desiredPosition + DataLabel.ActualWidth - chart.DrawMargin.Width + 2;

            if (desiredPosition < 0) desiredPosition = 0;

            return desiredPosition;
        }

        protected double CorrectYLabel(double desiredPosition, ChartCore chart)
        {
            desiredPosition -= (Shape == null ? 0 : Shape.ActualHeight * .5) + DataLabel.ActualHeight * .5 + 2;

            if (desiredPosition + DataLabel.ActualHeight > chart.DrawMargin.Height)
                desiredPosition -= desiredPosition + DataLabel.ActualHeight - chart.DrawMargin.Height + 2;

            if (desiredPosition < 0) desiredPosition = 0;

            return desiredPosition;
        }
    }
}

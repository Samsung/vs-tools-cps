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

using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Tizen.VisualStudio.ManifestEditor
{
    public class RuleTextBox : TextBox
    {
        public static DependencyProperty TextRuleProperty = DependencyProperty.Register("TextRule", typeof(string), typeof(RuleTextBox));
        public string TextRule
        {
            get
            {
                return (string)base.GetValue(TextRuleProperty);
            }

            set
            {
                base.SetValue(TextRuleProperty, value);
            }
        }

        public static DependencyProperty ErrorMessageProperty = DependencyProperty.Register("ErrorMessage", typeof(string), typeof(RuleTextBox));
        public string ErrorMessage
        {
            get
            {
                return (string)base.GetValue(ErrorMessageProperty);
            }

            set
            {

                base.SetValue(ErrorMessageProperty, value);
            }
        }

        private AdornerLayer adorner = null;
        private bool IsError = false;

        static RuleTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RuleTextBox), new
               FrameworkPropertyMetadata(typeof(RuleTextBox)));

        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (this.adorner == null)
            {
                this.adorner = AdornerLayer.GetAdornerLayer(this);
                adorner.Add(new NoteAdorner(this, this, this.ErrorMessage));
                if (IsError)
                {
                    adorner.Visibility = Visibility.Hidden;
                }
                else
                {
                    adorner.Visibility = Visibility.Visible;
                }
            }
        }

        public override void BeginInit()
        {
            base.BeginInit();
        }

        public override void EndInit()
        {
            base.EndInit();
            CheckText();
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);

            CheckText();
        }


        private void CheckText()
        {
            if (TextRule != null)
            {
                if (Regex.IsMatch(this.Text, TextRule))
                {
                    IsError = true;
                    if (adorner != null)
                    {
                        adorner.Visibility = Visibility.Hidden;
                    }
                }
                else
                {
                    IsError = false;
                    if (adorner != null)
                    {
                        adorner.Visibility = Visibility.Visible;
                    }
                }
            }
        }
    }

    public class NoteAdorner : Adorner
    {
        private FrameworkElement parent;// { get; set; }
        private string errorString;// { get; set; }
        public NoteAdorner(UIElement adornedElement, FrameworkElement fElement, string message) :
           base(adornedElement)
        {
            parent = fElement;
            errorString = message;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var adornedElementRect = new Rect(AdornedElement.RenderSize);

            const int SIZE = 10;

            var right = adornedElementRect.Right;
            var left = right - SIZE;
            var top = adornedElementRect.Top;
            var bottom = adornedElementRect.Bottom - SIZE;

            var segments = new[]
            {
                  new LineSegment(new Point(left, top), true),
                  new LineSegment(new Point(right, bottom), true),
                  new LineSegment(new Point(right, top), true)
            };

            var figure = new PathFigure(new Point(left, top), segments, true);
            var geometry = new PathGeometry(new[] { figure });
            drawingContext.DrawGeometry(Brushes.Red, null, geometry);
            drawingContext.DrawText(new FormattedText(errorString, new CultureInfo("en-US"), FlowDirection.LeftToRight, new Typeface("Arial"), 11, Brushes.Red),
               new Point(3, this.parent.ActualHeight));
        }
    }
}

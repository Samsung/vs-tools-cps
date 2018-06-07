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

using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Tizen.VisualStudio.LogViewer
{
    /// <summary>
    /// Partial class to handle the log tab
    /// </summary>
    partial class LogTab
    {
        private DataGrid CreateLogDataGrid()
        {
            DataGrid returnDataGrid = new DataGrid();

            returnDataGrid.Height = Double.NaN;
            returnDataGrid.Width = Double.NaN;
            returnDataGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
            returnDataGrid.VerticalAlignment = VerticalAlignment.Top;
            returnDataGrid.AutoGenerateColumns = false;
            returnDataGrid.IsReadOnly = true;
            returnDataGrid.RowHeaderWidth = 0;

            returnDataGrid.HorizontalGridLinesBrush = new SolidColorBrush(Resource.borderColor);
            returnDataGrid.VerticalGridLinesBrush = new SolidColorBrush(Resource.lightBorderColor);
            returnDataGrid.Background = Brushes.Transparent;
            returnDataGrid.RowBackground = Brushes.Transparent;//;new SolidColorBrush(Resource.bgColor);
            returnDataGrid.BorderThickness = new Thickness(0, 0, 0, 0);

            returnDataGrid.ColumnHeaderStyle = GenColumnHeaderStyle();

            InitColumnHeader(returnDataGrid);
            InitEventHadnler(returnDataGrid);

            return returnDataGrid;
        }

        private void InitEventHadnler(DataGrid returnDataGrid)
        {
            bool wasBottom = true;

            #region Scrollbar control
            returnDataGrid.PreviewMouseDown += (s, e) =>
            {
                wasBottom = IsLastRowLoaded(returnDataGrid);
                parentControl.scLockCheck.IsChecked = true;
            };

            returnDataGrid.PreviewMouseUp += (s, e) =>
            {
                parentControl.scLockCheck.IsChecked = !IsLastRowLoaded(returnDataGrid) || wasBottom;
            };
            #endregion

            returnDataGrid.PreviewMouseWheel += (s, e) =>
            {
                parentControl.scLockCheck.IsChecked = !IsLastRowLoaded(returnDataGrid);
            };
        }

        private bool IsLastRowLoaded(DataGrid returnDataGrid)
        {
            int count = returnDataGrid.Items.Count;

            if (count != 0)
            {
                DataGridRow lastRow = returnDataGrid.ItemContainerGenerator.ContainerFromIndex(count - 1) as DataGridRow;
                if (lastRow != null && !lastRow.IsSelected && lastRow.IsLoaded)
                {
                    return true;
                }
            }

            return false;
        }

        private Style GenColumnHeaderStyle()
        {
            Style hStyle = new Style(typeof(DataGridColumnHeader));
            hStyle.Setters.Add(new Setter(BackgroundProperty, Brushes.Transparent));

            //hStyle.Setters.Add(new Setter(, Brushes.Transparent));
            hStyle.Setters.Add(new Setter(HeightProperty, 24.0));
            hStyle.Setters.Add(new Setter(BorderBrushProperty, new SolidColorBrush(Resource.borderColor)));
            hStyle.Setters.Add(new Setter(BorderThicknessProperty, new Thickness(0, 0, 1, 1)));
            hStyle.Setters.Add(new Setter(ForegroundProperty, new SolidColorBrush(Resource.textColor)));
            hStyle.Setters.Add(new Setter(PaddingProperty, new Thickness(3, 0, 0, 0)));

            hStyle.Triggers.Add(GenMouseOverTrigger());

            return hStyle;
        }

        private MultiDataTrigger GenMouseOverTrigger()
        {
            MultiDataTrigger trigger = new MultiDataTrigger();
            Condition condition = new Condition();
            condition.Binding = new Binding() { Path = new PropertyPath("IsMouseOver"), RelativeSource = RelativeSource.Self };
            condition.Value = true;
            Setter setter = new Setter();
            setter.Property = BackgroundProperty;
            setter.Value = new SolidColorBrush(Resource.borderColor);
            trigger.Conditions.Add(condition);
            trigger.Setters.Add(setter);

            return trigger;
        }

        private static void SetDataGridColumnWidth(int idx, int _size, DataGrid _logDataGrid)
        {
            _logDataGrid.Columns[idx].Width = _size;
        }

        private static void InitColWidth(int width, DataGrid _logDataGrid)
        {
            SetDataGridColumnWidth(Resource.timeColIdx, Resource.timeColWidth, _logDataGrid);
            SetDataGridColumnWidth(Resource.levelColIdx, Resource.levelColWidth, _logDataGrid);
            SetDataGridColumnWidth(Resource.pidColIdx, Resource.pidColWidth, _logDataGrid);
            SetDataGridColumnWidth(Resource.tidColIdx, Resource.tidColWidth, _logDataGrid);
            SetDataGridColumnWidth(Resource.tagColIdx, Resource.tagColWidth, _logDataGrid);

            int witdhSum = Resource.timeColWidth + Resource.levelColWidth + Resource.pidColWidth
                           + Resource.tidColWidth + Resource.tagColWidth;

            SetDataGridColumnWidth(Resource.msgColIdx, witdhSum, _logDataGrid);
        }

        private static void InsertColumnToDataGrid(string name, DataGrid _logDataGrid)
        {
            DataGridTextColumn textColumn = new DataGridTextColumn();
            textColumn.Header = name;
            textColumn.Binding = new Binding(name);
            _logDataGrid.Columns.Add(textColumn);
        }

        private void InitColumnHeader(DataGrid _logDataGrid)
        {
            InsertColumnToDataGrid("Time", _logDataGrid);
            InsertColumnToDataGrid("Level", _logDataGrid);
            InsertColumnToDataGrid("Pid", _logDataGrid);
            InsertColumnToDataGrid("Tid", _logDataGrid);
            InsertColumnToDataGrid("Tag", _logDataGrid);
            InsertColumnToDataGrid("Msg", _logDataGrid);
        }

        private static void SetViewFont(DataGrid _logDataGrid)
        {
            //logDataGrid.FontFamily = new FontFamily("NanumGothic Light");
            _logDataGrid.FontSize = 12;
        }
    }
}

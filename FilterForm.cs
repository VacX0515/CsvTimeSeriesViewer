using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CsvTimeSeriesViewer
{
    /// <summary>
    /// 데이터 필터 설정을 위한 폼
    /// </summary>
    public partial class FilterForm : Form
    {
        private Dictionary<string, CsvFileInfo> csvFiles;

        public FilterForm(Dictionary<string, CsvFileInfo> files)
        {
            csvFiles = files;
            InitializeComponent();
            LoadFilters();
        }

        private void LoadFilters()
        {
            trvFilters.BeginUpdate();
            trvFilters.Nodes.Clear();

            foreach (var file in csvFiles)
            {
                var fileNode = new TreeNode(file.Value.FileName)
                {
                    Tag = file.Key
                };

                foreach (string column in file.Value.SelectedColumns)
                {
                    var filter = file.Value.Filters.ContainsKey(column)
                        ? file.Value.Filters[column]
                        : new DataFilter();

                    var columnNode = new TreeNode(column)
                    {
                        Tag = filter,
                        Checked = filter.Enabled
                    };

                    if (filter.Enabled)
                    {
                        columnNode.Text += $" [{filter.MinValue:F2} ~ {filter.MaxValue:F2}]";
                    }

                    fileNode.Nodes.Add(columnNode);
                }

                fileNode.Expand();
                trvFilters.Nodes.Add(fileNode);
            }

            trvFilters.EndUpdate();
        }

        private void TrvFilters_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Level == 1) // 컬럼 노드
            {
                var filter = (DataFilter)e.Node.Tag;
                filter.Enabled = e.Node.Checked;

                string filePath = e.Node.Parent.Tag.ToString();
                string columnName = e.Node.Text.Split('[')[0].Trim();

                if (!csvFiles[filePath].Filters.ContainsKey(columnName))
                {
                    csvFiles[filePath].Filters[columnName] = filter;
                }

                if (e.Node.Checked && filter.MinValue == double.MinValue)
                {
                    ShowFilterRangeDialog(e.Node, filter);
                }
            }
        }

        private void TrvFilters_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Level == 1) // 컬럼 노드
            {
                var filter = (DataFilter)e.Node.Tag;
                ShowFilterRangeDialog(e.Node, filter);
            }
        }

        private void ShowFilterRangeDialog(TreeNode node, DataFilter filter)
        {
            using (var dialog = new FilterRangeDialog(filter))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    // 노드 텍스트 업데이트
                    string columnName = node.Text.Split('[')[0].Trim();
                    if (filter.Enabled)
                    {
                        node.Text = $"{columnName} [{filter.MinValue:F2} ~ {filter.MaxValue:F2}]";
                    }
                    else
                    {
                        node.Text = columnName;
                    }
                }
            }
        }
    }
}
using System.Drawing;
using System.Windows.Forms;

namespace Pharma.Ui;

internal static class UiTheme
{
    public static readonly Color Navy = Color.FromArgb(18, 52, 86);
    public static readonly Color NavyHover = Color.FromArgb(35, 78, 118);
    public static readonly Color Green = Color.FromArgb(46, 117, 98);
    public static readonly Color PageBackground = Color.FromArgb(244, 247, 250);
    public static Button Button(string text) => new() { Text=text, BackColor=Navy, ForeColor=Color.White, FlatStyle=FlatStyle.Flat, Height=38, TextAlign=ContentAlignment.MiddleCenter, Font=new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold), Cursor=Cursors.Hand, Padding=new Padding(10, 0, 10, 0), FlatAppearance={BorderSize=0, MouseOverBackColor=NavyHover, MouseDownBackColor=Color.FromArgb(12, 38, 64)} };
    public static void StyleGrid(DataGridView grid)
    {
        grid.Dock = DockStyle.Fill;
        grid.BackgroundColor = Color.White;
        grid.AutoGenerateColumns = true;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.BorderStyle = BorderStyle.None;
        grid.RowHeadersVisible = false;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.GridColor = Color.FromArgb(231, 236, 240);
        grid.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F);
        grid.DefaultCellStyle.ForeColor = Color.FromArgb(45, 58, 70);
        grid.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
        grid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Navy;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
        grid.ColumnHeadersHeight = 36;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(218, 237, 232);
        grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(25, 55, 65);
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(247, 249, 251);
        grid.RowTemplate.Height = 30;
    }
}

namespace SharePointToolbox.UI.Theming;

/// <summary>
/// Stile comune delle selezioni negli elenchi applicativi.
/// </summary>
internal static class InstitutionalSelectionStyle
{
    private static Color SelectionColor =>
        AppThemeManager.PopupActiveText.IsEmpty
            ? Color.FromArgb(62, 88, 133)
            : AppThemeManager.PopupActiveText;

    public static void Apply(ListView list)
    {
        list.OwnerDraw = true;
        list.DrawColumnHeader += (_, e) => e.DrawDefault = true;
        list.DrawItem += (_, _) => { };
        list.DrawSubItem += DrawSubItem;
    }

    public static void Apply(DataGridView grid)
    {
        grid.DefaultCellStyle.SelectionBackColor = SelectionColor;
        grid.DefaultCellStyle.SelectionForeColor = Color.White;
        grid.RowsDefaultCellStyle.SelectionBackColor = SelectionColor;
        grid.RowsDefaultCellStyle.SelectionForeColor = Color.White;
        grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = SelectionColor;
        grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.White;
    }

    private static void DrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
    {
        if (sender is not ListView list || e.Item is null || e.SubItem is null) return;
        ListViewItem item = e.Item;
        ListViewItem.ListViewSubItem subItem = e.SubItem;

        bool selected = item.Selected;
        Color itemBackground = item.BackColor.IsEmpty ? list.BackColor : item.BackColor;
        Color itemForeground = item.ForeColor.IsEmpty ? list.ForeColor : item.ForeColor;
        Color background = selected ? SelectionColor : itemBackground;
        Color foreground = selected ? Color.White : itemForeground;

        using (var brush = new SolidBrush(background))
            e.Graphics.FillRectangle(brush, e.Bounds);

        int textLeft = e.Bounds.Left + 4;
        int imageIndex = item.ImageIndex;
        if (imageIndex < 0
            && list.SmallImageList is not null
            && !string.IsNullOrWhiteSpace(item.ImageKey))
            imageIndex = list.SmallImageList.Images.IndexOfKey(item.ImageKey);

        if (e.ColumnIndex == 0
            && list.SmallImageList is not null
            && imageIndex >= 0
            && imageIndex < list.SmallImageList.Images.Count)
        {
            Image image = list.SmallImageList.Images[imageIndex];
            int imageTop = e.Bounds.Top + Math.Max(0, (e.Bounds.Height - image.Height) / 2);
            e.Graphics.DrawImage(image, textLeft, imageTop, image.Width, image.Height);
            textLeft += image.Width + 4;
        }

        TextRenderer.DrawText(
            e.Graphics,
            subItem.Text,
            item.Font ?? list.Font,
            new Rectangle(
                textLeft,
                e.Bounds.Top,
                Math.Max(0, e.Bounds.Right - textLeft - 3),
                e.Bounds.Height),
            foreground,
            TextFormatFlags.Left
            | TextFormatFlags.VerticalCenter
            | TextFormatFlags.SingleLine
            | TextFormatFlags.EndEllipsis
            | TextFormatFlags.NoPrefix);
    }
}

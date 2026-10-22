using Simple_Samba_Connect;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

public class MainForm : Form
{
    private readonly TextBox serverBox = new() { Width = 250, PlaceholderText = @"\\сервер\папка" };
    private readonly TextBox userBox = new() { Width = 250 };
    private readonly TextBox passwordBox = new() { Width = 250, UseSystemPasswordChar = true };
    private readonly ComboBox driveBox = new() { Width = 80, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox reconnectBox = new() { Text = "Подключать при следующем входе в Windows", AutoSize = true };

    public MainForm()
    {
        Text = "Подключение сетевого диска";
        Width = 470;
        Height = 290;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        driveBox.Items.AddRange(
            Enumerable.Range('D', 'Z' - 'D' + 1)
                .Select(c => $"{(char)c}:")
                .ToArray());
        driveBox.SelectedIndex = 0;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 2,
            RowCount = 6,
            AutoSize = true
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddRow(layout, 0, "Адрес ресурса:", serverBox);
        AddRow(layout, 1, "Имя пользователя:", userBox);
        AddRow(layout, 2, "Пароль:", passwordBox);
        AddRow(layout, 3, "Буква диска:", driveBox);

        layout.Controls.Add(reconnectBox, 1, 4);

        var connectButton = new Button { Text = "Подключить", AutoSize = true };
        connectButton.Click += (_, _) => ConnectDrive();

        layout.Controls.Add(connectButton, 1, 5);
        Controls.Add(layout);
    }

    private static void AddRow(TableLayoutPanel panel, int row, string label, Control input)
    {
        panel.Controls.Add(new Label
        {
            Text = label,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 8, 12, 3)
        }, 0, row);

        input.Anchor = AnchorStyles.Left;
        panel.Controls.Add(input, 1, row);
    }

    private void ConnectDrive()
    {
        string remotePath = serverBox.Text.Trim();
        string drive = driveBox.Text;
        string user = userBox.Text.Trim();
        string password = passwordBox.Text;

        if (!remotePath.StartsWith(@"\\") || remotePath.Length < 5)
        {
            MessageBox.Show(@"Введите путь вида \\сервер\папка.");
            return;
        }

        var resource = new NetResource
        {
            Type = 1, // RESOURCETYPE_DISK
            LocalName = drive,
            RemoteName = remotePath
        };

        // UPDATE_PROFILE сохраняет подключение для следующих входов в Windows.
        // Без него подключение будет временным.
        int flags = reconnectBox.Checked ? 1 : 0;

        int result = WNetAddConnection2(
            ref resource,
            string.IsNullOrEmpty(password) ? null : password,
            string.IsNullOrEmpty(user) ? null : user,
            flags);

        if (result == 0)
        {
            MessageBox.Show($"Диск {drive} подключён к {remotePath}.");
        }
        else
        {
            MessageBox.Show(
                $"Не удалось подключить диск.\n{new Win32Exception(result).Message}\nКод ошибки: {result}",
                "Ошибка",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NetResource
    {
        public int Scope;
        public int Type;
        public int DisplayType;
        public int Usage;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? LocalName;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? RemoteName;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? Comment;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? Provider;
    }

    [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
    private static extern int WNetAddConnection2(
        ref NetResource netResource,
        string? password,
        string? username,
        int flags);
}
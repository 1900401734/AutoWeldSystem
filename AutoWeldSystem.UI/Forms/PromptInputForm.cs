using AutoWeldSystem.UI.Infrastructure;

namespace AutoWeldSystem.UI.Forms;

public sealed class PromptInputForm : Form
{
    private readonly TextBox _input = new();

    public PromptInputForm(string title, string prompt, string defaultValue, string okText, string cancelText)
    {
        AppAssets.ApplyWindowIcon(this);

        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(420, 140);

        var label = new Label
        {
            Text = prompt,
            AutoSize = false,
            Location = new Point(16, 16),
            Size = new Size(388, 24)
        };

        _input.Location = new Point(16, 48);
        _input.Size = new Size(388, 28);
        _input.Text = defaultValue;

        var btnOk = new Button
        {
            Text = okText,
            DialogResult = DialogResult.OK,
            Location = new Point(228, 96),
            Size = new Size(84, 30)
        };

        var btnCancel = new Button
        {
            Text = cancelText,
            DialogResult = DialogResult.Cancel,
            Location = new Point(320, 96),
            Size = new Size(84, 30)
        };

        Controls.Add(label);
        Controls.Add(_input);
        Controls.Add(btnOk);
        Controls.Add(btnCancel);

        AcceptButton = btnOk;
        CancelButton = btnCancel;
        Shown += (_, _) =>
        {
            _input.Focus();
            _input.SelectAll();
        };
    }

    public string Value => _input.Text.Trim();

    public static bool TryShow(
        IWin32Window owner,
        string title,
        string prompt,
        string defaultValue,
        string okText,
        string cancelText,
        out string value)
    {
        using var form = new PromptInputForm(title, prompt, defaultValue, okText, cancelText);
        if (form.ShowDialog(owner) == DialogResult.OK)
        {
            value = form.Value;
            return true;
        }

        value = string.Empty;
        return false;
    }
}

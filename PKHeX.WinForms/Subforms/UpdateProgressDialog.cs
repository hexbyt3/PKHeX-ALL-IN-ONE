using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PKHeX.Core;

namespace PKHeX.WinForms;

public partial class UpdateProgressDialog : Form
{
    private readonly string _downloadUrl;
    private readonly string _versionTag;
    private readonly PKHeXSettings _settings;
    private CancellationTokenSource? _cancellationTokenSource;
    
    private ProgressBar progressBar = null!;
    private Label statusLabel = null!;
    private Button cancelButton = null!;
    private Label percentLabel = null!;

    public UpdateProgressDialog(string downloadUrl, string versionTag, PKHeXSettings settings)
    {
        _downloadUrl = downloadUrl;
        _versionTag = versionTag;
        _settings = settings;
        InitializeComponent();
        StartPosition = FormStartPosition.CenterParent;
    }

    private void InitializeComponent()
    {
        Text = "PKHeX Update";
        Size = new Size(400, 180);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        
        statusLabel = new Label
        {
            Text = $"Downloading PKHeX {_versionTag}...",
            Location = new Point(12, 20),
            Size = new Size(360, 23),
            TextAlign = ContentAlignment.MiddleLeft
        };
        
        progressBar = new ProgressBar
        {
            Location = new Point(12, 50),
            Size = new Size(360, 23),
            Style = ProgressBarStyle.Continuous
        };
        
        percentLabel = new Label
        {
            Text = "0%",
            Location = new Point(12, 80),
            Size = new Size(360, 23),
            TextAlign = ContentAlignment.MiddleCenter
        };
        
        cancelButton = new Button
        {
            Text = "Cancel",
            Location = new Point(297, 110),
            Size = new Size(75, 23),
            UseVisualStyleBackColor = true
        };
        cancelButton.Click += CancelButton_Click;
        
        Controls.Add(statusLabel);
        Controls.Add(progressBar);
        Controls.Add(percentLabel);
        Controls.Add(cancelButton);
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        await StartDownload();
    }

    private async Task StartDownload()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        var progress = new Progress<int>(percent =>
        {
            progressBar.Value = percent;
            percentLabel.Text = $"{percent}%";
            
            if (percent == 100)
            {
                statusLabel.Text = "Download complete. Preparing update...";
                cancelButton.Enabled = false;
            }
        });

        try
        {
            var downloadPath = await UpdateUtil.DownloadUpdateAsync(_downloadUrl, progress, _cancellationTokenSource.Token);
            
            if (downloadPath != null)
            {
                statusLabel.Text = "Update downloaded. Installing after restart...";
                
                var exePath = Environment.ProcessPath ?? Application.ExecutablePath;
                if (UpdateUtil.PrepareUpdateScript(downloadPath, exePath))
                {
                    var result = MessageBox.Show(
                        "Update downloaded successfully!\n\nPKHeX will now restart to apply the update.\n\nYour current installation will be backed up.",
                        "Update Ready",
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Information);
                    
                    if (result == DialogResult.OK)
                    {
                        // Save the revision as checked/downloaded before updating
                        _settings.Startup.LastCheckedRevision = _versionTag.TrimStart('v');
                        await PKHeXSettings.SaveSettings(Program.PathConfig, _settings).ConfigureAwait(false);
                        
                        UpdateUtil.ExecuteUpdate(exePath);
                    }
                    else
                    {
                        statusLabel.Text = "Update cancelled. Files will remain in temp folder.";
                        
                        // Mark as checked even if cancelled to avoid repeated notifications
                        _settings.Startup.LastCheckedRevision = _versionTag.TrimStart('v');
                        await PKHeXSettings.SaveSettings(Program.PathConfig, _settings).ConfigureAwait(false);
                    }
                }
                else
                {
                    MessageBox.Show("Failed to prepare update script.", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                MessageBox.Show("Failed to download update. Please try again later.", "Download Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (OperationCanceledException)
        {
            statusLabel.Text = "Download cancelled.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"An error occurred during update: {ex.Message}", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Close();
        }
    }

    private void CancelButton_Click(object? sender, EventArgs e)
    {
        _cancellationTokenSource?.Cancel();
        cancelButton.Enabled = false;
        statusLabel.Text = "Cancelling...";
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _cancellationTokenSource?.Cancel();
        base.OnFormClosing(e);
    }
}
using System;
using System.Drawing;
using System.Windows.Forms;
using ILDebugging.Monitor.Properties;
using ILDebugging.Visualizer;

namespace ILDebugging.Monitor
{
    public partial class ILMonitorForm : Form
    {
        private AbstractXmlDataMonitor<MethodBodyInfo> m_monitor;

        public ILMonitorForm()
        {
            InitializeComponent();
        }

        private void ExitToolsStripMenuItem_Click(object sender, EventArgs e) => Application.Exit();

        private void CascadeToolStripMenuItem_Click(object sender, EventArgs e)        => LayoutMdi(MdiLayout.Cascade);
        private void TileVerticalToolStripMenuItem_Click(object sender, EventArgs e)   => LayoutMdi(MdiLayout.TileVertical);
        private void TileHorizontalToolStripMenuItem_Click(object sender, EventArgs e) => LayoutMdi(MdiLayout.TileHorizontal);
        private void ArrangeIconsToolStripMenuItem_Click(object sender, EventArgs e)   => LayoutMdi(MdiLayout.ArrangeIcons);

        private void CloseAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var childForm in MdiChildren)
                childForm.Close();
        }

        private void ILMonitorForm_Load(object sender, EventArgs e)
        {
            m_monitor = new TcpDataMonitor<MethodBodyInfo>(port: 22017);

            m_monitor.MonitorStatusChange += OnMonitorStatusChange;
            m_monitor.VisualizerDataReady += OnVisualizerDataReady;

            if (Settings.Default.AutomaticallyStart)
            {
                autoStartToolStripMenuItem.Checked = true;
                m_monitor.Start();
            }
        }

        private void OnMonitorStatusChange(object sender, MonitorStatusChangeEventArgs e)
        {
            if (e.Status == MonitorStatus.Monitoring)
            {
                toolStripStatusLabel.Text = "Monitoring";
                toolStripStatusLabel.ForeColor = Color.Blue;
                startToolStripMenuItem.Enabled = false;
                stopToolStripMenuItem.Enabled = true;
            }
            else
            {
                toolStripStatusLabel.Text = "Not Monitoring";
                toolStripStatusLabel.ForeColor = Color.Red;
                startToolStripMenuItem.Enabled = true;
                stopToolStripMenuItem.Enabled = false;
            }
        }

        private void OnVisualizerDataReady(object sender, VisualizerDataEventArgs<MethodBodyInfo> e)
        {
            var mbi = e.VisualizerData;
            var childForm = FindOrCreateChildForm(mbi);

            var imbi = childForm.CurrentData != null
                ? IncrementalMethodBodyInfo.Create(mbi, childForm.CurrentData.LengthHistory)
                : IncrementalMethodBodyInfo.Create(mbi);

            childForm.UpdateWith(imbi);
        }

        private MiniBrowser FindOrCreateChildForm(MethodBodyInfo mbi)
        {
            foreach (var form in MdiChildren)
            {
                var miniBrowser = form as MiniBrowser;

                if (mbi.Identity == miniBrowser?.CurrentData?.Identity)
                {
                    miniBrowser.Focus();
                    return miniBrowser;
                }
            }

            var newChild = new MiniBrowser
            {
                Text = mbi.MethodToString,
                MdiParent = this
            };
            newChild.Show();
            return newChild;
        }

        private void stopToolStripMenuItem_Click(object sender, EventArgs e)  => m_monitor.Stop();
        private void startToolStripMenuItem_Click(object sender, EventArgs e) => m_monitor.Start();

        private void ILMonitorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            var s = Settings.Default;
            s.AutomaticallyStart = autoStartToolStripMenuItem.Checked;
            s.Save();

            if (stopToolStripMenuItem.Enabled)
            {
                m_monitor.Stop();
            }
        }

        private void autoStartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            autoStartToolStripMenuItem.Checked = !autoStartToolStripMenuItem.Checked;
        }

        private void showStatusBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            statusStrip.Visible = showStatusBarToolStripMenuItem.Checked;
        }
    }
}
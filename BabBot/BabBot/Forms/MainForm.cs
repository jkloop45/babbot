﻿/*
    This file is part of BabBot.

    BabBot is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    BabBot is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with BabBot.  If not, see <http://www.gnu.org/licenses/>.
  
    Copyright 2009 BabBot Team
*/
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using BabBot.Bot;
using BabBot.Common;
using BabBot.Manager;
using BabBot.Wow;

namespace BabBot.Forms
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            Process.EnterDebugMode();

            // Load the configuration file
            LoadConfig();

            // Custom initialization of some components
            Initialize();

            // ProcessManager events binding
            ProcessManager.WoWProcessStarted += wow_ProcessStarted;
            ProcessManager.WoWProcessEnded += wow_ProcessEnded;
            ProcessManager.WoWProcessFailed += wow_ProcessFailed;
            ProcessManager.WoWProcessAccessFailed += wow_ProcessAccessFailed;

            // Starts the bot thread
            ProcessManager.PlayerUpdate += PlayerUpdate;
            ProcessManager.PlayerWayPoint += PlayerWayPoint;
        }

        #region Exception Handler
        public void UnhandledThreadExceptionHandler(object sender, ThreadExceptionEventArgs e)
        {
            this.HandleUnhandledException(e.Exception);
        }

        public void HandleUnhandledException(Exception e)
        {
            // do what you want here.
            if (MessageBox.Show("An unexpected error has occurred. Continue?",
                "My application", MessageBoxButtons.YesNo, MessageBoxIcon.Stop,
                MessageBoxDefaultButton.Button2) == DialogResult.No)
            {
                Application.Exit();
            }
        }
        #endregion

        private void Initialize()
        {
            // Set a default value for waypoint type combo
            comboWayPointTypes.SelectedIndex = 0;
        }

        private void PlayerWayPoint(Vector3D waypoint)
        {
            if (InvokeRequired)
            {
                PlayerWayPointDelegate del = PlayerWayPoint;
                object[] parameters = {waypoint};
                Invoke(del, parameters);
            }
            else
            {
                /*
                if (cbWPRecord.Checked)
                {
                    lbWayPoints.Items.Insert(0, waypoint.ToString());
                }
                */

                if (cbAutoAddWaypoints.Checked)
                {
                    WayPoint wp = new WayPoint(waypoint);
                    switch (comboWayPointTypes.SelectedItem.ToString())
                    {
                        case "Vendor":
                            wp.WPType = WayPointType.Vendor;
                            break;
                        case "Repair":
                            wp.WPType = WayPointType.Repair;
                            break;
                        case "Ghost":
                            wp.WPType = WayPointType.Ghost;
                            break;
                        case "Normal":
                            wp.WPType = WayPointType.Normal;
                            break;
                    }
                    WayPointManager.Instance.AddWayPoint(wp);
                }
            }
        }

        private void PlayerUpdate()
        {
            if (InvokeRequired)
            {
                PlayerUpdateDelegate del = PlayerUpdate;
                Invoke(del);
            }
            else
            {
                if (ProcessManager.ProcessRunning)
                {
                    //Misc info updates for the "Player" tab

                    tbLocation.Text = String.Format("Loc: {0}, {1}, {2} | {3}", ProcessManager.Player.Location.X,
                                                    ProcessManager.Player.Location.Y, ProcessManager.Player.Location.Z,
                                                    ProcessManager.Player.CurTargetGuid);
                    tbOrientation.Text = String.Format("Or.: {0}", ProcessManager.Player.Orientation);
                    tbPlayerHp.Text = ProcessManager.Player.Hp.ToString();
                    tbPlayerMaxHp.Text = ProcessManager.Player.MaxHp.ToString();
                    tbPlayerMp.Text = ProcessManager.Player.Mp.ToString();
                    tbPlayerMaxMp.Text = ProcessManager.Player.MaxMp.ToString();
                    tbPlayerXp.Text = ProcessManager.Player.Xp.ToString();
                    tbPlayerTarget.Text = string.Format("{0:X}", ProcessManager.Player.CurTargetGuid);
                    tbPlayerTargetName.Text = ProcessManager.Player.CurTargetName;
                    tbPlayerNearObjects.Text = "Objects" + Environment.NewLine + "===========" + Environment.NewLine +
                                               ProcessManager.Player.NearObjectsAsTextList + Environment.NewLine + "Mobs" +
                                               Environment.NewLine +
                                               "===========" + Environment.NewLine +
                                               ProcessManager.Player.NearMobsAsTextList;
                }

                txtCurrentX.Text = ProcessManager.Player.Location.X.ToString();
                txtCurrentY.Text = ProcessManager.Player.Location.Y.ToString();
                txtCurrentZ.Text = ProcessManager.Player.Location.Z.ToString();
                txtLastDistance.Text = ProcessManager.Player.LastDistance.ToString();
                txtFaceRadian.Text = ProcessManager.Player.LastFaceRadian.ToString();

                var orientation = (float)((ProcessManager.Player.Orientation * 180) / Math.PI);
                txtCurrentFace.Text = string.Format("{0}°", orientation);

                var facing = (float)((ProcessManager.Player.LastFaceRadian * 180) / Math.PI);
                txtComputedFacing.Text = string.Format("{0}°", facing);

                tbPlayerIsSitting.Text = ProcessManager.Player.IsSitting().ToString();
                txtTravelTime.Text = string.Format("{0} ms", ProcessManager.Player.TravelTime);

                tbCountNormal.Text = WayPointManager.Instance.NormalNodeCount.ToString();
                tbCountVendor.Text = WayPointManager.Instance.VendorNodeCount.ToString();
                tbCountGhost.Text = WayPointManager.Instance.GhostNodeCount.ToString();
                tbCountRepair.Text = WayPointManager.Instance.RepairNodeCount.ToString();
            }
        }

        private void wow_ProcessEnded(int process)
        {
            // Cross-Thread operation
            if (InvokeRequired)
            {
                // Setup the cross-thread call
                ProcessEndedDelegate del = wow_ProcessEnded;
                object[] parameters = { process };
                Invoke(del, parameters);
            }
            else
            {
                // Main Thread
                btnRun.Enabled = true;
                btnAttachToWow.Enabled = true;

                // Stop the reading thread
                ProcessManager.BotManager.Stop();
            }
        }

        private static void wow_ProcessFailed(string error)
        {
            MessageBox.Show(error, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void wow_ProcessStarted(int process)
        {
            btnRun.Enabled = false;
            btnAttachToWow.Enabled = false;

            // Start the reading thread
            ProcessManager.BotManager.Start();

        }

        private void ActivateDebugMode()
        {
            tabControlMain.TabPages["tabPageDebug"].Enabled = true;
            tabControlMain.TabPages["tabPageDebug2"].Enabled = true;
        }


        private void DeactivateDebugMode()
        {
            tabControlMain.TabPages["tabPageDebug"].Enabled = false;
            tabControlMain.TabPages["tabPageDebug2"].Enabled = false;
        }

        #region UI Event Handlers

        private static void wow_ProcessAccessFailed(string error)
        {
            MessageBox.Show(error, "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            ProcessManager.StartWow();
        }

        private void btnFindTLS_Click(object sender, EventArgs e)
        {
            try
            {
                ProcessManager.FindTLS();
                tbTLS.Text = string.Format("{0:X}", ProcessManager.TLS);
                tbClientConnectionPointer.Text = string.Format("{0:X}", Globals.ClientConnectionPointer);
                tbClientConnectionOffset.Text = string.Format("{0:X}", Globals.ClientConnectionOffset);
                tbPlayerBaseOffset.Text = string.Format("{0:X}", Globals.PlayerBaseOffset);
                tbCurMgr.Text = string.Format("{0:X}", Globals.CurMgr);
                tbLocalGUID.Text = ProcessManager.ObjectManager.GetLocalGUID().ToString();
                tbWndHandle.Text = ProcessManager.WowHWND.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnLoadProfile_Click(object sender, EventArgs e)
        {
            var dlg = new OpenFileDialog { Multiselect = false, Filter = "BabBot Profile (*.xml)|*.xml" };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                Common.Serializer<Bot.Profile> serializer = new Serializer<Profile>();
                ProcessManager.Profile = serializer.Load(dlg.FileName);
                ProcessManager.Profile.FileName = dlg.FileName;
                tbProfileName.Text = ProcessManager.Profile.Name;
                tbProfileDescription.Text = ProcessManager.Profile.Description;
                WayPointManager.Instance.NormalPath = ProcessManager.Profile.NormalWayPoints;
                WayPointManager.Instance.VendorPath = ProcessManager.Profile.VendorWayPoints;
                WayPointManager.Instance.RepairPath = ProcessManager.Profile.RepairWayPoints;
                WayPointManager.Instance.GhostPath = ProcessManager.Profile.GhostWayPoints;

                // UI Stuff
                tbProfile.Text = dlg.FileName;
                RefreshEnemiesList();

            }
        }

        private void btnSaveProfile_Click(object sender, EventArgs e)
        {
            var dlg = new SaveFileDialog { Filter = "BabBot Profile (*.xml)|*.xml" };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                ProcessManager.Profile.FileName = dlg.FileName;
                Common.Serializer<Bot.Profile> serializer = new Serializer<Profile>();
                ProcessManager.Profile.NormalWayPoints = WayPointManager.Instance.NormalPath;
                ProcessManager.Profile.VendorWayPoints = WayPointManager.Instance.VendorPath;
                ProcessManager.Profile.RepairWayPoints = WayPointManager.Instance.RepairPath;
                ProcessManager.Profile.GhostWayPoints = WayPointManager.Instance.GhostPath;
                serializer.Save(dlg.FileName, ProcessManager.Profile);

                // UI Stuff
                tbProfile.Text = dlg.FileName;
            }
        }

        private void btnAttachToWow_Click(object sender, EventArgs e)
        {
            ProcessManager.AttachToWow();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var f = new AboutForm();
            f.ShowDialog();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            ProcessManager.BotManager.Stop();
            Process.LeaveDebugMode();
        }

        private void btnMovementTest_Click(object sender, EventArgs e)
        {
            var destPos = new Vector3D();
            try
            {
                destPos.X = float.Parse(txtX.Text);
                destPos.Y = float.Parse(txtY.Text);
                destPos.Z = float.Parse(txtZ.Text);
                ProcessManager.Player.MoveTo(destPos);
            }
            catch
            {
            }
        }

        private void btnStopMovement_Click(object sender, EventArgs e)
        {
            ProcessManager.Player.Stop();
        }

        private void btnWPTest_Click(object sender, EventArgs e)
        {
            cbWPRecord.Checked = false;
            if (lbWayPoints.Items.Count > 0)
            {
                foreach (string wp in lbWayPoints.Items)
                {
                    string[] arrWp = wp.Split('|');
                    var dest = new Vector3D(float.Parse(arrWp[0]), float.Parse(arrWp[1]), float.Parse(arrWp[2]));
                    if (ProcessManager.Player.MoveTo(dest) == PlayerState.WayPointTimeout)
                    {
                        MessageBox.Show("Sono babbato da un babbo ostacolo!");
                        break;
                    }
                }
            }
        }

        private void btnClearWP_Click(object sender, EventArgs e)
        {
            cbWPRecord.Checked = false;
            lbWayPoints.Items.Clear();
        }

        private void cbWPRecord_CheckedChanged(object sender, EventArgs e)
        {
            if (!cbWPRecord.Checked)
            {
                ProcessManager.ResetWayPoint();
            }
        }

        private void tbProfileName_TextChanged(object sender, EventArgs e)
        {
            ProcessManager.Profile.Name = tbProfileName.Text;
        }

        private void tbProfileDescription_TextChanged(object sender, EventArgs e)
        {
            ProcessManager.Profile.Description = tbProfileDescription.Text;
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OptionsForm form = new OptionsForm();
            DialogResult res;
            res = form.ShowDialog();
            if (res == DialogResult.OK)
            {
                SaveConfig();
            }
        }

        private void btnStartBot_Click(object sender, EventArgs e)
        {
            StateManager.Instance.Start();
        }

        private void btnStopBot_Click(object sender, EventArgs e)
        {
            StateManager.Instance.Stop();
        }

        private void btnAddEnemyToList_Click(object sender, EventArgs e)
        {
            if (ProcessManager.Player.CurTargetName != "")
            {
                Enemy enemy = new Enemy(ProcessManager.Player.CurTargetName);
                ProcessManager.Profile.Enemies.Add(enemy);
                RefreshEnemiesList();
              
            }
        }

        private void btnRemoveEnemyFromList_Click(object sender, EventArgs e)
        {
            if (lbEnemies.Items.Count == 0) return;

            if (lbEnemies.SelectedItem != null)
            {
                Enemy enemy = new Enemy(lbEnemies.SelectedItem.ToString());
                ProcessManager.Profile.Enemies.Remove(enemy);
                RefreshEnemiesList();
            }
        }

        private void RefreshEnemiesList()
        {
            lbEnemies.Items.Clear();
            foreach (var en in ProcessManager.Profile.Enemies)
            {
                lbEnemies.Items.Add(en.Name);
            }            
        }

        #endregion

        #region Load/Save Config

        private void LoadConfig()
        {
            string fileName = "config.xml";
            Common.Serializer<Config> serializer = new Serializer<Config>();

            try
            {
                ProcessManager.Config = serializer.Load(fileName);
                if (ProcessManager.Config.DebugMode)
                {
                    ActivateDebugMode();
                }
                else
                {
                    DeactivateDebugMode();
                }
            }
            catch (System.IO.FileNotFoundException ex)
            {
                // No configuration file, don't worry
            }
        }

        private void SaveConfig()
        {
            string fileName = "config.xml";
            Common.Serializer<Config> serializer = new Serializer<Config>();
            serializer.Save(fileName, ProcessManager.Config);

            if (ProcessManager.Config.DebugMode)
            {
                ActivateDebugMode();
            } 
            else
            {
                DeactivateDebugMode();
            }
        }
        #endregion


        #region Nested type: PlayerUpdateDelegate

        private delegate void PlayerUpdateDelegate();

        #endregion

        #region Nested type: PlayerWayPointDelegate

        private delegate void PlayerWayPointDelegate(Vector3D waypoint);

        #endregion

        #region Nested type: ProcessEndedDelegate

        private delegate void ProcessEndedDelegate(int process);

        #endregion











    }
}
﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Net;
using System.IO;
using BabBot.Wow;
using BabBot.Manager;
using System.Xml.Serialization;

namespace BabBot.Forms
{
    public partial class TalentsForm : Form
    {
        // Template on WoW Armory URL 
        Regex trex;
        // Currently selected talents list
        Talents CurTalents = null;
        // Change tracking
        private bool _changed = false;
        // Talents profile dir
        private string wdir;

        public TalentsForm()
        {
            InitializeComponent();

            trex = new Regex(ProcessManager.CurVersion.TalentConfig.ArmoryPattern);
        }

        private int LevelLabel
        {
            get
            {
                return (labelLevelNum.Text.Equals("")) ? 0 : 
                            Convert.ToInt32(labelLevelNum.Text);
            }
        }

        private string ReadURL(string url)
        {
            // Create a request for the URL.         
            WebRequest request = WebRequest.Create(url);
            // If required by the server, set the credentials.
            request.Credentials = CredentialCache.DefaultCredentials;
            // Get the response.
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            // Display the status.
            // response.StatusDescription;
            // Get the stream containing content returned by the server.
            Stream stream = response.GetResponseStream();
            // Open the stream using a StreamReader for easy access.
            StreamReader reader = new StreamReader(stream);
            // Read the content.
            string res = reader.ReadToEnd();
            // Cleanup the streams and the response.
            reader.Close();
            stream.Close();
            response.Close();

            return res;
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            try
            {
                string url = tbTalentURL.Text;
                // Check for class id
                Match m = trex.Match(url);

                if (m.Success && (m.Groups.Count == 3))
                {
                    string s = m.Groups[1].ToString();
                    int cid = Convert.ToInt32(s);
                    string response = ReadURL(url);
                } else {
                    MessageBox.Show(string.Format(
                        "Invalid URL. '{0}' excpected", trex.ToString()));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable read URL: " + ex.Message);
            }
        }

        private void tbTalentURL_TextChanged(object sender, EventArgs e)
        {
            btnImport.Enabled = !tbTalentURL.Text.Equals("");
        }

        private void BindClasses()
        {
            if (cbWoWVersion.SelectedItem != null)
                cbClass.DataSource = ((WoWVersion)cbWoWVersion.
                            SelectedItem).Classes.ClassListByName;
            cbClass.SelectedItem = null;
        }

        private void TalentsForm_Load(object sender, EventArgs e)
        {
            wdir = ProcessManager.Config.ProfilesDir +
                        Path.DirectorySeparatorChar + "Talents";

            cbWoWVersion.DataSource = ProcessManager.WoWVersions;
            cbWoWVersion.SelectedItem = ProcessManager.CurVersion;

            BindClasses();

            // Test
            if (ProcessManager.Config.Test == 1)
                tbTalentURL_TextChanged(sender, e);

            _changed = false;
        }

        private void cbTalentTemplates_DropDown(object sender, EventArgs e)
        {
            cbTalentTemplates.Items.Clear();
            string[] dir;

            // Scan Profiles/Talents for list
            try
            {
                dir = Directory.GetFiles(wdir, "*.xml");
            }
            catch (System.IO.DirectoryNotFoundException)
            {
                MessageBox.Show("Directory '" + wdir + "' not found");
                return;
            }
            
            // Check each file
            foreach (string fname in dir)
            {
                try
                {
                    Talents tlist = ProcessManager.ReadTalents(fname);
                    if ((tlist != null) && (tlist.Description != null))
                        cbTalentTemplates.Items.Add(tlist);
                } catch { 
                    // Continue 
                }
            }
        }

        private void SelectClass()
        {
            if ((cbWoWVersion.SelectedItem != null) && (CurTalents != null))
            {
                cbClass.SelectedIndex = ((WoWVersion)cbWoWVersion.SelectedItem).
                    Classes.FindClassByShortName(CurTalents.Class);
                cbClass.Enabled = false;

            }
            else
                cbClass.SelectedItem = null;
        }

        private void cbTalentTemplates_SelectedIndexChanged(object sender, EventArgs e)
        {
            CurTalents = (Talents)cbTalentTemplates.SelectedItem;

            if (CurTalents.WoWVersion != null)
                cbWoWVersion.SelectedItem =
                    ProcessManager.FindWoWVersionByName(CurTalents.WoWVersion);
            else
                cbWoWVersion.SelectedItem = null;

            SelectClass();

            tbDescription.Text = CurTalents.Description;

            // Clear binding
            if (lbLevelList.DataSource != null)
                lbLevelList.DataSource = null;

            lbLevelList.Items.Clear();

            BindLevels();

            lbLevelList.SelectedIndex = 0;

            _changed = false;
            CheckSaveBtn();
        }

        private void CheckSaveBtn()
        {
            bool is_header_set = (!tbDescription.Text.Equals("") &&
                                        !cbClass.Text.Equals("") &&
                                        // Talent template not empty 
                                        !cbTalentTemplates.Text.Equals("") );
            bool not_empty = (lbLevelList.Items.Count > 0);
            bool is_selected = (lbLevelList.SelectedIndex >= 0);

            btnUpdate.Enabled = is_selected;
            btnSave.Enabled = (not_empty && is_header_set && _changed);

            btnAdd.Enabled = ((LevelLabel < ProcessManager.CurVersion.MaxLvl) && is_header_set);

            btnUp.Enabled = (lbLevelList.SelectedIndex > 0);
            btnDown.Enabled = (lbLevelList.SelectedIndex < (lbLevelList.Items.Count - 1));
            btnRemove.Enabled = (not_empty && 
                        (lbLevelList.SelectedIndex ==
                                        (lbLevelList.Items.Count - 1)));
        }

        private void lbLevelList_SelectedIndexChanged(object sender, EventArgs e)
        {
            Level l = (Level) lbLevelList.SelectedItem;

            if (l != null)
            {
                try
                {
                    labelLevelNum.Text = Convert.ToString(l.Num);
                    numTab.Value = l.TabId;
                    numTalent.Value = l.TalentId;
                    numRank.Value = l.Rank;

                    CheckSaveBtn();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);

                    // Unselect everything
                    lbLevelList.SelectedIndex = -1;
                }
            }

            CheckSaveBtn();
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            if (lbLevelList.SelectedValue == null) return;

            ((Level)lbLevelList.SelectedValue).Update((int) numTab.Value, 
                (int) numTalent.Value, (int) numRank.Value);

            RefreshLevelList();
            RegisterChange();
        }

        private void RegisterChange()
        {
            _changed = true;
            CheckSaveBtn();
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            // Select previous item
            lbLevelList.SelectedIndex = lbLevelList.Items.Count - 2;

            // Only can remove last item
            int idx = lbLevelList.Items.Count - 1;
            CurTalents.Levels.RemoveAt(idx);

            RefreshLevelList();
            RegisterChange();
        }

        private void tbDescription_TextChanged(object sender, EventArgs e)
        {
            RegisterChange();
        }

        private void cbClass_TextChanged(object sender, EventArgs e)
        {
            RegisterChange();
        }

        private void cbTalentTemplates_TextChanged(object sender, EventArgs e)
        {
            if (CurTalents != null)
                CurTalents.FullPath = wdir + Path.DirectorySeparatorChar + 
                                            cbTalentTemplates.Text + ".xml";

            RegisterChange();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                XmlSerializer s = new XmlSerializer(typeof(Talents));
                TextWriter w = new StreamWriter(CurTalents.FullPath);

                // Save parameters as well
                CurTalents.URL = tbTalentURL.Text;
                CurTalents.Description = tbDescription.Text;
                CurTalents.Class = ((CharClass) cbClass.SelectedItem).ShortName;
                CurTalents.WoWVersion = cbWoWVersion.Text;

                s.Serialize(w, CurTalents);
                w.Close();

                _changed = false;
                btnSave.Enabled = false;

                MessageBox.Show(this, "File " + CurTalents.FullPath +
                    " successfully saved", "SUCCESS", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "ERROR", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            int num = 10;
            int tab = 1;
            int talent = 1;
            int rank = 1;

            // Find last item
            if ((CurTalents != null) && (CurTalents.Levels.Count > 0))
            {
                Level last = (Level)CurTalents.Levels[CurTalents.Levels.Count - 1];

                num = last.Num + 1;
                tab = last.TabId;
                talent = last.TalentId;

                if (last.Rank < numRank.Maximum)
                    rank = last.Rank + 1;
                else
                    talent++;
            }

            Level l  = new Level(num , tab, talent, rank);

            bool is_new = (CurTalents == null);
            if (is_new)
            {
                // Don't forget add .xml extension for new file
                CurTalents = new Talents(cbTalentTemplates.Text + ".xml",
                                        tbTalentURL.Text, tbDescription.Text);
                BindLevels();
            }

            CurTalents.AddLevel(l);

            RefreshLevelList();
            lbLevelList.SelectedIndex = lbLevelList.Items.Count - 1;

            RegisterChange();
        }

        private void RefreshLevelList()
        {
            ((CurrencyManager)BindingContext[CurTalents.Levels]).Refresh();
        }

        private void BindLevels()
        {
            lbLevelList.DataSource = CurTalents.Levels;
        }

        private void btnUp_Click(object sender, EventArgs e)
        {
            if (lbLevelList.SelectedIndex <= 0) return;

            int idx = lbLevelList.SelectedIndex;
            SwitchLevels((Level)CurTalents.Levels[idx],
                        (Level)CurTalents.Levels[idx - 1]);
            
            lbLevelList.SelectedIndex = (idx - 1);

            RegisterChange();
        }

        private void SwitchLevels(Level cur, Level prev)
        {
            Level saved = (Level)cur.Clone();

            cur.Update(prev.TabId, prev.TalentId, prev.Rank);
            RefreshLevelList();
            prev.Update(saved.TabId, saved.TalentId, saved.Rank);
            RefreshLevelList();
        }

        private void btnDown_Click(object sender, EventArgs e)
        {
            if ((lbLevelList.SelectedIndex < 0) || 
                    (lbLevelList.SelectedIndex == lbLevelList.Items.Count - 1)) return;

            int idx = lbLevelList.SelectedIndex;
            SwitchLevels((Level)CurTalents.Levels[idx],
                        (Level)CurTalents.Levels[idx + 1]);

            lbLevelList.SelectedIndex = (idx + 1);

            RegisterChange();
        }

        private void TalentsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = (_changed &&
                (MessageBox.Show(this, "Are you sure you want close and cancel changes ?",
                    "Confirmation", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes));
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            if (CurTalents != null)
            {
                CurTalents.Levels.Clear();
                RefreshLevelList();

                RegisterChange();
            }
        }

        private void cbWoWVersion_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Change binding for Class
            BindClasses();
            SelectClass();
        }
    }
}

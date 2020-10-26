﻿using System;
using System.Text;
using System.Windows.Forms;
using DevExpress.LookAndFeel;
using DevExpress.Utils.Extensions;
using DBConnection;
using MDS00;
using System.Drawing;
using DevExpress.XtraPrinting;
using DevExpress.XtraGrid.Views.Grid;

namespace M05
{
    public partial class XtraForm1 : DevExpress.XtraBars.Ribbon.RibbonForm
    {
        private Functionality.Function FUNC = new Functionality.Function();
        public XtraForm1()
        {
            InitializeComponent();
            UserLookAndFeel.Default.StyleChanged += MyStyleChanged;
            iniConfig = new IniFile("Config.ini");
            UserLookAndFeel.Default.SetSkinStyle(iniConfig.Read("SkinName", "DevExpress"), iniConfig.Read("SkinPalette", "DevExpress"));
        }

        private IniFile iniConfig;

        private void MyStyleChanged(object sender, EventArgs e)
        {
            UserLookAndFeel userLookAndFeel = (UserLookAndFeel)sender;
            LookAndFeelChangedEventArgs lookAndFeelChangedEventArgs = (DevExpress.LookAndFeel.LookAndFeelChangedEventArgs)e;
            //MessageBox.Show("MyStyleChanged: " + lookAndFeelChangedEventArgs.Reason.ToString() + ", " + userLookAndFeel.SkinName + ", " + userLookAndFeel.ActiveSvgPaletteName);
            iniConfig.Write("SkinName", userLookAndFeel.SkinName, "DevExpress");
            iniConfig.Write("SkinPalette", userLookAndFeel.ActiveSvgPaletteName, "DevExpress");
        }

        private void XtraForm1_Load(object sender, EventArgs e)
        {
            bbiNew.PerformClick();
        }

        private void LoadData()
        {
            StringBuilder sbSQL = new StringBuilder();
            sbSQL.Append("SELECT OIDCURR AS No, Currency, CreateBy, CreateDate ");
            sbSQL.Append("FROM Currency ");
            sbSQL.Append("ORDER BY OIDCURR, Currency ");
            new ObjDevEx.setGridControl(gcCurrency, gvCurrency, sbSQL).getData(false, false, true, true);

        }

        private void NewData()
        {
            txeCurrency.Text = "";
            lblStatus.Text = "* Add Currency";
            lblStatus.ForeColor = Color.Green;

            txeID.Text = new DBQuery("SELECT CASE WHEN ISNULL(MAX(OIDCURR), '') = '' THEN 1 ELSE MAX(OIDCURR) + 1 END AS NewNo FROM Currency").getString();

            txeCREATE.Text = "0";
            txeDATE.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            //txeID.Focus();
        }

        private void bbiNew_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            LoadData();
            NewData();
        }

        private void gvCurrency_RowCellClick(object sender, DevExpress.XtraGrid.Views.Grid.RowCellClickEventArgs e)
        {
            lblStatus.Text = "* Edit Currency";
            lblStatus.ForeColor = Color.Red;

            txeID.Text = gvCurrency.GetFocusedRowCellValue("No").ToString();
            txeCurrency.Text = gvCurrency.GetFocusedRowCellValue("Currency").ToString();

            txeCREATE.Text = gvCurrency.GetFocusedRowCellValue("CreateBy").ToString();
            txeDATE.Text = gvCurrency.GetFocusedRowCellValue("CreateDate").ToString();
            
        }

        private void bbiSave_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (txeCurrency.Text.Trim() == "")
            {
                FUNC.msgWarning("Please input currency name.");
                txeCurrency.Focus();
            }
            else
            {
                txeCurrency.Text = txeCurrency.Text.ToUpper().Trim();
                bool chkCURR = chkDuplicate();
                
                if (chkCURR == true)
                {
                    if (FUNC.msgQuiz("Confirm save data ?") == true)
                    {
                        StringBuilder sbSQL = new StringBuilder();
                        string strCREATE = "0";
                        if (txeCREATE.Text.Trim() != "")
                        {
                            strCREATE = txeCREATE.Text.Trim();
                        }

                        sbSQL.Append("IF NOT EXISTS(SELECT OIDCURR FROM Currency WHERE OIDCURR = N'" + txeID.Text.Trim() + "') ");
                        sbSQL.Append(" BEGIN ");
                        sbSQL.Append("  INSERT INTO Currency(Currency, CreateBy, CreateDate) ");
                        sbSQL.Append("  VALUES(N'" + txeCurrency.Text.Trim().Replace("'", "''") + "', '" + strCREATE + "', GETDATE()) ");
                        sbSQL.Append(" END ");
                        sbSQL.Append("ELSE ");
                        sbSQL.Append(" BEGIN ");
                        sbSQL.Append("  UPDATE Currency SET ");
                        sbSQL.Append("      Currency = N'" + txeCurrency.Text.Trim().Replace("'", "''") + "' ");
                        sbSQL.Append("  WHERE(OIDCURR = '" + txeID.Text.Trim() + "') ");
                        sbSQL.Append(" END ");
                        //MessageBox.Show(sbSQL.ToString());
                        if (sbSQL.Length > 0)
                        {
                            try
                            {
                                bool chkSAVE = new DBQuery(sbSQL).runSQL();
                                if (chkSAVE == true)
                                {
                                    bbiNew.PerformClick();
                                    FUNC.msgInfo("Save complete.");   
                                }
                            }
                            catch (Exception)
                            { }
                        }
                    }
                }
            }

        }

        private void txeCurrency_LostFocus(object sender, EventArgs e)
        {
            bool chkDup = chkDuplicate();
            if (chkDup == false)
            {
                txeCurrency.Text = "";
                txeCurrency.Focus();
            }
        }

        private bool chkDuplicate()
        {
            bool chkDup = true;
            if (txeCurrency.Text != "")
            {
                txeCurrency.Text = txeCurrency.Text.ToUpper().Trim();
                if (txeCurrency.Text.Trim() != "" && lblStatus.Text == "* Add Currency")
                {
                    StringBuilder sbSQL = new StringBuilder();
                    sbSQL.Append("SELECT TOP(1) Currency FROM Currency WHERE (Currency = N'" + txeCurrency.Text.Trim().Replace("'", "''") + "') ");
                    if (new DBQuery(sbSQL).getString() != "")
                    {
                        FUNC.msgWarning("Duplicate currency. !! Please Change.");
                        txeCurrency.Text = "";
                        chkDup = false;
                    }
                }
                else if (txeCurrency.Text.Trim() != "" && lblStatus.Text == "* Edit Currency")
                {
                    StringBuilder sbSQL = new StringBuilder();
                    sbSQL.Append("SELECT TOP(1) OIDCURR ");
                    sbSQL.Append("FROM Currency ");
                    sbSQL.Append("WHERE (Currency = N'" + txeCurrency.Text.Trim().Replace("'", "''") + "') ");
                    string strCHK = new DBQuery(sbSQL).getString();
                    if (strCHK != "" && strCHK != txeID.Text.Trim())
                    {
                        FUNC.msgWarning("Duplicate currency. !! Please Change.");
                        txeCurrency.Text = "";
                        chkDup = false;
                    }
                }
            }
            return chkDup;
        }


        private void bbiExcel_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            string pathFile = new ObjSet.Folder(@"C:\MDS\Export\").GetPath() + "CurrencyList_" + DateTime.Now.ToString("yyyyMMdd") + ".xlsx";
            gvCurrency.ExportToXlsx(pathFile);
            System.Diagnostics.Process.Start(pathFile);
        }

        private void gvCurrency_RowStyle(object sender, DevExpress.XtraGrid.Views.Grid.RowStyleEventArgs e)
        {
            if (sender is GridView)
            {
                GridView gView = (GridView)sender;
                if (!gView.IsValidRowHandle(e.RowHandle)) return;
                int parent = gView.GetParentRowHandle(e.RowHandle);
                if (gView.IsGroupRow(parent))
                {
                    for (int i = 0; i < gView.GetChildRowCount(parent); i++)
                    {
                        if (gView.GetChildRowHandle(parent, i) == e.RowHandle)
                        {
                            e.Appearance.BackColor = i % 2 == 0 ? Color.AliceBlue : Color.White;
                        }
                    }
                }
                else
                {
                    e.Appearance.BackColor = e.RowHandle % 2 == 0 ? Color.AliceBlue : Color.White;
                }
            }
        }

        private void txeCurrency_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                txeID.Focus();
            }
        }
    }
}
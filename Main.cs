using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Kbg.NppPluginNET.PluginInfrastructure;
using MarkdownPlugin;

namespace Kbg.NppPluginNET
{
    class Main
    {

        static MarkdownGenerator generator = new MarkdownGenerator();

        internal const string PluginName = "MarkdownPlugin";
        static string iniFilePath = null;
        static bool someSetting = false;
        static int idMyDlg = -1;
        static Bitmap tbBmp = Properties.Resources.star;
        static Bitmap tbBmp_tbTab = Properties.Resources.star_bmp;
        static Icon tbIcon = null;

        public static void OnNotification(ScNotification notification)
        {  
            // This method is invoked whenever something is happening in notepad++
            // use eg. as
            // if (notification.Header.Code == (uint)NppMsg.NPPN_xxx)
            // { ... }
            // or
            //
            // if (notification.Header.Code == (uint)SciMsg.SCNxxx)
            // { ... }
        }

        internal static void CommandMenuInit()
        {
            StringBuilder sbIniFilePath = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint) NppMsg.NPPM_GETPLUGINSCONFIGDIR, Win32.MAX_PATH, sbIniFilePath);
            iniFilePath = sbIniFilePath.ToString();
            if (!Directory.Exists(iniFilePath)) Directory.CreateDirectory(iniFilePath);
            iniFilePath = Path.Combine(iniFilePath, PluginName + ".ini");
            someSetting = (Win32.GetPrivateProfileInt("SomeSection", "SomeKey", 0, iniFilePath) != 0);

            PluginBase.SetCommand(0, "Generate Table", myMenuFunction, new ShortcutKey(true, false, true, Keys.C));
            PluginBase.SetCommand(0, "Generate Diagram to Clipoard", generateDiagram);
            PluginBase.SetCommand(1, "Save DrawIO as File", saveAsDrawIO); 
            idMyDlg = 0;
        }

        private static void generateDiagram()
        {

            try
            {
                IntPtr currentScint = PluginBase.GetCurrentScintilla();
                ScintillaGateway scintillaGateway = new ScintillaGateway(currentScint);
                // Get selected text.
                string selectedText = scintillaGateway.GetSelText();
                var lines = selectedText.Split('\n');
                DrawIOBuilder builder = new DrawIOBuilder();
                for (var i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    lines[i] = line.Trim(new char[] { ' ', '\r' });

                }
                DrawIOComponent[] drawIOComponent = builder.FlowchartBuilder(lines);
                builder.CopyToClipBoard(drawIOComponent);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);

            }
        }

        internal static void SetToolBarIcon()
        {
            toolbarIcons tbIcons = new toolbarIcons();
            tbIcons.hToolbarBmp = tbBmp.GetHbitmap();
            IntPtr pTbIcons = Marshal.AllocHGlobal(Marshal.SizeOf(tbIcons));
            Marshal.StructureToPtr(tbIcons, pTbIcons, false);
            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint) NppMsg.NPPM_ADDTOOLBARICON, PluginBase._funcItems.Items[idMyDlg]._cmdID, pTbIcons);
            Marshal.FreeHGlobal(pTbIcons);
        }

        internal static void PluginCleanUp()
        {
            Win32.WritePrivateProfileString("SomeSection", "SomeKey", someSetting ? "1" : "0", iniFilePath);
        }


        internal static void myMenuFunction()
        {

            


            IntPtr currentScint = PluginBase.GetCurrentScintilla();
            ScintillaGateway scintillaGateway = new ScintillaGateway(currentScint);
                // Get selected text.
            string selectedText = scintillaGateway.GetSelText();

      
            var lines = selectedText.Split('\n');
            //var html = "<table style='box-sizing: inherit; font-family: arial, sans-serif; border-collapse: collapse; color: rgb(0, 0, 0); font-size: 15px; font-style: normal; font-variant-ligatures: normal; font-variant-caps: normal; font-weight: 400; letter-spacing: normal; orphans: 2; text-align: start; text-indent: 0px; text-transform: none; white-space: normal; widows: 2; word-spacing: 0px; -webkit-text-stroke-width: 0px; text-decoration-style: initial; text-decoration-color: initial;'>";

            var myHtml = generator.GenerateMarkdown(lines);

            ClipboardHelper.CopyToClipboard(selectedText, myHtml);

        }

        internal static void saveAsDrawIO()
        {
            try
            {
                frmMyDlg frmMyDlg = new frmMyDlg();
                var filename = frmMyDlg.filename;

                IntPtr currentScint = PluginBase.GetCurrentScintilla();
                ScintillaGateway scintillaGateway = new ScintillaGateway(currentScint);
                // Get selected text.
                string selectedText = scintillaGateway.GetSelText();
                var lines = selectedText.Split('\n'); for (var i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    lines[i] = line.Trim(new char[] { ' ', '\r' });

                }
                DrawIOBuilder builder = new DrawIOBuilder();
                DrawIOComponent[] drawIOComponent = builder.FlowchartBuilder(lines);
                builder.SaveToFile(filename, drawIOComponent);
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message);

            }



        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MarkdownPlugin
{
    public class ClipboardHelper
    {

        public static void CopyToClipboard(string text, string html) {


            var dataObject = new DataObject();



            if (html != null && html.Length > 0) { 
                var start = 99;
                var startFragment = 133;
                var endFragment = startFragment + html.Length;
                var endHtml = endFragment + 78;
                var htmlFormat = "Version:0.9\r\n";
                htmlFormat += "StartHTML: 000000102\r\n";
                htmlFormat += "EndHTML:" + (endHtml + "").PadLeft(9, '0') + "\r\n";
                htmlFormat += "StartFragment:000000134\r\n";
                htmlFormat += "EndFragment:" + (endFragment + "").PadLeft(9, '0') + "\r\n";
                htmlFormat += "<html><body><!--StartFragment-->" + html + "<!--EndFragment--></body></html>";
                dataObject.SetData(DataFormats.Html, htmlFormat);
            }
            dataObject.SetData(DataFormats.Text, text);
            dataObject.SetData(DataFormats.Rtf, text);
            //dataObject.SetData(DataFormats.UnicodeText, selectedText);
            Clipboard.SetDataObject(dataObject);


        }
    }
}

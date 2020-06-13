using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;

namespace MarkdownPlugin
{
    class FlowChart
    {
    }


    public class Component {
        string type;
        string text;
        string color = "#0f0fff";

        public void Draw(){
            Bitmap b = new Bitmap(200, 200);
            var graphics = Graphics.FromImage(b);
            graphics.Clear(Color.White);
            graphics.FillRectangle(Brushes.Black, new Rectangle(0, 0, 100, 100));
            
            b.Save(@"d:\tmp\a.png", ImageFormat.Png);
            graphics.Dispose();
            b.Dispose();
        }    
    
    }


    
}

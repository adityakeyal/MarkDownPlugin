using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;

namespace MarkdownPlugin
{
    public class DrawIOBuilder
    {
        public DrawIOComponent[] FlowchartBuilder(string[] lines)
        {

            string[] starticons = new string[] { "((" , "(" , "[" , "<" };
            string[] endicons = new string[] { "))" , ")" , "]" , ">" };
            string[] componenttypes = new string[] { "OVAL" , "ROUNDRECT" , "RECTANGLE", "DIAMOND" };

            List<DrawIOComponent> component = new List<DrawIOComponent> { };

            Dictionary<string, DrawIOComponent> componentMap = new Dictionary<string, DrawIOComponent>();
            
            //each line should be filtered to type of link or shape
            // link contains a --

            foreach (var line in lines){
                if (line.Trim().Length == 0) {
                    continue;
                }
                if (line.Trim().StartsWith("graph ")) {
                    DrawIOComponent.LR = line.Trim().Substring(6).Equals("LR");
                    continue;
                }

                if (!line.Contains("--"))
                {

                    // this is a component
                    //loop over the component and speparate text and Key

                    var isKey = true;
                    var isQuoted = false;

                    DrawIOComponent ct = null;
                        

                    var text = "";
                    var key = "";
                    var componentType = "";
                    //identify location of Symbol


                    int startIconIdx = -1;
                    int iconpos = -1;

                    for (int i = 0; i < starticons.Length; i++) {
                        var tmpIdx = line.IndexOf(starticons[i]);
                        if (tmpIdx > -1) {
                            if (tmpIdx < startIconIdx || startIconIdx == -1) {
                                startIconIdx = tmpIdx;
                                iconpos = i;
                            }
                        }
                    }


                    key = line.Substring(0,startIconIdx);
                    int symbolstart = startIconIdx + starticons[iconpos].Length;
                    int symbolend = line.IndexOf(endicons[iconpos]);
                    text = line.Substring(symbolstart, symbolend - symbolstart).Trim();
                    if (text.StartsWith("\"") && text.EndsWith("\"")) {
                        text=text.Substring(1, text.Length - 2);
                    }
                    
                    
                    componentType = componenttypes[iconpos];



                    switch (componentType)
                    {
                        case "RECTANGLE":
                            ct = new Rectangle(0, 0, 100, 50);
                            break;
                        case "ROUNDRECT":
                            ct = new RoundedRectangle(0, 0, 100, 50);
                            break;
                        case "OVAL":
                            ct = new Circle(0, 0,100);
                            break;
                        case "DIAMOND":
                            ct = new Rectangle(0, 0, 100, 50);
                            break;
                    }

                    //design
                    string addlInfo = line.Substring(symbolend + endicons[iconpos].Length);
                    if (addlInfo.Length > 0) {
                        string[] addInfoArr = addlInfo.Split(';');
                        foreach (string addl in addInfoArr) {
                            if (addl.Trim().StartsWith("#"))
                            {
                                ct.Fill(addl);
                            }
                            else {
                                ct.AddCustomStyle(addl);
                            }

                        }

                    }


                    Console.WriteLine(key + " :  " + text + " : " + ct.GetType());
                    ct.Text(text);
                    component.Add(ct);
                    componentMap.Add(key, ct);


                }

                
            }


            foreach (var line in lines) {

                if (line.Contains("--")) {


                    // Links can be --XXX--> or -->

                    // split by -->
                    int index = line.IndexOf("-->");
                    var toComponent = line.Substring(index + 3);
                    var fromComponent = line.Substring(0, index);

                    var fromIdx = fromComponent.IndexOf("--");

                    var text = "";

                    if (fromIdx >0 -1) {
                        //the text is blank
                        fromComponent=fromComponent.Substring(0, fromIdx);
                        var toIdx = line.IndexOf("-->");

                        text = line.Substring(fromIdx + 2, toIdx - fromComponent.Length-2).Replace("\"","");
                    }


                    var fc = componentMap[fromComponent];
                    var tc = componentMap[toComponent];
                    fc.To(tc,text);
                    // link fromComponent to toComponent
                    // Remove from parent list since it no longer is a parent but a chile ement
                    
                    component.Remove(tc);
                }

            }

            return component.ToArray();


        }


        public void DrawToClipBoard(params DrawIOComponent[] components) {

            Tuple<int, int> max = null;
            foreach (DrawIOComponent comp in components) {
                max = comp.DefineWidth(100, 0);
            }

            Bitmap bp = new Bitmap(max.Item1+5, max.Item2+5);
            var graphics = Graphics.FromImage(bp);
            graphics.Clear(Color.White);

            foreach (DrawIOComponent comp in components) {
                comp.Diagram(graphics);
            }

            Clipboard.SetImage(bp);

            bp.Save(@"d:\tmp\a.png", ImageFormat.Png);
            graphics.Dispose();
            bp.Dispose();

        }
        public void CopyToClipBoard(params DrawIOComponent[] components) {

            var htmlDocument = PrepareEncodedHtml(components);
            ClipboardHelper.CopyToClipboard(htmlDocument, "");
        }

        public string PrepareEncodedHtml(params DrawIOComponent[] components)
        {

            var xml = "";
            for (var i = 0; i < components.Length; i++)
            {

                foreach (var c in components[i].Generate()) {
                    xml += c.ToString();
                }
            }

            var finalGraph = @"<mxGraphModel dx=""581"" dy=""377"" grid=""1"" gridSize=""10"" guides=""1"" tooltips=""1"" connect=""1"" arrows=""1"" fold=""1"" page=""1"" pageScale=""1"" pageWidth=""850"" pageHeight=""1100"" math=""0"" shadow=""0"">
  <root>
    <mxCell id=""0""/>
    <mxCell id=""1"" parent=""0""/>
" + xml + @"
  </root>
</mxGraphModel>";

            string htmlDocument = Uri.EscapeDataString(finalGraph.Replace("\r", ""));

            return htmlDocument;
            
        }

        public void SaveToFile(string filename, params DrawIOComponent[] components) {

            var htmlDocument = PrepareEncodedHtml(components);

            byte[] v1 = ZippingUtility.ZipStr(htmlDocument);

            var base64Diagram = Convert.ToBase64String(v1);

            var drawioText = @"<mxfile host=""Electron"" modified=""2020-06-11T05:50:50.121Z"" agent=""5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) draw.io/12.9.13 Chrome/80.0.3987.163 Electron/8.2.1 Safari/537.36"" etag=""yi-1HzVJONjMD6kCr-6Y"" version=""12.9.13"" type=""device"">
	<diagram id=""gKpNtO0FCgyKS3AscSpZ"" name=""Page-1"">" + base64Diagram + @"</diagram>
</mxfile>";

            File.WriteAllText(filename, drawioText);

        }


    }

    public abstract class DrawIOComponent
    {
        private string id = System.Guid.NewGuid().ToString();
        private int x;
        private int y;
        private int h;
        private int w;
       

        protected DrawIOComponent(int x, int y, int w, int h)
        {
            this.x = x;
            this.y = y;
            this.h = h;
            this.w = w;
        }

        private string text = "";
        private string fillColor = null;
        private string strokeColor = "#000000";

        private ArrayList style = new ArrayList();



        private List<DrawIOComponentLink> destinationComponent = new List<DrawIOComponentLink> ();
        private List<DrawIOComponent> parents = new List<DrawIOComponent>();
        public static bool LR = false;
        private readonly int deltaXArrow = 50;
        private readonly int deltaYArrow = 50;

        private bool generated = false;


        /**
         * The purpose of this method is to define the location where the elements will appear on the map.
         * The logic is based on the below:
         * The lowest value will be the value of the element in the left most branch of the tree
         */




        public Tuple<int,int> DefineWidth(int x , int y)
        {

            if (!LR)
            {

                this.x = x;
                this.y = y;


                var endingPoint = new Tuple<int, int>(x, y);

                int deltaX = 0;
                foreach (var dc in destinationComponent)
                {
                    endingPoint = dc.Destination.DefineWidth(endingPoint.Item1 + deltaX, this.y + this.h + 50);
                    deltaX = 50;
                    
                }

                int xDelta = 0;
                if (destinationComponent.Count == 0)
                {
                    xDelta = this.w;
                }

                return new Tuple<int, int>(endingPoint.Item1 + xDelta, endingPoint.Item2 + this.h);
            }


            this.x = x;
            this.y = y;


            var endingPoint1 = new Tuple<int, int>(x, y);

            int count1 = 0;
            foreach (var dc in destinationComponent)
            {
                endingPoint1 = dc.Destination.DefineWidth(endingPoint1.Item1 + 50 , this.y + this.h + 50 * count1);
                count1++;
            }

            int yDelta = 0;
            if (destinationComponent.Count == 0)
            {
                yDelta  = this.h;
            }

            return new Tuple<int, int>(endingPoint1.Item1 + this.w, endingPoint1.Item2 + yDelta);
        }

        

        public void Diagram(Graphics gd) {
            if (!generated) {

                using(Brush b = new SolidBrush(HexToColor(this.fillColor)))
                using (Font drawFont = new Font("Arial", 8))
                using (SolidBrush drawBrush = new SolidBrush(Color.Black))
                using (StringFormat drawFormat = new StringFormat())
                {

                    System.Drawing.Rectangle r = new System.Drawing.Rectangle(x, y, w, h);
                    populateGraphic(gd, b, r);

                    drawFormat.FormatFlags = StringFormatFlags.DirectionVertical;
                    SizeF sizeF = gd.MeasureString(this.text, drawFont);

                    gd.DrawString(this.text, drawFont, drawBrush, this.x + (this.w - sizeF.Width) / 2, this.y + (this.h - sizeF.Height) / 2);

                }

                // 
            }
            //

            /*var count = 0;*/
            var exitX = LR ? 1 : 0.5;
            var exitY = LR ? 0.5 : 1;
            var enterX = LR ? 0 : 0.5;
            var enterY = LR ? 0.5 : 0;


            //TODO Draw a link

            using (Font drawFont = new Font("Arial", 8))
            using (SolidBrush drawBrush = new SolidBrush(Color.Black))
            using (SolidBrush fillBrush = new SolidBrush(Color.White))
            using (StringFormat drawFormat = new StringFormat())
            using (Pen p = new Pen(Color.Black))
            {
                List<Holder> l = new List<Holder>();
                foreach (var dc in destinationComponent)
              {

                dc.Destination.Diagram(gd);

               
                    

                    // if x points match
                    int pointExitX = (int)(this.x + this.w * exitX);
                    int pointEnterX = (int)(dc.Destination.x + dc.Destination.w * enterX);
                    int pointExitY = (int)(this.y + this.h * exitY);
                    int pointEnterY = (int)(dc.Destination.y + dc.Destination.h * enterY);

                    bool xMatch =  pointExitX == pointEnterX;
                    bool yMatch = pointExitY == pointEnterY;

                    if (xMatch || yMatch)
                    {
                        p.CustomEndCap = new AdjustableArrowCap(5, 5);
                        gd.DrawLine(p, new Point(pointExitX, pointExitY), new Point(pointEnterX, pointEnterY));

                        //Fill Text
                        if (dc.Text != null)
                        {


                            var h = new Holder(gd, dc, drawFont, drawBrush, fillBrush, drawFormat, pointEnterX, pointExitY, pointEnterY);
                            l.Add(h);


                           /* drawFormat.FormatFlags = StringFormatFlags.DirectionVertical;
                            SizeF sizeF = gd.MeasureString(dc.Text, drawFont);
                            gd.FillRectangle(fillBrush, (float)((this.x + this.w * exitX + dc.Destination.x + dc.Destination.w * enterX) / 2) - sizeF.Width / 2, (float)((this.y + this.h * exitY + (dc.Destination.y + dc.Destination.h * enterY)) / 2) - sizeF.Height / 2, sizeF.Width, sizeF.Height);
*/
                           /* gd.DrawString(dc.Text, drawFont, drawBrush, (float)((this.x + this.w * exitX + dc.Destination.x + dc.Destination.w * enterX) / 2) - sizeF.Width / 2, (float)((this.y + this.h * exitY + (dc.Destination.y + dc.Destination.h * enterY)) / 2) - sizeF.Height / 2);*/
                        }

                    }
                    else {
                        p.CustomEndCap = new AdjustableArrowCap(0,0);
                        // build a patch
                        if (Math.Abs(pointEnterX - pointExitX) > Math.Abs(pointEnterY - pointExitY)) {

                            // draw 3 lines 

                            // exitPoint to point 1
                            // point 1 to point 2
                            // point 2 to enter


                            gd.DrawLine(p, new Point(pointExitX, pointExitY), new Point(pointExitX, pointExitY + (pointEnterY -pointExitY) / 2));
                            gd.DrawLine(p, new Point(pointExitX, pointExitY + (pointEnterY - pointExitY) / 2) , new Point(pointEnterX, pointExitY + (pointEnterY - pointExitY) / 2));
                            p.CustomEndCap = new AdjustableArrowCap(5, 5);
                            gd.DrawLine(p, new Point(pointEnterX, pointExitY + (pointEnterY - pointExitY) / 2), new Point(pointEnterX, pointEnterY));

                            //Fill Text
                            if (dc.Text != null)
                            {
                                var h= new Holder(gd, dc, drawFont, drawBrush, fillBrush, drawFormat, pointEnterX, pointExitY, pointEnterY);
                                l.Add(h);
                                
                            }

                        }


                    }


                   
                    

                }





                //WooHoo
                foreach (Holder h in l)
                {
                    DrawString(h);
                }

            }


            //

        }

        private static void DrawString(Holder h) {

            h.drawFormat.FormatFlags = StringFormatFlags.DirectionVertical;
            SizeF sizeF = h.gd.MeasureString(h.dc.Text, h.drawFont);
            h.gd.FillRectangle(h.fillBrush, h.pointEnterX - sizeF.Width / 2, h.pointExitY + (h.pointEnterY - h.pointExitY) / 2 - sizeF.Height / 2, sizeF.Width, sizeF.Height);
            h.gd.DrawString(h.dc.Text, h.drawFont, h.drawBrush, h.pointEnterX - sizeF.Width / 2, h.pointExitY + (h.pointEnterY - h.pointExitY) / 2 - sizeF.Height / 2);
        }

        protected abstract void populateGraphic(Graphics gd, Brush b, System.Drawing.Rectangle r);

        public XElement[] Generate()
        {
                
            
            List<XElement> list = new List<XElement>();
            if (!generated)
            {

                XElement elem = new XElement("mxCell",
                new XAttribute("id", this.id),
                new XAttribute("value", this.text),
                new XAttribute("style", this.fetchStyle()),
                new XAttribute("vertex", "1"),
                new XAttribute("parent", "1"),
                new XElement("mxGeometry",
                    new XAttribute("x", this.x),
                    new XAttribute("y", this.y),
                    new XAttribute("width", this.w),
                    new XAttribute("height", this.h),
                    new XAttribute("as", "geometry")
                    )
            );


                list.Add(elem);
                generated = true;

            }


                var count = 0;
            var exitX = LR ? 1 : 0.5;
            var exitY = LR ? 0.5 : 1;
            var enterX = LR ? 0 : 0.5;
            var enterY = LR ? 0.5 : 0;



            foreach (var dc in destinationComponent) {

                if (LR) { 
                    dc.Destination.x = this.x + this.w + deltaXArrow;
                    dc.Destination.y = this.y + (this.h + deltaYArrow) * count;
                }
                else {
                    dc.Destination.x = this.x + (this.w + deltaXArrow) * count;
                    dc.Destination.y = this.y + this.h + deltaYArrow;
                }
                count++;

                XElement[] xElement = dc.Destination.Generate();
                list.AddRange(xElement);


                XElement link = new XElement("mxCell",
                    new XAttribute("id", System.Guid.NewGuid().ToString()),
                    new XAttribute("value", dc.Text),
                    new XAttribute("style", $"edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;html=1;exitX={exitX};exitY={exitY};entryX={enterX};entryY={enterY};"),
                    new XAttribute("edge", "1"),
                    new XAttribute("parent", "1"),
                    new XAttribute("source", this.id),
                    new XAttribute("target", dc.Destination.id),
                    new XElement("mxGeometry",
                        new XAttribute("relative", "1"),
                        new XAttribute("as", "geometry"),
                        new XElement("mxPoint",
                            new XAttribute("as", "sourcePoint")
                            ),
                            new XElement("mxPoint",
                                new XAttribute("as", "targetPoint")
                                )
                        )
                );

                list.Add(link);
            }

            return list.ToArray();
        }

        public DrawIOComponent Text(string text)
        {
            this.text = text;
            return this;
        }

        public DrawIOComponent Fill(string color)
        {
            this.fillColor = color;
            return this;
        }

        public void AddCustomStyle(string style) {
            this.style.Add(style);
        }


        public DrawIOComponent Stroke(string color)
        {
            this.strokeColor = color;
            return this;
        }


        protected virtual string fetchStyle()
        {
            var fillColor = this.fillColor == null ? "#ffffff" : this.fillColor; 
            return $"whiteSpace=wrap;html=1;aspect=fixed;fillColor={fillColor};strokeColor={this.strokeColor};{string.Join(";",this.style.ToArray()).Trim()};";
        }

        internal void To(DrawIOComponent tc,string text)
        {
            destinationComponent.Add(new DrawIOComponentLink(tc, text));
            tc.From(this);
        }

        private void From(DrawIOComponent parentNode)
        {
            parents.Add(parentNode);
            
        }

        public static Color HexToColor(string hexString)
        {
            hexString = hexString == null ? "#000000" : hexString;
            hexString = hexString.Trim();
            //replace # occurences
            if (hexString.IndexOf('#') != -1)
                hexString = hexString.Replace("#", "");

            int r, g, b;
            Console.WriteLine(hexString);

            r = int.Parse(hexString.Substring(0, 2), NumberStyles.AllowHexSpecifier);
            g = int.Parse(hexString.Substring(2, 2), NumberStyles.AllowHexSpecifier);
            b = int.Parse(hexString.Substring(4, 2), NumberStyles.AllowHexSpecifier);

            return Color.FromArgb(r, g, b);
        }

    }

    internal class Holder
    {
        internal Graphics gd;
        internal DrawIOComponentLink dc;
        internal Font drawFont;
        internal SolidBrush drawBrush;
        internal SolidBrush fillBrush;
        internal StringFormat drawFormat;
        internal int pointEnterX;
        internal int pointExitY;
        internal int pointEnterY;

        public Holder(Graphics gd, DrawIOComponentLink dc, Font drawFont, SolidBrush drawBrush, SolidBrush fillBrush, StringFormat drawFormat, int pointEnterX, int pointExitY, int pointEnterY)
        {
            this.gd = gd;
            this.dc = dc;
            this.drawFont = drawFont;
            this.drawBrush = drawBrush;
            this.fillBrush = fillBrush;
            this.drawFormat = drawFormat;
            this.pointEnterX = pointEnterX;
            this.pointExitY = pointExitY;
            this.pointEnterY = pointEnterY;
        }
    }

    public class Rectangle : DrawIOComponent
    {
        public Rectangle(int x, int y, int w, int h) : base(x, y, w, h)
        {

        }

        protected override void populateGraphic(Graphics gd, Brush b, System.Drawing.Rectangle r)
        {

            using (Pen p = new Pen(b, 1))
            {
                gd.DrawRectangle(p, r);
            }

        }
    }

    public class Circle : DrawIOComponent
    {
        public Circle(int x, int y, int w) : base(x, y, w, w)
        {

        }


        protected override string fetchStyle()
        {
            var style = base.fetchStyle();
            return $"ellipse;{style};rounded=1;";

        }

        protected override void populateGraphic(Graphics gd, Brush b, System.Drawing.Rectangle r)
        {
            using(Pen p = new Pen(Color.Black, 1)){
                gd.DrawEllipse(p, r);
            }


        }
    }

    public class RoundedRectangle : DrawIOComponent
    {
        public RoundedRectangle(int x, int y, int w, int h) : base(x, y, w, h)
        {
        }

        protected override string fetchStyle()
        {
            var style = base.fetchStyle();
            return $"{style};rounded=1;";
        }

        protected override void populateGraphic(Graphics gd, Brush b, System.Drawing.Rectangle r)
        {
            using (Pen p = new Pen(Color.Black, 1))
            {
                gd.DrawLine(p, r.X,r.Y,r.X+r.Width,r.Y);
                gd.DrawLine(p, r.X, r.Y+r.Height, r.X + r.Width, r.Y + r.Height);
                gd.DrawArc(p, r.X-r.Height/2, r.Y , r.Height, r.Height, 90, 180);
                gd.DrawArc(p, r.X+r.Width - r.Height/ 2, r.Y , r.Height, r.Height, 270, 180);

            }

        }
    }

    public class ImageComponent : RoundedRectangle
    {
        private string filepath;

        public ImageComponent(int x, int y, int w, int h, string filepath) : base(x, y, w, h)
        {
            this.filepath = filepath;
        }

        protected override string fetchStyle()
        {
            var style = base.fetchStyle();

            Byte[] bytes = File.ReadAllBytes(this.filepath);
            String imageTxt = Convert.ToBase64String(bytes);
            return $"{style};aspect=fixed;imageAspect=0;image=data:image/png,{imageTxt};";
        }


    }

    public class XYHolder {

        public static int X;
        public static int Y;

    }

  

}

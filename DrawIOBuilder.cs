using System;
using System.Collections;
using System.Collections.Generic;
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
                            ct = new Circle(0, 0, 100);
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
                        text = fromComponent.Substring(fromIdx + 2).Replace("\"","");
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
        private string fillColor = "#ffffff";
        private string strokeColor = "#000000";

        private ArrayList style = new ArrayList();



        private List<DrawIOComponentLink> destinationComponent = new List<DrawIOComponentLink> ();
        public static bool LR = false;
        private readonly int deltaXArrow = 50;
        private readonly int deltaYArrow = 50;

        private bool generated = false;

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
            
            
            return $"whiteSpace=wrap;html=1;aspect=fixed;fillColor={this.fillColor};strokeColor={this.strokeColor};{string.Join(";",this.style.ToArray()).Trim()};";

        }

        internal void To(DrawIOComponent tc,string text)
        {
            destinationComponent.Add(new DrawIOComponentLink(tc, text));
        }
    }

    public class Rectangle : DrawIOComponent
    {
        public Rectangle(int x, int y, int w, int h) : base(x, y, w, h)
        {

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

}

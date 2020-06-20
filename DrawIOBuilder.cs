using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;

namespace MarkdownPlugin
{
    public class DrawIOBuilder
    {
        public void FlowchartBuilder(string[] lines)
        {

            List<DrawIOComponent> component = new List<DrawIOComponent> { };

            Dictionary<string, DrawIOComponent> componentMap = new Dictionary<string, DrawIOComponent>();
            
            //each line should be filtered to type of link or shape
            // link contains a --

            foreach (var line in lines){

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
                    for (var i=0;i<line.Length;i++) {

                        var character = line[i];

                        // if this is a quote then just move forward

                        if (isQuoted && character != '"') {
                            text = text + character;
                        }
                        else
                        if (character == '[' || character == '<' || character == '(')
                        {
                            isKey = false;
                            // this indicates a token
                            if (character == '[')
                            {
                                componentType = "RECTANGLE";
                                ct = new Rectangle(0, 0, 100, 50);
                            }
                            else if (character == '<')
                            {
                                componentType = "DIAMOND";

                            }
                            else if (character == '(' && ((i > 1 && line[i - 1] == '(') || (i + 1 < line.Length && line[i + 1] == ')')))
                            {
                                componentType = "OVAL";
                                ct = new RoundedRectangle(0, 0, 100, 50);
                            }
                            
                            else if (character == '(')
                            {
                                componentType = "CIRCLE";
                                ct = new Circle(0, 0, 50);
                            }



                        }
                        else if (character == ')' || character == ']' || character == '>')
                        {
                            // closing of the structure
                            componentMap.Add(key, ct);

                        }

                        else if (character == '"')
                        {
                            isQuoted = !isQuoted;
                        }
                        else {
                            //
                                if (isKey)
                                {
                                    key = key + character;
                                }
                                else
                                {
                                    text = text + character;
                                }
                                // this is a character
                           
                            //
                        }
                        
                        
                    }

                    Console.WriteLine(text + " : " + componentType + " : " + key);
                    ct.Text(text);
                    component.Add(ct);

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
            Console.WriteLine(component.Count);

            Build(component.ToArray());







        }


        public void Build(params DrawIOComponent[] components)
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

            byte[] v1 = ZippingUtility.ZipStr(htmlDocument);

            var base64Diagram = Convert.ToBase64String(v1);
            Console.WriteLine(xml);

            var drawioText = @"<mxfile host=""Electron"" modified=""2020-06-11T05:50:50.121Z"" agent=""5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) draw.io/12.9.13 Chrome/80.0.3987.163 Electron/8.2.1 Safari/537.36"" etag=""yi-1HzVJONjMD6kCr-6Y"" version=""12.9.13"" type=""device"">
	<diagram id=""gKpNtO0FCgyKS3AscSpZ"" name=""Page-1"">" + base64Diagram + @"</diagram>
</mxfile>";

            ClipboardHelper.CopyToClipboard(htmlDocument, "");
            File.WriteAllText("d:\\tmp\\a.drawio", drawioText);
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



        private List<DrawIOComponentLink> destinationComponent = new List<DrawIOComponentLink> ();
        private readonly bool LR = false;
        private readonly int deltaXArrow = 50;
        private readonly int deltaYArrow = 50;

        public XElement[] Generate()
        {
            List<XElement> list = new List<XElement>();
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
                    dc.Destination.y = this.y + (this.w + deltaXArrow) * count;
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


        public DrawIOComponent Stroke(string color)
        {
            this.strokeColor = color;
            return this;
        }


        protected virtual string fetchStyle()
        {
            return $"whiteSpace=wrap;html=1;aspect=fixed;fillColor={this.fillColor};strokeColor={this.strokeColor};";

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
            return $"{style};rounded=10";

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
            return $"{style};rounded=1";
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

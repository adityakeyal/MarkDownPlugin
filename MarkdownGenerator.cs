using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MarkdownPlugin
{
    public class MarkdownGenerator
    {
        public string GenerateMarkdown(string input) {

            var lines = input.Split('\n');
            return GenerateMarkdown(lines);
        }

        public string GenerateMarkdown(string[] lines) {

            // convert into a table

            var html = @"<table style=""margin: 1.2em 0px;padding: 0px; border-collapse: collapse; border-spacing: 0px; font: inherit; border: 0px;"">";
            // split into rows
            

            var idx = 0;
            foreach (var line in lines)
            {
                if (!line.Contains("|"))
                {
                    continue;
                }

                var newline = line;

                if (idx % 2 == 0)
                {
                    html += @"<tr style=""border-width: 1px 0px 0px; border-right-style: initial; border-bottom-style: initial; border-left-style: initial; border-right-color: initial; border-bottom-color: initial; border-left-color: initial; border-image: initial; border-top-style: solid; border-top-color: rgb(204, 204, 204); background-color: white; margin: 0px; padding: 0px;background-color: rgb(248, 248, 248);"">";
                }
                else
                {
                    html += @"<tr style=""border-width: 1px 0px 0px; border-right-style: initial; border-bottom-style: initial; border-left-style: initial; border-right-color: initial; border-bottom-color: initial; border-left-color: initial; border-image: initial; border-top-style: solid; border-top-color: rgb(204, 204, 204); background-color: white; margin: 0px; padding: 0px;"">";
                }

                if (line.StartsWith("|"))
                {
                    newline = line.Substring(1);
                }


                var columns = newline.Split('|');

                foreach (var col in columns)
                {
                    html += @"<td  style=""font-size: 1em; border: 1px solid rgb(204, 204, 204); margin: 0px; padding: 0.5em 1em; "">" + col + "</td>";
                }
                html += "</tr>";
                idx++;
            }
            html += "</html>";


            return html;
        }


    }
}

namespace MarkdownPlugin
{
    internal class DrawIOComponentLink
    {
        public string Text { get; }
        public DrawIOComponent Destination { get; }

        public DrawIOComponentLink(DrawIOComponent destination, string text)
        {
            this.Text = text;
            this.Destination = destination;
        }
    }
}
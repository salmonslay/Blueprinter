#pragma warning disable CA1416

using System.Drawing;
using System.Globalization;
using TextCopy;

namespace Blueprinter
{
    internal class Program
    {
        /// <summary>
        /// The tolerance for the average color of a node to be considered a comment node.
        /// Alpha values under this will not be included.
        /// </summary>
        private const float ColorTolerance = 0.1f;

        private const int NodeSizeInUnrealUnits = 48;

        [STAThread]
        private static void Main(string[] args)
        {
            Console.Write("Image path: ");
            var path = Console.ReadLine();
            Bitmap bmp = new Bitmap(path);

            Console.Write("Image height in nodes (90 default): "); // width is calculated 
            string? height = Console.ReadLine();
            int imageHeightInNodes = 90;
            if (!string.IsNullOrEmpty(height))
                imageHeightInNodes = int.Parse(height);

            float aspectRatio = bmp.Width / (float) bmp.Height;
            int imageWidthInNodes = (int) (imageHeightInNodes * aspectRatio);

            Bitmap resized = ResizeImageBasedOnNodes(bmp, imageHeightInNodes, imageWidthInNodes);
            int pixelsPerNode = resized.Width / imageWidthInNodes;

            Console.WriteLine($"Image in nodes: {imageWidthInNodes}x{imageHeightInNodes} (total {imageWidthInNodes * imageHeightInNodes})");

            string output = "";
            int predictedNodeCount = imageHeightInNodes * imageWidthInNodes;
            int currentNodes = 0;
            for (int y = 0; y < imageHeightInNodes; y++)
            {
                for (int x = 0; x < imageWidthInNodes; x++)
                {
                    // print progress
                    if (++currentNodes % (predictedNodeCount / 10) == 0)
                        Console.WriteLine($"{currentNodes / (float) predictedNodeCount * 100}%");

                    // make sure we're not out of bounds when we clone (for example, we can't clone 48x48 in a 512x512 image at 500,500)
                    int croppedWidth = Math.Min(pixelsPerNode, resized.Width - x * pixelsPerNode);
                    int croppedHeight = Math.Min(pixelsPerNode, resized.Height - y * pixelsPerNode);

                    Bitmap node = resized.Clone(new Rectangle(x * pixelsPerNode, y * pixelsPerNode, croppedWidth, croppedHeight), resized.PixelFormat);
                    Color averageColor = CalculateAverageColor(node);

                    // skip transparent nodes
                    if (averageColor.A / 255f < ColorTolerance)
                        continue;

                    output += CreateNode(averageColor, x * NodeSizeInUnrealUnits, y * NodeSizeInUnrealUnits,
                        NodeSizeInUnrealUnits, NodeSizeInUnrealUnits);
                }
            }


            ClipboardService.SetText(output);
            Console.WriteLine("Copied to clipboard!");
        }

        private static string CreateNode(Color color, int x, int y, int sizeW, int sizeH)
        {
            return $"\nBegin Object Class=/Script/UnrealEd.EdGraphNode_Comment" +
                   $"\nCommentColor=(R={ColorByteToFloat(color.R)},G={ColorByteToFloat(color.G)},B={ColorByteToFloat(color.B)})" +
                   $"\nNodePosX={x}" +
                   $"\nNodePosY={y}" +
                   $"\nNodeWidth={sizeW}" +
                   $"\nNodeHeight={sizeH}" +
                   $"\nbCommentBubbleVisible=False" +
                   $"\nEnd Object";
        }

        #region helpers

        /// <summary>
        /// Converts a byte to a float between 0 and 1, with a dot as decimal separator.
        /// </summary>
        /// <param name="b">The byte to convert</param>
        /// <returns>The byte clamped to 0-1, with 3 decimal places</returns>
        private static string ColorByteToFloat(byte b)
        {
            string color = (b / 255f).ToString(CultureInfo.InvariantCulture);

            // any data we can save is great, so we only use 3 decimal places
            if (color.Length > 5)
                color = color.Substring(0, 5);

            return color;
        }

        private static Color CalculateAverageColor(Bitmap bmp)
        {
            int r = 0;
            int g = 0;
            int b = 0;
            int a = 0;

            for (int x = 0; x < bmp.Width; x++)
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    Color clr = bmp.GetPixel(x, y);

                    r += clr.R;
                    g += clr.G;
                    b += clr.B;
                    a += clr.A;
                }
            }

            //Calculate the average 
            int pixels = bmp.Width * bmp.Height;
            r /= pixels;
            g /= pixels;
            b /= pixels;
            a /= pixels;

            return Color.FromArgb(a, r, g, b);
        }

        private static Bitmap ResizeImageBasedOnNodes(Image bmp, int imageHeightInNodes, int imageWidthInNodes)
        {
            Size s = bmp.Size;
            if (s.Width < 120) // make sure the image is at least 120x120 to prevent aspect ratio issues
                s *= 3;

            while (s.Width % imageWidthInNodes != 0)
                s.Width++;
            while (s.Height % imageHeightInNodes != 0)
                s.Height++;

            Console.WriteLine($"Resized image from {bmp.Size.Width}x{bmp.Size.Height} to {s.Width}x{s.Height}");
            return new Bitmap(bmp, s);
        }

        #endregion
    }
}
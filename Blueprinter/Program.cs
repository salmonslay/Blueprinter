using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
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

            Console.Write("Image height in nodes (90 default): ");
            string? height = Console.ReadLine();
            int imageHeightInNodes = 90;
            if (!string.IsNullOrEmpty(height))
                imageHeightInNodes = int.Parse(height);

            float aspectRatio = bmp.Width / (float) bmp.Height;
            int imageWidthInNodes = (int) (imageHeightInNodes * aspectRatio);
            int pixelsPerNode = (int) (bmp.Width / (float) imageWidthInNodes);


            Console.WriteLine($"Image in nodes: {imageWidthInNodes}x{imageHeightInNodes} (total {imageWidthInNodes * imageHeightInNodes})");

            string output = "";
            int predictedNodeCount = imageHeightInNodes * imageWidthInNodes;
            int currentNodes = 0;
            for (int y = 0; y < imageHeightInNodes; y++)
            {
                for (int x = 0; x < imageWidthInNodes; x++)
                {
                    // make sure we're not out of bounds when we clone (for example, we can't clone 48x48 in a 512x512 image at 500,500)
                    int croppedWidth = Math.Min(pixelsPerNode, bmp.Width - x * pixelsPerNode);
                    int croppedHeight = Math.Min(pixelsPerNode, bmp.Height - y * pixelsPerNode);

                    Bitmap node = bmp.Clone(new Rectangle(x * pixelsPerNode, y * pixelsPerNode, croppedWidth, croppedHeight), bmp.PixelFormat);
                    Color averageColor = CalculateAverageColor(node);
                    if (averageColor.A < 255 * ColorTolerance)
                        continue;

                    output += CreateNode(averageColor, x * NodeSizeInUnrealUnits, y * NodeSizeInUnrealUnits, NodeSizeInUnrealUnits, NodeSizeInUnrealUnits);
                    currentNodes++;

                    if (currentNodes % (predictedNodeCount / 10) == 0)
                        Console.WriteLine($"{currentNodes / (float) predictedNodeCount * 100}%");
                }
            }


            ClipboardService.SetText(output);
            Console.WriteLine("Copied to clipboard!");
        }

        private static string CreateNode(Color color, int x, int y, int sizeW, int sizeH)
        {
            return $"\nBegin Object Class=/Script/UnrealEd.EdGraphNode_Comment" +
                   $"\nCommentColor=(R={ColorByteToFloat(color.R)},G={ColorByteToFloat(color.G)},B={ColorByteToFloat(color.B)},A=1)" +
                   $"\nbCommentBubbleVisible_InDetailsPanel=False" +
                   $"\nNodePosX={x}" +
                   $"\nNodePosY={y}" +
                   $"\nNodeWidth={sizeW}" +
                   $"\nNodeHeight={sizeH}" +
                   $"\nbCommentBubblePinned=False" +
                   $"\nbCommentBubbleVisible=False" +
                   $"\nEnd Object";
        }

        #region color helpers

        /// <summary>
        /// Converts a byte to a float between 0 and 1, with a dot as decimal separator.
        /// </summary>
        /// <param name="b">The byte to convert</param>
        /// <returns>The byte clamped to 0-1.</returns>
        private static string ColorByteToFloat(byte b)
        {
            return (b / 255f).ToString(CultureInfo.InvariantCulture);
        }

        private static Color CalculateAverageColor(Bitmap bmp)
        {
            //source: https://stackoverflow.com/a/1068399
            int r = 0;
            int g = 0;
            int b = 0;

            int total = 0;

            for (int x = 0; x < bmp.Width; x++)
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    Color clr = bmp.GetPixel(x, y);

                    r += clr.R;
                    g += clr.G;
                    b += clr.B;

                    total++;
                }
            }

            //Calculate average
            r /= total;
            g /= total;
            b /= total;

            return Color.FromArgb(r, g, b);
        }

        #endregion
    }
}
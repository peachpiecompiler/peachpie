using System;
using System.Collections.Generic;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Processors;
using SixLabors.Primitives;

namespace Peachpie.Library.Graphics
{
    internal class FloodFillProcessor<TPixel> : IImageProcessor<TPixel>
                where TPixel : struct, IPixel<TPixel>
    {
        private readonly Point _startPoint;
        private readonly TPixel _fillColor;
        private readonly bool _toBorder;
        private readonly TPixel _borderColor;

        /// <summary>
        /// Creates a processor to perform a flood fill of an image. Either until a border with specified color is reached, or the region with original color.
        /// </summary>
        /// <param name="startPoint">The location where to start the fill from.</param>
        /// <param name="fillColor">Color to be filled with.</param>
        /// <param name="toBorder">False - color the connected region of same color, True - color until border is reached.</param>
        /// <param name="borderColor">Color of the region border, used if toBorder = True.</param>
        public FloodFillProcessor(Point startPoint, TPixel fillColor, bool toBorder, TPixel borderColor)
        {
            _startPoint = startPoint;
            _fillColor = fillColor;
            _toBorder = toBorder;
            _borderColor = borderColor;
        }

        public void Apply(Image<TPixel> image, Rectangle sourceRectangle)
        {
            var floodFrom = image[_startPoint.X, _startPoint.Y];

            // The same color cannot be filled with itself
            if (floodFrom.Equals(_fillColor))
                return;

            var pixelSpan = image.GetPixelSpan();
            int rowLength = image.Width;

            var pointQueue = new Queue<Point>();
            pointQueue.Enqueue(_startPoint);

            while (pointQueue.Count > 0)
            {
                var currentPoint = pointQueue.Dequeue();
                var currentY = currentPoint.Y;
                var currentX = currentPoint.X;

                int leftEdge, rightEdge;
                leftEdge = rightEdge = currentX;

                // Filling until reaching a border of specified color
                if (_toBorder)
                {
                    // Get the row segment to be colored
                    while (rightEdge + 1 < image.Width)
                    {
                        var edgeColor = GetPixel(pixelSpan, rowLength, rightEdge + 1, currentY);
                        if (edgeColor.Equals(_borderColor) || edgeColor.Equals(_fillColor))
                            break;

                        rightEdge++;
                    }
                    while (leftEdge - 1 < image.Width)
                    {
                        var edgeColor = GetPixel(pixelSpan, rowLength, leftEdge - 1, currentY);
                        if (edgeColor.Equals(_borderColor) || edgeColor.Equals(_fillColor))
                            break;

                        leftEdge--;
                    }

                    // Actually color the row
                    for (int workingX = leftEdge; workingX <= rightEdge; workingX++)
                    {
                        SetPixel(pixelSpan, rowLength, workingX, currentY, _fillColor);

                        //Add the pixels above and below to the queue
                        if (currentY > 0)
                        {
                            var aboveColor = GetPixel(pixelSpan, rowLength, workingX, currentY - 1);
                            if (!aboveColor.Equals(_borderColor) && !aboveColor.Equals(_fillColor))
                                pointQueue.Enqueue(new Point(workingX, currentY - 1));
                        }
                        if (currentY + 1 < image.Height)
                        {
                            var belowColor = GetPixel(pixelSpan, rowLength, workingX, currentY + 1);
                            if (!belowColor.Equals(_borderColor) && !belowColor.Equals(_fillColor))
                                pointQueue.Enqueue(new Point(workingX, currentY + 1));
                        }
                    }
                }
                else
                // Filling whole region of same color
                {
                    // Get the row segment to be colored
                    while (rightEdge + 1 < image.Width && GetPixel(pixelSpan, rowLength, rightEdge + 1, currentY).Equals(floodFrom))
                        rightEdge++;
                    while (leftEdge > 0 && GetPixel(pixelSpan, rowLength, leftEdge - 1, currentY).Equals(floodFrom))
                        leftEdge--;

                    // Actually color the row
                    for (int workingX = leftEdge; workingX <= rightEdge; workingX++)
                    {
                        SetPixel(pixelSpan, rowLength, workingX, currentY, _fillColor);

                        //Add the pixels above and below to the queue
                        if (currentY > 0 && GetPixel(pixelSpan, rowLength, workingX, currentY - 1).Equals(floodFrom))
                            pointQueue.Enqueue(new Point(workingX, currentY - 1));
                        if (currentY + 1 < image.Height && GetPixel(pixelSpan, rowLength, workingX, currentY + 1).Equals(floodFrom))
                            pointQueue.Enqueue(new Point(workingX, currentY + 1));
                    }
                }
            }
        }

        private static TPixel GetPixel(Span<TPixel> span, int rowLength, int x, int y) => span[y * rowLength + x];

        private static void SetPixel(Span<TPixel> span, int rowLength, int x, int y, TPixel color) => span[y * rowLength + x] = color;
    }
}

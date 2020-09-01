using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Processors;

namespace Peachpie.Library.Graphics
{
    internal class FloodFillProcessor<TPixel> : ImageProcessor<TPixel>
        where TPixel : unmanaged, IPixel<TPixel>
    {
        private readonly Point _startPoint;
        private readonly TPixel _fillColor;
        private readonly bool _toBorder;
        private readonly TPixel _borderColor;

        public FloodFillProcessor(Configuration configuration, FloodFillProcessor definition, Image<TPixel> source, Rectangle sourceRectangle)
            : base(configuration, source, sourceRectangle)
        {
            _startPoint = definition.StartPoint;
            _fillColor = definition.FillColor.ToPixel<TPixel>();
            _toBorder = definition.ToBorder;
            _borderColor = definition.BorderColor.ToPixel<TPixel>();
        }

        protected override void OnFrameApply(ImageFrame<TPixel> source)
        {
            var floodFrom = source[_startPoint.X, _startPoint.Y];

            // The same color cannot be filled with itself
            if (floodFrom.Equals(_fillColor))
                return;

            //var pixelSpan = source.GetPixelSpan();
            //int rowLength = source.Width;

            var segmentQueue = new Queue<(Point point, int rightEdge)>();
            segmentQueue.Enqueue((_startPoint, _startPoint.X));

            while (segmentQueue.Count > 0)
            {
                var (currentPoint, rightEdge) = segmentQueue.Dequeue();
                var currentY = currentPoint.Y;
                var currentX = currentPoint.X;

                var rowSpan = source.GetPixelRowSpan(currentY);

                int leftEdge;
                leftEdge = currentX;

                // Filling until reaching a border of specified color
                if (_toBorder)
                {
                    // Get the row segment to be colored
                    while (rightEdge + 1 < source.Width)
                    {
                        var edgeColor = GetPixel(rowSpan, rightEdge + 1);
                        if (edgeColor.Equals(_borderColor) || edgeColor.Equals(_fillColor))
                            break;

                        rightEdge++;
                    }
                    while (leftEdge - 1 < source.Width)
                    {
                        var edgeColor = GetPixel(rowSpan, leftEdge - 1);
                        if (edgeColor.Equals(_borderColor) || edgeColor.Equals(_fillColor))
                            break;

                        leftEdge--;
                    }

                    // Actually color the row
                    SetPixelRow(rowSpan, leftEdge, rightEdge, _fillColor);

                    // Add the segments to be filled above and below to the queue
                    if (currentY > 0)
                        AddFillingSegmentsToQueueWithBorder(floodFrom, source.GetPixelRowSpan(currentY - 1), segmentQueue, leftEdge, rightEdge, currentY - 1);
                    if (currentY + 1 < source.Height)
                        AddFillingSegmentsToQueueWithBorder(floodFrom, source.GetPixelRowSpan(currentY + 1), segmentQueue, leftEdge, rightEdge, currentY + 1);
                }
                else
                // Filling whole region of same color
                {
                    // Get the row segment to be colored
                    while (rightEdge + 1 < source.Width && GetPixel(rowSpan, rightEdge + 1).Equals(floodFrom))
                        rightEdge++;
                    while (leftEdge > 0 && GetPixel(rowSpan, leftEdge - 1).Equals(floodFrom))
                        leftEdge--;

                    // Actually color the row
                    SetPixelRow(rowSpan, leftEdge, rightEdge, _fillColor);

                    // Add the segments to be filled above and below to the queue
                    if (currentY > 0)
                        AddFillingSegmentsToQueue(floodFrom, source.GetPixelRowSpan(currentY - 1), segmentQueue, leftEdge, rightEdge, currentY - 1);
                    if (currentY + 1 < source.Height)
                        AddFillingSegmentsToQueue(floodFrom, source.GetPixelRowSpan(currentY + 1), segmentQueue, leftEdge, rightEdge, currentY + 1);
                }
            }
        }

        private static void AddFillingSegmentsToQueue(TPixel floodFrom, Span<TPixel> rowSpan, Queue<(Point, int)> segmentQueue, int xStart, int xEnd, int y)
        {
            int? markStart = null;

            for (int x = xStart; x <= xEnd; x++)
            {
                var color = rowSpan[x];
                if (markStart != null)
                {
                    if (!color.Equals(floodFrom))
                    {
                        segmentQueue.Enqueue((new Point(markStart.Value, y), x - 1));
                        markStart = null;
                    }
                }
                else
                {
                    if (color.Equals(floodFrom))
                    {
                        markStart = x;
                    }
                }
            }

            if (markStart != null)
            {
                segmentQueue.Enqueue((new Point(markStart.Value, y), xEnd));
            }
        }

        private void AddFillingSegmentsToQueueWithBorder(TPixel floodFrom, Span<TPixel> rowSpan, Queue<(Point, int)> segmentQueue, int xStart, int xEnd, int y)
        {
            int? markStart = null;

            for (int x = xStart; x <= xEnd; x++)
            {
                var color = rowSpan[x];
                if (markStart != null)
                {
                    if (color.Equals(_borderColor) || color.Equals(_fillColor))
                    {
                        segmentQueue.Enqueue((new Point(markStart.Value, y), x - 1));
                        markStart = null;
                    }
                }
                else
                {
                    if (!color.Equals(_borderColor) && !color.Equals(_fillColor))
                    {
                        markStart = x;
                    }
                }
            }

            if (markStart != null)
            {
                segmentQueue.Enqueue((new Point(markStart.Value, y), xEnd));
            }
        }

        private static TPixel GetPixel(Span<TPixel> row, int x) => row[x];

        private static void SetPixelRow(Span<TPixel> row, int xFrom, int xTo, TPixel color) => row.Slice(xFrom, xTo - xFrom + 1).Fill(color);
    }
}

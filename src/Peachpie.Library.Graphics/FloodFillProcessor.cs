using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Processors;

namespace Peachpie.Library.Graphics
{
    internal class FloodFillProcessor : IImageProcessor
    {
        /// <summary>
        /// Creates a processor to perform a flood fill of an image. Either until a border with specified color is reached, or the region with original color.
        /// </summary>
        /// <param name="startPoint">The location where to start the fill from.</param>
        /// <param name="fillColor">Color to be filled with.</param>
        /// <param name="toBorder">False - color the connected region of same color, True - color until border is reached.</param>
        /// <param name="borderColor">Color of the region border, used if toBorder = True.</param>
        public FloodFillProcessor(Point startPoint, Color fillColor, bool toBorder, Color borderColor)
        {
            StartPoint = startPoint;
            FillColor = fillColor;
            ToBorder = toBorder;
            BorderColor = borderColor;
        }

        public Point StartPoint { get; }

        public Color FillColor { get; }

        public bool ToBorder { get; }

        public Color BorderColor { get; }

        public IImageProcessor<TPixel> CreatePixelSpecificProcessor<TPixel>(Configuration configuration, Image<TPixel> source, Rectangle sourceRectangle)
            where TPixel : unmanaged, IPixel<TPixel>
        {
            return new FloodFillProcessor<TPixel>(configuration, this, source, sourceRectangle);
        }
    }
}

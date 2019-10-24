using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Processors;
using SixLabors.Primitives;

namespace Peachpie.Library.Graphics
{
    /// <summary>
    /// The base class for all pixel specific image processors.
    /// </summary>
    /// <remarks>
    /// This is a copy of the base class fom the ImageSharp project and can be deleted 
    /// once the ImageSharp RC1 is shipped.
    /// </remarks>
    /// <typeparam name="TPixel">The pixel format.</typeparam>
    internal abstract class ImageProcessor<TPixel> : IImageProcessor<TPixel>
        where TPixel : struct, IPixel<TPixel>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageProcessor{TPixel}"/> class.
        /// </summary>
        /// <param name="source">The source <see cref="Image{TPixel}"/> for the current processor instance.</param>
        /// <param name="sourceRectangle">The source area to process for the current processor instance.</param>
        protected ImageProcessor(Image<TPixel> source, Rectangle sourceRectangle)
        {
            Source = source;
            SourceRectangle = sourceRectangle;
            Configuration = Source.GetConfiguration();
        }

        /// <summary>
        /// Gets The source <see cref="Image{TPixel}"/> for the current processor instance.
        /// </summary>
        protected Image<TPixel> Source { get; }

        /// <summary>
        /// Gets The source area to process for the current processor instance.
        /// </summary>
        protected Rectangle SourceRectangle { get; }

        /// <summary>
        /// Gets the <see cref="Configuration"/> instance to use when performing operations.
        /// </summary>
        protected Configuration Configuration { get; }

        /// <inheritdoc/>
        void IImageProcessor<TPixel>.Apply()
        {
            try
            {
                BeforeImageApply();

                foreach (ImageFrame<TPixel> sourceFrame in Source.Frames)
                {
                    Apply(sourceFrame);
                }

                AfterImageApply();
            }
#if DEBUG
            catch (Exception)
            {
                throw;
#else
            catch (Exception ex)
            {
                throw new ImageProcessingException($"An error occurred when processing the image using {this.GetType().Name}. See the inner exception for more detail.", ex);
#endif
            }
        }

        /// <summary>
        /// Applies the processor to a single image frame.
        /// </summary>
        /// <param name="source">the source image.</param>
        public void Apply(ImageFrame<TPixel> source)
        {
            try
            {
                BeforeFrameApply(source);
                OnFrameApply(source);
                AfterFrameApply(source);
            }
#if DEBUG
            catch (Exception)
            {
                throw;
#else
            catch (Exception ex)
            {
                throw new ImageProcessingException($"An error occurred when processing the image using {this.GetType().Name}. See the inner exception for more detail.", ex);
#endif
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// This method is called before the process is applied to prepare the processor.
        /// </summary>
        protected virtual void BeforeImageApply()
        {
        }

        /// <summary>
        /// This method is called before the process is applied to prepare the processor.
        /// </summary>
        /// <param name="source">The source image. Cannot be null.</param>
        protected virtual void BeforeFrameApply(ImageFrame<TPixel> source)
        {
        }

        /// <summary>
        /// Applies the process to the specified portion of the specified <see cref="ImageFrame{TPixel}" /> at the specified location
        /// and with the specified size.
        /// </summary>
        /// <param name="source">The source image. Cannot be null.</param>
        protected abstract void OnFrameApply(ImageFrame<TPixel> source);

        /// <summary>
        /// This method is called after the process is applied to prepare the processor.
        /// </summary>
        /// <param name="source">The source image. Cannot be null.</param>
        protected virtual void AfterFrameApply(ImageFrame<TPixel> source)
        {
        }

        /// <summary>
        /// This method is called after the process is applied to prepare the processor.
        /// </summary>
        protected virtual void AfterImageApply()
        {
        }

        /// <summary>
        /// Disposes the object and frees resources for the Garbage Collector.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed and unmanaged objects.</param>
        protected virtual void Dispose(bool disposing)
        {
        }
    }
}

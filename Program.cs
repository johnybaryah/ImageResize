using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ImageResize
{
    class Program
    {
        async static Task Main(string[] args)
        {
            var resizer = new ImageResizer(1500, 100L);
            var outputStream = resizer.Resize(@"C:\original_images\app_bzIwMDkyM0VvM215ZURKNjZyM0VZWDlxeW1f_4639613.tiff", ImageFormat.Tiff);
            using (outputStream)
            using (var file = File.Create($@"C:\imageresize\{Guid.NewGuid()}.{ImageFormat.Tiff.ToString()}"))
            {
                outputStream.Seek(0, SeekOrigin.Begin);
                await outputStream.CopyToAsync(file);
            }
        }
    }

    public class ImageResizer
    {
        // assumes defaults
        public ImageResizer(): this(1000, 100L) {}

        public ImageResizer(int size, long quality)
        {
            this.size = size;
            this.quality = quality;    
        }

        private int size;
        private long quality;

        public Stream Resize(string inputPath, ImageFormat format)
        {
            if (format == null)
                throw new ArgumentNullException(nameof(format), "Must specify a format of System.Drawing.Imaging.ImageFormat"); 

            if (string.IsNullOrEmpty(inputPath))
                throw new ArgumentNullException(nameof(inputPath), "input path is null. What do you expect me to resize?");

            using (var image = new Bitmap(System.Drawing.Image.FromFile(inputPath)))
            {
                int width, height = 0;
                
                if (image.Width > image.Height)
                {
                    width = size;
                    height = Convert.ToInt32(image.Height * size / (double)image.Width);
                }
                else
                {
                    width = Convert.ToInt32(image.Width * size / (double)image.Height);
                    height = size;
                }

                return DoResizeAndSave(image, width, height, format);
            }
        }

        private Stream DoResizeAndSave(Bitmap image, int width, int height, ImageFormat format)
        {
            var resized = new Bitmap(width, height);

            using (var graphics = Graphics.FromImage(resized))
            {
                graphics.CompositingQuality = CompositingQuality.AssumeLinear;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.DrawImage(image, 0, 0, width, height);
                
                var qualityParamId = Encoder.Quality;
                var encoderParameters = new EncoderParameters(1);
                encoderParameters.Param[0] = new EncoderParameter(qualityParamId, quality);

                var codec = ImageCodecInfo.GetImageDecoders()?.FirstOrDefault<ImageCodecInfo>(c => c.FormatID == format.Guid);

                if (codec == null)
                    throw new InvalidOperationException($"Failed to load {format.ToString()} codec");

                var output = new MemoryStream();
                resized.Save(output, codec, encoderParameters);
                return output;
            }
        }
    }
}

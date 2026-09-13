using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using SanteDB.Core.Applets.Model;
using SharpCompress.Compressors.ZStandard.Unsafe;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SanteDB.PakMan.Packers
{
    /// <summary>
    /// Packer for image files
    /// </summary>
    public class ImagePacker : IFilePacker
    {

        public const string OPTIMIZATION_GRAY2 = "gray2";
        public const string OPTIMIZATION_GRAY4 = "gray4";
        public const string OPTIMIZATION_2BPP = "2bpp";
        public const string OPTIMIZATION_4BPP = "4bpp";
        public const string OPTIMIZATION_8BPP = "8bpp";

        /// <inhertidoc/>
        public string[] Extensions => new string[] { ".png", ".jpg", ".jpeg" };

        /// <inhertidoc/>
        public string GetMimeType(string file) => MimeMapping.MimeUtility.GetMimeMapping(file);

        /// <inhertidoc/>
        public AppletAsset Process(string file, bool optimize, AppletManifest manifest)
        {
            try
            {
                var mime = MimeMapping.MimeUtility.GetMimeMapping(file);
                byte[] contents = null;

                var optimization = manifest.GetSetting(PakmanConstants.ImageOptimizationMethod);
                if (!String.IsNullOrEmpty(optimization))
                {
                    try
                    {
                        using (var sourceImage = SixLabors.ImageSharp.Image.Load(File.ReadAllBytes(file)))
                        {
                            IImageEncoder encoder = null;
                            switch (mime)
                            {
                                case "image/jpeg":
                                    encoder = this.GetJpgEncoder(optimization);
                                    if (optimization == ImagePacker.OPTIMIZATION_GRAY2 ||
                                       optimization == ImagePacker.OPTIMIZATION_GRAY4)
                                    {
                                        sourceImage.Mutate(x => x.Grayscale());
                                    }
                                    break;
                                case "image/png":
                                    encoder = this.GetPngEncoder(optimization);
                                   
                                    break;
                                default:
                                    throw new InvalidOperationException("Should not be here!");
                            }

                            using (var ms = new MemoryStream())
                            {
                                sourceImage.Save(ms, encoder);
                                contents = ms.ToArray();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("WARN: Could not open {0} - {1}", file, e.ToHumanReadableString());
                        contents = File.ReadAllBytes(file);
                    }
                }
                else
                {
                    contents = File.ReadAllBytes(file);
                }

                if (optimize)
                {
                    return new AppletAsset()
                    {
                        MimeType = mime,
                        Content = PakManTool.CompressContent(contents)
                    };
                }
                else
                {
                    return new AppletAsset()
                    {
                        MimeType = mime,
                        Content = contents
                    };
                }
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Cannot process {file}", e);
            }
        }

        /// <summary>
        /// Get PNG file encoder
        /// </summary>
        private IImageEncoder GetPngEncoder(string optimization)
        {
            var encoder = new PngEncoder();
            encoder.CompressionLevel = PngCompressionLevel.BestCompression;
            encoder.ChunkFilter = PngChunkFilter.ExcludeAll;
            encoder.IgnoreMetadata = true;
            encoder.TransparentColorMode = PngTransparentColorMode.Clear;
            switch (optimization)
            {
                case ImagePacker.OPTIMIZATION_GRAY2:
                    encoder.BitDepth = PngBitDepth.Bit2;
                    encoder.ColorType = PngColorType.Grayscale;
                    break;
                case ImagePacker.OPTIMIZATION_GRAY4:
                    encoder.BitDepth = PngBitDepth.Bit4;
                    encoder.ColorType = PngColorType.Grayscale;
                    break;
                case ImagePacker.OPTIMIZATION_2BPP:
                    encoder.BitDepth = PngBitDepth.Bit2;
                    encoder.ColorType = PngColorType.Palette;
                    break;
                case ImagePacker.OPTIMIZATION_4BPP:
                    encoder.BitDepth = PngBitDepth.Bit4;
                    encoder.ColorType = PngColorType.Palette;
                    break;
                case ImagePacker.OPTIMIZATION_8BPP:
                    encoder.BitDepth = PngBitDepth.Bit8;
                    encoder.ColorType = PngColorType.Palette;
                    break;
            }
            return encoder;
        }

        /// <summary>
        /// Get JPG encoder
        /// </summary>
        private IImageEncoder GetJpgEncoder(string optimization)
        {
            var encoder = new JpegEncoder();
            encoder.Quality = 35;
            return encoder;
        }
    }
}

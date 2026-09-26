using System.IO;
using System.IO.Compression;

namespace BattleBomb.Core.Net
{
    /// <summary>Deflate for payloads that are mostly repeated field names — a save's JSON shrinks ten-fold.
    /// Inflating is bounded, so a hostile payload is refused rather than filling memory.</summary>
    public static class NetDeflate
    {
        public static byte[] Pack(byte[] raw)
        {
            using (var output = new MemoryStream())
            {
                using (var deflate = new DeflateStream(output, CompressionLevel.Fastest, true))
                {
                    deflate.Write(raw, 0, raw.Length);
                }

                return output.ToArray();
            }
        }

        public static byte[] Unpack(byte[] packed, int maxBytes)
        {
            try
            {
                using (var input = new MemoryStream(packed))
                using (var deflate = new DeflateStream(input, CompressionMode.Decompress))
                using (var output = new MemoryStream())
                {
                    var buffer = new byte[8192];
                    int read;
                    while ((read = deflate.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        if (output.Length + read > maxBytes)
                        {
                            throw new NetFormatException($"A payload that inflates past {maxBytes} bytes.");
                        }

                        output.Write(buffer, 0, read);
                    }

                    return output.ToArray();
                }
            }
            catch (InvalidDataException e)
            {
                throw new NetFormatException($"A payload that is not deflate: {e.Message}");
            }
            catch (IOException e)
            {
                // Mono's zlib-backed DeflateStream reports corrupt data as a plain IOException.
                throw new NetFormatException($"A payload that is not deflate: {e.Message}");
            }
        }
    }
}

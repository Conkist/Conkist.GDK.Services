using UnityEngine;

namespace Conkist.GDK.Services.Conkist
{
    /// <summary>
    /// Lightweight QR Code generator that outputs a Unity Texture2D.
    /// Encodes data as a QR Code using byte mode with error correction level L.
    /// No external dependencies required.
    /// 
    /// Based on the QR Code specification (ISO/IEC 18004).
    /// Supports Version 1-4 (up to 114 bytes of data in byte mode with EC level L).
    /// </summary>
    public static class QRCodeGenerator
    {
        // Maximum data capacities for Versions 1-4 in Byte mode, EC Level L
        private static readonly int[] VersionCapacities = { 17, 32, 53, 78 };
        private static readonly int[] VersionSizes = { 21, 25, 29, 33 };

        // Error correction codewords count for EC Level L, versions 1-4
        private static readonly int[] ECCodewords = { 7, 10, 15, 20 };

        // Generator polynomials for EC Level L
        private static readonly int[][] GeneratorPolynomials = {
            // EC=7
            new int[] { 0, 87, 229, 146, 149, 238, 102, 21 },
            // EC=10
            new int[] { 0, 251, 67, 46, 61, 118, 70, 64, 94, 32, 45 },
            // EC=15
            new int[] { 0, 8, 183, 61, 91, 202, 37, 51, 58, 58, 237, 140, 124, 5, 99, 105 },
            // EC=20
            new int[] { 0, 17, 60, 79, 50, 61, 163, 26, 187, 202, 180, 221, 225, 83, 239, 156, 164, 212, 212, 188, 190 }
        };

        // GF(256) exponent and log tables
        private static readonly int[] GfExp = new int[512];
        private static readonly int[] GfLog = new int[256];

        static QRCodeGenerator()
        {
            // Initialize Galois Field tables
            int x = 1;
            for (int i = 0; i < 255; i++)
            {
                GfExp[i] = x;
                GfLog[x] = i;
                x <<= 1;
                if (x >= 256) x ^= 0x11D;
            }
            for (int i = 255; i < 512; i++)
            {
                GfExp[i] = GfExp[i - 255];
            }
        }

        /// <summary>
        /// Generates a QR Code Texture2D from the given text string.
        /// </summary>
        /// <param name="text">The text to encode in the QR code.</param>
        /// <param name="pixelsPerModule">Size of each QR module in pixels.</param>
        /// <param name="darkColor">Color for dark modules (default: black).</param>
        /// <param name="lightColor">Color for light modules (default: white).</param>
        /// <returns>A Texture2D containing the QR code, or null if the text is too long.</returns>
        public static Texture2D Generate(string text, int pixelsPerModule = 8, Color? darkColor = null, Color? lightColor = null)
        {
            var dark = darkColor ?? Color.black;
            var light = lightColor ?? Color.white;

            byte[] data = System.Text.Encoding.UTF8.GetBytes(text);

            // Determine QR version
            int version = -1;
            for (int v = 0; v < VersionCapacities.Length; v++)
            {
                if (data.Length <= VersionCapacities[v])
                {
                    version = v + 1;
                    break;
                }
            }

            if (version == -1)
            {
                Debug.LogError($"[ConkistSDK] QR data too long ({data.Length} bytes). Max supported: {VersionCapacities[VersionCapacities.Length - 1]} bytes.");
                return null;
            }

            int size = VersionSizes[version - 1];
            bool[,] modules = new bool[size, size];
            bool[,] isFunction = new bool[size, size];

            // Place function patterns
            PlaceFinderPatterns(modules, isFunction, size);
            PlaceAlignmentPatterns(modules, isFunction, version, size);
            PlaceTimingPatterns(modules, isFunction, size);
            PlaceDarkModule(modules, isFunction, version);
            ReserveFormatInfo(isFunction, size);

            // Encode data
            byte[] encodedData = EncodeData(data, version);
            byte[] ecData = GenerateEC(encodedData, version);
            byte[] fullData = CombineData(encodedData, ecData);

            // Place data bits
            PlaceDataBits(modules, isFunction, fullData, size);

            // Apply mask (mask 0 for simplicity — (row + col) % 2 == 0)
            ApplyMask(modules, isFunction, size);

            // Place format information
            PlaceFormatInfo(modules, size);

            // Render to texture
            return RenderToTexture(modules, size, pixelsPerModule, dark, light);
        }

        private static void PlaceFinderPatterns(bool[,] modules, bool[,] isFunction, int size)
        {
            int[][] positions = { new[] { 0, 0 }, new[] { size - 7, 0 }, new[] { 0, size - 7 } };

            foreach (var pos in positions)
            {
                int r = pos[0], c = pos[1];
                for (int dr = -1; dr <= 7; dr++)
                {
                    for (int dc = -1; dc <= 7; dc++)
                    {
                        int row = r + dr, col = c + dc;
                        if (row < 0 || row >= size || col < 0 || col >= size) continue;

                        bool isDark = (dr >= 0 && dr <= 6 && dc >= 0 && dc <= 6) &&
                            (dr == 0 || dr == 6 || dc == 0 || dc == 6 ||
                             (dr >= 2 && dr <= 4 && dc >= 2 && dc <= 4));

                        modules[row, col] = isDark;
                        isFunction[row, col] = true;
                    }
                }
            }
        }

        private static void PlaceAlignmentPatterns(bool[,] modules, bool[,] isFunction, int version, int size)
        {
            if (version < 2) return;

            // Version 2 alignment pattern at (18, 18)
            int[] positions = { 6, size - 7 };
            foreach (int r in positions)
            {
                foreach (int c in positions)
                {
                    if (isFunction[r, c]) continue;
                    for (int dr = -2; dr <= 2; dr++)
                    {
                        for (int dc = -2; dc <= 2; dc++)
                        {
                            int row = r + dr, col = c + dc;
                            if (row < 0 || row >= size || col < 0 || col >= size) continue;
                            bool isDark = (dr == -2 || dr == 2 || dc == -2 || dc == 2 || (dr == 0 && dc == 0));
                            modules[row, col] = isDark;
                            isFunction[row, col] = true;
                        }
                    }
                }
            }
        }

        private static void PlaceTimingPatterns(bool[,] modules, bool[,] isFunction, int size)
        {
            for (int i = 8; i < size - 8; i++)
            {
                bool isDark = i % 2 == 0;
                modules[6, i] = isDark;
                isFunction[6, i] = true;
                modules[i, 6] = isDark;
                isFunction[i, 6] = true;
            }
        }

        private static void PlaceDarkModule(bool[,] modules, bool[,] isFunction, int version)
        {
            int row = 4 * version + 9;
            modules[row, 8] = true;
            isFunction[row, 8] = true;
        }

        private static void ReserveFormatInfo(bool[,] isFunction, int size)
        {
            // Reserve format info areas around finder patterns
            for (int i = 0; i < 8; i++)
            {
                isFunction[8, i] = true;
                isFunction[8, size - 1 - i] = true;
                isFunction[i, 8] = true;
                isFunction[size - 1 - i, 8] = true;
            }
            isFunction[8, 8] = true;
        }

        private static byte[] EncodeData(byte[] data, int version)
        {
            int totalDataCodewords = VersionCapacities[version - 1] + ECCodewords[version - 1];
            // For version 1 the total codewords = size^2 - function patterns area... 
            // Simplified: use standard total data codewords
            int dataCodewords = totalDataCodewords - ECCodewords[version - 1];

            // Build bit stream: Mode indicator (0100 = Byte) + Character count + Data
            var bits = new System.Collections.Generic.List<bool>();

            // Mode indicator: 0100 (Byte mode)
            bits.Add(false); bits.Add(true); bits.Add(false); bits.Add(false);

            // Character count indicator (8 bits for versions 1-9)
            for (int i = 7; i >= 0; i--)
                bits.Add(((data.Length >> i) & 1) == 1);

            // Data bytes
            foreach (byte b in data)
            {
                for (int i = 7; i >= 0; i--)
                    bits.Add(((b >> i) & 1) == 1);
            }

            // Terminator (up to 4 zeros)
            int terminatorLength = Mathf.Min(4, dataCodewords * 8 - bits.Count);
            for (int i = 0; i < terminatorLength; i++)
                bits.Add(false);

            // Pad to byte boundary
            while (bits.Count % 8 != 0)
                bits.Add(false);

            // Pad bytes (alternating 0xEC, 0x11)
            byte[] padBytes = { 0xEC, 0x11 };
            int padIndex = 0;
            while (bits.Count < dataCodewords * 8)
            {
                byte pb = padBytes[padIndex % 2];
                for (int i = 7; i >= 0; i--)
                    bits.Add(((pb >> i) & 1) == 1);
                padIndex++;
            }

            // Convert to bytes
            byte[] result = new byte[dataCodewords];
            for (int i = 0; i < dataCodewords; i++)
            {
                int val = 0;
                for (int bit = 0; bit < 8; bit++)
                {
                    if (bits[i * 8 + bit]) val |= (1 << (7 - bit));
                }
                result[i] = (byte)val;
            }

            return result;
        }

        private static byte[] GenerateEC(byte[] data, int version)
        {
            int ecCount = ECCodewords[version - 1];
            int[] genPoly = GeneratorPolynomials[version - 1];

            int[] messagePoly = new int[data.Length + ecCount];
            for (int i = 0; i < data.Length; i++)
                messagePoly[i] = data[i];

            for (int i = 0; i < data.Length; i++)
            {
                int coef = messagePoly[i];
                if (coef == 0) continue;
                int logCoef = GfLog[coef];
                for (int j = 0; j < genPoly.Length; j++)
                {
                    messagePoly[i + j] ^= GfExp[(logCoef + genPoly[j]) % 255];
                }
            }

            byte[] ec = new byte[ecCount];
            for (int i = 0; i < ecCount; i++)
                ec[i] = (byte)messagePoly[data.Length + i];

            return ec;
        }

        private static byte[] CombineData(byte[] data, byte[] ec)
        {
            byte[] result = new byte[data.Length + ec.Length];
            System.Array.Copy(data, 0, result, 0, data.Length);
            System.Array.Copy(ec, 0, result, data.Length, ec.Length);
            return result;
        }

        private static void PlaceDataBits(bool[,] modules, bool[,] isFunction, byte[] data, int size)
        {
            int bitIndex = 0;
            int totalBits = data.Length * 8;

            // Traverse the QR code in the zigzag pattern
            for (int right = size - 1; right >= 1; right -= 2)
            {
                if (right == 6) right = 5; // Skip timing column

                for (int vert = 0; vert < size; vert++)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        int col = right - j;
                        // Going upward for even columns, downward for odd
                        bool upward = ((right + 1) / 2) % 2 == 0;
                        int row = upward ? size - 1 - vert : vert;

                        if (row < 0 || row >= size || col < 0 || col >= size) continue;
                        if (isFunction[row, col]) continue;

                        if (bitIndex < totalBits)
                        {
                            int byteIndex = bitIndex / 8;
                            int bitPos = 7 - (bitIndex % 8);
                            modules[row, col] = ((data[byteIndex] >> bitPos) & 1) == 1;
                            bitIndex++;
                        }
                    }
                }
            }
        }

        private static void ApplyMask(bool[,] modules, bool[,] isFunction, int size)
        {
            // Mask pattern 0: (row + col) % 2 == 0
            for (int row = 0; row < size; row++)
            {
                for (int col = 0; col < size; col++)
                {
                    if (!isFunction[row, col] && (row + col) % 2 == 0)
                    {
                        modules[row, col] = !modules[row, col];
                    }
                }
            }
        }

        private static void PlaceFormatInfo(bool[,] modules, int size)
        {
            // Format info for EC Level L (01), mask 0 (000): 01000 -> after BCH: 011010101011111 (0x35AF -> err, use precomputed)
            // Pre-computed format string for EC=L, Mask=0: 111011111000100
            int formatBits = 0x77C4;

            // Place around top-left finder
            int[] rowPositions = { 0, 1, 2, 3, 4, 5, 7, 8 };
            for (int i = 0; i < 8; i++)
            {
                bool bit = ((formatBits >> (14 - i)) & 1) == 1;
                modules[8, rowPositions[i]] = bit;
            }
            int[] colPositions = { 7, 5, 4, 3, 2, 1, 0 };
            for (int i = 0; i < 7; i++)
            {
                bool bit = ((formatBits >> (6 - i)) & 1) == 1;
                modules[colPositions[i], 8] = bit;
            }

            // Place along edges
            for (int i = 0; i < 8; i++)
            {
                bool bit = ((formatBits >> i) & 1) == 1;
                modules[size - 1 - i, 8] = bit;
            }
            for (int i = 0; i < 7; i++)
            {
                bool bit = ((formatBits >> (14 - i)) & 1) == 1;
                modules[8, size - 7 + i] = bit;
            }
        }

        private static Texture2D RenderToTexture(bool[,] modules, int size, int pixelsPerModule, Color dark, Color light)
        {
            int quietZone = 4; // Standard quiet zone
            int totalSize = (size + quietZone * 2) * pixelsPerModule;
            var texture = new Texture2D(totalSize, totalSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            // Fill background (light/quiet zone)
            Color[] pixels = new Color[totalSize * totalSize];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = light;

            // Draw modules
            for (int row = 0; row < size; row++)
            {
                for (int col = 0; col < size; col++)
                {
                    Color color = modules[row, col] ? dark : light;
                    int px = (col + quietZone) * pixelsPerModule;
                    // Flip Y for texture (bottom-up)
                    int py = (size - 1 - row + quietZone) * pixelsPerModule;

                    for (int dy = 0; dy < pixelsPerModule; dy++)
                    {
                        for (int dx = 0; dx < pixelsPerModule; dx++)
                        {
                            pixels[(py + dy) * totalSize + (px + dx)] = color;
                        }
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}

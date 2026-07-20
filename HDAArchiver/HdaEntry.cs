namespace HDAArchiver
{
    /// <summary>
    /// Represents one file entry shown
    /// in the ListView inside the archiver.
    /// </summary>
    public class HdaEntry
    {
        // Slot index inside the HDA table
        public int SlotIndex { get; set; }

        // Detected filename
        // e.g. BOY_00000.rdtb
        public string FileName { get; set; }

        // Detected extension
        // e.g. .rdtb
        public string Extension { get; set; }

        // Uncompressed size in bytes
        public long DecompressedSize { get; set; }

        // Stored size in bytes
        // (compressed or same as decompressed)
        public long StoredSize { get; set; }

        // True if this slot is compressed
        public bool IsCompressed { get; set; }

        // True if this slot is empty gap
        public bool IsEmpty { get; set; }

        // Raw decompressed bytes
        // (loaded on demand / after extraction)
        public byte[] Data { get; set; }

        // Compression ratio string for display
        public string RatioDisplay
        {
            get
            {
                if (!IsCompressed ||
                    DecompressedSize == 0)
                    return "—";

                double ratio =
                    (double)StoredSize
                    / DecompressedSize * 100.0;

                return ratio.ToString("F1") + "%";
            }
        }

        // Human-readable size
        public string SizeDisplay =>
            IsEmpty
                ? "—"
                : FormatSize(DecompressedSize);

        public string StoredDisplay =>
            IsEmpty
                ? "—"
                : FormatSize(StoredSize);

        private static string FormatSize(long bytes)
        {
            if (bytes >= 1024 * 1024)
                return string.Format(
                    "{0:F2} MB",
                    bytes / (1024.0 * 1024.0));

            if (bytes >= 1024)
                return string.Format(
                    "{0:F1} KB",
                    bytes / 1024.0);

            return bytes + " B";
        }
    }
}
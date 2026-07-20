namespace HDAArchiver
{
    public class HdaEntry
    {
        public int SlotIndex { get; set; }
        public string FileName { get; set; }
        public string Extension { get; set; }
        public long DecompressedSize { get; set; }
        public long StoredSize { get; set; }
        public bool IsCompressed { get; set; }
        public bool IsEmpty { get; set; }

        // ── True if this entry is a slot
        //    layout manifest .bin file.
        //    Written automatically by
        //    HarvestDataArchive.Unpack()
        //    when the HDA has gap slots.
        //    Detected by "SLOTS=" content.
        //    Never compressed. Never renamed.
        //    Must stay as last entry so
        //    Pack() finds it correctly.
        public bool IsManifest { get; set; }

        public byte[] Data { get; set; }

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

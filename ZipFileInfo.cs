namespace ZipInfoShell
{
    public class ZipFileInfo
    {
        public uint FileCount { get; set; }

        public int FirstLevelFileCount { get; set; }

        public int FirstLevelDirectoryCount { get; set; }

        public bool Unzip2NewFolder
        {
            get
            {
                if (FirstLevelDirectoryCount == 1 && FirstLevelFileCount == 0)
                {
                    return false;
                }
                return true;
            }
        }

        public override string ToString()
        {
            return $"总文件数: {FileCount} (首层文件数: {FirstLevelFileCount},目录数: {FirstLevelDirectoryCount})";
        }

    }
}

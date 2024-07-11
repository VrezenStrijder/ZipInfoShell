using CSShellExtContextMenuHandler;
using SevenZip;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using ZipInfoShell.Properties;
using IDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;


namespace ZipInfoShell
{
    [ClassInterface(ClassInterfaceType.None)]
    [Guid("E07DD184-C1C7-4315-90C0-9A8A6582E671"), ComVisible(true)]
    public class FileContextMenuExt : IShellExtInit, IContextMenu
    {
        private string selectedFile;

        private string menuText = "&压缩文件信息";
        private IntPtr menuBmp = IntPtr.Zero;
        private string verb = "zipFileInfo";
        private string verbCanonicalName = "GetZipFileInfo";
        private string verbHelpText = "ExtractZipFileInfo";
        private uint IDM_DISPLAY = 0;


        public FileContextMenuExt()
        {
            // Load the bitmap for the menu item.
            Bitmap bmp = Resources.zip;
            bmp.MakeTransparent(bmp.GetPixel(0, 0));
            this.menuBmp = bmp.GetHbitmap();
        }

        ~FileContextMenuExt()
        {
            if (this.menuBmp != IntPtr.Zero)
            {
                NativeMethods.DeleteObject(this.menuBmp);
                this.menuBmp = IntPtr.Zero;
            }
        }


        void Unzip2NewFolder(IntPtr hWnd)
        {
            SetLibraryPath();

            string zipFilePath = this.selectedFile; 
            FileInfo fi= new FileInfo(zipFilePath);
            if (!fi.Exists)
            {
                return;
            }

            string pureFileName = Path.GetFileNameWithoutExtension(zipFilePath);
            string extractFolder = Path.Combine(fi.Directory.FullName,pureFileName);
            if (!Directory.Exists(extractFolder))
            {
                Directory.CreateDirectory(extractFolder);
            }

            using (var extractor = new SevenZipExtractor(zipFilePath))
            {
                extractor.ExtractArchive(extractFolder);
            }

        }

        void UnzipContent(IntPtr hWnd)
        {
            SetLibraryPath();

            string zipFilePath = this.selectedFile;
            FileInfo fi = new FileInfo(zipFilePath);
            if (!fi.Exists)
            {
                return;
            }

            string extractFolder = fi.Directory.FullName;
            using (var extractor = new SevenZipExtractor(zipFilePath))
            {
                extractor.ExtractArchive(extractFolder);
            }
        }

        private void SetLibraryPath()
        {
            //var filePath = @"E:\zyx\projects\ToolProject\ZipInfoShell\bin\Debug";
            var filePath = Environment.CurrentDirectory;
            SevenZipBase.SetLibraryPath(Path.Combine(filePath, "7z.dll"));
        }

        private ZipFileInfo SummaryZipInfo(string fileName)
        {
            var zipInfo = new ZipFileInfo();

            SetLibraryPath();
            using (var entries = new SevenZipExtractor(fileName))
            {
                zipInfo.FileCount = entries.FilesCount;

                var firstLevelEntries = entries.ArchiveFileData
                    .Where(entry => !entry.FileName.Contains(Path.DirectorySeparatorChar))
                    .ToList();

                zipInfo.FirstLevelFileCount = firstLevelEntries.Count(entry => !entry.IsDirectory);
                zipInfo.FirstLevelDirectoryCount = firstLevelEntries.Count(entry => entry.IsDirectory);

            }
            return zipInfo;
        }


        /// <summary>
        /// 关联的文件类型
        /// </summary>

        [ComRegisterFunction()]
        public static void Register(Type t)
        {
            try
            {
                ShellExtReg.RegisterShellExtContextMenuHandler(t.GUID, ".zip", "ZipInfoShell.FileContextMenuExt Class");
                ShellExtReg.RegisterShellExtContextMenuHandler(t.GUID, ".7z", "ZipInfoShell.FileContextMenuExt Class");
                ShellExtReg.RegisterShellExtContextMenuHandler(t.GUID, ".rar", "ZipInfoShell.FileContextMenuExt Class");
                ShellExtReg.RegisterShellExtContextMenuHandler(t.GUID, ".gz", "ZipInfoShell.FileContextMenuExt Class");
                ShellExtReg.RegisterShellExtContextMenuHandler(t.GUID, ".tar", "ZipInfoShell.FileContextMenuExt Class");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message); // Log the error
                throw;  // Re-throw the exception
            }
        }

        [ComUnregisterFunction()]
        public static void Unregister(Type t)
        {
            try
            {
                ShellExtReg.UnregisterShellExtContextMenuHandler(t.GUID, ".zip");
                ShellExtReg.UnregisterShellExtContextMenuHandler(t.GUID, ".7z");
                ShellExtReg.UnregisterShellExtContextMenuHandler(t.GUID, ".rar");
                ShellExtReg.UnregisterShellExtContextMenuHandler(t.GUID, ".gz");
                ShellExtReg.UnregisterShellExtContextMenuHandler(t.GUID, ".tar");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message); // Log the error
                throw;  // Re-throw the exception
            }
        }


        /// <summary>
        /// Initialize the context menu handler.
        /// </summary>
        /// <param name="pidlFolder">
        /// A pointer to an ITEMIDLIST structure that uniquely identifies a folder.
        /// </param>
        /// <param name="pDataObj">
        /// A pointer to an IDataObject interface object that can be used to retrieve 
        /// the objects being acted upon.
        /// </param>
        /// <param name="hKeyProgID">
        /// The registry key for the file object or folder type.
        /// </param>
        public void Initialize(IntPtr pidlFolder, IntPtr pDataObj, IntPtr hKeyProgID)
        {
            if (pDataObj == IntPtr.Zero)
            {
                throw new ArgumentException();
            }

            FORMATETC fe = new FORMATETC();
            fe.cfFormat = (short)CLIPFORMAT.CF_HDROP;
            fe.ptd = IntPtr.Zero;
            fe.dwAspect = DVASPECT.DVASPECT_CONTENT;
            fe.lindex = -1;
            fe.tymed = TYMED.TYMED_HGLOBAL;
            STGMEDIUM stm = new STGMEDIUM();

            // The pDataObj pointer contains the objects being acted upon. In this 
            // example, we get an HDROP handle for enumerating the selected files 
            // and folders.
            IDataObject dataObject = (IDataObject)Marshal.GetObjectForIUnknown(pDataObj);
            dataObject.GetData(ref fe, out stm);

            try
            {
                // Get an HDROP handle.
                IntPtr hDrop = stm.unionmember;
                if (hDrop == IntPtr.Zero)
                {
                    throw new ArgumentException();
                }

                // Determine how many files are involved in this operation.
                uint nFiles = NativeMethods.DragQueryFile(hDrop, UInt32.MaxValue, null, 0);

                // This code sample displays the custom context menu item when only 
                // one file is selected. 
                if (nFiles == 1)
                {
                    // Get the path of the file.
                    StringBuilder fileName = new StringBuilder(260);
                    if (0 == NativeMethods.DragQueryFile(hDrop, 0, fileName, fileName.Capacity))
                    {
                        Marshal.ThrowExceptionForHR(WinError.E_FAIL);
                    }
                    this.selectedFile = fileName.ToString();
                }
                else
                {
                    Marshal.ThrowExceptionForHR(WinError.E_FAIL);
                }

                // [-or-]

                // Enumerate the selected files and folders.
                //if (nFiles > 0)
                //{
                //    StringCollection selectedFiles = new StringCollection();
                //    StringBuilder fileName = new StringBuilder(260);
                //    for (uint i = 0; i < nFiles; i++)
                //    {
                //        // Get the next file name.
                //        if (0 != NativeMethods.DragQueryFile(hDrop, i, fileName,
                //            fileName.Capacity))
                //        {
                //            // Add the file name to the list.
                //            selectedFiles.Add(fileName.ToString());
                //        }
                //    }
                //
                //    // If we did not find any files we can work with, throw 
                //    // exception.
                //    if (selectedFiles.Count == 0)
                //    {
                //        Marshal.ThrowExceptionForHR(WinError.E_FAIL);
                //    }
                //}
                //else
                //{
                //    Marshal.ThrowExceptionForHR(WinError.E_FAIL);
                //}
            }
            finally
            {
                NativeMethods.ReleaseStgMedium(ref stm);
            }
        }


        /// <summary>
        /// Add commands to a shortcut menu.
        /// </summary>
        /// <param name="hMenu">A handle to the shortcut menu.</param>
        /// <param name="iMenu">
        /// The zero-based position at which to insert the first new menu item.
        /// </param>
        /// <param name="idCmdFirst">
        /// The minimum value that the handler can specify for a menu item ID.
        /// </param>
        /// <param name="idCmdLast">
        /// The maximum value that the handler can specify for a menu item ID.
        /// </param>
        /// <param name="uFlags">
        /// Optional flags that specify how the shortcut menu can be changed.
        /// </param>
        /// <returns>
        /// If successful, returns an HRESULT value that has its severity value set 
        /// to SEVERITY_SUCCESS and its code value set to the offset of the largest 
        /// command identifier that was assigned, plus one.
        /// </returns>
        public int QueryContextMenu(IntPtr hMenu, uint iMenu, uint idCmdFirst, uint idCmdLast, uint uFlags)
        {
            // If uFlags include CMF_DEFAULTONLY then we should not do anything.
            if (((uint)CMF.CMF_DEFAULTONLY & uFlags) != 0)
            {
                return WinError.MAKE_HRESULT(WinError.SEVERITY_SUCCESS, 0, 0);
            }

            var zipInfo = SummaryZipInfo(this.selectedFile);
            menuText = $"&{zipInfo.ToString()}";
            string subMenu1Text = zipInfo.Unzip2NewFolder ? $"解压至新目录(推荐)" : "解压至新目录";
            string subMenu2Text = zipInfo.Unzip2NewFolder ? $"解压至当前目录" : "解压至当前目录(推荐)";

            //string subMenu1Text = "test1";
            //string subMenu2Text = "test2";


            IntPtr hSubMenu = NativeMethods.CreatePopupMenu();

            // 向子菜单添加项
            uint idCmd = IDM_DISPLAY;
            NativeMethods.InsertMenu(hSubMenu, (uint)0, (uint)MF.MF_BYPOSITION | (uint)MFT.MFT_STRING, idCmdFirst + (idCmd++), subMenu1Text);
            NativeMethods.InsertMenu(hSubMenu, (uint)1, (uint)MF.MF_BYPOSITION | (uint)MFT.MFT_STRING, idCmdFirst + (idCmd++), subMenu2Text);

            // Use either InsertMenu or InsertMenuItem to add menu items.
            MENUITEMINFO mii = new MENUITEMINFO();
            mii.cbSize = (uint)Marshal.SizeOf(mii);
            mii.fMask = MIIM.MIIM_BITMAP | MIIM.MIIM_STRING | MIIM.MIIM_FTYPE | MIIM.MIIM_SUBMENU | MIIM.MIIM_ID | MIIM.MIIM_STATE;
            mii.wID = idCmdFirst + IDM_DISPLAY;
            mii.fType = MFT.MFT_STRING;
            mii.hSubMenu = hSubMenu;
            mii.dwTypeData = this.menuText;
            mii.fState = MFS.MFS_ENABLED;
            mii.hbmpItem = this.menuBmp;
            if (!NativeMethods.InsertMenuItem(hMenu, iMenu, true, ref mii))
            {
                return Marshal.GetHRForLastWin32Error();
            }

            // Add a separator.
            MENUITEMINFO sep = new MENUITEMINFO();
            sep.cbSize = (uint)Marshal.SizeOf(sep);
            sep.fMask = MIIM.MIIM_TYPE;
            sep.fType = MFT.MFT_SEPARATOR;
            if (!NativeMethods.InsertMenuItem(hMenu, iMenu + 1, true, ref sep))
            {
                return Marshal.GetHRForLastWin32Error();
            }

            // Return an HRESULT value with the severity set to SEVERITY_SUCCESS. 
            // Set the code value to the offset of the largest command identifier 
            // that was assigned, plus one (1).
            return WinError.MAKE_HRESULT(WinError.SEVERITY_SUCCESS, 0, idCmd);
        }

        /// <summary>
        /// Carry out the command associated with a shortcut menu item.
        /// </summary>
        /// <param name="pici">
        /// A pointer to a CMINVOKECOMMANDINFO or CMINVOKECOMMANDINFOEX structure 
        /// containing information about the command. 
        /// </param>
        public void InvokeCommand(IntPtr pici)
        {
            bool isUnicode = false;

            // Determine which structure is being passed in, CMINVOKECOMMANDINFO or 
            // CMINVOKECOMMANDINFOEX based on the cbSize member of lpcmi. Although 
            // the lpcmi parameter is declared in Shlobj.h as a CMINVOKECOMMANDINFO 
            // structure, in practice it often points to a CMINVOKECOMMANDINFOEX 
            // structure. This struct is an extended version of CMINVOKECOMMANDINFO 
            // and has additional members that allow Unicode strings to be passed.
            CMINVOKECOMMANDINFO ici = (CMINVOKECOMMANDINFO)Marshal.PtrToStructure(pici, typeof(CMINVOKECOMMANDINFO));
            CMINVOKECOMMANDINFOEX iciex = new CMINVOKECOMMANDINFOEX();
            if (ici.cbSize == Marshal.SizeOf(typeof(CMINVOKECOMMANDINFOEX)))
            {
                if ((ici.fMask & CMIC.CMIC_MASK_UNICODE) != 0)
                {
                    isUnicode = true;
                    iciex = (CMINVOKECOMMANDINFOEX)Marshal.PtrToStructure(pici, typeof(CMINVOKECOMMANDINFOEX));
                }
            }

            // Determines whether the command is identified by its offset or verb.
            // There are two ways to identify commands:
            // 
            //   1) The command's verb string 
            //   2) The command's identifier offset
            // 
            // If the high-order word of lpcmi->lpVerb (for the ANSI case) or 
            // lpcmi->lpVerbW (for the Unicode case) is nonzero, lpVerb or lpVerbW 
            // holds a verb string. If the high-order word is zero, the command 
            // offset is in the low-order word of lpcmi->lpVerb.

            // For the ANSI case, if the high-order word is not zero, the command's 
            // verb string is in lpcmi->lpVerb. 
            if (!isUnicode && NativeMethods.HighWord(ici.verb.ToInt32()) != 0)
            {
                // Is the verb supported by this context menu extension?
                if (Marshal.PtrToStringAnsi(ici.verb) == this.verb)
                {

                }
                else
                {
                    // If the verb is not recognized by the context menu handler, it 
                    // must return E_FAIL to allow it to be passed on to the other 
                    // context menu handlers that might implement that verb.
                    Marshal.ThrowExceptionForHR(WinError.E_FAIL);
                }
            }

            // For the Unicode case, if the high-order word is not zero, the 
            // command's verb string is in lpcmi->lpVerbW. 
            else if (isUnicode && NativeMethods.HighWord(iciex.verbW.ToInt32()) != 0)
            {
                // Is the verb supported by this context menu extension?
                if (Marshal.PtrToStringUni(iciex.verbW) == this.verb)
                {

                }
                else
                {
                    // If the verb is not recognized by the context menu handler, it 
                    // must return E_FAIL to allow it to be passed on to the other 
                    // context menu handlers that might implement that verb.
                    Marshal.ThrowExceptionForHR(WinError.E_FAIL);
                }
            }

            // If the command cannot be identified through the verb string, then 
            // check the identifier offset.
            else
            {
                // Is the command identifier offset supported by this context menu 
                // extension?
                if (NativeMethods.LowWord(ici.verb.ToInt32()) == IDM_DISPLAY)
                {
                    Unzip2NewFolder(ici.hwnd);
                }
                else if (NativeMethods.LowWord(ici.verb.ToInt32()) == (IDM_DISPLAY + 1))
                {
                    UnzipContent(ici.hwnd);
                }
                else
                {
                    // If the verb is not recognized by the context menu handler, it 
                    // must return E_FAIL to allow it to be passed on to the other 
                    // context menu handlers that might implement that verb.
                    Marshal.ThrowExceptionForHR(WinError.E_FAIL);
                }
            }
        }

        /// <summary>
        /// Get information about a shortcut menu command, including the help string 
        /// and the language-independent, or canonical, name for the command.
        /// </summary>
        /// <param name="idCmd">Menu command identifier offset.</param>
        /// <param name="uFlags">
        /// Flags specifying the information to return. This parameter can have one 
        /// of the following values: GCS_HELPTEXTA, GCS_HELPTEXTW, GCS_VALIDATEA, 
        /// GCS_VALIDATEW, GCS_VERBA, GCS_VERBW.
        /// </param>
        /// <param name="pReserved">Reserved. Must be IntPtr.Zero</param>
        /// <param name="pszName">
        /// The address of the buffer to receive the null-terminated string being 
        /// retrieved.
        /// </param>
        /// <param name="cchMax">
        /// Size of the buffer, in characters, to receive the null-terminated string.
        /// </param>
        public void GetCommandString(UIntPtr idCmd, uint uFlags, IntPtr pReserved, StringBuilder pszName, uint cchMax)
        {
            if (idCmd.ToUInt32() == IDM_DISPLAY)
            {
                switch ((GCS)uFlags)
                {
                    case GCS.GCS_VERBW:
                        if (this.verbCanonicalName.Length > cchMax - 1)
                        {
                            Marshal.ThrowExceptionForHR(WinError.STRSAFE_E_INSUFFICIENT_BUFFER);
                        }
                        else
                        {
                            pszName.Clear();
                            pszName.Append(this.verbCanonicalName);
                        }
                        break;

                    case GCS.GCS_HELPTEXTW:
                        if (this.verbHelpText.Length > cchMax - 1)
                        {
                            Marshal.ThrowExceptionForHR(WinError.STRSAFE_E_INSUFFICIENT_BUFFER);
                        }
                        else
                        {
                            pszName.Clear();
                            pszName.Append(this.verbHelpText);
                        }
                        break;
                }
            }
        }


    }

}

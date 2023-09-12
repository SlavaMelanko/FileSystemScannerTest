using System.Collections.Generic;
using System.IO;

namespace Ashampoo
{
    public class VolumeProvider
    {
        public static List<string> GetVolumeList()
        {
            List<string> systemDriveLetters = new List<string>();

            DriveInfo[] drives = DriveInfo.GetDrives();

            foreach (DriveInfo drive in drives)
            {
                if (drive.IsReady)
                {
                    systemDriveLetters.Add(drive.Name);
                }
            }

            return systemDriveLetters;
        }
    }
}

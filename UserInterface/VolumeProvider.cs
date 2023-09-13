using System.Collections.Generic;
using System.IO;

namespace Ashampoo
{
    public class VolumeProvider
    {
        public static List<string> GetVolumeList()
        {
            List<string> systemDriveLetters = new();

            foreach (DriveInfo drive in DriveInfo.GetDrives())
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

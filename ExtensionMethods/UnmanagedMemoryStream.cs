using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unleasharp.DB.Base.ExtensionMethods;
public static class UnmanagedMemoryStreamExtension {
    public static byte[] ToByteArray(this UnmanagedMemoryStream ums) {
        if (ums == null)
            throw new ArgumentNullException(nameof(ums));

        byte[] buffer = new byte[ums.Length];

        ums.Position  = 0; // rewind to start
        int bytesRead = ums.Read(buffer, 0, buffer.Length);

        if (bytesRead != buffer.Length) {
            // Handle unexpected partial read if needed
            Array.Resize(ref buffer, bytesRead);
        }

        return buffer;
    }

    public static MemoryStream ToMemoryStream(this UnmanagedMemoryStream ums) {
        using (MemoryStream ms = new MemoryStream()) {
            ums.CopyTo(ms);

            return ms;
        }
    }

    public static byte[] ToByteArray(this MemoryStream ms) {
        return ms.ToArray();
    }
}

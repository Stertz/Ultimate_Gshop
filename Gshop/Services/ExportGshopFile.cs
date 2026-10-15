using System.Text;
using Ultimate_Gshop.Gshop.Misc;

namespace Ultimate_Gshop.Gshop.Services;

public enum GshopExportTarget { Client, Server }

public static class ExportGshopFile
{
    public static void Handle(Shop shop, string path, GshopExportTarget target)
    {
        ArgumentNullException.ThrowIfNull(shop);
        if (target is not (GshopExportTarget.Client or GshopExportTarget.Server))
            throw new ArgumentOutOfRangeException(nameof(target));

        ProtectedFile.Write(path, stream =>
        {
            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
            if (target == GshopExportTarget.Server)
                ExportGshopServer.Handle(writer, shop);
            else
                ExportGshopClient.Handle(writer, shop);
        });
    }
}

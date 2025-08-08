using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Ultimate_Gshop.Misc;
namespace Ultimate_Gshop.Gshop.Services
{
    public static class ContinueGshop
    {
        public static async Task<Shop> Handle(Window owner)
        {

            if (!ApplicationSettings.GshopExists)
            {
                return await ImportGshop.Handle(owner);
            }

            return LoadGshop.Handle(ApplicationSettings.GshopTemp);

        }
    }
}

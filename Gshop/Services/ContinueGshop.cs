using Ultimate_Gshop.Misc;
namespace Ultimate_Gshop.Gshop.Services
{
    using Ultimate_Gshop.Gshop.Misc;
    public static class ContinueGshop
    {
        public static Shop Handle()
            => Handle(ApplicationSettings.AppFolder);

        internal static Shop Handle(string directory)
            => SessionTransaction.Read(directory, () => LoadGshop.Handle(Path.Combine(directory, "gshop_temp.data")));
    }
}

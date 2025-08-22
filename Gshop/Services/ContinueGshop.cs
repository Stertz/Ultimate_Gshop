using Ultimate_Gshop.Misc;
namespace Ultimate_Gshop.Gshop.Services
{
    public static class ContinueGshop
    {
        public static Shop Handle()
            => LoadGshop.Handle(ApplicationSettings.GshopTemp);
    }
}

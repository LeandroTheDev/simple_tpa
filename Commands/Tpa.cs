using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;

namespace SimpleTPA.Commands
{
    internal class Tpa : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        public string Name => "tpa";

        public string Help => "Teleport to a player";

        public string Syntax => "/tpa [playerName]";

        public List<string> Aliases => new();

        public List<string> Permissions => new();

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (caller is not UnturnedPlayer)
            {
                UnturnedChat.Say(caller, SimpleTPAPlugin.instance!.Translate("Invalid_Player"), Palette.COLOR_R);
                return;
            };

            if (command[0].Length < 1)
            {
                UnturnedChat.Say(caller, SimpleTPAPlugin.instance!.Translate("Invalid_Arguments"), Palette.COLOR_R);
                return;
            }

            UnturnedPlayer playerCommanded = (UnturnedPlayer)caller;
            UnturnedPlayer playerParameter = UnturnedPlayer.FromName(command[0]);

            switch (command[0].ToLower())
            {
                #region abort
                case "ab":
                    SimpleTPAPlugin.instance!.tpaSystem!.TpaAbort(playerCommanded); return;
                case "abort":
                    SimpleTPAPlugin.instance!.tpaSystem!.TpaAbort(playerCommanded); return;
                #endregion
                #region deny
                case "d":
                    SimpleTPAPlugin.instance!.tpaSystem!.TpaDeny(playerCommanded); return;
                case "deny":
                    SimpleTPAPlugin.instance!.tpaSystem!.TpaDeny(playerCommanded); return;
                #endregion
                #region accept
                case "a":
                    SimpleTPAPlugin.instance!.tpaSystem!.TpaAccept(playerCommanded); return;
                case "accept":
                    SimpleTPAPlugin.instance!.tpaSystem!.TpaAccept(playerCommanded); return;
                #endregion

                default:
                    SimpleTPAPlugin.instance!.tpaSystem!.TpaRequest(playerCommanded, playerParameter); return;
            }
        }
    }
}

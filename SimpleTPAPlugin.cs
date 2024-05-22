extern alias UnityEngineCoreModule;
using Rocket.API.Collections;
using Rocket.Core.Plugins;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;
using UnityCoreModule = UnityEngineCoreModule.UnityEngine;

namespace SimpleTPA
{
    public class SimpleTPAPlugin : RocketPlugin<SimpleTPAConfiguration>
    {
        public static SimpleTPAPlugin? instance;
        public SimpleTPASystem? tpaSystem;
        public override void LoadPlugin()
        {
            base.LoadPlugin();
            tpaSystem = gameObject.AddComponent<SimpleTPASystem>();
            Rocket.Unturned.Events.UnturnedPlayerEvents.OnPlayerUpdatePosition += tpaSystem.PositionUpdated;
            instance = this;
        }
        public override TranslationList DefaultTranslations => new()
        {
            {"Invalid_Player", "Cannot use this command because you are invalid"},
            {"Request_Self", "Cannot Tpa to yourself"},
            {"Invalid_Arguments", "Invalid Arguments"},
            {"Request_Aborted_By_Other", "Your Tpa request has been aborted because other player has requested"},
            {"Already_Requested", "You already have a Tpa request"},
            {"Pending_Request_Expired", "Tpa request has been expired"},
            {"Request_Send", "Tpa to {0} has been send"},
            {"Request_Received", "Tpa from {0}, accept using /tpa a"},
            {"Request_Accepted", "Tpa accepted don't move for {0} seconds"},
            {"Accepted_Request", "Tpa accepted"},
            {"No_Request_To_Accept", "No Tpa request to accept"},
            {"No_Request_To_Deny", "No Tpa request to deny"},
            {"No_Request_To_Abort", "No Tpa request to abort"},
            {"Tpa_Aborted_Moving", "Tpa aborted because you moved"},
            {"Tpa_Denied", "Tpa request has been denied"},
            {"Denied_Tpa", "You denied the Tpa from {0}"},
            {"Tpa_Aborted", "Tpa request has been aborted"},
        };
    }

    public class SimpleTPASystem : MonoBehaviour
    {
        private Dictionary<UnturnedPlayer, Dictionary<string, object>> tpaPlayers = new();

        public void Update()
        {
            #region tpa request
            List<UnturnedPlayer> tpaPlayersToRemove = new();
            Dictionary<UnturnedPlayer, Dictionary<string, object>> tpaPlayersNew = new(tpaPlayers);

            // Swipe all pending tpa
            foreach (KeyValuePair<UnturnedPlayer, Dictionary<string, object>> playerData in tpaPlayers)
            {
                #region requisting expiration
                // Check requesting status
                if (playerData.Value["Status"] is ETPAStatus reqStatus && reqStatus == ETPAStatus.Requesting)
                {
                    // Get default value
                    Dictionary<string, object> updatedData = new(playerData.Value);
                    // Reduce expiration data
                    updatedData["Time"] = (uint)updatedData["Time"] - 1;

                    // Check if is expirated
                    if ((uint)updatedData["Time"] <= 0)
                    {
                        UnturnedChat.Say(playerData.Key, SimpleTPAPlugin.instance!.Translate("Pending_Request_Expired"), Palette.COLOR_Y);
                        tpaPlayersToRemove.Add(playerData.Key);
                    }

                    // Update data
                    tpaPlayersNew[playerData.Key] = updatedData;
                }
                #endregion
                #region accepted delay
                // Check requesting status
                if (playerData.Value["Status"] is ETPAStatus acStatus && acStatus == ETPAStatus.Accepted)
                {
                    // Get default value
                    Dictionary<string, object> updatedData = new(playerData.Value);
                    // Reduce delay data
                    updatedData["Time"] = (uint)updatedData["Time"] - 1;

                    // Check if is finished
                    if ((uint)updatedData["Time"] <= 0)
                    {
                        // Teleport the player
                        if (updatedData["To"] is UnturnedPlayer playerReceiving)
                            playerData.Key.Teleport(playerReceiving);

                        tpaPlayersToRemove.Add(playerData.Key);
                    }

                    // Update data
                    tpaPlayersNew[playerData.Key] = updatedData;
                }
                #endregion
            }

            // Remove finished requests outside the iteration
            foreach (UnturnedPlayer player in tpaPlayersToRemove) tpaPlayersNew.Remove(player);

            // Finally we update the real tpaPlayers requests
            tpaPlayers = tpaPlayersNew;
            #endregion
        }

        public void TpaRequest(UnturnedPlayer playerGoing, UnturnedPlayer playerReceiving)
        {
            // Check if player is requesting to self
            if (playerGoing == playerReceiving)
            {
                // Inform him
                UnturnedChat.Say(playerGoing, SimpleTPAPlugin.instance!.Translate("Request_Self"), Palette.COLOR_Y);
                return;
            }
            // Check if player already requested
            if (tpaPlayers.TryGetValue(playerGoing, out _))
            {
                // Inform him
                UnturnedChat.Say(playerGoing, SimpleTPAPlugin.instance!.Translate("Already_Requested"), Palette.COLOR_Y);
                return;
            }

            // Check other requests for this player, if exist we need to remove it
            foreach (KeyValuePair<UnturnedPlayer, Dictionary<string, object>> playerData in tpaPlayers)
            {
                // Check the status requesting
                if (playerData.Value["Status"] is ETPAStatus status && status == ETPAStatus.Requesting)
                {
                    // Check if the receiver is the same as the player command
                    if (playerData.Value["To"] is UnturnedPlayer player && player == playerReceiving)
                    {
                        // Remove the old request
                        tpaPlayers.Remove(playerData.Key);
                        // Inform the player about their request cancelled
                        UnturnedChat.Say(playerData.Key, SimpleTPAPlugin.instance!.Translate("Request_Aborted_By_Other"), Palette.COLOR_Y);
                        break;
                    }
                }
            }

            // Add pending request
            tpaPlayers.Add(playerGoing, new()
            {
                {"To", playerReceiving},
                {"Time", SimpleTPAPlugin.instance!.Configuration.Instance.TickrateToExpire },
                {"Status", ETPAStatus.Requesting},
            });

            // Inform the going player
            UnturnedChat.Say(playerGoing, SimpleTPAPlugin.instance.Translate("Request_Send", playerGoing.DisplayName), Palette.COLOR_Y);

            // Inform the receiving player
            UnturnedChat.Say(playerReceiving, SimpleTPAPlugin.instance.Translate("Request_Received", playerGoing.DisplayName), Palette.COLOR_Y);
        }
        public void TpaAccept(UnturnedPlayer playerReceiving)
        {
            // Check if player has been requested
            foreach (KeyValuePair<UnturnedPlayer, Dictionary<string, object>> playerData in tpaPlayers)
            {
                // Check the status requesting
                if (playerData.Value["Status"] is ETPAStatus status && status == ETPAStatus.Requesting)
                {
                    // Check if player receiving is you
                    if (playerData.Value["To"] is UnturnedPlayer player && player == playerReceiving)
                    {
                        // Get default value
                        Dictionary<string, object> updatedData = playerData.Value;

                        // Change values
                        updatedData["Status"] = ETPAStatus.Accepted;
                        updatedData["Time"] = SimpleTPAPlugin.instance!.Configuration.Instance.TickrateToTeleport;

                        // Update data
                        tpaPlayers[playerData.Key] = updatedData;

                        // Inform the receiving player
                        UnturnedChat.Say(playerReceiving, SimpleTPAPlugin.instance!.Translate("Accepted_Request"), Palette.COLOR_Y);

                        // Inform the going player
                        UnturnedChat.Say(playerData.Key, SimpleTPAPlugin.instance!.Translate("Request_Accepted", (int)Math.Round((double)SimpleTPAPlugin.instance.Configuration.Instance.TickrateToTeleport / SimpleTPAPlugin.instance.Configuration.Instance.ServerTickrate)), Palette.COLOR_Y);
                        return;
                    }
                }
            }
            // If the function goes here is because theres no request for this player, lets inform him
            UnturnedChat.Say(playerReceiving, SimpleTPAPlugin.instance!.Translate("No_Request_To_Accept"), Palette.COLOR_Y);
        }
        public void TpaDeny(UnturnedPlayer playerReceiving)
        {
            // Check if player has been requested
            foreach (KeyValuePair<UnturnedPlayer, Dictionary<string, object>> playerData in tpaPlayers)
            {
                // Check the status requesting
                if (playerData.Value["Status"] is ETPAStatus status && status == ETPAStatus.Requesting)
                {
                    // Check if player receiving is you
                    if (playerData.Value["To"] is UnturnedPlayer player && player == playerReceiving)
                    {
                        // Inform the receiving player
                        UnturnedChat.Say(playerReceiving, SimpleTPAPlugin.instance!.Translate("Denied_Tpa", playerData.Key.DisplayName), Palette.COLOR_Y);

                        // Inform the going player
                        UnturnedChat.Say(playerData.Key, SimpleTPAPlugin.instance!.Translate("Tpa_Denied"), Palette.COLOR_R);

                        // Remove it from requests
                        tpaPlayers.Remove(playerData.Key);
                        return;
                    }
                }
            }
            // If the function goes here is because theres no request for this player, lets inform him
            UnturnedChat.Say(playerReceiving, SimpleTPAPlugin.instance!.Translate("No_Request_To_Deny"), Palette.COLOR_Y);
        }
        public void TpaAbort(UnturnedPlayer playerCalled)
        {
            foreach (KeyValuePair<UnturnedPlayer, Dictionary<string, object>> playerData in tpaPlayers)
            {
                // Check the status requesting
                if (playerData.Value["Status"] is ETPAStatus status && status == ETPAStatus.Accepted)
                {
                    // Check if player receiving is you
                    if (playerData.Value["To"] is UnturnedPlayer player && player == playerCalled)
                    {
                        // Inform the receiving player
                        UnturnedChat.Say(playerCalled, SimpleTPAPlugin.instance!.Translate("Tpa_Aborted"), Palette.COLOR_R);

                        // Inform the going player
                        UnturnedChat.Say(playerData.Key, SimpleTPAPlugin.instance!.Translate("Tpa_Aborted"), Palette.COLOR_R);

                        // Remove it from requests
                        tpaPlayers.Remove(playerData.Key);
                        return;
                    }
                }
                // Check if the player going is you
                if (playerData.Key == playerCalled)
                {
                    // Check if receiving player is valid
                    if (playerData.Value["To"] is UnturnedPlayer playerReceiving)
                    {
                        // Inform the receiving player
                        UnturnedChat.Say(playerReceiving, SimpleTPAPlugin.instance!.Translate("Tpa_Aborted"), Palette.COLOR_R);
                    }

                    // Inform the going player
                    UnturnedChat.Say(playerCalled, SimpleTPAPlugin.instance!.Translate("Tpa_Aborted"), Palette.COLOR_R);

                    // Remove it from requests
                    tpaPlayers.Remove(playerData.Key);
                    return;
                }
            }
            // If the function goes here is because theres no request for this player, lets inform him
            UnturnedChat.Say(playerCalled, SimpleTPAPlugin.instance!.Translate("No_Request_To_Abort"), Palette.COLOR_Y);
        }

        public void PositionUpdated(UnturnedPlayer player, UnityCoreModule.Vector3 _)
        {
            // Check if player has a tpa request
            if (tpaPlayers.TryGetValue(player, out var updatedData))
            {
                // Check if request is accepted
                if (updatedData["Status"] is ETPAStatus acStatus && acStatus == ETPAStatus.Accepted)
                {
                    // Remove request
                    tpaPlayers.Remove(player);
                    // Inform hes moved during request acception
                    UnturnedChat.Say(player, SimpleTPAPlugin.instance!.Translate("Tpa_Aborted_Moving"), Palette.COLOR_R);
                    return;
                }
            }
        }
    }

    public enum ETPAStatus
    {
        Requesting = 0,
        Accepted = 1,
    }
}

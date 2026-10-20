using System.Threading.Tasks;
using Fusion.Menu;
using TMPro;
using UnityEngine;

namespace Edgegap.SimpleFPS
{
    /// <summary>
    /// Replaces FusionMenuUIParty on the Startup scene's Party menu (Tools > Edgegap Simple FPS > Set Up Project
    /// does the swap and keeps every inspector value). Join takes the Edgegap deployment FQDN as the session
    /// name, kept as typed because session names are case-sensitive, and the Photon region code the server
    /// registered in. Create Game is unchanged.
    /// </summary>
    public class EdgegapPartyMenu : FusionMenuUIParty
    {
        /// <summary>The Photon region code input field (for example "us" or "cae").</summary>
        [SerializeField] protected TMP_InputField _regionCodeField;

        public const int SessionFieldLength = 30; // "<12-char request id>.pr.edgegap.net" is 27

        public override void Show()
        {
            base.Show();
            // base.Show() fills the code field with dashes sized for a generated party code.
            _sessionCodeField.SetTextWithoutNotify(string.Empty);
            _sessionCodeField.characterLimit = SessionFieldLength;
            if (_regionCodeField != null)
                _regionCodeField.SetTextWithoutNotify(string.Empty);
        }

        protected override async Task ConnectAsync(bool creating)
        {
            if (creating)
            {
                await base.ConnectAsync(true);
                return;
            }

            var sessionName = _sessionCodeField.text.Trim();
            var region = _regionCodeField != null ? _regionCodeField.text.Trim().ToLowerInvariant() : string.Empty;
            if (string.IsNullOrEmpty(sessionName) || string.IsNullOrEmpty(region))
            {
                await Controller.PopupAsync("Enter the deployment FQDN and the Photon region code (for example us or eu).",
                    "Missing Information");
                return;
            }

            ConnectionArgs.Session = sessionName;
            ConnectionArgs.Region = region;
            ConnectionArgs.Creating = false;

            Controller.Show<FusionMenuUILoading>();
            var result = await Connection.ConnectAsync(ConnectionArgs);
            await Controller.HandleConnectionResult(result, Controller);
        }
    }
}

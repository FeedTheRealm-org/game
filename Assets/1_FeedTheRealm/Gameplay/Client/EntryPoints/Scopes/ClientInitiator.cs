using FTR.Core.Common.Config;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Client.EntryPoints.Scopes
{
    /// <summary>
    /// Lifetime scope responsible for initializing the client application, including setting up dependency injection and starting the client entry point.
    /// </summary>
    public class ClientInitiator : LifetimeScope
    {
        [SerializeField]
        private SceneReference mainScene;

        [Header("Auth Prefabs")]
        [SerializeField]
        private GameObject loginPrefab;

        [SerializeField]
        private GameObject signUpPrefab;

        [SerializeField]
        private GameObject verifyCodePrefab;

        [Header("Main Menu Prefabs")]
        [SerializeField]
        private GameObject worldFeedMenuPrefab;

        [SerializeField]
        private GameObject navBarPrefab;

        [SerializeField]
        private GameObject profileMenuPrefab;

        [SerializeField]
        private Session.Session session;

        [SerializeField]
        private Config config;

        [SerializeField]
        private Logging.Logger logger;

        protected override void Configure(IContainerBuilder builder)
        {
            if (config.RuntimeRole != RuntimeRole.Client)
                throw new System.InvalidOperationException(
                    "Invalid runtime role for ClientInitiator"
                );

            builder.RegisterInstance(session);
            builder
                .RegisterEntryPoint<ClientEntryPoint>()
                .WithParameter("mainScene", mainScene)
                .WithParameter("loginPrefab", loginPrefab)
                .WithParameter("signUpPrefab", signUpPrefab)
                .WithParameter("verifyCodePrefab", verifyCodePrefab)
                .WithParameter("worldFeedMenuPrefab", worldFeedMenuPrefab)
                .WithParameter("navBarPrefab", navBarPrefab)
                .WithParameter("profileMenuPrefab", profileMenuPrefab);

            logger?.Log("ClientInitiator: Registered client entrypoint", this);
        }
    }
}

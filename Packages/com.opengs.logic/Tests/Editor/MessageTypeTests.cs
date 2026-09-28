using NUnit.Framework;

namespace OpenGSCore.Tests
{
    /// <summary>
    /// C0 requires the canonical message names to win over legacy aliases.
    /// These aliases used to collapse onto unrelated canonical names, which
    /// made the wire contract ambiguous.
    /// </summary>
    public class MessageTypeTests
    {
        [Test]
        public void LobbyAliasesUseTheirOwnWireNames()
        {
            Assert.That(MessageType.LobbyEnter, Is.EqualTo("LobbyEnter"));
            Assert.That(MessageType.LobbyLeave, Is.EqualTo("LobbyLeave"));
        }

        [Test]
        public void WaitRoomUpdateNotificationIsNotAnAliasOfUpdateRoomResponse()
        {
            // Both are sent on the lobby TCP stream, so they must stay distinct.
            Assert.That(
                MessageType.WaitRoomUpdateNotification,
                Is.Not.EqualTo(MessageType.UpdateRoomResponse));
        }

        [Test]
        public void NormalizeResolvesLegacyMatchEndAlias()
        {
            Assert.That(MessageType.Normalize("MatchEnd"), Is.EqualTo(MessageType.MatchEndNotification));
        }

        [Test]
        public void NormalizeKeepsCanonicalNamesUnchanged()
        {
            Assert.That(
                MessageType.Normalize(MessageType.LobbyEnter),
                Is.EqualTo(MessageType.LobbyEnter));

            Assert.That(
                MessageType.Normalize(MessageType.WaitRoomUpdateNotification),
                Is.EqualTo(MessageType.WaitRoomUpdateNotification));
        }

        [Test]
        public void NormalizeResolvesSendEnterRoomAlias()
        {
            Assert.That(MessageType.Normalize("SendEnterRoom"), Is.EqualTo(MessageType.JoinRoomRequest));
        }

        [Test]
        public void NormalizeLeavesUnknownNamesUntouched()
        {
            // Names the contract does not know must survive so the handler can
            // report them instead of silently dispatching elsewhere.
            Assert.That(MessageType.Normalize("JoinRoom"), Is.EqualTo("JoinRoom"));
        }
    }
}
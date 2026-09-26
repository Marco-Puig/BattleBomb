using System;
using BattleBomb.Core.Items;
using BattleBomb.Gameplay.Simulation;

namespace BattleBomb.Gameplay.Items
{
    /// <summary>
    /// A player whose menus are on this machine and whose bag is simulated here: the couch, a solo game,
    /// and the host's own player. The request runs inside <see cref="Send"/> — which is what keeps the
    /// couch exactly as it was: the answer arrives before the screen's next line runs.
    /// </summary>
    internal sealed class LocalPlayerRequests : IPlayerRequests
    {
        private readonly SimulationDriver _driver;
        private readonly int _playerId;

        internal LocalPlayerRequests(SimulationDriver driver, int playerId)
        {
            _driver = driver;
            _playerId = playerId;
        }

        public bool Pending => false;

        public void Send(PlayerRequest request, Action<RequestOutcome> answered)
        {
            PlayerInventory bag = _driver.InventoryOf(_playerId);
            PlayerRequest stamped = request.For(_playerId).WithRevision(bag != null ? bag.Inventory.Sack.Revision : 0);
            RequestOutcome outcome = PlayerRequestRunner.Run(stamped, bag, _driver);
            answered?.Invoke(outcome);
        }
    }
}

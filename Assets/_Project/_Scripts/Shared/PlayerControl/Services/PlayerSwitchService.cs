using System;
using System.Collections.Generic;
using CaseStudy.Shared.PlayerControl.Contracts;
using UnityEngine;

namespace CaseStudy.Shared.PlayerControl.Services
{
    /// <summary>
    /// Tracks controllable players and switches active control in registration order.
    /// </summary>
    public sealed class PlayerSwitchService : IPlayerSwitchService
    {
        private readonly List<IPlayerControlNode> _players = new(4);
        private IPlayerControlNode _activePlayer;
        private int _lastSwitchFrame = -1;

        public void Register(IPlayerControlNode player)
        {
            if (player == null || _players.Contains(player))
            {
                return;
            }

            _players.Add(player);

            if (_activePlayer == null)
            {
                SetActivePlayer(player);
                return;
            }

            player.SetControlState(false);
        }

        public void Unregister(IPlayerControlNode player)
        {
            if (player == null)
            {
                return;
            }

            bool wasActive = ReferenceEquals(_activePlayer, player);
            _players.Remove(player);

            if (!wasActive)
            {
                return;
            }

            _activePlayer = null;
            SetFirstAvailableAsActive();
        }

        public bool RequestSwitch(IPlayerControlNode requester)
        {
            if (requester == null || !ReferenceEquals(_activePlayer, requester))
            {
                return false;
            }

            int currentFrame = Time.frameCount;
            if (_lastSwitchFrame == currentFrame)
            {
                return false;
            }

            bool switched = SwitchToNextAvailable();
            if (switched)
            {
                _lastSwitchFrame = currentFrame;
            }

            return switched;
        }

        private bool SwitchToNextAvailable()
        {
            int count = _players.Count;
            if (count <= 1 || _activePlayer == null)
            {
                return false;
            }

            int activeIndex = _players.IndexOf(_activePlayer);
            if (activeIndex < 0)
            {
                SetFirstAvailableAsActive();
                return _activePlayer != null;
            }

            for (int offset = 1; offset <= count; offset++)
            {
                int candidateIndex = (activeIndex + offset) % count;
                IPlayerControlNode candidate = _players[candidateIndex];

                if (candidate == null || !candidate.IsAvailable)
                {
                    continue;
                }

                if (ReferenceEquals(candidate, _activePlayer))
                {
                    return false;
                }

                SetActivePlayer(candidate);
                return true;
            }

            return false;
        }

        private void SetFirstAvailableAsActive()
        {
            int count = _players.Count;
            for (int i = 0; i < count; i++)
            {
                IPlayerControlNode candidate = _players[i];
                if (candidate == null || !candidate.IsAvailable)
                {
                    continue;
                }

                SetActivePlayer(candidate);
                return;
            }
        }

        private void SetActivePlayer(IPlayerControlNode nextActive)
        {
            if (nextActive == null)
            {
                return;
            }

            if (ReferenceEquals(_activePlayer, nextActive))
            {
                nextActive.SetControlState(true);
                return;
            }

            _activePlayer?.SetControlState(false);
            _activePlayer = nextActive;
            _activePlayer.SetControlState(true);
        }
    }
}

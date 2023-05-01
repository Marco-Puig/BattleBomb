/***************************************************************************\
Project:      Daily Rewards
Copyright (c) Niobium Studios.
Author:       Guilherme Nunes Barbosa (gnunesb@gmail.com)
\***************************************************************************/
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace NiobiumStudios
{
    /**
    * Timed Rewards Canvas is the User interface to show Timed rewards
    **/
    public class TimedRewards : DailyRewardsCore<TimedRewards>
    {
        public DateTime lastRewardTime;     // The last time the user clicked in a reward
        public TimeSpan timer;
        public float maxTime = 3600f; // How many seconds until the player can claim the reward
        public bool isRewardRandom = false;     // Is the reward random or the player chooses from available rewards?

        public List<Reward> rewards;

        public delegate void OnCanClaim();              // When the player can claim the reward
        public OnCanClaim onCanClaim;

        public delegate void OnClaimPrize(int index);   // When the player claims the prize
        public OnClaimPrize onClaimPrize;

        private bool canClaim;              // Checks if the user can claim the reward

        // Needed Constants
        private const string TIMED_REWARDS_TIME = "TimedRewardsTime";
        private const string FMT = "O";

        void Start()
        {
            StartCoroutine(InitializeTimer());
        }

        // Initializes the timer in case the user already have a player preference
        private IEnumerator InitializeTimer()
        {
            yield return StartCoroutine(base.InitializeDate());

            if (base.isErrorConnect)
            {
                if(onInitialize!=null)
                    onInitialize(true, base.errorMessage);
            }
            else
            {
                LoadTimerData();

                if(onInitialize!=null)
                    onInitialize();

                CheckCanClaim();
            }
        }

        void LoadTimerData ()
        {
            string lastRewardTimeStr = PlayerPrefs.GetString(GetTimedRewardsTimeKey());

            if (!string.IsNullOrEmpty(lastRewardTimeStr))
            {
                lastRewardTime = DateTime.ParseExact(lastRewardTimeStr, FMT, CultureInfo.InvariantCulture);
                timer = (lastRewardTime - now).Add(TimeSpan.FromSeconds(maxTime));
            }
            else
            {
                timer = TimeSpan.FromSeconds(maxTime);
            }
        }

        protected override void OnApplicationPause(bool pauseStatus)
        {
            base.OnApplicationPause(pauseStatus);
            LoadTimerData();
            CheckCanClaim();
        }

        public override void TickTime()
        {
            base.TickTime();
            if(isInitialized)
            {
                // Keeps ticking until the player claims
                if (!canClaim)
                {
                    timer = timer.Subtract(TimeSpan.FromSeconds(Time.unscaledDeltaTime));
                    CheckCanClaim();
                }
            }
        }

        public void CheckCanClaim ()
        {
            if (timer.TotalSeconds <= 0)
            {
                canClaim = true;
                if (onCanClaim != null)
                    onCanClaim();
            }
            else
            {
                // We need to save the player time every tick. If the player exits the game the information keeps logged
                // For perfomance issues you can save this information when the player switches scenes or quits the application
                PlayerPrefs.SetString(GetTimedRewardsTimeKey(), now.Add(timer - TimeSpan.FromSeconds(maxTime)).ToString(FMT));
            }
        }

        //Returns the TimeRewardsTime playerPrefs key depending on instanceId
        private string GetTimedRewardsTimeKey()
        {
            if (instanceId == 0)
                return TIMED_REWARDS_TIME;

            return string.Format("{0}_{1}", TIMED_REWARDS_TIME, instanceId);
        }

        // The player claimed the prize. We need to reset to restart the timer
        public void ClaimReward(int rewardIdx)
        {
            PlayerPrefs.SetString(GetTimedRewardsTimeKey(), now.Add(timer - TimeSpan.FromSeconds(maxTime)).ToString(FMT));
            timer = TimeSpan.FromSeconds(maxTime);

            canClaim = false;

            if (onClaimPrize != null)
                onClaimPrize(rewardIdx);
        }

        public string GetFormattedTime()
        {
			if(timer.Days > 0) 
				return string.Format("{0:D2} days {1:D2}:{2:D2}:{0:D3}", timer.Days, timer.Hours, timer.Minutes, timer.Seconds);
			else
				return string.Format("{0:D2}:{1:D2}:{2:D2}", timer.Hours, timer.Minutes, timer.Seconds);
        }

        // Resets the Timed Rewards. For debug purposes
        public void Reset()
        {
            PlayerPrefs.DeleteKey(GetTimedRewardsTimeKey());
            canClaim = true;
            timer = TimeSpan.FromSeconds(0);

            if (onCanClaim != null)
                onCanClaim();
        }

        // Returns a reward from an index
        public Reward GetReward(int index)
        {
            return rewards[index];
        }
    }
}
/***************************************************************************\
Project:      Daily Rewards
Copyright (c) Niobium Studios
Author:       Guilherme Nunes Barbosa (gnunesb@gmail.com)
\***************************************************************************/
using System;
using System.Globalization;
using UnityEngine;
using System.Collections;
using SimpleJSON;

namespace NiobiumStudios
{
    /**
     * Daily Rewards common methods
     **/
    public abstract class DailyRewardsCore<T> : MonoBehaviour where T : DailyRewardsCore<T>
    {
        public int instanceId;
        public bool isSingleton = true;                         // Checks if should be used as singleton
        public string worldClockUrl = "http://worldclockapi.com/api/json/est/now";  // The World Clock JSON API
        public string worldClockFMT = "yyyy-MM-dd'T'HH:mmzzz";  // World Clock Format

        public bool useWorldClockApi = true;                    // Use World Clock API
        public WorldClock worldClock;                           // The World Clock object

        public string errorMessage;                             // Error Message
        public bool isErrorConnect;                             // Some error happened on connecting?
        public DateTime now;                                    // The actual date. Either returned by the using the world clock or the player device clock

        public int maxRetries = 3;                              // The maximum number of retries for the World Clock connection

        public delegate void OnInitialize(bool error = false, string errorMessage = ""); // When the timer initializes. Sends an error message in case it happens. Should wait for this delegate if using World Clock API
        public OnInitialize onInitialize;

        protected bool isInitialized = false;

        // Initializes the current DateTime. If the player is using the World Clock initializes it
        public IEnumerator InitializeDate()
        {
            if (useWorldClockApi)
            {
                if (WorldClockBuilder.currentState == WorldClockBuilder.State.NotInitialized)
                {
                    // worldClock is not initialized, we initialize it
                    yield return StartCoroutine(WorldClockBuilder.InitializeWorldClock(worldClockUrl, maxRetries, worldClockFMT));
                }
                else if (WorldClockBuilder.currentState == WorldClockBuilder.State.Initializing)
                {
                    // worldClock is being initialized by another script, we wait until it finishes
                    while (WorldClockBuilder.currentState == WorldClockBuilder.State.Initializing)
                        yield return null;
                }

                if (WorldClockBuilder.currentState == WorldClockBuilder.State.Initialized)
                {
                    worldClock = WorldClockBuilder.instance;
                    now = WorldClockBuilder.worldClockDate;
                    isInitialized = true;
                }
                else if (WorldClockBuilder.currentState == WorldClockBuilder.State.FailedToInitialize)
                {
                    isErrorConnect = true;
                    errorMessage = WorldClockBuilder.errorMessage;
                }
            }
            else
            {
                now = DateTime.Now;
                isInitialized = true;
            }
        }

        public void RefreshTime ()
        {
            if (useWorldClockApi)
                now = WorldClockBuilder.worldClockDate;
            else
                now = DateTime.Now;
        }

        public static T GetInstance(int id = 0)
        {
            var instances = FindObjectsOfType<T>();
            for (int i = 0; i < instances.Length; i++)
            {
                if ((instances[i]).instanceId == id)
                    return instances[i];
            }
            return null;
        }

        //Updates the current time
        public virtual void TickTime()
        {
            if (!isInitialized)
				return;

            now = now.AddSeconds(Time.unscaledDeltaTime);

			if(useWorldClockApi)
				WorldClockBuilder.worldClockDate = now;
        }

        public string GetFormattedTime (TimeSpan span)
        {
            return string.Format("{0:D2}:{1:D2}:{2:D2}", span.Hours, span.Minutes, span.Seconds);
        }

		protected virtual void Awake()
		{
			if (isSingleton)
			{
				DontDestroyOnLoad(this.gameObject);
				
				var instanceCount = GetInstanceCount ();
				
				if (instanceCount > 1)
					Destroy(gameObject);
			}
		}
		
		// Returns the amount of instances with the same id
		private int GetInstanceCount()
		{
			var instances = FindObjectsOfType<T>();
			var count = 0;
			for (int i = 0; i < instances.Length; i++)
			{
				if ((instances[i]).instanceId == instanceId)
					count ++;
			}
			return count;
		}

        protected virtual void OnApplicationPause(bool pauseStatus)
        {
            if(!pauseStatus)
                RefreshTime();
        }
    }
}
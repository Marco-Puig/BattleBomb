/***************************************************************************\
Project:      Daily Rewards
Copyright (c) Niobium Studios
Author:       Guilherme Nunes Barbosa (gnunesb@gmail.com)
\***************************************************************************/
using UnityEngine;
using System;
using System.Collections;
using System.Globalization;
using SimpleJSON;

namespace NiobiumStudios
{
    /**
     * Representation of the world clock JSON Object as shown at http://worldclockapi.com/
     **/
    [Serializable]
    public class WorldClock
    {
        public string currentDateTime;
        public string utcOffset;
        public string dayOfTheWeek;
        public string timeZoneName;
        public double currentFileTime;
        public string ordinalDate;
        public string serviceResponse;
    }

    /**
    * Handles the worldClock global instance
    **/
    public static class WorldClockBuilder
    {
        public static string errorMessage = String.Empty;              // Failed error message
        private static int connectrionRetries;                         // Retries counter
        public static WorldClock instance;                             // Global WorldClock instance
        public static DateTime worldClockDate;                         // Global DateTime
        public static State currentState;                              // WorldClock current status

        public enum State {NotInitialized,
            Initializing, Initialized, FailedToInitialize};            // WorldClock possible status

        public static IEnumerator InitializeWorldClock(string worldClockUrl, int maxRetries, string worldClockFMT)
        {
            currentState = State.Initializing;
            string result = String.Empty;
            while(connectrionRetries < maxRetries)
            {
                WWW www = new WWW(worldClockUrl);
                while (!www.isDone)
                    yield return null;

                if (!string.IsNullOrEmpty(www.error))
                {
                    connectrionRetries++;
                    Debug.LogError("Error Loading World Clock. Retrying connection " + connectrionRetries);
                    errorMessage = www.error;
                }
                else
                {
                    result = www.text;
                    break;
                }
            }
            if(!string.IsNullOrEmpty(result))
            {
                var clockJson = result;

                // Loads the world clock from JSON
#if UNITY_5_3_OR_NEWER
                var worldClock = JsonUtility.FromJson<WorldClock>(clockJson);
#else
                var worldClockJson = JSON.Parse(clockJson);
                WorldClock worldClock = new WorldClock();
                worldClock.currentDateTime = worldClockJson["currentDateTime"].Value;
                worldClock.utcOffset = worldClockJson["utcOffset"].Value;
                worldClock.dayOfTheWeek = worldClockJson["dayOfTheWeek"].Value;
                worldClock.timeZoneName = worldClockJson["timeZoneName"].Value;
                worldClock.currentFileTime = worldClockJson["currentFileTime"].AsDouble;
                worldClock.ordinalDate = worldClockJson["ordinalDate"].Value;
                worldClock.serviceResponse = worldClockJson["serviceResponse"].Value;
#endif
                // save the worldClock instance
                instance = worldClock;

                var dateTimeStr = worldClock.currentDateTime;

                worldClockDate = DateTime.ParseExact(dateTimeStr, worldClockFMT, CultureInfo.InvariantCulture);
                // World Clock don't count the seconds. So we pick the seconds from the local machine
                worldClockDate = worldClockDate.AddSeconds(DateTime.Now.Second);

                var time = string.Format("{0:D4}/{1:D2}/{2:D2} {3:D2}:{4:D2}:{5:D2}", worldClockDate.Year, worldClockDate.Month, worldClockDate.Day, worldClockDate.Hour, worldClockDate.Minute, worldClockDate.Second);

                Debug.Log("World Clock Time: " + time);
                currentState = State.Initialized;
            }
            else
            {
                Debug.LogError("Error Loading World Clock:" + errorMessage);
                currentState = State.FailedToInitialize;
            }
        }
    }
}
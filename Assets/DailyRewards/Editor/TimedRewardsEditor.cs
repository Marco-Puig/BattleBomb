/***************************************************************************\
Project:      Daily Rewards
Copyright (c) Niobium Studios.
Author:       Guilherme Nunes Barbosa (gnunesb@gmail.com)
\***************************************************************************/
using UnityEditor;
using UnityEngine;
using System;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;

namespace NiobiumStudios
{
    [CustomEditor(typeof(TimedRewards))]
    public class TimedRewardsEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            SerializedProperty instanceIdProp = serializedObject.FindProperty("instanceId");
            SerializedProperty useWorldClockApiProp = serializedObject.FindProperty("useWorldClockApi");
            SerializedProperty rewardsProp = serializedObject.FindProperty("rewards");
            SerializedProperty maxTimeProp = serializedObject.FindProperty("maxTime");
            SerializedProperty isSingletonProp = serializedObject.FindProperty("isSingleton");
            SerializedProperty isRewardRandomProp = serializedObject.FindProperty("isRewardRandom");

            EditorGUILayout.PropertyField(instanceIdProp, new GUIContent("Instance ID", "Change this ID number if you are using multiple instances"));
            EditorGUILayout.PropertyField(isSingletonProp, new GUIContent("Is Singleton?", "Is it singleton or the reward is reloaded on each scene?"));
            EditorGUILayout.PropertyField(maxTimeProp, new GUIContent("Reward Time", "Time in seconds for the reward countdown"));
            EditorGUILayout.PropertyField(isRewardRandomProp, new GUIContent("Is Random Reward?", "Is the reward random or the player chooses from available rewards?"));

            useWorldClockApiProp.boolValue = EditorGUILayout.Toggle(new GUIContent("Use World Clock?"), useWorldClockApiProp.boolValue);
            if (useWorldClockApiProp.boolValue && EditorTools.DrawHeader("World Clock"))
            {
                SerializedProperty worldClockUrlProp = serializedObject.FindProperty("worldClockUrl");
                SerializedProperty worldClockFmtProp = serializedObject.FindProperty("worldClockFMT");

                EditorGUILayout.PropertyField(worldClockUrlProp, new GUIContent("World Clock URL"));
                EditorGUILayout.PropertyField(worldClockFmtProp, new GUIContent("World Clock Format"));
            }

            if (EditorTools.DrawHeader("Rewards"))
            {
                // Rewards
                for (int i = 0; i < rewardsProp.arraySize; i++)
                {
                    if (EditorTools.DrawHeader("Reward " + (i + 1)))
                    {
                        EditorTools.BeginContents();
                        SerializedProperty rewardProp = rewardsProp.GetArrayElementAtIndex(i);

                        var unitRewardProp = rewardProp.FindPropertyRelative("unit");
                        var rewardQtProp = rewardProp.FindPropertyRelative("reward");
                        var rewardSpriteProp = rewardProp.FindPropertyRelative("sprite");

                        EditorGUILayout.PropertyField(unitRewardProp, new GUIContent("Unit"));
                        EditorGUILayout.PropertyField(rewardQtProp, new GUIContent("Reward"));
                        rewardSpriteProp.objectReferenceValue = EditorGUILayout.ObjectField("Sprite", rewardSpriteProp.objectReferenceValue, typeof(Sprite), false);

                        EditorTools.EndContents();

                        if (GUILayout.Button("Remove Reward"))
                        {
                            rewardsProp.DeleteArrayElementAtIndex(i);
                        }
                    }
                }

                if (GUILayout.Button("Add Reward"))
                {
                    rewardsProp.InsertArrayElementAtIndex(rewardsProp.arraySize);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

}
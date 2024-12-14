using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CardHouse
{
    public class GroupSetup : MonoBehaviour
    {
        [Serializable]
        public struct GroupPopulationData
        {
            public CardGroup Group;
            public GameObject CardPrefab;
            public int CardCount;
        }



        public bool RunOnStart = true;

        public List<GameObject> CardPrefabs; // 54 cards
        public List<GroupPopulationData> GroupPopulationList;
        public List<CardGroup> GroupsToShuffle;
        public List<TimedEvent> OnSetupCompleteEventChain;

        void Start()
        {
            InitializeGroupPopulationData();
            if (RunOnStart)
            {
                DoSetup();
            }
        }


        public void InitializeGroupPopulationData()
        {
            // Shuffle the card prefabs to ensure randomness
            var shuffledCards = CardPrefabs.OrderBy(_ => UnityEngine.Random.value).ToList();

            for (int i = 0; i < GroupPopulationList.Count; i++)
            {
                // Retrieve the struct from the list
                var groupData = GroupPopulationList[i];

                // Ensure enough cards remain to assign
                if (shuffledCards.Count == 0)
                {
                    Debug.LogWarning("Not enough unique cards left to populate all groups!");
                    break;
                }

                // Assign a random card from the shuffled list and remove it
                int randomIndex = UnityEngine.Random.Range(0, shuffledCards.Count);
                groupData.CardPrefab = shuffledCards[randomIndex];
                shuffledCards.RemoveAt(randomIndex); // Remove assigned card from the pool

                // Assign the modified struct back to the list
                GroupPopulationList[i] = groupData;
            }
        }



        public void DoSetup()
        {
            StartCoroutine(SetupCoroutine());
        }

        IEnumerator SetupCoroutine()
        {
            var homing = new InstantVector3Seeker();
            var turning = new InstantFloatSeeker();
            var newThingDict = new Dictionary<GroupPopulationData, List<GameObject>>();
            foreach (var group in GroupPopulationList)
            {
                newThingDict[group] = new List<GameObject>();
                for (var i = 0; i < group.CardCount; i++)
                {
                    var newThing = Instantiate(group.CardPrefab, Vector3.down * 10, Quaternion.identity);
                    newThingDict[group].Add(newThing);
                }
            }

            yield return new WaitForEndOfFrame();

            foreach (var kvp in newThingDict)
            {
                foreach (var newThing in kvp.Value)
                {
                    kvp.Key.Group.Mount(newThing.GetComponent<Card>(), instaFlip: true, seekerSets: new SeekerSetList { new SeekerSet { Homing = homing, Turning = turning } });
                }
            }

            foreach (var group in GroupsToShuffle)
            {
                group.Shuffle(true);
            }

            StartCoroutine(TimedEvent.ExecuteChain(OnSetupCompleteEventChain));
        }
    }
}

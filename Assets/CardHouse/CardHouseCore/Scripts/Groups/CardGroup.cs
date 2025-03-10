using PokerGameClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Events;
using static CardHouse.GroupSetup;

namespace CardHouse
{
    [RequireComponent(typeof(CardGroupSettings))]
    public class CardGroup : MonoBehaviour
    {
        public bool HilightOnCardEntry = true;
        public GameObject Hilight;
        public Dictionary<CardGroup,CardGroup> abdool;
        public List<CardGroup> handsAndDecks;

        public GateCollection<DropParams> DropGates;

        public SeekerScriptable<Vector3> ShuffleStrategy;

        public List<Card> AvailableCards = new List<Card>(); // Predefined list of cards for random selection
        public List<Card> MountedCards = new List<Card>();


        CardGroupSettings Strategy;

        public UnityEvent OnGroupChanged;

        public static Action<CardGroup> OnNewActiveGroup;
        public static Action<Card, Card> OnCardUsedOnTarget;

        static List<CardGroup> GroupsHoveredWithObjects = new List<CardGroup>();

        int CollidersEntered = 0;
        private bool isGrid = false;
        public bool capture = false;
        public GameObject CaptureCardsText;

        public static CardGroup HilightedGroup
        {
            get
            {
                return GroupsHoveredWithObjects.Count > 0 ? GroupsHoveredWithObjects[GroupsHoveredWithObjects.Count - 1] : null;
            }
        }

        public static void AddHoveredGroup(CardGroup group)
        {
            GroupsHoveredWithObjects.Remove(group);
            GroupsHoveredWithObjects.Add(group);
            OnNewActiveGroup?.Invoke(group);
        }

        public static void RemoveHoveredGroup(CardGroup group)
        {
            GroupsHoveredWithObjects.Remove(group);
            if (GroupsHoveredWithObjects.Count > 0)
            {
                var newActiveGroup = GroupsHoveredWithObjects[GroupsHoveredWithObjects.Count - 1];
                newActiveGroup.SetHilightState(true);
                OnNewActiveGroup?.Invoke(newActiveGroup);
            }
        }

        public static Card GetActiveCard(DragDetector draggable)
        {
            var closestIndex = HilightedGroup.GetClosestMountedCardIndex(draggable.transform.position);
            if (closestIndex == null)
                return null;

            return HilightedGroup.MountedCards[(int)closestIndex];
        }

        void Awake()
        {
            Strategy = GetComponent<CardGroupSettings>();
        }

        void Start()
        {
            isGrid = this.gameObject.CompareTag("Grid");
            if (isGrid)
            {
                CaptureCardsText.gameObject.SetActive(false);
                abdool = new Dictionary<CardGroup, CardGroup>();
                for (int i = 0;i < handsAndDecks.Count;i += 2)
                {
                    abdool[handsAndDecks[i]] = handsAndDecks[i + 1];
                }
            }
             
            OnNewActiveGroup += HandleNewActiveGroup;

            if (Dragging.Instance == null)
            {
                Debug.LogWarning("Groups require the Dragging component on the System prefab, and will not function otherwise");
                return;
            }

            Dragging.Instance.OnDrag += HandleDragStart;
            Dragging.Instance.OnDrop += HandleDragDrop;
            Dragging.Instance.PostDrop += HandlePostDrop;
            // Initialize the group with 4 random cards
            //InitializeGroup();
        }

        private void InitializeGroup()
        {
            // Validate the available cards list
            if (AvailableCards.Count == 0)
            {
                Debug.LogWarning("No available cards to initialize the group.");
                return;
            }

            // Select 4 random cards without repetition
            var randomCards = AvailableCards.OrderBy(_ => UnityEngine.Random.value).Take(4).ToList();

            // Mount the selected cards
            foreach (var card in randomCards)
            {
                SafeMount(card);
            }

            Debug.Log($"{randomCards.Count} cards have been randomly mounted to the group at the start.");
        }
        void OnDestroy()
        {
            OnNewActiveGroup -= HandleNewActiveGroup;
            if (Dragging.Instance != null)
            {
                Dragging.Instance.OnDrag -= HandleDragStart;
                Dragging.Instance.OnDrop -= HandleDragDrop;
                Dragging.Instance.PostDrop -= HandlePostDrop;
            }
        }

        void HandleNewActiveGroup(CardGroup newActiveGroup)
        {
            if (newActiveGroup != this)
            {
                Hilight?.SetActive(false);
            }
        }

        void HandleDragStart(DragDetector draggedCard)
        {
            CollidersEntered = 0;
        }

        void HandleDragDrop(DragDetector dragDetector)
        {
            var cardComponent = dragDetector.GetComponent<Card>();
            var cardDragHandler = dragDetector.GetComponent<DragOperator>();
            var failedDragFromThisGroup = HilightedGroup == null && cardComponent != null && MountedCards.Contains(cardComponent);
            if (failedDragFromThisGroup)
            {
                Strategy.Apply(MountedCards);
            }

            if (HilightedGroup != this)
                return;

            if (cardComponent != null && cardDragHandler != null)
            {
                var dropParams = new DropParams
                {
                    Source = cardComponent?.Group,
                    Target = this,
                    Card = cardComponent,
                    DragType = cardDragHandler == null ? DragAction.None : cardDragHandler.DragAction
                };

                var isTargetable = true;
                if (dropParams.DragType == DragAction.UseOnTargetAndDiscard)
                {
                    var closestIndex = GetClosestMountedCardIndex(cardComponent.transform.position);
                    if (closestIndex != null)
                    {
                        var targetCardParams = new TargetCardParams
                        {
                            Source = cardComponent,
                            Target = MountedCards[(int)closestIndex]
                        };
                        isTargetable = targetCardParams.Source.GetComponent<DragDetector>().TargetCardGates.AllUnlocked(targetCardParams)
                                       && (targetCardParams.Target.GetComponent<DragDetector>()?.TargetCardGates.AllUnlocked(targetCardParams) ?? true);
                    }
                }

                if (!DropGates.AllUnlocked(dropParams) || !dragDetector.GroupDropGates.AllUnlocked(dropParams) || !isTargetable) // Return to sender
                {
                    cardComponent.Group?.ApplyStrategy();
                    return;
                }

                switch (cardComponent.GetComponent<DragOperator>().DragAction)
                {
                    case DragAction.Mount:
                        int? insertPoint = null;

                        switch (Strategy.DragMountingMode)
                        {
                            case MountingMode.Top:
                                break;
                            case MountingMode.Bottom:
                                insertPoint = 0;
                                break;
                            case MountingMode.Closest:
                                var closestIndex = GetClosestMountedCardIndex(dragDetector.transform.position);
                                if (closestIndex == null)
                                    break;

                                var diff = MountedCards[(int)closestIndex].transform.position - dragDetector.transform.position;
                                insertPoint = diff.x > 0 ? closestIndex : closestIndex + 1;
                                break;
                        }

                        if (cardComponent.GetComponent<CardLoyalty>() != null
                            && GroupRegistry.Instance?.GetOwnerIndex(cardComponent.Group) != null
                            && GroupRegistry.Instance?.GetOwnerIndex(this) == null)
                        {
                            cardComponent.GetComponent<CardLoyalty>().PlayerIndex = (int)GroupRegistry.Instance.GetOwnerIndex(cardComponent.Group);
                        }

                        Mount(cardComponent, insertPoint);
                        cardComponent.HandlePlayed();
                        break;
                    case DragAction.UseAndDiscard:
                        var discardGroup = GroupRegistry.Instance?.Get(GroupName.Discard, PhaseManager.Instance == null ? null : PhaseManager.Instance.PlayerIndex);
                        if (discardGroup == null)
                        {
                            Debug.LogWarningFormat("{0}: Could not find Discard group to discard this card", name);
                            cardComponent.Group?.ApplyStrategy();
                            break;
                        }

                        var seekerSets = new SeekerSetList();
                        var presentationTransform = PhaseManager.Instance?.CurrentPhase?.CardPresentationPosition;
                        if (presentationTransform != null)
                        {
                            seekerSets.Add(new SeekerSet
                            {
                                Card = cardComponent,
                                Homing = cardDragHandler.PresentationSeekers.Homing?.GetStrategy(presentationTransform.position),
                                Turning = cardDragHandler.PresentationSeekers.Turning?.GetStrategy(CardHouse.Utils.CorrectAngle(presentationTransform.rotation.eulerAngles.z)),
                                Scaling = cardDragHandler.PresentationSeekers.Scaling?.GetStrategy(presentationTransform.lossyScale.x)
                            });
                        }
                        discardGroup.Mount(cardComponent, seekerSets: seekerSets);
                        cardComponent.HandlePlayed();
                        break;
                    case DragAction.UseOnTargetAndDiscard:
                        var discardGroup1 = GroupRegistry.Instance?.Get(GroupName.Discard, PhaseManager.Instance == null ? null : PhaseManager.Instance.PlayerIndex);
                        var closestIndex1 = GetClosestMountedCardIndex(dragDetector.transform.position);
                        if (discardGroup1 == null || closestIndex1 == null)
                        {
                            cardComponent.Group?.ApplyStrategy();
                            break;
                        }

                        var seekerSets1 = new SeekerSetList();
                        var presentationTransform1 = PhaseManager.Instance?.CurrentPhase?.CardPresentationPosition;
                        if (presentationTransform1 != null)
                        {
                            seekerSets1.Add(new SeekerSet
                            {
                                Card = cardComponent,
                                Homing = cardDragHandler.PresentationSeekers.Homing?.GetStrategy(presentationTransform1.position),
                                Turning = cardDragHandler.PresentationSeekers.Turning?.GetStrategy(CardHouse.Utils.CorrectAngle(presentationTransform1.rotation.eulerAngles.z)),
                                Scaling = cardDragHandler.PresentationSeekers.Scaling?.GetStrategy(presentationTransform1.lossyScale.x)
                            });
                        }
                        discardGroup1.Mount(cardComponent, seekerSets: seekerSets1);
                        var targetCard = MountedCards[(int)closestIndex1];
                        OnCardUsedOnTarget?.Invoke(cardComponent, targetCard);
                        cardComponent.HandlePlayed();
                        break;
                }
            }
        }

        public int? GetClosestMountedCardIndex(Vector3 position)
        {
            var closestDist = Mathf.Infinity;
            int? closestIndex = null;
            for (var i = 0; i < MountedCards.Count; i++)
            {
                var card = MountedCards[i];
                var diff = card.transform.position - position;
                var dist = diff.magnitude;
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestIndex = i;
                }
            }
            return closestIndex;
        }

        void HandlePostDrop(DragDetector dragDetector)
        {
            GroupsHoveredWithObjects.Clear();
            Hilight?.SetActive(false);
        }

        void OnTriggerEnter2D(Collider2D col)
        {
            HandleTriggerEnter2D(col);
        }

        public void HandleTriggerEnter2D(Collider2D col)
        {
            var draggable = col.gameObject.GetComponent<DragOperator>();
            if ((draggable == null || draggable.DragAction == DragAction.Mount) && !HasRoom())
                return;

            RespondToObjectCrossingBoundary(col, true);
        }

        void OnTriggerExit2D(Collider2D col)
        {
            HandleTriggerExit2D(col);
        }

        public void HandleTriggerExit2D(Collider2D col)
        {
            RespondToObjectCrossingBoundary(col, false);
        }

        void RespondToObjectCrossingBoundary(Collider2D col, bool isEntry)
        {
            var thingBeingDragged = Dragging.Instance?.GetTarget();
            var cardComponent = thingBeingDragged?.GetComponent<Card>();
            var dragHandler = thingBeingDragged?.GetComponent<DragOperator>();
            var dropParams = new DropParams
            {
                Source = cardComponent?.Group,
                Target = this,
                Card = cardComponent,
                DragType = dragHandler == null ? DragAction.None : dragHandler.DragAction
            };
            if (cardComponent != null
                && dragHandler != null
                && DropGates.AllUnlocked(dropParams)
                && dragHandler.GetComponent<DragDetector>().GroupDropGates.AllUnlocked(dropParams))
            {
                CollidersEntered += isEntry ? 1 : -1;
                SetAsActiveGroup(CollidersEntered > 0);
            }
        }

        void SetAsActiveGroup(bool newState)
        {
            SetHilightState(newState);

            if (newState)
            {
                AddHoveredGroup(this);
            }
            else
            {
                RemoveHoveredGroup(this);
            }
        }

        public void SetHilightState(bool newState)
        {
            if (HilightOnCardEntry)
            {
                Hilight.SetActive(newState);
            }
        }

        public void ApplyStrategy()
        {
            Strategy.Apply(MountedCards);
        }

        public bool HasRoom()
        {
            return Strategy.CardLimit < 0 || MountedCards.Count < Strategy.CardLimit;
        }
        public string tempName = "";
        public CardGroup oldGroup;

        public void Mount(Card card, int? index = null, bool instaFlip = false, SeekerSetList seekerSets = null, SeekerSet seekersForUnmounting = null)
        {
            if (capture) {

                capture = false;
                CaptureCardsText.gameObject.SetActive(false);
            }
            oldGroup = card.Group;

            card.Group?.UnMount(card, seekersForUnmounting);
            

            if (index == null || index >= MountedCards.Count)
            {
                MountedCards.Add(card);
            }
            else if (index < 0)
            {
                MountedCards.Insert(0, card);
            }
            else
            {
                MountedCards.Insert((int)index, card);
            }

            card.Group = this;

            card.TriggerMountEvents(this);
            OnGroupChanged?.Invoke();

            Strategy.Apply(MountedCards, instaFlip, seekerSets);
            if (card.cardName == tempName && isGrid && oldGroup != this && oldGroup != null)
            {
                CaptureLastTwoCards(abdool[oldGroup],card);
                capture = true;
                CaptureCardsText.gameObject.SetActive(true);
            }
            tempName = card.cardName;
        }

        public void CaptureLastTwoCards(CardGroup targetGroup, Card card)
        {
            // Ensure we have enough cards to capture
            if (MountedCards.Count < 2)
            {
                Debug.LogWarning("CaptureLastTwoCards failed: Not enough cards in MountedCards.");
                return;
            }

            // Ensure the target group is valid
            if (targetGroup == null)
            {
                Debug.LogWarning("CaptureLastTwoCards failed: Target group is null.");
                return;
            }

            // Find two instances of the specified card in MountedCards
            var matchingCards = MountedCards.Where(c => c.cardName == card.cardName).Take(2).ToList();


            // Ensure there are at least two matching cards
            if (matchingCards.Count < 2)
            {
                Debug.LogWarning($"CaptureLastTwoCards failed: Less than two instances of {card.name} found in MountedCards.");
                return;
            }

            // Remove the matching cards from this group and add them to the target group
            foreach (var matchingCard in matchingCards)
            {
                UnMount(matchingCard); // Unmount from the current group
                targetGroup.Mount(matchingCard); // Mount to the target group
                Debug.Log($"Card {matchingCard.name} captured and moved to {targetGroup.name}");
            }

            // Optional: Trigger any relevant events
            OnGroupChanged?.Invoke();
        }



        public bool SafeMount(Card card, int? index = null)
        {
            var hasRoom = HasRoom();
            if (hasRoom)
            {
                Mount(card, index);
            }
            return hasRoom;
        }

        public int? UnMount(Card card, SeekerSet seekersForUnmounting = null)
        { // returns index of card if found, -1 if not found
            int? index = null;
            if (MountedCards != null && MountedCards.Contains(card))
            {
                index = MountedCards.IndexOf(card);
                UnMount(index, seekersForUnmounting);
            }

            return index;
        }

        public Card UnMount(int? index = null, SeekerSet seekersForUnmounting = null)
        {
            var card = Get(index);
            if (card != null)
            {
                MountedCards.Remove(card);
                if (capture)
                {
                    capture = false;
                    CaptureCardsText.gameObject.SetActive(false);
                }
                card.Group = null;
                Strategy.Apply(MountedCards, seekerSets: new SeekerSetList { seekersForUnmounting });

                card.TriggerUnMountEvents(GroupRegistry.Instance?.GetGroupName(this) ?? GroupName.None);
                OnGroupChanged?.Invoke();
            }
            return card;
        }

        public Card Get(int? index = null)
        {
            if (MountedCards.Count == 0)
                return null;

            Card card;
            if (index == null || index >= MountedCards.Count)
            {
                card = MountedCards[MountedCards.Count - 1];
            }
            else if (index < 0)
            {
                card = MountedCards[0];
            }
            else
            {
                card = MountedCards[(int)index];
            }

            return card;
        }

        public List<Card> Get(GroupTargetType targetType, int count)
        {
            var output = new List<Card>();

            var pool = MountedCards.ToList();
            for (int i = 0; i < count; i++)
            {
                Card cardToAdd = null;
                if (pool.Count > 0)
                {
                    switch (targetType)
                    {
                        case GroupTargetType.First:
                            cardToAdd = pool[0];
                            break;
                        case GroupTargetType.Last:
                            cardToAdd = pool[pool.Count - 1];
                            break;
                        case GroupTargetType.Random:
                            cardToAdd = pool[UnityEngine.Random.Range(0, pool.Count - 1)];
                            break;
                    }
                }

                if (cardToAdd != null)
                {
                    pool.Remove(cardToAdd);
                    output.Add(cardToAdd);
                }

            }
            return output;
        }

        public int? IndexOf(Card card)
        {
            if (MountedCards.Contains(card))
            {
                return MountedCards.IndexOf(card);
            }
            return null;
        }

        public void Shuffle(bool isInstant = false)
        {
            var newMountedCards = new List<Card>();
            while (MountedCards.Count > 0)
            {
                var chosenIndex = UnityEngine.Random.Range(0, MountedCards.Count);
                newMountedCards.Add(MountedCards[chosenIndex]);
                MountedCards.RemoveAt(chosenIndex);
            }
            MountedCards = newMountedCards;

            var seekerSetList = new SeekerSetList();
            if (isInstant)
            {
                seekerSetList.Add(new SeekerSet { Homing = new InstantVector3Seeker(), Turning = new InstantFloatSeeker(), Scaling = new InstantFloatSeeker() });
            }
            else
            {
                foreach (var card in MountedCards)
                {
                    seekerSetList.Add(new SeekerSet { Card = card, Homing = ShuffleStrategy?.GetStrategy() ?? new ExponentialVector3Seeker() });
                }
            }

            foreach (var card in MountedCards)
            {
                if (card.CanBeUpsideDown)
                {
                    card.SetUpsideDown(UnityEngine.Random.Range(0f, 1f) < card.UpsideDownChance);
                }
            }

            Strategy.Apply(MountedCards, instaFlip: isInstant, seekerSets: seekerSetList);
        }

        public void ShuffleIn(List<Card> cards, bool isInstant = false)
        {
            if (cards.Count == 0)
                return;

            foreach (var card in cards.ToList())
            {
                Mount(card, instaFlip: isInstant);
            }
            Shuffle(isInstant);
        }


        //    public void CaptureMatchingCards(CardGroup boardGroup, CardGroup playerCaptureGroup)
        //    {
        //        var groupedCards = boardGroup.MountedCards
        //            .GroupBy(card => card.Value) // Use card.Suit if matching by suit
        //            .Where(group => group.Count() > 1) // Only consider groups with duplicates
        //            .ToList();

        //        foreach (var group in groupedCards)
        //        {
        //            Debug.Log($"Capturing {group.Count()} cards with value {group.Key}");

        //            foreach (var card in group)
        //            {
        //                boardGroup.UnMount(card);
        //                playerCaptureGroup.Mount(card);

        //                Debug.Log($"Card {card.name} captured and moved to {playerCaptureGroup.name}");
        //            }
        //        }

        //        boardGroup.OnGroupChanged?.Invoke();
        //        playerCaptureGroup.OnGroupChanged?.Invoke();
        //    }

        //}

    }

    public enum GroupTargetType
    {
        First,
        Last,
        Random
    }
}
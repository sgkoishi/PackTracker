// Author: Ellekappae <https://github.com/Ellekappae>
using HearthDb.Enums;
using Hearthstone_Deck_Tracker.Commands;
using PackTracker.Entity;
using PackTracker.Storage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using CardClass = HearthDb.Enums.CardClass;
using CardSet = HearthDb.Enums.CardSet;
using HDTCard = Hearthstone_Deck_Tracker.Hearthstone.Card;

namespace PackTracker.View
{
    internal class ManualPackInsert : INotifyPropertyChanged
    {
        private readonly History _history;
        private readonly IHistoryStorage _historyStorage;
        private DateTime? _selectedDateTime;
        private List<int> _sets;
        private int _selectedSet;
        private ObservableCollection<HDTCard> _cardsInCurrentSet = new ObservableCollection<HDTCard>();

        private ObservableCollection<CardViewModel> _packCards = new ObservableCollection<CardViewModel>();

        public DateTime? SelectedDateTime
        {
            get => this._selectedDateTime;
            set
            {
                this._selectedDateTime = value;
                this.OnPropertyChanged(nameof(this.SelectedDateTime));
            }
        }

        public int SelectedSet
        {
            get => this._selectedSet;
            set
            {
                this._selectedSet = value;
                this.RefreshCardsInCurrentSet();
                this.OnPropertyChanged(nameof(this.SelectedSet));
            }
        }

        public List<int> Sets
        {
            get => this._sets;
            set
            {
                this._sets = value;
                this.OnPropertyChanged(nameof(this.Sets));
            }
        }

        public ObservableCollection<HDTCard> CardsInCurrentSet
        {
            get => this._cardsInCurrentSet;
            set
            {
                this._cardsInCurrentSet = value;
                this.OnPropertyChanged(nameof(this.CardsInCurrentSet));
            }
        }

        public ObservableCollection<CardViewModel> PackCards
        {
            get => this._packCards;
            set
            {
                this._packCards = value;
                this.OnPropertyChanged(nameof(this.PackCards));
            }
        }

        public bool AddNewPackEnabled => this.SelectedDateTime != null && this.PackCards.All(c => c.HDTCard != null);

        private readonly Dictionary<int, List<HDTCard>> _setsCache = new Dictionary<int, List<HDTCard>>();

        internal static readonly List<int> GoldenPacks = new List<int> { 23, 603, 643, 686, 716, 737, 841, 850, 874, 904, 921, 932, 937, 938, 939, 945, 952, 970, 977, 985, 986, 990, 1031, 1040, 1045, 1048, 1055 };
        private static readonly Dictionary<int, Func<HearthDb.Card, bool>> _filter = new Dictionary<int, Func<HearthDb.Card, bool>>
        {
            [1] = card => card.Set == CardSet.EXPERT1,
            [9] = card => card.Set == CardSet.GVG,
            [10] = card => card.Set == CardSet.TGT,
            [11] = card => card.Set == CardSet.OG,
            [18] = card => card.Set == CardSet.EXPERT1,
            [19] = card => card.Set == CardSet.GANGS,
            [20] = card => card.Set == CardSet.UNGORO,
            [21] = card => card.Set == CardSet.ICECROWN,
            [23] = card => card.Set == CardSet.EXPERT1,
            [30] = card => card.Set == CardSet.LOOTAPALOOZA,
            [31] = card => card.Set == CardSet.GILNEAS,
            [38] = card => card.Set == CardSet.BOOMSDAY,
            [40] = card => card.Set == CardSet.TROLL,
            [41] = card => card.Set is CardSet.UNGORO or CardSet.ICECROWN or CardSet.LOOTAPALOOZA,
            [49] = card => card.Set == CardSet.DALARAN,
            [128] = card => card.Set == CardSet.ULDUM,
            [347] = card => card.Set == CardSet.DRAGONS,
            [423] = card => card.Set == CardSet.BLACK_TEMPLE,
            [465] = card => card.Set == CardSet.EXPERT1,
            [468] = card => card.Set == CardSet.SCHOLOMANCE,
            [470] = card => card.Class == CardClass.HUNTER && (int)card.Set > (int)CardSet.TROLL,
            [498] = card => card.Set is CardSet.DALARAN or CardSet.ULDUM or CardSet.DRAGONS,
            [545] = card => card.Class == CardClass.MAGE && (int)card.Set > (int)CardSet.TROLL,
            [553] = card => card.Set == CardSet.THE_BARRENS,
            [602] = card => card.Set == CardSet.STORMWIND,
            [603] = card => card.Set == CardSet.SCHOLOMANCE,
            [616] = card => card.Set == CardSet.DARKMOON_FAIRE,
            [629] = _ => true, // Mercenaries Pack, untrackable yet
            [631] = card => card.Class == CardClass.DRUID && (int)card.Set > (int)CardSet.TROLL,
            [632] = card => card.Class == CardClass.PALADIN && (int)card.Set > (int)CardSet.TROLL,
            [633] = card => card.Class == CardClass.WARRIOR && (int)card.Set > (int)CardSet.TROLL,
            [634] = card => card.Class == CardClass.PRIEST && (int)card.Set > (int)CardSet.TROLL,
            [635] = card => card.Class == CardClass.ROGUE && (int)card.Set > (int)CardSet.TROLL,
            [636] = card => card.Class == CardClass.SHAMAN && (int)card.Set > (int)CardSet.TROLL,
            [637] = card => card.Class == CardClass.WARLOCK && (int)card.Set > (int)CardSet.TROLL,
            [638] = card => card.Class == CardClass.DEMONHUNTER && (int)card.Set > (int)CardSet.TROLL,
            [643] = card => card.Set == CardSet.DARKMOON_FAIRE,
            [665] = card => card.Set == CardSet.ALTERAC_VALLEY,
            [686] = card => card.Set == CardSet.THE_BARRENS,
            [688] = card => card.Set is CardSet.BLACK_TEMPLE or CardSet.SCHOLOMANCE or CardSet.DARKMOON_FAIRE,
            [694] = card => card.Set == CardSet.THE_SUNKEN_CITY,
            [713] = _ => true, // Standard, may change over time
            [714] = _ => true, // Wild, may change over time
            [716] = _ => true, // Golden Standard, may change over time
            [729] = card => card.Set == CardSet.REVENDRETH,
            [737] = card => card.Set == CardSet.STORMWIND,
            [819] = card => card.Set == CardSet.TITANS,
            [821] = card => card.Set == CardSet.RETURN_OF_THE_LICH_KING,
            [841] = card => card.Set == CardSet.ALTERAC_VALLEY,
            [850] = card => card.Set == CardSet.THE_SUNKEN_CITY,
            [854] = card => card.Set == CardSet.BATTLE_OF_THE_BANDS,
            [874] = card => card.Set == CardSet.REVENDRETH,
            [894] = card => card.Set == CardSet.WONDERS,
            [904] = _ => true, // Golden Wild, may change over time
            [918] = card => card.Class == CardClass.DEATHKNIGHT && (int)card.Set > (int)CardSet.TROLL,
            [921] = card => card.Set == CardSet.RETURN_OF_THE_LICH_KING,
            [922] = card => card.Set == CardSet.WONDERS,
            [932] = card => card.Set == CardSet.BATTLE_OF_THE_BANDS,
            [933] = card => card.Set == CardSet.WHIZBANGS_WORKSHOP,
            [937] = card => card.Set == CardSet.TITANS,
            [938] = card => card.Set is CardSet.BLACK_TEMPLE or CardSet.SCHOLOMANCE or CardSet.DARKMOON_FAIRE,
            [939] = card => card.Set == CardSet.BLACK_TEMPLE,
            [941] = card => card.Set == CardSet.ISLAND_VACATION,
            [944] = card => card.Set is CardSet.THE_SUNKEN_CITY or CardSet.REVENDRETH or CardSet.RETURN_OF_THE_LICH_KING or CardSet.BATTLE_OF_THE_BANDS or CardSet.TITANS,
            [945] = card => card.Set == CardSet.WONDERS,
            [952] = card => card.Set == CardSet.WONDERS,
            [965] = card => card.Set is CardSet.SPACE,
            [970] = card => card.Set == CardSet.WHIZBANGS_WORKSHOP,
            [971] = card => card.Set is CardSet.BATTLE_OF_THE_BANDS or CardSet.TITANS or CardSet.WONDERS,
            [975] = card => card.Set is CardSet.EMERALD_DREAM,
            [977] = card => card.Set is CardSet.ISLAND_VACATION,
            [978] = card => card.Set is CardSet.BATTLE_OF_THE_BANDS or CardSet.TITANS or CardSet.WONDERS, // Why another Whizbang's Workshop Catch-up?
            [982] = card => card.Set is CardSet.THE_LOST_CITY,
            [984] = card => card.Set is CardSet.WHIZBANGS_WORKSHOP or CardSet.ISLAND_VACATION or CardSet.SPACE,
            [985] = card => card.Set is CardSet.WHIZBANGS_WORKSHOP or CardSet.ISLAND_VACATION or CardSet.SPACE,
            [986] = card => card.Set is CardSet.SPACE,
            [987] = card => card.Set is CardSet.BATTLE_OF_THE_BANDS or CardSet.TITANS or CardSet.WONDERS or CardSet.WHIZBANGS_WORKSHOP or CardSet.ISLAND_VACATION,
            [989] = card => card.Set == CardSet.TIME_TRAVEL,
            [990] = card => card.Set is CardSet.EMERALD_DREAM,
            [1030] = card => card.Set is CardSet.CATACLYSM,
            [1031] = card => card.Set is CardSet.CATACLYSM,
            [1033] = card => card.Set is CardSet.WHIZBANGS_WORKSHOP or CardSet.ISLAND_VACATION or CardSet.SPACE,
            [1040] = card => card.Set is CardSet.THE_LOST_CITY,
            [1044] = card => card.Set is CardSet.EMERALD_DREAM or CardSet.THE_LOST_CITY, // TODO: Year of the Raptor
            [1045] = card => card.Set is CardSet.EMERALD_DREAM or CardSet.THE_LOST_CITY, // TODO: Year of the Raptor Golden
            [1046] = card => card.Set is CardSet.WHIZBANGS_WORKSHOP or CardSet.ISLAND_VACATION or CardSet.SPACE or CardSet.EMERALD_DREAM,
            [1047] = card => card.Set == CardSet.ESCAPEFROM_VIOLET_HOLD,
            [1048] = card => card.Set == CardSet.ESCAPEFROM_VIOLET_HOLD,
            [1055] = card => card.Set == CardSet.TIME_TRAVEL,
            [1056] = card => card.Set is CardSet.WHIZBANGS_WORKSHOP or CardSet.ISLAND_VACATION or CardSet.SPACE or CardSet.EMERALD_DREAM or CardSet.THE_LOST_CITY,
            [1063] = card => card.Set is CardSet.EMERALD_DREAM or CardSet.THE_LOST_CITY or CardSet.TIME_TRAVEL,
            [1065] = card => card.Set is CardSet.EMERALD_DREAM or CardSet.THE_LOST_CITY or CardSet.TIME_TRAVEL or CardSet.CATACLYSM,
        };

        public ManualPackInsert()
        {
            this._selectedDateTime = DateTime.Now;

            this._sets = PackNameConverter.PackNames.Select(set => set.Key).ToList();

            this.SelectedSet = this._sets.FirstOrDefault();

            this.ResetPackCards();
        }

        public ManualPackInsert(History History) : this()
        {
            this._history = History;
            this._historyStorage = new XmlHistory();
        }

        private void RefreshCardsInCurrentSet()
        {
            this.CardsInCurrentSet.Clear();

            if (!this._setsCache.ContainsKey(this.SelectedSet))
            {
                this._setsCache[this.SelectedSet] = HearthDb.Cards.Collectible.Values
                    .Where(_filter.ContainsKey(this.SelectedSet) ? _filter[this.SelectedSet] : _ => false)
                    .OrderBy(card => card.Rarity)
                    .ThenBy(card => card.Cost)
                    .ThenBy(card => card.Name)
                    .Select(card => new HDTCard(card))
                    .ToList();
            }

            this._setsCache[this.SelectedSet].ForEach(c => this.CardsInCurrentSet.Add(c));
        }

        private ICommand _addNewPackCommand;

        public ICommand AddNewPackCommand
        {
            get
            {
                if (this._addNewPackCommand == null)
                {
                    this._addNewPackCommand = new Command(this.AddNewPack);
                }
                return this._addNewPackCommand;
            }
        }

        private void AddNewPack()
        {
            var Cards = new List<Entity.Card>();

            if (this.SelectedDateTime != null && this.PackCards.All(c => c.HDTCard != null))
            {
                this.PackCards.ToList().ForEach(c => Cards.Add(new Entity.Card(c.HDTCard, c.Premium)));

                var newPack = new Pack(this.SelectedSet, (DateTime)this.SelectedDateTime, Cards);

                this._history.Add(newPack);
                this._historyStorage.Store(this._history.Ascending);

                this.ClearData();
            }
        }

        private void ResetPackCards()
        {
            this.PackCards.Clear();

            Enumerable.Range(1, 5).ToList().ForEach(i => this.PackCards.Add(new CardViewModel()));

            foreach (var cardViewModel in this.PackCards)
            {
                cardViewModel.PropertyChanged += this.CardViewModel_PropertyChanged;
            }
        }

        private void CardViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CardViewModel.HDTCard))
            {
                this.OnPropertyChanged(nameof(this.AddNewPackEnabled));
            }
            if (GoldenPacks.Contains(this.SelectedSet))
            {
                foreach (var item in this.PackCards)
                {
                    if (!item.Premium)
                    {
                        item.Premium = true;
                    }
                }
            }
        }

        private void ClearData()
        {
            this.SelectedDateTime = DateTime.Now;

            this.ResetPackCards();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}

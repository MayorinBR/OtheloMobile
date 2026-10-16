using System.Collections.Generic;
using System.Linq;
using Othello.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Othello.Unity
{
    /// <summary>
    /// Shows every owned cosmetic, one category at a time behind the three
    /// tab buttons (<see cref="_pieceTabButton"/>, <see cref="_boardTabButton"/>,
    /// <see cref="_avatarTabButton"/>) — keeps the list short and lets the
    /// player find a given item without scrolling through every category
    /// mixed together.
    /// <para>
    /// "Owned" already includes the three <see cref="InventoryCatalog"/>
    /// items <see cref="PlayerSession"/> unlocks for every player up front
    /// (one per category), so a fresh account still has something to equip
    /// in each tab without a trip to the Shop.
    /// </para>
    /// </summary>
    public sealed class CustomizationScreen : UIScreen
    {
        [SerializeField]
        private PlayerSessionController _playerSession;

        [SerializeField]
        private RectTransform _listContainer;

        [SerializeField]
        private LocalizationController _localization;

        [SerializeField]
        private Button _pieceTabButton;

        [SerializeField]
        private Button _boardTabButton;

        [SerializeField]
        private Button _avatarTabButton;

        [SerializeField]
        private Button _backButton;

        [SerializeField]
        private Button _goToShopButton;

        private readonly List<GameObject> _spawnedRows = new();
        private ItemCategory _selectedCategory = ItemCategory.PieceSkin;

        private void Awake()
        {
            _backButton?.onClick.AddListener(() => Manager.GoBack());
            _goToShopButton?.onClick.AddListener(() => Manager.Show<ShopView>());
            _pieceTabButton?.onClick.AddListener(() => SelectCategory(ItemCategory.PieceSkin));
            _boardTabButton?.onClick.AddListener(() => SelectCategory(ItemCategory.BoardSkin));
            _avatarTabButton?.onClick.AddListener(() => SelectCategory(ItemCategory.Avatar));
        }

        protected override void OnShown() => Rebuild();

        private void SelectCategory(ItemCategory category)
        {
            if (_selectedCategory == category)
            {
                return;
            }

            _selectedCategory = category;
            Rebuild();
        }

        private void Rebuild()
        {
            foreach (var row in _spawnedRows)
            {
                Destroy(row);
            }
            _spawnedRows.Clear();

            UpdateTabButtons();

            if (_listContainer == null || _playerSession == null || _playerSession.Session == null)
            {
                return;
            }

            PlayerSession session = _playerSession.Session;
            var owned = InventoryCatalog.All.Where(item =>
                item.Category == _selectedCategory && session.UnlockedSkinIds.Contains(item.Id));

            foreach (var item in owned)
            {
                _spawnedRows.Add(BuildRow(item, session));
            }

            if (_spawnedRows.Count == 0)
            {
                _spawnedRows.Add(BuildEmptyRow());
            }
        }

        /// <summary>
        /// A tab reads as "current" by being non-interactable, the same
        /// visual language <see cref="ShopView"/> uses for an owned item's
        /// buy button — no extra art needed for a selection indicator.
        /// </summary>
        private void UpdateTabButtons()
        {
            if (_pieceTabButton != null)
            {
                _pieceTabButton.interactable = _selectedCategory != ItemCategory.PieceSkin;
            }
            if (_boardTabButton != null)
            {
                _boardTabButton.interactable = _selectedCategory != ItemCategory.BoardSkin;
            }
            if (_avatarTabButton != null)
            {
                _avatarTabButton.interactable = _selectedCategory != ItemCategory.Avatar;
            }
        }

        private GameObject BuildRow(ItemDefinition item, PlayerSession session)
        {
            var row = new GameObject($"Item_{item.Id}", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(_listContainer, false);

            // Same fix as ShopView.BuildRow: an unconfigured HorizontalLayoutGroup
            // leaves icon/label/button at their own uncontrolled size.
            var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.spacing = 12f;

            var rowElement = row.GetComponent<LayoutElement>();
            rowElement.preferredHeight = 72f;
            rowElement.flexibleWidth = 1f;
            rowElement.flexibleHeight = 0f; // stay at preferredHeight — Container's Child Force Expand (Height) would otherwise stretch each row to fill leftover space

            UIWidgets.CreateIcon(row.transform, CosmeticArt.LoadItemThumbnail(item));

            TMP_Text label = UIWidgets.CreateLabel(row.transform, _localization.Service.Get(item.NameKey));
            var labelElement = label.gameObject.AddComponent<LayoutElement>();
            labelElement.flexibleWidth = 1f;
            labelElement.minWidth = 100f;

            bool equipped = IsEquipped(item, session);
            var button = UIWidgets.CreateButton(
                row.transform,
                equipped
                    ? _localization.Service.Get(LocalizationKey.CustomizationEquipped)
                    : _localization.Service.Get(LocalizationKey.CustomizationEquip));
            button.interactable = !equipped;
            button.onClick.AddListener(() =>
            {
                if (session.TryEquip(item.Id))
                {
                    Rebuild();
                }
            });
            var buttonElement = button.gameObject.AddComponent<LayoutElement>();
            buttonElement.preferredWidth = 140f;
            buttonElement.minWidth = 100f;

            return row;
        }

        private GameObject BuildEmptyRow()
        {
            var row = new GameObject("EmptyRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(_listContainer, false);

            var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = false;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;

            var rowElement = row.GetComponent<LayoutElement>();
            rowElement.preferredHeight = 72f;
            rowElement.flexibleWidth = 1f;
            rowElement.flexibleHeight = 0f;

            UIWidgets.CreateLabel(row.transform, _localization.Service.Get(LocalizationKey.CustomizationEmpty), TextAlignmentOptions.Center);
            return row;
        }

        private static bool IsEquipped(ItemDefinition item, PlayerSession session) => item.Category switch
        {
            ItemCategory.PieceSkin => session.EquippedPieceSkinId == item.Id,
            ItemCategory.BoardSkin => session.EquippedBoardSkinId == item.Id,
            ItemCategory.Avatar => session.AvatarId == item.Id,
            _ => false
        };
    }
}

using System.Collections.Generic;
using Othello.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Othello.Unity
{
    /// <summary>
    /// Procedural list of every purchasable <see cref="InventoryCatalog"/>
    /// item, one category at a time behind the three tab buttons
    /// (<see cref="_pieceTabButton"/>, <see cref="_boardTabButton"/>,
    /// <see cref="_avatarTabButton"/>) — same split as
    /// <see cref="CustomizationScreen"/>, so a player looking for a
    /// specific kind of item doesn't have to scroll through everything
    /// else first. Free/bundled items are never listed here — every
    /// player already owns them, see <see cref="PlayerSession"/>.
    /// <para>
    /// Opening and closing are entirely <see cref="ScreenManager"/>'s job
    /// now: this class used to manage its own show/hide state, which is
    /// what let a single "Loja" button open the panel without any button
    /// ever being wired to close it again. Now the panel closes through the
    /// same <see cref="ScreenManager.GoBack"/> every other screen uses.
    /// </para>
    /// <para>
    /// Editor setup: assign <see cref="UIScreen"/>'s root field to this
    /// panel, <see cref="_listContainer"/> to an empty RectTransform with a
    /// <c>Vertical Layout Group</c>, and wire a "Fechar" button to
    /// <see cref="Close"/>.
    /// </para>
    /// </summary>
    public sealed class ShopView : UIScreen
    {
        [SerializeField]
        private ShopController _shopController;

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
        private Button _closeButton;

        private ShopService _shop;
        private readonly List<GameObject> _spawnedRows = new();
        private ItemCategory _selectedCategory = ItemCategory.PieceSkin;

        private void Awake()
        {
            _closeButton?.onClick.AddListener(Close);
            _pieceTabButton?.onClick.AddListener(() => SelectCategory(ItemCategory.PieceSkin));
            _boardTabButton?.onClick.AddListener(() => SelectCategory(ItemCategory.BoardSkin));
            _avatarTabButton?.onClick.AddListener(() => SelectCategory(ItemCategory.Avatar));
        }

        protected override void OnShown()
        {
            _shop = _shopController.GetShopService();
            Rebuild();
        }

        public void Close() => Manager.GoBack();

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

            foreach (var item in InventoryCatalog.All)
            {
                if (item.Category != _selectedCategory)
                {
                    continue;
                }

                // Free/bundled items (see InventoryCatalog.PieceSkinDefault
                // and friends) aren't for sale — every player already owns
                // them, so listing one here would be a permanently
                // "Possuído" row cluttering the list for no reason.
                if (item.Price.Kind == PriceKind.Free)
                {
                    continue;
                }

                _spawnedRows.Add(BuildRow(item));
            }
        }

        /// <summary>
        /// A tab reads as "current" by being non-interactable, the same
        /// visual language <see cref="CustomizationScreen"/> uses for its
        /// own category tabs — no extra art needed for a selection indicator.
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

        private GameObject BuildRow(ItemDefinition item)
        {
            var row = new GameObject($"Item_{item.Id}", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(_listContainer, false);

            // The row's own children (icon/label/button) only get positioned
            // and sized correctly if this layout group is told to control
            // them — Unity's default HorizontalLayoutGroup leaves every
            // child at its own uncontrolled RectTransform size, which is
            // what produced the near-zero-width label text seen before.
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

            bool owned = _shop.IsOwned(item.Id);
            var button = UIWidgets.CreateButton(
                row.transform, owned ? _localization.Service.Get(LocalizationKey.ShopOwned) : DescribePrice(item.Price));
            button.interactable = !owned;
            button.onClick.AddListener(() => Buy(item.Id));
            var buttonElement = button.gameObject.AddComponent<LayoutElement>();
            buttonElement.preferredWidth = 140f;
            buttonElement.minWidth = 100f;

            return row;
        }

        private void Buy(string itemId)
        {
            _shop.Purchase(itemId, success =>
            {
                if (success)
                {
                    Rebuild();
                }
                else
                {
                    Debug.Log($"Compra de '{itemId}' falhou (saldo insuficiente ou compra recusada).");
                }
            });
        }

        private string DescribePrice(ItemPrice price)
        {
            if (price.Kind != PriceKind.Currency)
            {
                return _localization.Service.Get(LocalizationKey.ShopBuy);
            }

            var currencyKey = price.CurrencyType == CurrencyType.Soft
                ? LocalizationKey.CurrencySoftName
                : LocalizationKey.CurrencyHardName;
            return $"{price.Amount} {_localization.Service.Get(currencyKey)}";
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using PokemonWolfBot.Models;

namespace PokemonWolfBot.Services
{
    /// <summary>
    /// خدمة إدارة البطاقات
    /// </summary>
    public class CardService
    {
        private readonly Dictionary<string, PokemonCard> _cards = new Dictionary<string, PokemonCard>();
        private readonly Random _random = new Random();
        
        public CardService()
        {
            LoadCards();
        }
        
        /// <summary>
        /// الحصول على بطاقة بالمعرف
        /// </summary>
        public PokemonCard? GetCard(string cardId)
        {
            return _cards.TryGetValue(cardId, out var card) ? card : null;
        }
        
        /// <summary>
        /// الحصول على جميع البطاقات
        /// </summary>
        public List<PokemonCard> GetAllCards()
        {
            return _cards.Values.ToList();
        }
        
        /// <summary>
        /// الحصول على بطاقة عشوائية بناءً على الندرة
        /// </summary>
        public PokemonCard GetRandomCard()
        {
            // احتمالات الندرة
            var roll = _random.NextDouble() * 100;
            
            CardRarity targetRarity;
            if (roll < 60) // 60%
                targetRarity = CardRarity.Common;
            else if (roll < 85) // 25%
                targetRarity = CardRarity.Uncommon;
            else if (roll < 95) // 10%
                targetRarity = CardRarity.Rare;
            else if (roll < 99) // 4%
                targetRarity = CardRarity.Epic;
            else // 1%
                targetRarity = CardRarity.Legendary;
            
            // الحصول على بطاقات من هذه الندرة
            var cardsOfRarity = _cards.Values
                .Where(c => c.Rarity == targetRarity)
                .ToList();
            
            if (cardsOfRarity.Count == 0)
            {
                // إذا لم توجد بطاقات من هذه الندرة، أعد أي بطاقة
                return _cards.Values.ElementAt(_random.Next(_cards.Count));
            }
            
            return cardsOfRarity[_random.Next(cardsOfRarity.Count)];
        }
        
        /// <summary>
        /// الحصول على ذهب عشوائي
        /// </summary>
        public int GetRandomGold()
        {
            // ذهب عشوائي بين 10 و 50
            return _random.Next(10, 51);
        }
        
        /// <summary>
        /// الحصول على بطاقات للمتجر
        /// </summary>
        public List<PokemonCard> GetShopCards()
        {
            // إرجاع جميع البطاقات مرتبة حسب السعر
            return _cards.Values
                .OrderBy(c => c.Price)
                .ToList();
        }
        
        /// <summary>
        /// الحصول على اسم الندرة بالعربية
        /// </summary>
        public static string GetRarityNameAr(CardRarity rarity)
        {
            return rarity switch
            {
                CardRarity.Common => "⚪ عادية",
                CardRarity.Uncommon => "🟢 غير عادية",
                CardRarity.Rare => "🔵 نادرة",
                CardRarity.Epic => "🟣 أسطورية",
                CardRarity.Legendary => "🟡 خرافية",
                _ => "❓ غير معروف"
            };
        }
        
        /// <summary>
        /// الحصول على رمز النوع
        /// </summary>
        public static string GetTypeEmoji(string type)
        {
            return type.ToLower() switch
            {
                "fire" => "🔥",
                "water" => "💧",
                "grass" => "🌿",
                "electric" => "⚡",
                "psychic" => "🔮",
                "dragon" => "🐉",
                "normal" => "⭐",
                "fighting" => "👊",
                "ghost" => "👻",
                "rock" => "🪨",
                _ => "❓"
            };
        }
        
        /// <summary>
        /// تحميل البطاقات من الملف
        /// </summary>
        private void LoadCards()
        {
            try
            {
                var dataFile = "Data/pokemon-cards.json";
                if (File.Exists(dataFile))
                {
                    var json = File.ReadAllText(dataFile);
                    var cards = JsonConvert.DeserializeObject<List<PokemonCard>>(json);
                    
                    if (cards != null)
                    {
                        foreach (var card in cards)
                        {
                            _cards[card.Id] = card;
                        }
                    }
                    
                    Console.WriteLine($"✅ تم تحميل {_cards.Count} بطاقة بوكيمون");
                }
                else
                {
                    Console.WriteLine($"❌ لم يتم العثور على ملف البطاقات: {dataFile}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ خطأ في تحميل البطاقات: {ex.Message}");
            }
        }
    }
}


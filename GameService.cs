using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PokemonWolfBot.Models;

namespace PokemonWolfBot.Services
{
    /// <summary>
    /// خدمة نظام اللعب والمعارك
    /// </summary>
    public class GameService
    {
        private readonly CardService _cardService;
        private readonly PlayerService _playerService;
        private readonly Random _random = new Random();
        
        // أسعار المعززات
        private const int PersonalBoostCost = 50;
        private const int GroupBoostCost = 300;
        
        public GameService(CardService cardService, PlayerService playerService)
        {
            _cardService = cardService;
            _playerService = playerService;
        }
        
        /// <summary>
        /// البحث عن بطاقة
        /// </summary>
        public SearchResult Search(Player player)
        {
            // التحقق من إمكانية البحث
            if (!player.Energy.CanSearch())
            {
                var timeUntilNext = player.Energy.GetTimeUntilNextSearch();
                return new SearchResult
                {
                    Success = false,
                    Message = $"⏰ يجب الانتظار {timeUntilNext.Minutes} دقيقة و {timeUntilNext.Seconds} ثانية للبحث مرة أخرى!"
                };
            }
            
            // استهلاك الطاقة
            if (!player.Energy.ConsumeEnergy())
            {
                return new SearchResult
                {
                    Success = false,
                    Message = "❌ ليس لديك طاقة كافية! استخدم !بوكيمون معزز لاستعادة الطاقة."
                };
            }
            
            // الحصول على بطاقة عشوائية
            var card = _cardService.GetRandomCard();
            var gold = _cardService.GetRandomGold();
            
            // إضافة البطاقة والذهب
            player.AddCard(card.Id);
            player.Gold += gold;
            player.Stats.TotalSearches++;
            player.Stats.CardsCollected++;
            player.Stats.TotalGoldEarned += gold;
            
            _playerService.SavePlayer(player);
            
            var rarityName = CardService.GetRarityNameAr(card.Rarity);
            var typeEmoji = CardService.GetTypeEmoji(card.Type);
            
            return new SearchResult
            {
                Success = true,
                Card = card,
                GoldEarned = gold,
                Message = $"🎉 وجدت بطاقة!\n\n" +
                         $"{typeEmoji} **{card.NameAr}** ({card.Name})\n" +
                         $"⚡ القوة: {card.BasePower}\n" +
                         $"✨ الندرة: {rarityName}\n" +
                         $"💰 +{gold} ذهب\n\n" +
                         $"⚡ الطاقة المتبقية: {player.Energy.CurrentEnergy}/3"
            };
        }
        
        /// <summary>
        /// شراء بطاقة من المتجر
        /// </summary>
        public PurchaseResult PurchaseCard(Player player, string cardId)
        {
            var card = _cardService.GetCard(cardId);
            if (card == null)
            {
                return new PurchaseResult
                {
                    Success = false,
                    Message = "❌ البطاقة غير موجودة!"
                };
            }
            
            if (player.Gold < card.Price)
            {
                return new PurchaseResult
                {
                    Success = false,
                    Message = $"❌ ليس لديك ذهب كافٍ! تحتاج {card.Price} ذهب ولديك {player.Gold} فقط."
                };
            }
            
            player.Gold -= card.Price;
            player.AddCard(cardId);
            player.Stats.TotalGoldSpent += card.Price;
            player.Stats.CardsCollected++;
            
            _playerService.SavePlayer(player);
            
            var typeEmoji = CardService.GetTypeEmoji(card.Type);
            
            return new PurchaseResult
            {
                Success = true,
                Card = card,
                Message = $"✅ تم شراء البطاقة!\n\n" +
                         $"{typeEmoji} **{card.NameAr}**\n" +
                         $"💰 الذهب المتبقي: {player.Gold}"
            };
        }
        
        /// <summary>
        /// تطوير بطاقة
        /// </summary>
        public UpgradeResult UpgradeCard(Player player, string cardId)
        {
            var playerCard = player.GetCard(cardId);
            if (playerCard == null)
            {
                return new UpgradeResult
                {
                    Success = false,
                    Message = "❌ لا تملك هذه البطاقة!"
                };
            }
            
            if (!playerCard.CanUpgrade())
            {
                return new UpgradeResult
                {
                    Success = false,
                    Message = "❌ البطاقة وصلت للمستوى الأقصى (3)!"
                };
            }
            
            var card = _cardService.GetCard(cardId);
            if (card == null)
            {
                return new UpgradeResult
                {
                    Success = false,
                    Message = "❌ خطأ في البطاقة!"
                };
            }
            
            var cost = card.GetUpgradeCost(playerCard.Level);
            if (player.Gold < cost)
            {
                return new UpgradeResult
                {
                    Success = false,
                    Message = $"❌ ليس لديك ذهب كافٍ! تحتاج {cost} ذهب ولديك {player.Gold} فقط."
                };
            }
            
            player.Gold -= cost;
            playerCard.Level++;
            player.Stats.TotalGoldSpent += cost;
            player.Stats.CardsUpgraded++;
            
            _playerService.SavePlayer(player);
            
            var typeEmoji = CardService.GetTypeEmoji(card.Type);
            var newPower = card.GetPowerAtLevel(playerCard.Level);
            
            return new UpgradeResult
            {
                Success = true,
                Message = $"⬆️ تم تطوير البطاقة!\n\n" +
                         $"{typeEmoji} **{card.NameAr}**\n" +
                         $"📊 المستوى: {playerCard.Level}/3\n" +
                         $"⚡ القوة الجديدة: {newPower}\n" +
                         $"💰 الذهب المتبقي: {player.Gold}"
            };
        }
        
        /// <summary>
        /// شراء معزز شخصي
        /// </summary>
        public BoostResult BuyPersonalBoost(Player player)
        {
            if (player.Gold < PersonalBoostCost)
            {
                return new BoostResult
                {
                    Success = false,
                    Message = $"❌ ليس لديك ذهب كافٍ! تحتاج {PersonalBoostCost} ذهب ولديك {player.Gold} فقط."
                };
            }
            
            player.Gold -= PersonalBoostCost;
            player.Energy.CurrentEnergy = 3;
            player.Energy.LastEnergyUpdate = DateTime.UtcNow;
            player.Stats.TotalGoldSpent += PersonalBoostCost;
            player.Stats.BoostsUsed++;
            
            _playerService.SavePlayer(player);
            
            return new BoostResult
            {
                Success = true,
                IsGroup = false,
                Message = $"⚡ تم استعادة طاقتك بالكامل!\n\n" +
                         $"🔋 الطاقة: 3/3\n" +
                         $"💰 الذهب المتبقي: {player.Gold}"
            };
        }
        
        /// <summary>
        /// شراء معزز جماعي (للغرفة)
        /// </summary>
        public BoostResult BuyGroupBoost(Player player, List<string> roomMemberIds)
        {
            if (player.Gold < GroupBoostCost)
            {
                return new BoostResult
                {
                    Success = false,
                    Message = $"❌ ليس لديك ذهب كافٍ! تحتاج {GroupBoostCost} ذهب ولديك {player.Gold} فقط."
                };
            }
            
            player.Gold -= GroupBoostCost;
            player.Stats.TotalGoldSpent += GroupBoostCost;
            player.Stats.BoostsUsed++;
            
            // استعادة طاقة جميع اللاعبين في الغرفة
            int boostedCount = 0;
            foreach (var memberId in roomMemberIds)
            {
                var member = _playerService.GetPlayer(memberId);
                if (member != null)
                {
                    member.Energy.CurrentEnergy = 3;
                    member.Energy.LastEnergyUpdate = DateTime.UtcNow;
                    _playerService.SavePlayer(member);
                    boostedCount++;
                }
            }
            
            _playerService.SavePlayer(player);
            
            return new BoostResult
            {
                Success = true,
                IsGroup = true,
                BoostedCount = boostedCount,
                Message = $"⚡💫 معزز جماعي!\n\n" +
                         $"👥 تم استعادة طاقة {boostedCount} لاعب في الغرفة!\n" +
                         $"🎉 شكراً لك {player.Username}!\n" +
                         $"💰 الذهب المتبقي: {player.Gold}"
            };
        }
        
        /// <summary>
        /// التبرع بالذهب للاعب آخر
        /// </summary>
        public DonationResult DonateGold(Player donor, Player recipient, int amount)
        {
            if (amount < 10)
            {
                return new DonationResult
                {
                    Success = false,
                    Message = "❌ الحد الأدنى للتبرع هو 10 ذهب!"
                };
            }
            
            if (donor.Gold < amount)
            {
                return new DonationResult
                {
                    Success = false,
                    Message = $"❌ ليس لديك ذهب كافٍ! لديك {donor.Gold} ذهب فقط."
                };
            }
            
            donor.Gold -= amount;
            recipient.Gold += amount;
            donor.Stats.GoldDonated += amount;
            recipient.Stats.GoldReceived += amount;
            
            _playerService.SavePlayer(donor);
            _playerService.SavePlayer(recipient);
            
            return new DonationResult
            {
                Success = true,
                Message = $"💝 تم التبرع بنجاح!\n\n" +
                         $"💰 تبرعت بـ {amount} ذهب لـ {recipient.Username}\n" +
                         $"💰 الذهب المتبقي: {donor.Gold}"
            };
        }
        
        /// <summary>
        /// مهاجمة لاعب آخر
        /// </summary>
        public BattleResult Battle(Player attacker, Player defender)
        {
            // التحقق من cooldown المعركة
            var timeSinceLastBattle = DateTime.UtcNow - attacker.LastBattleTime;
            if (timeSinceLastBattle.TotalMinutes < 5)
            {
                var remaining = TimeSpan.FromMinutes(5) - timeSinceLastBattle;
                return new BattleResult
                {
                    Success = false,
                    Message = $"⏰ يجب الانتظار {remaining.Minutes} دقيقة و {remaining.Seconds} ثانية للمعركة التالية!"
                };
            }
            
            var cardDatabase = _cardService.GetAllCards().ToDictionary(c => c.Id);
            
            var attackerPower = attacker.GetTotalPower(cardDatabase);
            var defenderPower = defender.GetTotalPower(cardDatabase);
            
            // إضافة عنصر عشوائي للمعركة
            var attackerRoll = _random.Next(0, 21); // 0-20
            var defenderRoll = _random.Next(0, 21);
            
            var attackerTotal = attackerPower + attackerRoll;
            var defenderTotal = defenderPower + defenderRoll;
            
            bool attackerWins = attackerTotal > defenderTotal;
            
            // حساب الذهب المسروق (10-30% من ذهب الخاسر)
            var goldStolen = 0;
            if (attackerWins && defender.Gold > 0)
            {
                var percentage = _random.Next(10, 31) / 100.0;
                goldStolen = (int)(defender.Gold * percentage);
                goldStolen = Math.Max(goldStolen, 10); // على الأقل 10 ذهب
                goldStolen = Math.Min(goldStolen, defender.Gold); // لا يتجاوز ذهب المدافع
                
                attacker.Gold += goldStolen;
                defender.Gold -= goldStolen;
                attacker.Stats.TotalGoldEarned += goldStolen;
            }
            else if (!attackerWins && attacker.Gold > 0)
            {
                var percentage = _random.Next(10, 31) / 100.0;
                goldStolen = (int)(attacker.Gold * percentage);
                goldStolen = Math.Max(goldStolen, 10);
                goldStolen = Math.Min(goldStolen, attacker.Gold);
                
                defender.Gold += goldStolen;
                attacker.Gold -= goldStolen;
                defender.Stats.TotalGoldEarned += goldStolen;
            }
            
            // تحديث الإحصائيات
            attacker.LastBattleTime = DateTime.UtcNow;
            attacker.Stats.TotalBattles++;
            defender.Stats.TotalBattles++;
            
            if (attackerWins)
            {
                attacker.Stats.BattlesWon++;
                defender.Stats.BattlesLost++;
            }
            else
            {
                defender.Stats.BattlesWon++;
                attacker.Stats.BattlesLost++;
            }
            
            _playerService.SavePlayer(attacker);
            _playerService.SavePlayer(defender);
            
            var result = new StringBuilder();
            result.AppendLine("⚔️ **معركة بوكيمون!**");
            result.AppendLine();
            result.AppendLine($"👤 {attacker.Username}");
            result.AppendLine($"⚡ القوة: {attackerPower} + 🎲 {attackerRoll} = **{attackerTotal}**");
            result.AppendLine();
            result.AppendLine("🆚");
            result.AppendLine();
            result.AppendLine($"👤 {defender.Username}");
            result.AppendLine($"⚡ القوة: {defenderPower} + 🎲 {defenderRoll} = **{defenderTotal}**");
            result.AppendLine();
            result.AppendLine("━━━━━━━━━━━━━━━━");
            result.AppendLine();
            
            if (attackerWins)
            {
                result.AppendLine($"🎉 **{attacker.Username} فاز!**");
                result.AppendLine($"💰 سرق {goldStolen} ذهب من {defender.Username}!");
            }
            else
            {
                result.AppendLine($"🎉 **{defender.Username} فاز!**");
                result.AppendLine($"💰 سرق {goldStolen} ذهب من {attacker.Username}!");
            }
            
            return new BattleResult
            {
                Success = true,
                AttackerWins = attackerWins,
                GoldStolen = goldStolen,
                Message = result.ToString()
            };
        }
    }
    
    // نماذج النتائج
    public class SearchResult
    {
        public bool Success { get; set; }
        public PokemonCard? Card { get; set; }
        public int GoldEarned { get; set; }
        public string Message { get; set; } = string.Empty;
    }
    
    public class PurchaseResult
    {
        public bool Success { get; set; }
        public PokemonCard? Card { get; set; }
        public string Message { get; set; } = string.Empty;
    }
    
    public class UpgradeResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
    
    public class BoostResult
    {
        public bool Success { get; set; }
        public bool IsGroup { get; set; }
        public int BoostedCount { get; set; }
        public string Message { get; set; } = string.Empty;
    }
    
    public class DonationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
    
    public class BattleResult
    {
        public bool Success { get; set; }
        public bool AttackerWins { get; set; }
        public int GoldStolen { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}


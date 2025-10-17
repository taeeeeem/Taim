using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonWolfBot.Models
{
    /// <summary>
    /// نموذج اللاعب
    /// </summary>
    public class Player
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public int Gold { get; set; } = 100; // ذهب البداية
        public List<PlayerCard> Cards { get; set; } = new List<PlayerCard>();
        public PlayerEnergy Energy { get; set; } = new PlayerEnergy();
        public DateTime LastBattleTime { get; set; } = DateTime.MinValue;
        public PlayerStats Stats { get; set; } = new PlayerStats();
        public string? AllianceId { get; set; } = null;
        public PlayerBoost? ActiveBoost { get; set; } = null;
        
        /// <summary>
        /// حساب القوة الإجمالية للاعب
        /// </summary>
        public int GetTotalPower(Dictionary<string, PokemonCard> cardDatabase)
        {
            int totalPower = 0;
            
            foreach (var playerCard in Cards)
            {
                if (cardDatabase.TryGetValue(playerCard.CardId, out var card))
                {
                    totalPower += card.GetPowerAtLevel(playerCard.Level);
                }
            }
            
            return totalPower;
        }
        
        /// <summary>
        /// الحصول على بطاقة معينة
        /// </summary>
        public PlayerCard? GetCard(string cardId)
        {
            return Cards.FirstOrDefault(c => c.CardId == cardId);
        }
        
        /// <summary>
        /// إضافة بطاقة جديدة
        /// </summary>
        public void AddCard(string cardId)
        {
            Cards.Add(new PlayerCard
            {
                CardId = cardId,
                Level = 1,
                AcquiredAt = DateTime.UtcNow
            });
        }
        
        /// <summary>
        /// هل يملك اللاعب البطاقة؟
        /// </summary>
        public bool HasCard(string cardId)
        {
            return Cards.Any(c => c.CardId == cardId);
        }
    }
    
    /// <summary>
    /// نظام الطاقة للاعب
    /// </summary>
    public class PlayerEnergy
    {
        public int CurrentEnergy { get; set; } = 3;
        public DateTime LastEnergyUpdate { get; set; } = DateTime.UtcNow;
        public DateTime LastSearchTime { get; set; } = DateTime.MinValue;
        
        private const int MaxEnergy = 3;
        private const int EnergyRegenMinutes = 15;
        private const int SearchCooldownMinutes = 5;
        
        /// <summary>
        /// تحديث الطاقة بناءً على الوقت
        /// </summary>
        public void UpdateEnergy()
        {
            var now = DateTime.UtcNow;
            var timeSinceLastUpdate = now - LastEnergyUpdate;
            
            // حساب عدد الطاقات المستعادة
            int energyToRestore = (int)(timeSinceLastUpdate.TotalMinutes / EnergyRegenMinutes);
            
            if (energyToRestore > 0)
            {
                CurrentEnergy = Math.Min(CurrentEnergy + energyToRestore, MaxEnergy);
                LastEnergyUpdate = now;
            }
        }
        
        /// <summary>
        /// هل يمكن البحث الآن؟
        /// </summary>
        public bool CanSearch()
        {
            UpdateEnergy();
            
            if (CurrentEnergy <= 0)
                return false;
            
            var timeSinceLastSearch = DateTime.UtcNow - LastSearchTime;
            return timeSinceLastSearch.TotalMinutes >= SearchCooldownMinutes;
        }
        
        /// <summary>
        /// استهلاك طاقة للبحث
        /// </summary>
        public bool ConsumeEnergy()
        {
            if (!CanSearch())
                return false;
            
            CurrentEnergy--;
            LastSearchTime = DateTime.UtcNow;
            return true;
        }
        
        /// <summary>
        /// الحصول على الوقت المتبقي للبحث التالي
        /// </summary>
        public TimeSpan GetTimeUntilNextSearch()
        {
            var timeSinceLastSearch = DateTime.UtcNow - LastSearchTime;
            var cooldownTime = TimeSpan.FromMinutes(SearchCooldownMinutes);
            
            if (timeSinceLastSearch >= cooldownTime)
                return TimeSpan.Zero;
            
            return cooldownTime - timeSinceLastSearch;
        }
        
        /// <summary>
        /// الحصول على الوقت المتبقي للطاقة التالية
        /// </summary>
        public TimeSpan GetTimeUntilNextEnergy()
        {
            if (CurrentEnergy >= MaxEnergy)
                return TimeSpan.Zero;
            
            var timeSinceLastUpdate = DateTime.UtcNow - LastEnergyUpdate;
            var regenTime = TimeSpan.FromMinutes(EnergyRegenMinutes);
            
            var timeInCurrentCycle = TimeSpan.FromTicks(timeSinceLastUpdate.Ticks % regenTime.Ticks);
            return regenTime - timeInCurrentCycle;
        }
    }
    
    /// <summary>
    /// إحصائيات اللاعب
    /// </summary>
    public class PlayerStats
    {
        public int TotalSearches { get; set; } = 0;
        public int TotalBattles { get; set; } = 0;
        public int BattlesWon { get; set; } = 0;
        public int BattlesLost { get; set; } = 0;
        public int TotalGoldEarned { get; set; } = 0;
        public int TotalGoldSpent { get; set; } = 0;
        public int CardsCollected { get; set; } = 0;
        public int CardsUpgraded { get; set; } = 0;
        public int BoostsUsed { get; set; } = 0;
        public int GoldDonated { get; set; } = 0;
        public int GoldReceived { get; set; } = 0;
        
        /// <summary>
        /// نسبة الفوز
        /// </summary>
        public double WinRate
        {
            get
            {
                if (TotalBattles == 0)
                    return 0;
                return (double)BattlesWon / TotalBattles * 100;
            }
        }
    }
    
    /// <summary>
    /// معزز الطاقة
    /// </summary>
    public class PlayerBoost
    {
        public BoostType Type { get; set; }
        public DateTime ActivatedAt { get; set; } = DateTime.UtcNow;
        public int Duration { get; set; } // بالدقائق
        
        /// <summary>
        /// هل المعزز لا يزال نشطاً؟
        /// </summary>
        public bool IsActive()
        {
            var elapsed = DateTime.UtcNow - ActivatedAt;
            return elapsed.TotalMinutes < Duration;
        }
        
        /// <summary>
        /// الوقت المتبقي للمعزز
        /// </summary>
        public TimeSpan GetTimeRemaining()
        {
            var elapsed = DateTime.UtcNow - ActivatedAt;
            var remaining = TimeSpan.FromMinutes(Duration) - elapsed;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }
    
    /// <summary>
    /// نوع المعزز
    /// </summary>
    public enum BoostType
    {
        EnergyRestore = 1,  // استعادة الطاقة
        PowerBoost = 2      // زيادة القوة
    }
}


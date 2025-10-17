using System;

namespace PokemonWolfBot.Models
{
    /// <summary>
    /// نموذج بطاقة بوكيمون
    /// </summary>
    public class PokemonCard
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public int BasePower { get; set; }
        public CardRarity Rarity { get; set; }
        public string Type { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public int Price { get; set; }
        
        /// <summary>
        /// حساب القوة بناءً على المستوى
        /// </summary>
        public int GetPowerAtLevel(int level)
        {
            if (level < 1 || level > 3)
                throw new ArgumentException("المستوى يجب أن يكون بين 1 و 3");
            
            // كل مستوى يزيد القوة بنسبة 50%
            return BasePower * level;
        }
        
        /// <summary>
        /// حساب تكلفة التطوير
        /// </summary>
        public int GetUpgradeCost(int currentLevel)
        {
            if (currentLevel >= 3)
                return 0; // لا يمكن التطوير بعد المستوى 3
            
            // تكلفة التطوير تزيد مع المستوى
            return Price * currentLevel;
        }
    }
    
    /// <summary>
    /// درجة ندرة البطاقة
    /// </summary>
    public enum CardRarity
    {
        Common = 1,      // عادية - 60% احتمال
        Uncommon = 2,    // غير عادية - 25% احتمال
        Rare = 3,        // نادرة - 10% احتمال
        Epic = 4,        // أسطورية - 4% احتمال
        Legendary = 5    // خرافية - 1% احتمال
    }
    
    /// <summary>
    /// بطاقة بوكيمون مملوكة من قبل لاعب
    /// </summary>
    public class PlayerCard
    {
        public string CardId { get; set; } = string.Empty;
        public int Level { get; set; } = 1;
        public DateTime AcquiredAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// هل يمكن تطوير البطاقة؟
        /// </summary>
        public bool CanUpgrade()
        {
            return Level < 3;
        }
    }
}


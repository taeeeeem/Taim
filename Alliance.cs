using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonWolfBot.Models
{
    /// <summary>
    /// نموذج التحالف
    /// </summary>
    public class Alliance
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string LeaderId { get; set; } = string.Empty;
        public List<string> MemberIds { get; set; } = new List<string>();
        public int Treasury { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public AllianceStats Stats { get; set; } = new AllianceStats();
        
        private const int MaxMembers = 10;
        
        /// <summary>
        /// هل التحالف ممتلئ؟
        /// </summary>
        public bool IsFull()
        {
            return MemberIds.Count >= MaxMembers;
        }
        
        /// <summary>
        /// هل المستخدم عضو في التحالف؟
        /// </summary>
        public bool IsMember(string userId)
        {
            return MemberIds.Contains(userId);
        }
        
        /// <summary>
        /// هل المستخدم قائد التحالف؟
        /// </summary>
        public bool IsLeader(string userId)
        {
            return LeaderId == userId;
        }
        
        /// <summary>
        /// إضافة عضو جديد
        /// </summary>
        public bool AddMember(string userId)
        {
            if (IsFull() || IsMember(userId))
                return false;
            
            MemberIds.Add(userId);
            return true;
        }
        
        /// <summary>
        /// إزالة عضو
        /// </summary>
        public bool RemoveMember(string userId)
        {
            if (IsLeader(userId))
                return false; // لا يمكن للقائد المغادرة
            
            return MemberIds.Remove(userId);
        }
        
        /// <summary>
        /// إضافة ذهب للخزينة
        /// </summary>
        public void AddToTreasury(int amount)
        {
            Treasury += amount;
            Stats.TotalDonations += amount;
        }
        
        /// <summary>
        /// سحب من الخزينة
        /// </summary>
        public bool WithdrawFromTreasury(int amount)
        {
            if (Treasury < amount)
                return false;
            
            Treasury -= amount;
            return true;
        }
    }
    
    /// <summary>
    /// إحصائيات التحالف
    /// </summary>
    public class AllianceStats
    {
        public int TotalBattles { get; set; } = 0;
        public int BattlesWon { get; set; } = 0;
        public int TotalDonations { get; set; } = 0;
        public int TotalPower { get; set; } = 0;
        
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
    /// دعوة للانضمام للتحالف
    /// </summary>
    public class AllianceInvite
    {
        public string AllianceId { get; set; } = string.Empty;
        public string InvitedUserId { get; set; } = string.Empty;
        public string InvitedByUserId { get; set; } = string.Empty;
        public DateTime InvitedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// هل انتهت صلاحية الدعوة؟
        /// </summary>
        public bool IsExpired()
        {
            return DateTime.UtcNow - InvitedAt > TimeSpan.FromHours(24);
        }
    }
}


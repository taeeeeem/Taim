using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using PokemonWolfBot.Models;

namespace PokemonWolfBot.Services
{
    /// <summary>
    /// خدمة إدارة التحالفات
    /// </summary>
    public class AllianceService
    {
        private readonly Dictionary<string, Alliance> _alliances = new Dictionary<string, Alliance>();
        private readonly Dictionary<string, AllianceInvite> _invites = new Dictionary<string, AllianceInvite>();
        private readonly string _dataFile = "Data/alliances.json";
        
        public AllianceService()
        {
            LoadAlliances();
        }
        
        /// <summary>
        /// إنشاء تحالف جديد
        /// </summary>
        public Alliance? CreateAlliance(string name, string leaderId)
        {
            // التحقق من أن الاسم غير مستخدم
            if (_alliances.Values.Any(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                return null;
            
            var alliance = new Alliance
            {
                Name = name,
                LeaderId = leaderId,
                MemberIds = new List<string> { leaderId }
            };
            
            _alliances[alliance.Id] = alliance;
            SaveAlliances();
            
            return alliance;
        }
        
        /// <summary>
        /// الحصول على تحالف بالمعرف
        /// </summary>
        public Alliance? GetAlliance(string allianceId)
        {
            return _alliances.TryGetValue(allianceId, out var alliance) ? alliance : null;
        }
        
        /// <summary>
        /// الحصول على تحالف اللاعب
        /// </summary>
        public Alliance? GetPlayerAlliance(string userId)
        {
            return _alliances.Values.FirstOrDefault(a => a.IsMember(userId));
        }
        
        /// <summary>
        /// دعوة لاعب للتحالف
        /// </summary>
        public bool InvitePlayer(string allianceId, string invitedUserId, string invitedByUserId)
        {
            var alliance = GetAlliance(allianceId);
            if (alliance == null || alliance.IsFull())
                return false;
            
            // التحقق من أن المدعو ليس عضواً بالفعل
            if (alliance.IsMember(invitedUserId))
                return false;
            
            // التحقق من أن المدعو ليس في تحالف آخر
            if (GetPlayerAlliance(invitedUserId) != null)
                return false;
            
            var invite = new AllianceInvite
            {
                AllianceId = allianceId,
                InvitedUserId = invitedUserId,
                InvitedByUserId = invitedByUserId
            };
            
            _invites[invitedUserId] = invite;
            return true;
        }
        
        /// <summary>
        /// قبول دعوة التحالف
        /// </summary>
        public Alliance? AcceptInvite(string userId)
        {
            if (!_invites.TryGetValue(userId, out var invite))
                return null;
            
            if (invite.IsExpired())
            {
                _invites.Remove(userId);
                return null;
            }
            
            var alliance = GetAlliance(invite.AllianceId);
            if (alliance == null || alliance.IsFull())
            {
                _invites.Remove(userId);
                return null;
            }
            
            if (alliance.AddMember(userId))
            {
                _invites.Remove(userId);
                SaveAlliances();
                return alliance;
            }
            
            return null;
        }
        
        /// <summary>
        /// رفض دعوة التحالف
        /// </summary>
        public bool DeclineInvite(string userId)
        {
            return _invites.Remove(userId);
        }
        
        /// <summary>
        /// الحصول على دعوة اللاعب
        /// </summary>
        public AllianceInvite? GetPlayerInvite(string userId)
        {
            if (_invites.TryGetValue(userId, out var invite) && !invite.IsExpired())
                return invite;
            
            if (invite != null && invite.IsExpired())
                _invites.Remove(userId);
            
            return null;
        }
        
        /// <summary>
        /// مغادرة التحالف
        /// </summary>
        public bool LeaveAlliance(string userId)
        {
            var alliance = GetPlayerAlliance(userId);
            if (alliance == null)
                return false;
            
            if (alliance.IsLeader(userId))
            {
                // إذا كان القائد، حذف التحالف بالكامل
                _alliances.Remove(alliance.Id);
            }
            else
            {
                alliance.RemoveMember(userId);
            }
            
            SaveAlliances();
            return true;
        }
        
        /// <summary>
        /// التبرع لخزينة التحالف
        /// </summary>
        public bool DonateToAlliance(string userId, int amount)
        {
            var alliance = GetPlayerAlliance(userId);
            if (alliance == null)
                return false;
            
            alliance.AddToTreasury(amount);
            SaveAlliances();
            return true;
        }
        
        /// <summary>
        /// الحصول على أفضل التحالفات
        /// </summary>
        public List<Alliance> GetTopAlliances(int count = 10)
        {
            return _alliances.Values
                .OrderByDescending(a => a.Stats.TotalPower)
                .ThenByDescending(a => a.Stats.BattlesWon)
                .Take(count)
                .ToList();
        }
        
        /// <summary>
        /// حفظ التحالف
        /// </summary>
        public void SaveAlliance(Alliance alliance)
        {
            _alliances[alliance.Id] = alliance;
            SaveAlliances();
        }
        
        /// <summary>
        /// تحميل التحالفات من الملف
        /// </summary>
        private void LoadAlliances()
        {
            try
            {
                if (File.Exists(_dataFile))
                {
                    var json = File.ReadAllText(_dataFile);
                    var alliances = JsonConvert.DeserializeObject<List<Alliance>>(json);
                    
                    if (alliances != null)
                    {
                        foreach (var alliance in alliances)
                        {
                            _alliances[alliance.Id] = alliance;
                        }
                    }
                    
                    Console.WriteLine($"✅ تم تحميل {_alliances.Count} تحالف");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ خطأ في تحميل التحالفات: {ex.Message}");
            }
        }
        
        /// <summary>
        /// حفظ التحالفات إلى الملف
        /// </summary>
        private void SaveAlliances()
        {
            try
            {
                var directory = Path.GetDirectoryName(_dataFile);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                var json = JsonConvert.SerializeObject(_alliances.Values.ToList(), Formatting.Indented);
                File.WriteAllText(_dataFile, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ خطأ في حفظ التحالفات: {ex.Message}");
            }
        }
    }
}


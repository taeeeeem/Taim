using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using PokemonWolfBot.Models;

namespace PokemonWolfBot.Services
{
    /// <summary>
    /// خدمة إدارة اللاعبين
    /// </summary>
    public class PlayerService
    {
        private readonly Dictionary<string, Player> _players = new Dictionary<string, Player>();
        private readonly string _dataFile = "Data/players.json";
        
        public PlayerService()
        {
            LoadPlayers();
        }
        
        /// <summary>
        /// الحصول على لاعب أو إنشاء واحد جديد
        /// </summary>
        public Player GetOrCreatePlayer(string userId, string username)
        {
            if (_players.TryGetValue(userId, out var player))
            {
                // تحديث الطاقة
                player.Energy.UpdateEnergy();
                return player;
            }
            
            // إنشاء لاعب جديد
            player = new Player
            {
                UserId = userId,
                Username = username,
                Gold = 100,
                Cards = new List<PlayerCard>(),
                Energy = new PlayerEnergy(),
                Stats = new PlayerStats()
            };
            
            _players[userId] = player;
            SavePlayers();
            
            return player;
        }
        
        /// <summary>
        /// الحصول على لاعب
        /// </summary>
        public Player? GetPlayer(string userId)
        {
            if (_players.TryGetValue(userId, out var player))
            {
                player.Energy.UpdateEnergy();
                return player;
            }
            return null;
        }
        
        /// <summary>
        /// حفظ تغييرات اللاعب
        /// </summary>
        public void SavePlayer(Player player)
        {
            _players[player.UserId] = player;
            SavePlayers();
        }
        
        /// <summary>
        /// الحصول على جميع اللاعبين
        /// </summary>
        public List<Player> GetAllPlayers()
        {
            return _players.Values.ToList();
        }
        
        /// <summary>
        /// الحصول على أفضل اللاعبين
        /// </summary>
        public List<Player> GetTopPlayers(int count = 10)
        {
            return _players.Values
                .OrderByDescending(p => p.Stats.BattlesWon)
                .ThenByDescending(p => p.Gold)
                .Take(count)
                .ToList();
        }
        
        /// <summary>
        /// تحميل اللاعبين من الملف
        /// </summary>
        private void LoadPlayers()
        {
            try
            {
                if (File.Exists(_dataFile))
                {
                    var json = File.ReadAllText(_dataFile);
                    var players = JsonConvert.DeserializeObject<List<Player>>(json);
                    
                    if (players != null)
                    {
                        foreach (var player in players)
                        {
                            _players[player.UserId] = player;
                        }
                    }
                    
                    Console.WriteLine($"✅ تم تحميل {_players.Count} لاعب");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ خطأ في تحميل اللاعبين: {ex.Message}");
            }
        }
        
        /// <summary>
        /// حفظ اللاعبين إلى الملف
        /// </summary>
        private void SavePlayers()
        {
            try
            {
                var directory = Path.GetDirectoryName(_dataFile);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                var json = JsonConvert.SerializeObject(_players.Values.ToList(), Formatting.Indented);
                File.WriteAllText(_dataFile, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ خطأ في حفظ اللاعبين: {ex.Message}");
            }
        }
    }
}


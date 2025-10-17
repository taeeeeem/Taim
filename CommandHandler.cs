using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TehGM.Wolfringo;
using TehGM.Wolfringo.Messages;
using PokemonWolfBot.Models;

namespace PokemonWolfBot.Services
{
    /// <summary>
    /// معالج أوامر البوت
    /// </summary>
    public class CommandHandler
    {
        private readonly IWolfClient _client;
        private readonly PlayerService _playerService;
        private readonly CardService _cardService;
        private readonly GameService _gameService;
        private readonly AllianceService _allianceService;
        
        public CommandHandler(
            IWolfClient client,
            PlayerService playerService,
            CardService cardService,
            GameService gameService,
            AllianceService allianceService)
        {
            _client = client;
            _playerService = playerService;
            _cardService = cardService;
            _gameService = gameService;
            _allianceService = allianceService;
        }
        
        /// <summary>
        /// معالجة الرسالة
        /// </summary>
        public async Task HandleMessageAsync(ChatMessage message)
        {
            try
            {
                var text = message.Text?.Trim() ?? string.Empty;
                
                // تجاهل الرسائل الفارغة أو من البوت نفسه
                if (string.IsNullOrWhiteSpace(text) || message.SenderID == _client.CurrentUserID)
                    return;
                
                // التحقق من أن الرسالة تبدأ بـ !بوكيمون
                if (!text.StartsWith("!بوكيمون", StringComparison.OrdinalIgnoreCase))
                    return;
                
                var player = _playerService.GetOrCreatePlayer(
                    message.SenderID.ToString(),
                    "لاعب");
                
                var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var command = parts.Length > 1 ? parts[1].ToLower() : "";
                
                string response = command switch
                {
                    "بحث" => await HandleSearchCommand(player),
                    "عرض" => HandleProfileCommand(player),
                    "متجر" => HandleShopCommand(),
                    "شراء" => HandlePurchaseCommand(player, parts),
                    "تطوير" => HandleUpgradeCommand(player, parts),
                    "هجوم" => await HandleBattleCommand(player, message, parts),
                    "معزز" => HandleBoostCommand(player),
                    "معزز_جماعي" => await HandleGroupBoostCommand(player, message),
                    "تبرع" => await HandleDonateCommand(player, message, parts),
                    "تحالف" => await HandleAllianceCommand(player, parts),
                    "مساعدة" or "help" => GetHelpMessage(),
                    _ => "❓ أمر غير معروف! اكتب `!بوكيمون مساعدة` لعرض الأوامر المتاحة."
                };
                
                await _client.ReplyTextAsync(message, response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ خطأ في معالجة الرسالة: {ex.Message}");
                await _client.ReplyTextAsync(message, "❌ حدث خطأ! حاول مرة أخرى.");
            }
        }
        
        private async Task<string> HandleSearchCommand(Player player)
        {
            var result = _gameService.Search(player);
            return result.Message;
        }
        
        private string HandleProfileCommand(Player player)
        {
            var cardDatabase = _cardService.GetAllCards().ToDictionary(c => c.Id);
            var totalPower = player.GetTotalPower(cardDatabase);
            
            var sb = new StringBuilder();
            sb.AppendLine($"👤 **{player.Username}**");
            sb.AppendLine();
            sb.AppendLine($"💰 الذهب: {player.Gold}");
            sb.AppendLine($"⚡ القوة الإجمالية: {totalPower}");
            sb.AppendLine($"🔋 الطاقة: {player.Energy.CurrentEnergy}/3");
            sb.AppendLine($"🃏 عدد البطاقات: {player.Cards.Count}");
            sb.AppendLine();
            sb.AppendLine("📊 **الإحصائيات:**");
            sb.AppendLine($"🔍 عمليات بحث: {player.Stats.TotalSearches}");
            sb.AppendLine($"⚔️ معارك: {player.Stats.TotalBattles}");
            sb.AppendLine($"🏆 انتصارات: {player.Stats.BattlesWon}");
            sb.AppendLine($"💔 هزائم: {player.Stats.BattlesLost}");
            sb.AppendLine($"📈 نسبة الفوز: {player.Stats.WinRate:F1}%");
            
            // عرض التحالف
            var alliance = _allianceService.GetPlayerAlliance(player.UserId);
            if (alliance != null)
            {
                sb.AppendLine();
                sb.AppendLine($"🤝 التحالف: **{alliance.Name}**");
                sb.AppendLine($"👥 الأعضاء: {alliance.MemberIds.Count}/10");
            }
            
            // عرض أفضل 5 بطاقات
            if (player.Cards.Any())
            {
                sb.AppendLine();
                sb.AppendLine("🌟 **أفضل بطاقاتك:**");
                
                var topCards = player.Cards
                    .Select(pc => new { PlayerCard = pc, Card = _cardService.GetCard(pc.CardId) })
                    .Where(x => x.Card != null)
                    .OrderByDescending(x => x.Card!.GetPowerAtLevel(x.PlayerCard.Level))
                    .Take(5);
                
                foreach (var item in topCards)
                {
                    var card = item.Card!;
                    var pc = item.PlayerCard;
                    var typeEmoji = CardService.GetTypeEmoji(card.Type);
                    var power = card.GetPowerAtLevel(pc.Level);
                    sb.AppendLine($"{typeEmoji} {card.NameAr} - المستوى {pc.Level} - القوة {power}");
                }
            }
            
            return sb.ToString();
        }
        
        private string HandleShopCommand()
        {
            var cards = _cardService.GetShopCards();
            
            var sb = new StringBuilder();
            sb.AppendLine("🏪 **متجر البطاقات**");
            sb.AppendLine();
            sb.AppendLine("استخدم `!بوكيمون شراء [معرف]` لشراء بطاقة");
            sb.AppendLine();
            
            foreach (var card in cards)
            {
                var typeEmoji = CardService.GetTypeEmoji(card.Type);
                var rarityName = CardService.GetRarityNameAr(card.Rarity);
                sb.AppendLine($"🆔 `{card.Id}`");
                sb.AppendLine($"{typeEmoji} **{card.NameAr}** ({card.Name})");
                sb.AppendLine($"⚡ القوة: {card.BasePower} | ✨ {rarityName}");
                sb.AppendLine($"💰 السعر: {card.Price} ذهب");
                sb.AppendLine();
            }
            
            return sb.ToString();
        }
        
        private string HandlePurchaseCommand(Player player, string[] parts)
        {
            if (parts.Length < 3)
                return "❌ استخدام خاطئ! استخدم: `!بوكيمون شراء [معرف_البطاقة]`";
            
            var cardId = parts[2].ToLower();
            var result = _gameService.PurchaseCard(player, cardId);
            return result.Message;
        }
        
        private string HandleUpgradeCommand(Player player, string[] parts)
        {
            if (parts.Length < 3)
                return "❌ استخدام خاطئ! استخدم: `!بوكيمون تطوير [معرف_البطاقة]`";
            
            var cardId = parts[2].ToLower();
            var result = _gameService.UpgradeCard(player, cardId);
            return result.Message;
        }
        
        private async Task<string> HandleBattleCommand(Player attacker, ChatMessage message, string[] parts)
        {
            // البحث عن المدافع من الـ mentions
            if (message.RecipientID == null)
                return "❌ يجب تحديد اللاعب المراد مهاجمته! استخدم: `!بوكيمون هجوم @اسم_اللاعب`";
            
            var defenderId = message.RecipientID.ToString();
            var defender = _playerService.GetPlayer(defenderId);
            
            if (defender == null)
                return "❌ اللاعب المستهدف غير موجود في النظام!";
            
            if (attacker.UserId == defenderId)
                return "❌ لا يمكنك مهاجمة نفسك!";
            
            var result = _gameService.Battle(attacker, defender);
            return result.Message;
        }
        
        private string HandleBoostCommand(Player player)
        {
            var result = _gameService.BuyPersonalBoost(player);
            return result.Message;
        }
        
        private async Task<string> HandleGroupBoostCommand(Player player, ChatMessage message)
        {
            // TODO: الحصول على قائمة أعضاء الغرفة من Wolf API
            // حالياً سنستخدم قائمة فارغة كمثال
            var roomMembers = new System.Collections.Generic.List<string>();
            
            var result = _gameService.BuyGroupBoost(player, roomMembers);
            return result.Message;
        }
        
        private async Task<string> HandleDonateCommand(Player donor, ChatMessage message, string[] parts)
        {
            if (parts.Length < 3)
                return "❌ استخدام خاطئ! استخدم: `!بوكيمون تبرع @اسم_اللاعب [المبلغ]`";
            
            if (message.RecipientID == null)
                return "❌ يجب تحديد اللاعب المراد التبرع له!";
            
            if (!int.TryParse(parts[2], out var amount))
                return "❌ المبلغ يجب أن يكون رقماً!";
            
            var recipientId = message.RecipientID.ToString();
            var recipient = _playerService.GetPlayer(recipientId);
            
            if (recipient == null)
                return "❌ اللاعب المستهدف غير موجود في النظام!";
            
            if (donor.UserId == recipientId)
                return "❌ لا يمكنك التبرع لنفسك!";
            
            var result = _gameService.DonateGold(donor, recipient, amount);
            return result.Message;
        }
        
        private async Task<string> HandleAllianceCommand(Player player, string[] parts)
        {
            if (parts.Length < 3)
                return GetAllianceHelp();
            
            var subCommand = parts[2].ToLower();
            
            return subCommand switch
            {
                "إنشاء" => HandleAllianceCreate(player, parts),
                "دعوة" => HandleAllianceInvite(player, parts),
                "قبول" => HandleAllianceAccept(player),
                "رفض" => HandleAllianceDecline(player),
                "مغادرة" => HandleAllianceLeave(player),
                "عرض" => HandleAllianceShow(player),
                "تبرع" => HandleAllianceDonate(player, parts),
                _ => GetAllianceHelp()
            };
        }
        
        private string HandleAllianceCreate(Player player, string[] parts)
        {
            if (parts.Length < 4)
                return "❌ استخدام خاطئ! استخدم: `!بوكيمون تحالف إنشاء [اسم_التحالف]`";
            
            var existingAlliance = _allianceService.GetPlayerAlliance(player.UserId);
            if (existingAlliance != null)
                return "❌ أنت بالفعل عضو في تحالف! يجب المغادرة أولاً.";
            
            var allianceName = string.Join(" ", parts.Skip(3));
            var alliance = _allianceService.CreateAlliance(allianceName, player.UserId);
            
            if (alliance == null)
                return "❌ اسم التحالف مستخدم بالفعل!";
            
            player.AllianceId = alliance.Id;
            _playerService.SavePlayer(player);
            
            return $"🎉 تم إنشاء التحالف **{alliance.Name}** بنجاح!\n" +
                   $"👑 أنت الآن قائد التحالف!\n" +
                   $"👥 استخدم `!بوكيمون تحالف دعوة @اسم` لدعوة أعضاء.";
        }
        
        private string HandleAllianceInvite(Player player, string[] parts)
        {
            var alliance = _allianceService.GetPlayerAlliance(player.UserId);
            if (alliance == null)
                return "❌ أنت لست عضواً في أي تحالف!";
            
            if (!alliance.IsLeader(player.UserId))
                return "❌ فقط قائد التحالف يمكنه دعوة أعضاء!";
            
            // TODO: استخراج معرف المستخدم المدعو من الـ mention
            return "⏳ ميزة الدعوة قيد التطوير...";
        }
        
        private string HandleAllianceAccept(Player player)
        {
            var invite = _allianceService.GetPlayerInvite(player.UserId);
            if (invite == null)
                return "❌ ليس لديك دعوات معلقة!";
            
            var alliance = _allianceService.AcceptInvite(player.UserId);
            if (alliance == null)
                return "❌ فشل قبول الدعوة! ربما التحالف ممتلئ.";
            
            player.AllianceId = alliance.Id;
            _playerService.SavePlayer(player);
            
            return $"🎉 انضممت إلى التحالف **{alliance.Name}**!\n" +
                   $"👥 عدد الأعضاء: {alliance.MemberIds.Count}/10";
        }
        
        private string HandleAllianceDecline(Player player)
        {
            if (_allianceService.DeclineInvite(player.UserId))
                return "✅ تم رفض الدعوة.";
            
            return "❌ ليس لديك دعوات معلقة!";
        }
        
        private string HandleAllianceLeave(Player player)
        {
            var alliance = _allianceService.GetPlayerAlliance(player.UserId);
            if (alliance == null)
                return "❌ أنت لست عضواً في أي تحالف!";
            
            if (_allianceService.LeaveAlliance(player.UserId))
            {
                player.AllianceId = null;
                _playerService.SavePlayer(player);
                
                if (alliance.IsLeader(player.UserId))
                    return "✅ تم حذف التحالف بنجاح.";
                
                return "✅ غادرت التحالف بنجاح.";
            }
            
            return "❌ فشلت المغادرة!";
        }
        
        private string HandleAllianceShow(Player player)
        {
            var alliance = _allianceService.GetPlayerAlliance(player.UserId);
            if (alliance == null)
                return "❌ أنت لست عضواً في أي تحالف!";
            
            var sb = new StringBuilder();
            sb.AppendLine($"🤝 **{alliance.Name}**");
            sb.AppendLine();
            sb.AppendLine($"👑 القائد: {alliance.LeaderId}");
            sb.AppendLine($"👥 الأعضاء: {alliance.MemberIds.Count}/10");
            sb.AppendLine($"💰 الخزينة: {alliance.Treasury} ذهب");
            sb.AppendLine();
            sb.AppendLine("📊 **الإحصائيات:**");
            sb.AppendLine($"⚔️ معارك: {alliance.Stats.TotalBattles}");
            sb.AppendLine($"🏆 انتصارات: {alliance.Stats.BattlesWon}");
            sb.AppendLine($"📈 نسبة الفوز: {alliance.Stats.WinRate:F1}%");
            sb.AppendLine($"💝 إجمالي التبرعات: {alliance.Stats.TotalDonations}");
            
            return sb.ToString();
        }
        
        private string HandleAllianceDonate(Player player, string[] parts)
        {
            if (parts.Length < 4)
                return "❌ استخدام خاطئ! استخدم: `!بوكيمون تحالف تبرع [المبلغ]`";
            
            var alliance = _allianceService.GetPlayerAlliance(player.UserId);
            if (alliance == null)
                return "❌ أنت لست عضواً في أي تحالف!";
            
            if (!int.TryParse(parts[3], out var amount))
                return "❌ المبلغ يجب أن يكون رقماً!";
            
            if (amount < 10)
                return "❌ الحد الأدنى للتبرع هو 10 ذهب!";
            
            if (player.Gold < amount)
                return $"❌ ليس لديك ذهب كافٍ! لديك {player.Gold} ذهب فقط.";
            
            player.Gold -= amount;
            _allianceService.DonateToAlliance(player.UserId, amount);
            _playerService.SavePlayer(player);
            
            return $"💝 تم التبرع بـ {amount} ذهب لخزينة التحالف!\n" +
                   $"💰 الذهب المتبقي: {player.Gold}\n" +
                   $"🏦 خزينة التحالف: {alliance.Treasury + amount}";
        }
        
        private string GetAllianceHelp()
        {
            return "🤝 **أوامر التحالف:**\n\n" +
                   "`!بوكيمون تحالف إنشاء [اسم]` - إنشاء تحالف جديد\n" +
                   "`!بوكيمون تحالف دعوة @مستخدم` - دعوة لاعب\n" +
                   "`!بوكيمون تحالف قبول` - قبول دعوة\n" +
                   "`!بوكيمون تحالف رفض` - رفض دعوة\n" +
                   "`!بوكيمون تحالف مغادرة` - مغادرة التحالف\n" +
                   "`!بوكيمون تحالف عرض` - عرض معلومات التحالف\n" +
                   "`!بوكيمون تحالف تبرع [مبلغ]` - التبرع للخزينة";
        }
        
        private string GetHelpMessage()
        {
            return "🎮 **بوت بوكيمون - الأوامر المتاحة:**\n\n" +
                   "🔍 `!بوكيمون بحث` - البحث عن بطاقات وذهب\n" +
                   "👤 `!بوكيمون عرض` - عرض ملفك الشخصي\n" +
                   "🏪 `!بوكيمون متجر` - عرض متجر البطاقات\n" +
                   "💳 `!بوكيمون شراء [معرف]` - شراء بطاقة\n" +
                   "⬆️ `!بوكيمون تطوير [معرف]` - تطوير بطاقة\n" +
                   "⚔️ `!بوكيمون هجوم @مستخدم` - مهاجمة لاعب\n" +
                   "⚡ `!بوكيمون معزز` - استعادة الطاقة (50 ذهب)\n" +
                   "💫 `!بوكيمون معزز_جماعي` - معزز للغرفة (300 ذهب)\n" +
                   "💰 `!بوكيمون تبرع @مستخدم [مبلغ]` - تبرع بالذهب\n" +
                   "🤝 `!بوكيمون تحالف` - أوامر التحالف\n" +
                   "❓ `!بوكيمون مساعدة` - عرض هذه الرسالة\n\n" +
                   "━━━━━━━━━━━━━━━━\n" +
                   "⚡ الطاقة: 3 محاولات كحد أقصى\n" +
                   "🔄 التجديد: كل 15 دقيقة\n" +
                   "⏱️ Cooldown البحث: 5 دقائق\n" +
                   "⏱️ Cooldown المعركة: 5 دقائق";
        }
    }
}


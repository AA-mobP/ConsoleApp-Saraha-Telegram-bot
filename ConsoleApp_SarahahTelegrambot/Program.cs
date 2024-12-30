using Microsoft.EntityFrameworkCore;
using System;
using System.Net.Http.Headers;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;



namespace ConsoleApp_SarahahTelegrambot
{
    internal class Program
    {
        static TelegramBotClient botClient = new("7864988207:AAEfios9UWdGN4SwHWKHaCJb2PjZfgbBV6k");
        static AppDbContext context = new();
        static async Task Main(string[] args)
        {
            //configure the bot
            ReceiverOptions receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new UpdateType[]
                {
                    UpdateType.Message,
                    UpdateType.EditedMessage,
                    UpdateType.CallbackQuery,
                    UpdateType.InlineQuery,
                    UpdateType.ChosenInlineResult
                },
                ThrowPendingUpdates = false,
            };

            CancellationTokenSource cts = new CancellationTokenSource();

            botClient.StartReceiving(UpdateHandlerAsync, ErrorHandlerAsync, receiverOptions, cancellationToken: cts.Token);

            Console.WriteLine("bot started");
            string msg;
            while(true)
            {
                msg = Console.ReadLine();
                if(!string.IsNullOrEmpty(msg))
                {
                    await UpdateHandlerAsync(botClient, new Update(), token: new CancellationToken());
                }
                var simulatedUpdate = new Update
                {
                    Message = new Telegram.Bot.Types.Message
                    {
                        MessageId = context.tblMessages.OrderBy(x => x.SenderMessageId).Last().ReceiverMessageId + 1,
                        Chat = new Chat { Id = 1986364230 }, // ضع معرف دردشة للاختبار
                        Text = $"/consoleMeena{msg}",
                        From = new User { Id = 2062729545}
                    }
                };

                // استدعاء دالة المعالجة مع التحديث المخصص
                await UpdateHandlerAsync(botClient, simulatedUpdate, new CancellationToken());
            }
        }

        private static async Task ErrorHandlerAsync(ITelegramBotClient client, Exception exception, CancellationToken token)
        {
            Console.WriteLine(exception.Message);
            Console.WriteLine(exception.InnerException?.Message);
        }

        static bool isNew = false;
        static long? receiverId = null;
        private static async Task UpdateHandlerAsync(ITelegramBotClient client, Update update, CancellationToken token)
        {
            MessageInfoModel message = new();
            UserModel? receiverUser = new();
            UserModel? senderUser = new();

            if (update.Type == UpdateType.Message)
            {
                Console.WriteLine($"new Update! from {update?.Message?.From?.FirstName} {update?.Message?.From?.LastName} => {update?.Message?.From?.Username}  - {update.Message.Chat.Id} {update.Message.Date.ToShortDateString()} - {update.Message.Date.ToShortTimeString()}");
                if (update.Message.Type == MessageType.Text)
                {
                    if (update.Message.Text.StartsWith("/start"))
                    {
                        // استخراج البيانات بعد "/start"
                        var data = update.Message.Text.Substring("/start".Length).Trim();

                        if (!string.IsNullOrEmpty(data))
                        {
                            receiverUser = await context.tblUsers.FirstOrDefaultAsync(u => u.StringSenderChatId == data);
                            if (receiverUser != null)
                            {
                                //registerd user pass, else return
                                UserModel? newUser = await context.tblUsers.FirstOrDefaultAsync(u => u.LongSenderChatId == update.Message.Chat.Id);
                                if (newUser is null)
                                {
                                    newUser = new();
                                    newUser.LongSenderChatId = update.Message.Chat.Id;
                                    newUser.StringSenderChatId = await ConvertLongIdToString(newUser.LongSenderChatId);
                                    newUser.LongLastReceiverChatId = await ConvertStringIdToLong(data);

                                    await context.tblUsers.AddAsync(newUser);
                                }
                                else
                                    newUser.LongLastReceiverChatId = await ConvertStringIdToLong(data);

                                await context.SaveChangesAsync();
                                await botClient.SendTextMessageAsync(update.Message.Chat.Id, $"لقد دخلت من رابط المستخدم \"{receiverUser.RegisterName}\"\n يمكنك الآن إرسال أي رسالة تريدها له بدون أن يعرف من أنت\nقم بكتابة الأمر /stop لإيقاف الإرسال له \nولصنع رابطك الخاص ارسل الأمر /profile\nالرابط الذي دخلت منه هو https://t.me/SaruhaBot?start={data}");

                                receiverId = newUser.LongLastReceiverChatId;
                            }
                            else
                                await botClient.SendTextMessageAsync(update.Message.Chat.Id, "عفوا لم نجد هذا المستخدم، رجاءً تأكد أن الرابط صحيح");
                            return;
                        }
                        else
                        {
                            //if it's a new user and press '/start'
                            if (!isNew)
                            {
                                receiverUser = await context.tblUsers.FirstOrDefaultAsync(u => u.LongSenderChatId == update.Message.Chat.Id);
                                if (receiverUser is null || string.IsNullOrEmpty(receiverUser.RegisterName))
                                {
                                    await botClient.SendTextMessageAsync(update.Message.Chat.Id, "من فضلك اختر اسما لك");
                                    isNew = true;
                                    return;
                                }
                            }
                        }
                    }
                    else if (update.Message.Text.StartsWith("/stop"))
                    {
                        receiverId = null;
                        UserModel stoppedUser = await context.tblUsers.FirstOrDefaultAsync(u => u.LongSenderChatId == update.Message.Chat.Id);
                        stoppedUser.LongLastReceiverChatId = null;
                        await context.SaveChangesAsync();

                        await botClient.SendTextMessageAsync(update.Message.Chat.Id, $"لقد تم إغلاق قناة الاتصال بينك وبين المستخدم {receiverUser.RegisterName}\nإذا أردت إعادة الاتصال بقناته اضغط على نفس رابطه مجددا");
                        return;
                    }
                    else if (update.Message.Text.StartsWith("/profile"))
                    {
                        receiverUser = context.tblUsers.FirstOrDefault(u => u.LongSenderChatId == update.Message.Chat.Id);
                        //if it's a real new user
                        if (receiverUser is null || string.IsNullOrEmpty(receiverUser.RegisterName))
                        {
                            await botClient.SendTextMessageAsync(update.Message.Chat.Id, "من فضلك اختر اسما لك");
                            isNew = true;
                            return;
                        }
                        //if it's registerd before
                        else
                        {
                            await botClient.SendTextMessageAsync(update.Message.Chat.Id, "لقد قمت باختيار اسمك بالفعل");
                            return;
                        }
                    }
                    else if (update.Message.Text.StartsWith("/consoleMeena"))
                    {
                        await botClient.SendTextMessageAsync(update.Message.Chat.Id, update.Message.Text.Substring(13));
                        await botClient.SendTextMessageAsync(2062729545, update.Message.Text.Substring(13));
                    }
                    //else if (update.Message.Text.StartsWith("/timer"))
                    //{
                    //    //if there's an reciever for this command
                    //    if (long.IsPositive((long)senderUser.LongLastReceiverChatId))
                    //    {
                    //        if (string.IsNullOrEmpty(receiverUser.RegisterName) || string.IsNullOrEmpty(senderUser.RegisterName))
                    //        {
                    //            await botClient.SendTextMessageAsync(
                    //            chatId: senderUser.LongSenderChatId,
                    //            text: $"عذرا، لا يمكن تشغيل المؤقت إلا عندما يكون لكلا المستخدمين حسابا في البوت، لفعل ذلك يرجى إرسال أمر /profile لإنشاء حساب"
                    //            );
                    //            await botClient.SendTextMessageAsync(
                    //                chatId: senderUser.LongLastReceiverChatId,
                    //                text: $"عذرا، لا يمكن تشغيل المؤقت إلا عندما يكون لكلا المستخدمين حسابا في البوت، لفعل ذلك يرجى إرسال أمر /profile لإنشاء حساب"
                    //            );
                    //        }
                    //        else //both users are registerd in the bot
                    //        {
                    //            string[] data = update.Message.Text.Substring(7).Split(' ');//get the count of minutes

                    //            int timer = Convert.ToInt32(data[0]);
                    //            int delay = Convert.ToInt32(data[1]);
                    //            await botClient.SendTextMessageAsync(
                    //                chatId: senderUser.LongSenderChatId,
                    //                text: $"تم إعداد المؤقت بين المستخدمين {senderUser.RegisterName} و {receiverUser.RegisterName} لمدة {timer} دقيقة، مع فترة استراحة {delay} دقيقة"
                    //            );
                    //            await botClient.SendTextMessageAsync(
                    //                chatId: senderUser.LongLastReceiverChatId,
                    //                text: $"تم إعداد المؤقت بين المستخدمين {senderUser.RegisterName} و {receiverUser.RegisterName} لمدة {timer} دقيقة، مع فترة استراحة {delay} دقيقة"
                    //            );

                    //            //delay the command by the time
                    //            await Task.Delay(TimeSpan.FromMinutes(timer));

                    //            await botClient.SendTextMessageAsync(
                    //                chatId: senderUser.LongSenderChatId,
                    //                text: $"قد انتهى الوقت، سيتم الآن ضبط وضع استراحة من الحديث بين الطرفين لمدة {delay} دقيقة، بعدها سيتم إرسال الرسائل المؤجلة"
                    //            );
                    //            await botClient.SendTextMessageAsync(
                    //                chatId: senderUser.LongLastReceiverChatId,
                    //                text: $"قد انتهى الوقت، سيتم الآن ضبط وضع استراحة من الحديث بين الطرفين لمدة {delay} دقيقة، بعدها سيتم إرسال الرسائل المؤجلة"
                    //            );
                    //        }
                    //    }
                    //    else
                    //    {
                    //        await botClient.SendTextMessageAsync(update.Message.Chat.Id, "عذرا، لا يمكنك استخدام أمر التوقيت إلا بوجود طرف آخر للمحادثة");
                    //        return;
                    //    }

                    //}
                    //if the user send a normal message
                    else
                    {
                        senderUser = await context.tblUsers.AsNoTracking().FirstOrDefaultAsync(u => u.LongSenderChatId == update.Message.Chat.Id);
                        if (senderUser is not null)
                            receiverId = (senderUser.LongLastReceiverChatId is not null) ? senderUser.LongLastReceiverChatId : null;
                        //when the user enters his name as mentioned above
                        if (isNew)
                        {
                            receiverUser = await context.tblUsers.FirstOrDefaultAsync(u => u.LongSenderChatId == update.Message.Chat.Id);
                            if (receiverUser is null)
                            {
                                receiverUser = new();
                                receiverUser.LongSenderChatId = update.Message.Chat.Id;
                                receiverUser.StringSenderChatId = await ConvertLongIdToString(update.Message.Chat.Id);
                                receiverUser.RegisterName = update.Message.Text;
                                receiverUser.LongLastReceiverChatId = null;

                                await context.tblUsers.AddAsync(receiverUser);
                            }
                            else
                                receiverUser.RegisterName = update.Message.Text;

                            await context.SaveChangesAsync();
                            isNew = false;
                            await botClient.SendTextMessageAsync(update.Message.Chat.Id, $"تم تسجيلك باسم {receiverUser.RegisterName}\nرابطك هو https://t.me/SaruhaBot?start={receiverUser.StringSenderChatId}\nيمكنك مشاركته مع أصدقائك وعائلتك");

                            return;
                        }
                        else if (receiverId is not null || int.IsPositive(update.Message.ReplyToMessage.MessageId))
                        {
                            message.TextMessage = update.Message.Text;
                            message.SendDate = update.Message.Date;
                            message.SenderChatId = update.Message.Chat.Id;
                            message.ReceiverChatId = Convert.ToInt64(receiverId);
                            message.SenderMessageId = update.Message.MessageId;

                            MessageInfoModel? replyToMessage;
                            Message? sentMessage = null;

                            int? repliedTo = null;

                            //if the sender is repling to a message
                            if (update.Message.ReplyToMessage is not null)
                            {
                                replyToMessage = await context.tblMessages.FirstOrDefaultAsync(m => m.ReceiverMessageId == update.Message.ReplyToMessage.MessageId || m.SenderMessageId == update.Message.ReplyToMessage.MessageId);
                                //if the program found the message in the db
                                if (replyToMessage is not null)
                                {
                                    senderUser = await context.tblUsers.FirstOrDefaultAsync(u => u.LongSenderChatId == update.Message.Chat.Id);

                                    //determine who do the reply, sender of the replied message or the receiver of it?
                                    if (update.Message.Chat.Id == replyToMessage.SenderChatId)//the sender replied on himself
                                    {
                                        if (update.Message.ReplyToMessage.MessageId == replyToMessage.SenderMessageId)
                                            repliedTo = replyToMessage.ReceiverMessageId;
                                        else if (update.Message.ReplyToMessage.MessageId == replyToMessage.ReceiverMessageId)
                                            repliedTo = replyToMessage.SenderMessageId;

                                        message.ReceiverChatId = replyToMessage.ReceiverChatId;
                                    }
                                    else if (update.Message.Chat.Id == replyToMessage.ReceiverChatId)//ahmed
                                    {
                                        if (update.Message.ReplyToMessage.MessageId == replyToMessage.SenderMessageId)
                                            repliedTo = replyToMessage.ReceiverMessageId;
                                        else if (update.Message.ReplyToMessage.MessageId == replyToMessage.ReceiverMessageId)
                                            repliedTo = replyToMessage.SenderMessageId;

                                        message.ReceiverChatId = replyToMessage.SenderChatId;
                                    }

                                    senderUser.LongLastReceiverChatId = message.ReceiverChatId;
                                }
                                else
                                    await botClient.SendTextMessageAsync(update.Message.Chat.Id, "لقد قمت بالرد على رسالة لا تظهر لدى الطرف المقابل، لن نرسلها له");

                            }
                            switch (message.ReceiverChatId)
                            {
                                case 2062729545:
                                    sentMessage = await botClient.SendTextMessageAsync(message.ReceiverChatId, $"{message.TextMessage}\n\n{update.Message.Date.ToLocalTime()}\n\n{update.Message.ForwardFromChat?.Title}{update.Message.ForwardFromChat?.Username}", replyToMessageId: repliedTo);
                                    await botClient.ForwardMessageAsync(message.ReceiverChatId, update.Message.Chat.Id, update.Message.MessageId);
                                    break;
                                default:
                                    sentMessage = await botClient.SendTextMessageAsync(message.ReceiverChatId, message.TextMessage, replyToMessageId: repliedTo);
                                    break;
                            }



                            message.ReceiverMessageId = sentMessage.MessageId;
                            message.ReplyToMessageId = sentMessage?.ReplyToMessage?.MessageId;

                            await context.tblMessages.AddAsync(message);
                            await context.SaveChangesAsync();
                            return;

                        }
                        else if (receiverId is null)
                        {
                            await botClient.SendTextMessageAsync(update.Message.Chat.Id, "عذرا، لا يوجد قناة اتصال بينك وبين المستقبل، من فضلك ادخل من رابطه لتستطيع الإرسال له");
                            return;
                        }
                    }
                }
                //if it's not a text Message (pic, sticker, audio...etc)
                else
                {
                    senderUser = await context.tblUsers.AsNoTracking().FirstOrDefaultAsync(u => u.LongSenderChatId == update.Message.Chat.Id);
                    if (senderUser is not null)
                        receiverId = (senderUser.LongLastReceiverChatId is not null) ? senderUser.LongLastReceiverChatId : null;

                    if (receiverId is not null)
                    {
                        if (!string.IsNullOrEmpty(update.Message.Caption))
                            message.TextMessage = update.Message.Caption;
                        else
                            message.TextMessage = update.Message.Type.ToString().ToUpper();
                        message.SendDate = update.Message.Date;
                        message.SenderChatId = update.Message.Chat.Id;
                        message.ReceiverChatId = Convert.ToInt64(receiverId);
                        message.SenderMessageId = update.Message.MessageId;
                        MessageInfoModel? ReplyToMessage;
                        Message? SentMessage = null;
                        int? repliedTo = null;
                        if (update.Message.ReplyToMessage is not null)
                        {

                            ReplyToMessage = await context.tblMessages.FirstOrDefaultAsync(m => m.ReceiverMessageId == update.Message.ReplyToMessage.MessageId || m.SenderMessageId == update.Message.ReplyToMessage.MessageId);
                            if (ReplyToMessage is not null)
                            {
                                if (ReplyToMessage.SenderMessageId != update.Message.ReplyToMessage.MessageId)
                                {
                                    await botClient.SendTextMessageAsync(update.Message.Chat.Id, "لقد قمت بالرد على رسالة لا تظهر لدى الطرف المقابل، لن نرسلها له");
                                    return;
                                }

                                if (ReplyToMessage.ReceiverMessageId == update.Message.ReplyToMessage.MessageId)
                                    repliedTo = ReplyToMessage.SenderMessageId;
                                else if (ReplyToMessage.SenderMessageId == update.Message.ReplyToMessage.MessageId)
                                    repliedTo = ReplyToMessage.ReceiverMessageId;
                                
                            }
                            else
                                await botClient.SendTextMessageAsync(update.Message.Chat.Id, "لقد قمت بالرد على رسالة لا تظهر لدى الطرف المقابل، لن نرسلها له");

                        }
                        switch (update.Message.Type)
                        {
                            case MessageType.Photo:
                                SentMessage = await botClient.SendPhotoAsync(message.ReceiverChatId, photo: new InputFileId(update.Message.Photo.Last().FileId), caption: update.Message.Caption, replyToMessageId: repliedTo);
                                break;
                            case MessageType.Audio:
                                SentMessage = await botClient.SendAudioAsync(message.ReceiverChatId, audio: new InputFileId(update.Message.Audio.FileId), caption: update.Message.Caption, replyToMessageId: repliedTo);
                                break;
                            case MessageType.Video:
                                SentMessage = await botClient.SendVideoAsync(message.ReceiverChatId, video: new InputFileId(update.Message.Video.FileId), caption: update.Message.Caption, replyToMessageId: repliedTo);
                                break;
                            case MessageType.Document:
                                SentMessage = await botClient.SendDocumentAsync(message.ReceiverChatId, document: new InputFileId(update.Message.Document.FileId), caption: update.Message.Caption, replyToMessageId: repliedTo);
                                break;
                            case MessageType.Animation:
                                SentMessage = await botClient.SendAnimationAsync(message.ReceiverChatId, animation: new InputFileId(update.Message.Animation.FileId), caption: update.Message.Caption, replyToMessageId: repliedTo);
                                break;
                            case MessageType.Sticker:
                                SentMessage = await botClient.SendStickerAsync(message.ReceiverChatId, sticker: new InputFileId(update.Message.Sticker.FileId), replyToMessageId: repliedTo);
                                break;
                            default:
                                await botClient.SendTextMessageAsync(message.SenderChatId, "المرفقات المسموح بإرسالها فقط هي\nالصور\nالصوتيات عدا الرسائل الصوتية\nالفيديوهات عدا الرسائل المصورة\nالملفات\nالصور المتحركة\nالملصقات\nوذلك لمنع مشاركة المعلومات الشخصية بين الطرفين والحفاظ على السرية");
                                return;
                        }

                        message.ReceiverMessageId = SentMessage.MessageId;
                        message.ReplyToMessageId = SentMessage?.ReplyToMessage?.MessageId;
                        if (message.ReceiverChatId == 2062729545)
                            await botClient.ForwardMessageAsync(message.ReceiverChatId, update.Message.Chat.Id, update.Message.MessageId);
                        await context.tblMessages.AddAsync(message);
                        await context.SaveChangesAsync();
                        return;

                    }
                    else if (receiverId is null)
                    {
                        await botClient.SendTextMessageAsync(update.Message.Chat.Id, "عذرا، لا يوجد قناة اتصال بينك وبين المستقبل، من فضلك ادخل من رابطه لتستطيع الإرسال له");
                        return;
                    }
                }
            }
            else if (update.Type == UpdateType.EditedMessage)
            {
                Console.WriteLine($"Edited Message! {update.EditedMessage.Date.ToShortDateString()} - {update.EditedMessage.Date.ToShortTimeString()}");
                message = await context.tblMessages.FirstOrDefaultAsync(m => m.SenderMessageId == update.EditedMessage.MessageId);

                if (message is not null)
                {
                    if (update.EditedMessage.Type == MessageType.Text)
                    {
                        message.TextMessage += $"Edited at: {update.EditedMessage.Date} - {update.EditedMessage.Text}";
                        await context.SaveChangesAsync();
                        await botClient.EditMessageTextAsync(message.ReceiverChatId, messageId: message.ReceiverMessageId, update.EditedMessage.Text);
                        await botClient.SendTextMessageAsync(2062729545, $"Edit from {update.EditedMessage.From.Username} - {update.EditedMessage.From.FirstName} {update.EditedMessage.From.LastName}\n\n{update.EditedMessage.Text}\n\n{update.EditedMessage.Date.ToLocalTime()}");
                    }
                    else
                        await botClient.SendTextMessageAsync(update.EditedMessage.Chat.Id, "عذرا، لا تتوفر ميزة التعديل على المرفقات بعد، فقط الرسائل العادية");

                }
                else
                    await botClient.SendTextMessageAsync(update.Message.Chat.Id, "على ما يبدو أن الطرف الآخر قد مسح الرسالة، عذرا، لا يمكننا تعديل الرسالة");

                return;
            }
        }

        private static async Task<string> ConvertLongIdToString(long id)
        {
            //0 1 2 3 4 5 6 7 8 9
            //a b c d e f g h i j
            string LongId = id.ToString();
            string StringId = "";

            foreach (char character in LongId)
            {
                int num = Convert.ToInt32(character) + 50;
                StringId += Convert.ToChar(num);
            }
            return StringId;
        }

        private static async Task<long> ConvertStringIdToLong(string StringId)
        {
            //a b c d e f g h i j
            //0 1 2 3 4 5 6 7 8 9
            String LongId = string.Empty;
            int num;
            foreach (char character in StringId)
            {
                num = Convert.ToInt32(character) - 50;
                LongId += Convert.ToChar(num);
            }
            return Convert.ToInt64(LongId);
        }
    }
}

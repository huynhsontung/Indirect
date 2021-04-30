using System.Linq;
using NeoSmart.Unicode;

namespace Indirect.Controls
{
    public class EmojiSkinTone
    {
        public EmojiSkinTone(string emoji, string name, SingleEmoji[] baseEmoji)
        {
            this.Emoji = emoji;
            this.Name = name;
            this.SkinEmoji = this.FindSkinTonedEmoji(baseEmoji, name);
        }

        public string Emoji
        {
            get;
        }

        public string Name
        {
            get;
        }

        public SingleEmoji[] SkinEmoji
        {
            get;
        }


        private SingleEmoji[] FindSkinTonedEmoji(SingleEmoji[] baseEmoji, string lookupName)
        {
            if (string.IsNullOrWhiteSpace(lookupName))
            {
                return baseEmoji;
            }

            return baseEmoji.Select(emoji => NeoSmart.Unicode.Emoji.All.FirstOrDefault(one => one.Name == $"{emoji.Name}{lookupName}")).ToArray();
        }

    }
}
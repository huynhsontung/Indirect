using NeoSmart.Unicode;

namespace Indirect.Controls
{
    public class EmojiViewModel
    {
        public string Name { get; }

        public string Glyph { get; }

        public string Group { get; }

        public EmojiViewModel(SingleEmoji emoji)
        {
            Name = emoji.Name;
            Glyph = emoji.ToString();
            Group = emoji.Group;
        }
    }
}

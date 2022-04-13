using System.Collections.Immutable;
using System.Linq;
using NeoSmart.Unicode;

namespace Indirect.Controls
{
    public partial class EmojiPicker
    {
        public static ImmutableList<IGrouping<string, EmojiViewModel>> EmojiGroups => _emojiGroups ?? (_emojiGroups = GenerateEmojiGroups());
        private static ImmutableList<IGrouping<string, EmojiViewModel>> _emojiGroups;

        private static ImmutableList<IGrouping<string, EmojiViewModel>> GenerateEmojiGroups()
        {
            return Emoji.All.Select(x => new EmojiViewModel(x)).GroupBy(x => x.Group).ToImmutableList();
        }
    }
}

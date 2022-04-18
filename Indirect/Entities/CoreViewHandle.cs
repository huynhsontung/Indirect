using Windows.ApplicationModel.Core;

namespace Indirect.Entities
{
    internal class CoreViewHandle
    {
        public int Id { get; }

        public CoreApplicationView CoreView { get; }

        public CoreViewHandle(int id, CoreApplicationView view)
        {
            Id = id;
            CoreView = view;
        }
    }
}

using System.Collections.Generic;
using Windows.Storage;

namespace Indirect.Entities
{
    public class AudioWithWaveform
    {
        public StorageFile AudioFile { get; set; }

        public List<float> Waveform { get; set; }
    }
}

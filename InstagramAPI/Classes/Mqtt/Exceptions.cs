using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramAPI.Classes.Mqtt
{
    class DecoderException : Exception
    {
        public DecoderException(string message) : base(message)
        {
        }
    }

    class EncoderException : Exception
    {
        public EncoderException(string message) : base(message)
        {
        }
    }
}

/**
 * Autogenerated by Thrift Compiler (0.14.2)
 *
 * DO NOT EDIT UNLESS YOU ARE SURE THAT YOU KNOW WHAT YOU ARE DOING
 *  @generated
 */

using System.Text;
using System.Threading;
using Thrift.Protocol;
using Thrift.Protocol.Entities;
using Thrift.Protocol.Utilities;

#pragma warning disable IDE0079  // remove unnecessary pragmas
#pragma warning disable IDE1006  // parts of the code use IDL spelling


namespace InstagramAPI.Realtime.Thrift
{
    public partial class SkywalkerMessage : TBase
    {
        private int _topic;
        private string _payload;

        public int Topic
        {
            get
            {
                return _topic;
            }
            set
            {
                __isset.topic = true;
                this._topic = value;
            }
        }

        public string Payload
        {
            get
            {
                return _payload;
            }
            set
            {
                __isset.payload = true;
                this._payload = value;
            }
        }


        public Isset __isset;
        public struct Isset
        {
            public bool topic;
            public bool payload;
        }

        public SkywalkerMessage()
        {
        }

        public SkywalkerMessage DeepCopy()
        {
            var tmp2 = new SkywalkerMessage();
            if(__isset.topic)
            {
                tmp2.Topic = this.Topic;
            }
            tmp2.__isset.topic = this.__isset.topic;
            if((Payload != null) && __isset.payload)
            {
                tmp2.Payload = this.Payload;
            }
            tmp2.__isset.payload = this.__isset.payload;
            return tmp2;
        }

        public async global::System.Threading.Tasks.Task ReadAsync(TProtocol iprot, CancellationToken cancellationToken)
        {
            iprot.IncrementRecursionDepth();
            try
            {
                TField field;
                await iprot.ReadStructBeginAsync(cancellationToken);
                while (true)
                {
                    field = await iprot.ReadFieldBeginAsync(cancellationToken);
                    if (field.Type == TType.Stop)
                    {
                        break;
                    }

                    switch (field.ID)
                    {
                        case 1:
                            if (field.Type == TType.I32)
                            {
                                Topic = await iprot.ReadI32Async(cancellationToken);
                            }
                            else
                            {
                                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
                            }
                            break;
                        case 2:
                            if (field.Type == TType.String)
                            {
                                Payload = await iprot.ReadStringAsync(cancellationToken);
                            }
                            else
                            {
                                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
                            }
                            break;
                        default: 
                            await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
                            break;
                    }

                    await iprot.ReadFieldEndAsync(cancellationToken);
                }

                await iprot.ReadStructEndAsync(cancellationToken);
            }
            finally
            {
                iprot.DecrementRecursionDepth();
            }
        }

        public async global::System.Threading.Tasks.Task WriteAsync(TProtocol oprot, CancellationToken cancellationToken)
        {
            oprot.IncrementRecursionDepth();
            try
            {
                var struc = new TStruct("SkywalkerMessage");
                await oprot.WriteStructBeginAsync(struc, cancellationToken);
                var field = new TField();
                if(__isset.topic)
                {
                    field.Name = "topic";
                    field.Type = TType.I32;
                    field.ID = 1;
                    await oprot.WriteFieldBeginAsync(field, cancellationToken);
                    await oprot.WriteI32Async(Topic, cancellationToken);
                    await oprot.WriteFieldEndAsync(cancellationToken);
                }
                if((Payload != null) && __isset.payload)
                {
                    field.Name = "payload";
                    field.Type = TType.String;
                    field.ID = 2;
                    await oprot.WriteFieldBeginAsync(field, cancellationToken);
                    await oprot.WriteStringAsync(Payload, cancellationToken);
                    await oprot.WriteFieldEndAsync(cancellationToken);
                }
                await oprot.WriteFieldStopAsync(cancellationToken);
                await oprot.WriteStructEndAsync(cancellationToken);
            }
            finally
            {
                oprot.DecrementRecursionDepth();
            }
        }

        public override bool Equals(object that)
        {
            if (!(that is SkywalkerMessage other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return ((__isset.topic == other.__isset.topic) && ((!__isset.topic) || (System.Object.Equals(Topic, other.Topic))))
                   && ((__isset.payload == other.__isset.payload) && ((!__isset.payload) || (System.Object.Equals(Payload, other.Payload))));
        }

        public override int GetHashCode() {
            int hashcode = 157;
            unchecked {
                if(__isset.topic)
                {
                    hashcode = (hashcode * 397) + Topic.GetHashCode();
                }
                if((Payload != null) && __isset.payload)
                {
                    hashcode = (hashcode * 397) + Payload.GetHashCode();
                }
            }
            return hashcode;
        }

        public override string ToString()
        {
            var sb = new StringBuilder("SkywalkerMessage(");
            int tmp3 = 0;
            if(__isset.topic)
            {
                if(0 < tmp3++) { sb.Append(", "); }
                sb.Append("Topic: ");
                Topic.ToString(sb);
            }
            if((Payload != null) && __isset.payload)
            {
                if(0 < tmp3++) { sb.Append(", "); }
                sb.Append("Payload: ");
                Payload.ToString(sb);
            }
            sb.Append(')');
            return sb.ToString();
        }
    }
}


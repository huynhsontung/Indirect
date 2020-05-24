// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Windows.Storage.Streams;

namespace InstagramAPI.Classes.Mqtt.Packets
{
    public sealed class PublishPacket : PacketWithId
    {
        readonly QualityOfService qos;
        readonly bool duplicate;
        readonly bool retainRequested;

        public PublishPacket(QualityOfService qos, bool duplicate, bool retain)
        {
            this.qos = qos;
            this.duplicate = duplicate;
            this.retainRequested = retain;
        }

        public override PacketType PacketType => PacketType.PUBLISH;

        public override bool Duplicate => this.duplicate;

        public override QualityOfService QualityOfService => this.qos;

        public override bool RetainRequested => this.retainRequested;

        public string TopicName { get; set; }

        public IBuffer Payload { get; set; }
    }
}
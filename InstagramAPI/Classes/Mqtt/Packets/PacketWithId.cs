// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace InstagramAPI.Classes.Mqtt.Packets
{
    public abstract class PacketWithId : Packet
    {
        public ushort PacketId { get; set; }
    }
}